using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Parkly_Backend.Data.Repositories;
using Parkly_Backend.Models;
using Parkly_Backend.Models.DTOs;
using Parkly_Backend.Models.Enums;
using Parkly_Backend.Models.Response;
using Parkly_Backend.Interfaces;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Parkly_Backend.Configuration;

namespace Parkly_Backend.Services
{
    public class AccountService:IAccountService
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly IMapper _mapper;
        private readonly IEmailService _emailService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly JwtOptions _jwtOptions;
        private readonly ILogger<AccountService> _logger;
        private readonly IStorageService _storageService;
        private readonly SupabaseOptions _supabaseOptions;
        private static readonly Random _random = new Random();
        
        public AccountService(UserManager<AppUser> userManager, IMapper mapper, IEmailService emailService, IUnitOfWork unitOfWork, IOptions<JwtOptions> jwtOptions, ILogger<AccountService> logger, IStorageService storageService, IOptions<SupabaseOptions> supabaseOptions)
        {
            _userManager = userManager;
            _mapper = mapper;
            _emailService = emailService;
            _unitOfWork = unitOfWork;
            _jwtOptions = jwtOptions.Value;
            _logger = logger;
            _storageService = storageService;
            _supabaseOptions = supabaseOptions.Value;
        }
        public (string Token, string Jti) GenerateJwtToken(AppUser user)
        {
            var jti = Guid.NewGuid().ToString();
            var claims = new List<Claim>()
            {
              new Claim(ClaimTypes.Name,user.UserName),
              new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
              new Claim(ClaimTypes.Email,user.Email),
              new Claim(ClaimTypes.Role, user.Role.ToString()),
              new Claim(JwtRegisteredClaimNames.Jti, jti)
            };

            //SigningCredentials
            var SecretKey = _jwtOptions.SecretKey;
            var Key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SecretKey));
            var sc = new SigningCredentials(Key, SecurityAlgorithms.HmacSha256 );
            var token = new JwtSecurityToken(
                claims: claims,
                issuer: _jwtOptions.Issuer,
                audience: _jwtOptions.Audience,
                expires: DateTime.UtcNow.AddMinutes(_jwtOptions.JwtExpiresInMinutes),
                signingCredentials: sc
                
                );
            return (new JwtSecurityTokenHandler().WriteToken(token), jti);

        }

        private RefreshToken GenerateRefreshTokenString(Guid userId, string jti)
        {
            return new RefreshToken
            {
                Token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)),
                JwtId = jti,
                IsUsed = false,
                IsRevoked = false,
                AddedDate = DateTime.UtcNow,
                ExpiryDate = DateTime.UtcNow.AddMonths(_jwtOptions.RefreshTokenExpiresInMonths),
                UserId = userId
            };
        }

        public async Task<ApiResponse> Register(RegisterDTO user)
        {
            var exUser = await _userManager.FindByEmailAsync(user.Email);
            if(exUser!=null)
            {
                return ApiResponse.Failure("This Email is already exists");
            }
            var newUser = _mapper.Map<AppUser>(user);
            IdentityResult result = await _userManager.CreateAsync(newUser,user.Password);
            if (!result.Succeeded) {

                var errors = result.Errors.Select(e => e.Description).ToList();
                _logger.LogWarning("Account creation failed for email {Email}. Errors: {@Errors}", user.Email, errors);
                return ApiResponse.Failure("Account creation failed", errors);
            
            }

            var roleResult = await _userManager.AddToRoleAsync(newUser, UserRole.Driver.ToString());
            if (!roleResult.Succeeded)
            {
                _logger.LogWarning("Failed to assign role {Role} to user {Email}: {Errors}",
                    UserRole.Driver, user.Email, string.Join("; ", roleResult.Errors.Select(e => e.Description)));
            }
            
            await SendVerificationEmailAsync(newUser);

            _logger.LogInformation("Account created successfully for email {Email}", user.Email);
            return ApiResponse.Success("Account is created successfully! Please verify your email.");
        }

        public async Task<ApiResponse> RegisterOwner(OwnerRegisterDTO newOwner)
        {
            var exUser = await _userManager.FindByEmailAsync(newOwner.Email);
            if (exUser != null)
            {
                return ApiResponse.Failure("This Email is already exists");
            }

            var newUser = _mapper.Map<AppUser>(newOwner);
            newUser.Role = UserRole.ParkingOwner;

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var result = await _userManager.CreateAsync(newUser, newOwner.Password);
                if (!result.Succeeded)
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    var errors = result.Errors.Select(e => e.Description).ToList();
                    return ApiResponse.Failure("Account creation failed", errors);
                }

                var roleResult = await _userManager.AddToRoleAsync(newUser, UserRole.ParkingOwner.ToString());
                if (!roleResult.Succeeded)
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    var errors = roleResult.Errors.Select(e => e.Description).ToList();
                    _logger.LogWarning("Failed to assign role {Role} to owner {Email}: {Errors}",
                        UserRole.ParkingOwner, newOwner.Email, string.Join("; ", errors));
                    return ApiResponse.Failure("Account creation failed", errors);
                }

                var parkingOwner = new ParkingOwner
                {
                    OwnerId = newUser.Id,
                    CompanyName = newOwner.CompanyName,
                    PayoutAccount = newOwner.PayoutAccount,
                    VerificationStatus = VerificationStatus.Pending
                };
                await _unitOfWork.ParkingOwners.AddAsync(parkingOwner);
                await _unitOfWork.SaveChangesAsync();

                await SendVerificationEmailAsync(newUser);

                await _unitOfWork.CommitTransactionAsync();
                return ApiResponse.Success("Parking owner account created successfully! Please verify your email.");
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                return ApiResponse.Failure("Parking owner account creation failed.");
            }
        }
        public async Task<ApiResponse<LoginResponseDTO>> LogIn(LoginDTO login)
        {
           var user= await _userManager.FindByEmailAsync(login.Email);
            if (user == null) {
                _logger.LogWarning("Failed login attempt for email {Email}: User not found.", login.Email);
                return ApiResponse<LoginResponseDTO>.Failure("Invalid Email or Password");
            }
            var isPasswordValid = await _userManager.CheckPasswordAsync(user, login.Password);
            if (!isPasswordValid) {
                _logger.LogWarning("Failed login attempt for email {Email}: Invalid password.", login.Email);
                return ApiResponse<LoginResponseDTO>.Failure("Invalid Email or Password");
            }

            var isEmailConfirmed = await _userManager.IsEmailConfirmedAsync(user);
            if (!isEmailConfirmed)
            {
                _logger.LogWarning("Failed login attempt for email {Email}: Email not verified.", login.Email);
                return ApiResponse<LoginResponseDTO>.Failure("Please verify your email address to log in.");
            }

            var data = _mapper.Map<LoginResponseDTO>(user);
            var (jwtToken, jti) = GenerateJwtToken(user);
            var refreshToken = GenerateRefreshTokenString(user.Id, jti);

            await _unitOfWork.RefreshTokens.AddAsync(refreshToken);
            await _unitOfWork.SaveChangesAsync();

            data.Token = jwtToken;
            data.RefreshToken = refreshToken.Token;

            _logger.LogInformation("User {Email} logged in successfully.", login.Email);
            return ApiResponse<LoginResponseDTO>.Success("Login Successful", data);

        }

        private async Task SendVerificationEmailAsync(AppUser user)
        {
            var repo = _unitOfWork.EmailVerificationOtps;
            
            var activeOtps = await repo.GetActiveOtpsAsync(user.Id);
            foreach (var otp in activeOtps)
            {
                repo.Delete(otp);
            }

            var code = _random.Next(100000, 999999).ToString();
            var entity = new EmailVerificationOtp
            {
                UserId = user.Id,
                CodeHash = HashOtp(code),
                ExpiresAt = DateTime.UtcNow.AddMinutes(15),
                IsUsed = false
            };
            await repo.AddAsync(entity);
            await _unitOfWork.SaveChangesAsync();

            var body = $"<p>Your Parkly email verification code is: <strong>{code}</strong></p><p>This code is valid for 15 minutes.</p>";
            await _emailService.SendEmailAsync(user.Email, "Parkly - Verify your email address", body);
        }

        public async Task<ApiResponse> VerifyEmailAsync(VerifyEmailDTO verifyEmailDto)
        {
            var user = await _userManager.FindByEmailAsync(verifyEmailDto.Email);
            if (user == null)
            {
                return ApiResponse.Failure("Invalid OTP or the code has expired.");
            }

            var repo = _unitOfWork.EmailVerificationOtps;
            var otp = await repo.GetLatestValidOtpAsync(user.Id);

            if (otp == null || !VerifyOtp(verifyEmailDto.Otp, otp.CodeHash))
            {
                return ApiResponse.Failure("Invalid OTP or the code has expired.");
            }

            otp.IsUsed = true;
            repo.Update(otp);
            
            user.EmailConfirmed = true;
            await _userManager.UpdateAsync(user);

            var parkingOwner = await _unitOfWork.ParkingOwners.FirstOrDefaultAsync(owner => owner.OwnerId == user.Id);
            if (parkingOwner != null && (parkingOwner.VerificationStatus != VerificationStatus.Verified || !parkingOwner.BusinessVerifiedAt.HasValue))
            {
                parkingOwner.VerificationStatus = VerificationStatus.Verified;
                parkingOwner.BusinessVerifiedAt ??= DateTime.UtcNow;
                _unitOfWork.ParkingOwners.Update(parkingOwner);
            }

            await _unitOfWork.SaveChangesAsync();

            return ApiResponse.Success("Email verified successfully.");
        }

        public async Task<ApiResponse> ResendVerificationEmailAsync(ResendVerificationDTO resendVerificationDto)
        {
            var user = await _userManager.FindByEmailAsync(resendVerificationDto.Email);
            if (user == null)
            {
                return ApiResponse.Success("If the email is registered, a new verification OTP has been sent.");
            }

            if (await _userManager.IsEmailConfirmedAsync(user))
            {
                return ApiResponse.Failure("Email is already verified.");
            }

            await SendVerificationEmailAsync(user);

            return ApiResponse.Success("If the email is registered, a new verification OTP has been sent.");
        }

        public async Task<ApiResponse> ForgotPasswordAsync(ForgotPasswordDTO forgotPassword)
        {
            var user = await _userManager.FindByEmailAsync(forgotPassword.Email);
            if (user == null)
            {
                return ApiResponse.Success("If the email is registered, an OTP has been sent to reset your password.");
            }

            var repo = _unitOfWork.PasswordResetOtps;
            var activeOtps = await repo.GetActiveOtpsAsync(user.Id);
            foreach (var otp in activeOtps)
            {
                repo.Delete(otp);
            }

            var code = _random.Next(100000, 999999).ToString();
            var entity = new PasswordResetOtp
            {
                UserId = user.Id,
                CodeHash = HashOtp(code),
                ExpiresAt = DateTime.UtcNow.AddMinutes(10),
                IsUsed = false
            };
            await repo.AddAsync(entity);
            await _unitOfWork.SaveChangesAsync();

            var body = $"<p>Your Parkly password reset code is: <strong>{code}</strong></p><p>This code is valid for 10 minutes.</p>";
            await _emailService.SendEmailAsync(user.Email, "Parkly - Reset your password", body);

            return ApiResponse.Success("If the email is registered, an OTP has been sent to reset your password.");
        }

        public async Task<ApiResponse<string>> VerifyResetOtpAsync(VerifyResetOtpDTO verifyResetOtpDto)
        {
            var user = await _userManager.FindByEmailAsync(verifyResetOtpDto.Email);
            if (user == null)
            {
                return ApiResponse<string>.Failure("Invalid OTP or the code has expired.");
            }

            var repo = _unitOfWork.PasswordResetOtps;
            var otp = await repo.GetLatestValidOtpAsync(user.Id);

            if (otp == null || !VerifyOtp(verifyResetOtpDto.Otp, otp.CodeHash))
            {
                return ApiResponse<string>.Failure("Invalid OTP or the code has expired.");
            }

            otp.IsUsed = true;
            repo.Update(otp);
            await _unitOfWork.SaveChangesAsync();

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            return ApiResponse<string>.Success("OTP verified successfully.", token);
        }

        public async Task<ApiResponse> ResetPasswordAsync(ResetPasswordDTO resetPassword)
        {
            var user = await _userManager.FindByEmailAsync(resetPassword.Email);
            if (user == null)
            {
                return ApiResponse.Failure("Invalid request.");
            }

            var result = await _userManager.ResetPasswordAsync(user, resetPassword.ResetToken, resetPassword.NewPassword);
            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => e.Description).ToList();
                return ApiResponse.Failure("Password reset failed", errors);
            }

            return ApiResponse.Success("Your password has been reset successfully.");
        }

        private static string HashOtp(string code)
        {
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(code));
            return Convert.ToHexString(bytes);
        }

        private static bool VerifyOtp(string code, string hash)
        {
            return HashOtp(code).Equals(hash, StringComparison.OrdinalIgnoreCase);
        }

        public async Task<ApiResponse<ProfileDTO>> GetProfileAsync(Guid userId)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                return ApiResponse<ProfileDTO>.Failure("User not found.");
            }

            var profile = _mapper.Map<ProfileDTO>(user);

            return ApiResponse<ProfileDTO>.Success("Profile retrieved successfully.", profile);
        }

        public async Task<ApiResponse<ProfileDTO>> UpdateProfileAsync(Guid userId, UpdateProfileDTO dto)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                return ApiResponse<ProfileDTO>.Failure("User not found.");
            }

            user.FullName = $"{dto.FirstName} {dto.LastName}".Trim();
            user.PhoneNumber = dto.PhoneNumber;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => e.Description).ToList();
                return ApiResponse<ProfileDTO>.Failure("Profile update failed.", errors);
            }

            var profile = _mapper.Map<ProfileDTO>(user);

            return ApiResponse<ProfileDTO>.Success("Profile updated successfully.", profile);
        }

        public async Task<ApiResponse> LogoutAsync(TokenRequestDTO tokenRequest)
        {
            var refreshTokenRepo = _unitOfWork.RefreshTokens;
            var storedToken = await refreshTokenRepo.Query()
                .FirstOrDefaultAsync(rt => rt.Token == tokenRequest.RefreshToken);

            if (storedToken == null)
            {
                return ApiResponse.Failure("Refresh token not found.");
            }

            storedToken.IsRevoked = true;
            refreshTokenRepo.Update(storedToken);
            await _unitOfWork.SaveChangesAsync();

            return ApiResponse.Success("Logged out successfully.");
        }

        private ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
        {
            var SecretKey = _jwtOptions.SecretKey;
            var Key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SecretKey));
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateAudience = false, 
                ValidateIssuer = false,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = Key,
                ValidateLifetime = false // Here we are saying that we don't care about the token's expiration date
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            try
            {
                var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out SecurityToken securityToken);
                if (securityToken is not JwtSecurityToken jwtSecurityToken || 
                    !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                {
                    return null;
                }
                return principal;
            }
            catch
            {
                return null;
            }
        }

        public async Task<ApiResponse<LoginResponseDTO>> RefreshTokenAsync(TokenRequestDTO tokenRequest)
        {
            var principal = GetPrincipalFromExpiredToken(tokenRequest.Token);
            if (principal == null)
            {
                return ApiResponse<LoginResponseDTO>.Failure("Invalid access token.");
            }

            var jti = principal.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Jti)?.Value;
            var userIdClaim = principal.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(jti) || string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out Guid userId))
            {
                return ApiResponse<LoginResponseDTO>.Failure("Invalid token claims.");
            }

            var refreshTokenRepo = _unitOfWork.RefreshTokens;
            var storedToken = await refreshTokenRepo.Query()
                .FirstOrDefaultAsync(rt => rt.Token == tokenRequest.RefreshToken);

            if (storedToken == null)
                return ApiResponse<LoginResponseDTO>.Failure("Refresh token does not exist.");

            if (storedToken.IsUsed)
                return ApiResponse<LoginResponseDTO>.Failure("Refresh token has been used.");

            if (storedToken.IsRevoked)
                return ApiResponse<LoginResponseDTO>.Failure("Refresh token has been revoked.");

            if (storedToken.JwtId != jti)
                return ApiResponse<LoginResponseDTO>.Failure("Refresh token does not match the access token.");

            if (storedToken.ExpiryDate < DateTime.UtcNow)
                return ApiResponse<LoginResponseDTO>.Failure("Refresh token has expired.");

            // Mark as used
            storedToken.IsUsed = true;
            refreshTokenRepo.Update(storedToken);
            await _unitOfWork.SaveChangesAsync();

            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
                return ApiResponse<LoginResponseDTO>.Failure("User not found.");

            var data = _mapper.Map<LoginResponseDTO>(user);
            var (newJwtToken, newJti) = GenerateJwtToken(user);
            var newRefreshToken = GenerateRefreshTokenString(user.Id, newJti);

            await refreshTokenRepo.AddAsync(newRefreshToken);
            await _unitOfWork.SaveChangesAsync();

            data.Token = newJwtToken;
            data.RefreshToken = newRefreshToken.Token;

            _logger.LogInformation("Token refreshed successfully for UserId {UserId}.", user.Id);
            return ApiResponse<LoginResponseDTO>.Success("Token refreshed successfully.", data);
        }

        public async Task<ApiResponse<string>> UploadProfilePictureAsync(Guid userId, Microsoft.AspNetCore.Http.IFormFile image)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                return ApiResponse<string>.Failure("User not found.");
            }

            if (image == null || image.Length == 0)
            {
                return ApiResponse<string>.Failure("No image provided.");
            }

            // Optional: check for existing image and delete it if it's hosted on our Supabase
            if (!string.IsNullOrEmpty(user.ProfilePictureUrl) && user.ProfilePictureUrl.Contains(_supabaseOptions.Url))
            {
                try
                {
                    var uri = new Uri(user.ProfilePictureUrl);
                    var existingFileName = Path.GetFileName(uri.LocalPath);
                    if (!string.IsNullOrEmpty(existingFileName))
                    {
                        await _storageService.DeleteFileAsync(_supabaseOptions.BucketName, existingFileName);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete existing profile picture for user {UserId}", userId);
                }
            }

            var extension = Path.GetExtension(image.FileName);
            var newFileName = $"{userId}-{Guid.NewGuid()}{extension}";

            try
            {
                var url = await _storageService.UploadFileAsync(image, _supabaseOptions.BucketName, newFileName);
                
                user.ProfilePictureUrl = url;
                var result = await _userManager.UpdateAsync(user);
                
                if (!result.Succeeded)
                {
                    var errors = result.Errors.Select(e => e.Description).ToList();
                    return ApiResponse<string>.Failure("Failed to update user profile picture.", errors);
                }

                return ApiResponse<string>.Success("Profile picture uploaded successfully.", url);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading profile picture for user {UserId}", userId);
                return ApiResponse<string>.Failure("An error occurred while uploading the image.");
            }
        }

        public async Task<ApiResponse<ProfileSettingsDTO>> GetSettingsAsync(Guid userId)
        {
            var user = await _userManager.Users
                .Include(u => u.ParkingOwner)
                .FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
            {
                return ApiResponse<ProfileSettingsDTO>.Failure("User not found.");
            }

            var result = new ProfileSettingsDTO
            {
                Header = new ProfileSettingsHeaderDTO
                {
                    UserId = user.Id,
                    FullName = user.FullName,
                    Email = user.Email ?? string.Empty,
                    Role = user.Role.ToString(),
                    CreatedAt = user.CreatedAt,
                    ProfilePictureUrl = user.ProfilePictureUrl,
                    IsVerifiedOwner = user.ParkingOwner?.VerificationStatus == VerificationStatus.Verified,
                    Stats = await GetOwnerStatsAsync(user.Id)
                },
                Personal = ToPersonalSettings(user),
                Business = user.ParkingOwner == null ? null : ToBusinessSettings(user.ParkingOwner),
                Notifications = user.ParkingOwner == null ? null : ToNotificationSettings(user.ParkingOwner),
                Security = new SecuritySettingsDTO
                {
                    SmsTwoFactorEnabled = user.TwoFactorEnabled,
                    ActiveSessions = await BuildActiveSessionsAsync(user.Id)
                }
            };

            return ApiResponse<ProfileSettingsDTO>.Success("Profile settings retrieved successfully.", result);
        }

        public async Task<ApiResponse<PersonalSettingsDTO>> UpdatePersonalSettingsAsync(Guid userId, UpdatePersonalSettingsDTO dto)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                return ApiResponse<PersonalSettingsDTO>.Failure("User not found.");
            }

            if (!string.Equals(user.Email, dto.Email, StringComparison.OrdinalIgnoreCase))
            {
                var existing = await _userManager.FindByEmailAsync(dto.Email);
                if (existing != null && existing.Id != user.Id)
                {
                    return ApiResponse<PersonalSettingsDTO>.Failure("This Email is already exists");
                }

                user.Email = dto.Email;
                user.UserName = dto.Email;
                user.EmailConfirmed = false;
            }

            user.FullName = dto.FullName.Trim();
            user.PhoneNumber = dto.PhoneNumber;
            user.CityState = dto.CityState;
            user.Bio = dto.Bio;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                return ApiResponse<PersonalSettingsDTO>.Failure("Personal settings update failed.", result.Errors.Select(e => e.Description).ToList());
            }

            return ApiResponse<PersonalSettingsDTO>.Success("Personal settings updated successfully.", ToPersonalSettings(user));
        }

        public async Task<ApiResponse<BusinessSettingsDTO>> UpdateBusinessSettingsAsync(Guid userId, UpdateBusinessSettingsDTO dto)
        {
            var owner = await _unitOfWork.ParkingOwners.FirstOrDefaultAsync(x => x.OwnerId == userId);
            if (owner == null)
            {
                return ApiResponse<BusinessSettingsDTO>.Failure("Parking owner profile not found.");
            }

            owner.CompanyName = dto.BusinessName.Trim();
            owner.TaxId = dto.TaxId;
            owner.StreetAddress = dto.StreetAddress;
            owner.CityStateZip = dto.CityStateZip;
            _unitOfWork.ParkingOwners.Update(owner);
            await _unitOfWork.SaveChangesAsync();

            return ApiResponse<BusinessSettingsDTO>.Success("Business settings updated successfully.", ToBusinessSettings(owner));
        }

        public async Task<ApiResponse<NotificationSettingsDTO>> UpdateNotificationSettingsAsync(Guid userId, NotificationSettingsDTO dto)
        {
            var owner = await _unitOfWork.ParkingOwners.FirstOrDefaultAsync(x => x.OwnerId == userId);
            if (owner == null)
            {
                return ApiResponse<NotificationSettingsDTO>.Failure("Parking owner profile not found.");
            }

            owner.NotifyNewBookings = dto.NewBookings;
            owner.NotifyCancellations = dto.Cancellations;
            owner.NotifyRevenueMilestones = dto.RevenueMilestones;
            owner.NotifySpaceAlerts = dto.SpaceAlerts;
            owner.NotifyMarketingUpdates = dto.MarketingUpdates;
            _unitOfWork.ParkingOwners.Update(owner);
            await _unitOfWork.SaveChangesAsync();

            return ApiResponse<NotificationSettingsDTO>.Success("Notification preferences updated successfully.", ToNotificationSettings(owner));
        }

        public async Task<ApiResponse> ChangePasswordAsync(Guid userId, UpdatePasswordDTO dto)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                return ApiResponse.Failure("User not found.");
            }

            var result = await _userManager.ChangePasswordAsync(user, dto.CurrentPassword, dto.NewPassword);
            if (!result.Succeeded)
            {
                return ApiResponse.Failure("Password update failed.", result.Errors.Select(e => e.Description).ToList());
            }

            await RevokeOtherSessionsAsync(user.Id);
            return ApiResponse.Success("Password updated successfully.");
        }

        public async Task<ApiResponse> UpdateTwoFactorAsync(Guid userId, UpdateTwoFactorDTO dto)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                return ApiResponse.Failure("User not found.");
            }

            user.TwoFactorEnabled = dto.Enabled;
            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                return ApiResponse.Failure("Two-factor setting update failed.", result.Errors.Select(e => e.Description).ToList());
            }

            return ApiResponse.Success("Two-factor setting updated successfully.");
        }

        public async Task<ApiResponse<List<ActiveSessionDTO>>> GetActiveSessionsAsync(Guid userId)
        {
            var sessions = await BuildActiveSessionsAsync(userId);
            return ApiResponse<List<ActiveSessionDTO>>.Success("Active sessions retrieved successfully.", sessions);
        }

        public async Task<ApiResponse> RevokeSessionAsync(Guid userId, Guid sessionId)
        {
            var token = await _unitOfWork.RefreshTokens.FirstOrDefaultAsync(x => x.Id == sessionId && x.UserId == userId);
            if (token == null)
            {
                return ApiResponse.Failure("Session not found.");
            }

            token.IsRevoked = true;
            _unitOfWork.RefreshTokens.Update(token);
            await _unitOfWork.SaveChangesAsync();

            return ApiResponse.Success("Session revoked successfully.");
        }

        public async Task<ApiResponse> DeleteAccountAsync(Guid userId, DeleteAccountDTO dto)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                return ApiResponse.Failure("User not found.");
            }

            if (!await _userManager.CheckPasswordAsync(user, dto.Password))
            {
                return ApiResponse.Failure("Invalid password.");
            }

            var tokens = await _unitOfWork.RefreshTokens.Query().Where(x => x.UserId == userId).ToListAsync();
            foreach (var token in tokens)
            {
                token.IsRevoked = true;
                _unitOfWork.RefreshTokens.Update(token);
            }

            await _unitOfWork.SaveChangesAsync();

            IdentityResult result;
            try
            {
                result = await _userManager.DeleteAsync(user);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogWarning(ex, "Account deletion failed because related records exist for user {UserId}", userId);
                return ApiResponse.Failure("Account cannot be deleted while related parking, reservation, or payment records exist.");
            }

            if (!result.Succeeded)
            {
                return ApiResponse.Failure("Account deletion failed.", result.Errors.Select(e => e.Description).ToList());
            }

            return ApiResponse.Success("Account deleted successfully.");
        }

        private async Task<OwnerStatsDTO> GetOwnerStatsAsync(Guid ownerId)
        {
            var parkings = await _unitOfWork.Parkings.Query()
                .Where(p => p.OwnerId == ownerId)
                .Include(p => p.ParkingSpaces)
                .ToListAsync();

            var parkingIds = parkings.Select(p => p.ParkingId).ToList();
            var startOfMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
            var reservations = await _unitOfWork.Reservations.Query()
                .Where(r => parkingIds.Contains(r.ParkingSpace.ParkingId))
                .Include(r => r.ParkingSpace)
                .ToListAsync();

            return new OwnerStatsDTO
            {
                Locations = parkings.Count,
                TotalSpaces = parkings.Sum(p => p.ParkingSpaces.Count),
                Bookings = reservations.Count,
                CurrentMonthRevenue = reservations
                    .Where(r => r.ArrivalTime >= startOfMonth && r.Status != ReservationStatus.Cancelled)
                    .Sum(r => r.TotalPrice),
                AverageRating = parkings.Count == 0 ? 0 : Math.Round(parkings.Average(p => p.AverageRating), 1)
            };
        }

        private async Task<List<ActiveSessionDTO>> BuildActiveSessionsAsync(Guid userId)
        {
            return await _unitOfWork.RefreshTokens.Query()
                .Where(x => x.UserId == userId && !x.IsRevoked && !x.IsUsed && x.ExpiryDate > DateTime.UtcNow)
                .OrderByDescending(x => x.AddedDate)
                .Select(x => new ActiveSessionDTO
                {
                    SessionId = x.Id,
                    Device = "Unknown device",
                    Location = "Unknown location",
                    CreatedAt = x.AddedDate,
                    ExpiresAt = x.ExpiryDate,
                    IsCurrent = false
                })
                .ToListAsync();
        }

        private async Task RevokeOtherSessionsAsync(Guid userId)
        {
            var activeTokens = await _unitOfWork.RefreshTokens.Query()
                .Where(x => x.UserId == userId && !x.IsRevoked && !x.IsUsed && x.ExpiryDate > DateTime.UtcNow)
                .ToListAsync();

            foreach (var token in activeTokens)
            {
                token.IsRevoked = true;
                _unitOfWork.RefreshTokens.Update(token);
            }

            await _unitOfWork.SaveChangesAsync();
        }

        private static PersonalSettingsDTO ToPersonalSettings(AppUser user) => new()
        {
            FullName = user.FullName,
            Email = user.Email ?? string.Empty,
            PhoneNumber = user.PhoneNumber,
            CityState = user.CityState,
            Bio = user.Bio
        };

        private static BusinessSettingsDTO ToBusinessSettings(ParkingOwner owner) => new()
        {
            BusinessName = owner.CompanyName,
            TaxId = owner.TaxId,
            StreetAddress = owner.StreetAddress,
            CityStateZip = owner.CityStateZip,
            VerificationStatus = owner.VerificationStatus.ToString(),
            BusinessVerifiedAt = owner.BusinessVerifiedAt
        };

        private static NotificationSettingsDTO ToNotificationSettings(ParkingOwner owner) => new()
        {
            NewBookings = owner.NotifyNewBookings,
            Cancellations = owner.NotifyCancellations,
            RevenueMilestones = owner.NotifyRevenueMilestones,
            SpaceAlerts = owner.NotifySpaceAlerts,
            MarketingUpdates = owner.NotifyMarketingUpdates
        };
    }
}
