using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Parkly_Backend.Data.Repositories;
using Parkly_Backend.Models;
using Parkly_Backend.Models.DTOs;
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
        public string GenerateJwtToken(AppUser user)
        {
            var claims = new List<Claim>()
            {
              new Claim(ClaimTypes.Name,user.UserName),
              new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
              new Claim(ClaimTypes.Email,user.Email),
              new Claim(ClaimTypes.Role, user.Role.ToString()),
              new Claim(JwtRegisteredClaimNames.Jti,Guid.NewGuid().ToString())
            };

            //SigningCredentials
            var SecretKey = Environment.GetEnvironmentVariable("SecretKey");
            var Key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SecretKey));
            var sc = new SigningCredentials(Key, SecurityAlgorithms.HmacSha256 );
            var token = new JwtSecurityToken(
                claims: claims,
                issuer: Environment.GetEnvironmentVariable("Issuer"),
                audience: Environment.GetEnvironmentVariable("Audience"),
                expires: DateTime.Now.AddHours(1),
                signingCredentials: sc
                
                );
            return new JwtSecurityTokenHandler().WriteToken(token);

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
            data.Token = GenerateJwtToken(user);

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

    }
}
