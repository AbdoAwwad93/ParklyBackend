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
using Parkly_Backend.Services.Interfaces;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Parkly_Backend.Services.Implemention
{
    public class AccountService:IAccountService
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly IMapper _mapper;
        private readonly IEmailService _emailService;
        private readonly IUnitOfWork _unitOfWork;
        private static readonly Random _random = new Random();
        public AccountService(UserManager<AppUser> userManager, IMapper mapper, IEmailService emailService, IUnitOfWork unitOfWork)
        {
            _userManager = userManager;
            _mapper = mapper;
            _emailService = emailService;
            _unitOfWork = unitOfWork;
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
            var SecretKey = Environment.GetEnvironmentVariable("SecretKey");
            var Key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SecretKey));
            var sc = new SigningCredentials(Key, SecurityAlgorithms.HmacSha256 );
            var token = new JwtSecurityToken(
                claims: claims,
                issuer: Environment.GetEnvironmentVariable("Issuer"),
                audience: Environment.GetEnvironmentVariable("Audience"),
                expires: DateTime.UtcNow.AddMinutes(Convert.ToInt32(Environment.GetEnvironmentVariable("JwtExpiresInMinutes"))),
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
                ExpiryDate = DateTime.UtcNow.AddMonths(Convert.ToInt32(Environment.GetEnvironmentVariable("RefreshTokenExpiresInMonths"))),
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
                return ApiResponse.Failure("Account creation failed", errors);
            
            }
            return ApiResponse.Success("Account is created successfully!");
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

                var parkingOwner = new ParkingOwner
                {
                    OwnerId = newUser.Id,
                    CompanyName = newOwner.CompanyName,
                    PayoutAccount = newOwner.PayoutAccount,
                    VerificationStatus = VerificationStatus.Pending
                };
                await _unitOfWork.Repository<ParkingOwner>().AddAsync(parkingOwner);
                await _unitOfWork.SaveChangesAsync();

                await _unitOfWork.CommitTransactionAsync();
                return ApiResponse.Success("Parking owner account created successfully!");
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

                return ApiResponse<LoginResponseDTO>.Failure("Invalid Email or Password");
            }
            var isPasswordValid = await _userManager.CheckPasswordAsync(user, login.Password);
            if (!isPasswordValid) {

                return ApiResponse<LoginResponseDTO>.Failure("Invalid Email or Password");
            }

            var data = _mapper.Map<LoginResponseDTO>(user);
            var (jwtToken, jti) = GenerateJwtToken(user);
            var refreshToken = GenerateRefreshTokenString(user.Id, jti);

            await _unitOfWork.Repository<RefreshToken>().AddAsync(refreshToken);
            await _unitOfWork.SaveChangesAsync();

            data.Token = jwtToken;
            data.RefreshToken = refreshToken.Token;

            return ApiResponse<LoginResponseDTO>.Success("Login Successful", data);

        }

        public async Task<ApiResponse> ForgotPasswordAsync(ForgotPasswordDTO forgotPassword)
        {
            var user = await _userManager.FindByEmailAsync(forgotPassword.Email);
            if (user == null)
            {
                return ApiResponse.Success("If the email is registered, an OTP has been sent to reset your password.");
            }

            var repo = _unitOfWork.Repository<PasswordResetOtp>();
            var activeOtps = await repo.Query()
                .Where(o => o.UserId == user.Id && !o.IsUsed && o.ExpiresAt > DateTime.UtcNow)
                .ToListAsync();
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

        public async Task<ApiResponse> ResetPasswordAsync(ResetPasswordDTO resetPassword)
        {
            var user = await _userManager.FindByEmailAsync(resetPassword.Email);
            if (user == null)
            {
                return ApiResponse.Failure("Invalid OTP or the code has expired.");
            }

            var repo = _unitOfWork.Repository<PasswordResetOtp>();
            var otp = await repo.Query()
                .Where(o => o.UserId == user.Id && !o.IsUsed && o.ExpiresAt > DateTime.UtcNow)
                .OrderByDescending(o => o.CreatedAt)
                .FirstOrDefaultAsync();
            if (otp == null || !VerifyOtp(resetPassword.Otp, otp.CodeHash))
            {
                return ApiResponse.Failure("Invalid OTP or the code has expired.");
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, resetPassword.NewPassword);
            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => e.Description).ToList();
                return ApiResponse.Failure("Password reset failed", errors);
            }

            otp.IsUsed = true;
            repo.Update(otp);
            await _unitOfWork.SaveChangesAsync();

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
            var refreshTokenRepo = _unitOfWork.Repository<RefreshToken>();
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
            var SecretKey = Environment.GetEnvironmentVariable("SecretKey");
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

            var refreshTokenRepo = _unitOfWork.Repository<RefreshToken>();
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

            return ApiResponse<LoginResponseDTO>.Success("Token refreshed successfully.", data);
        }
    }
}
