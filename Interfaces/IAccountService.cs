using Parkly_Backend.Data;
using Parkly_Backend.Models.DTOs;
using Parkly_Backend.Models.Response;
using System;
using System.Threading.Tasks;

namespace Parkly_Backend.Services.Interfaces
{
   public interface IAccountService
    {
        public Task<ApiResponse> Register(RegisterDTO newUser);
        public Task<ApiResponse> RegisterOwner(OwnerRegisterDTO newOwner);
        public Task<ApiResponse<LoginResponseDTO>> LogIn(LoginDTO login);
        public Task<ApiResponse> ForgotPasswordAsync(ForgotPasswordDTO forgotPassword);
        public Task<ApiResponse> ResetPasswordAsync(ResetPasswordDTO resetPassword);
        Task<ApiResponse<ProfileDTO>> GetProfileAsync(Guid userId);
        Task<ApiResponse<ProfileDTO>> UpdateProfileAsync(Guid userId, UpdateProfileDTO dto);
        Task<ApiResponse> LogoutAsync(TokenRequestDTO tokenRequest);
        Task<ApiResponse<LoginResponseDTO>> RefreshTokenAsync(TokenRequestDTO tokenRequest);
    }
}
