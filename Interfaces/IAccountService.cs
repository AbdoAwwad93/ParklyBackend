using Parkly_Backend.Data;
using Parkly_Backend.Models.DTOs;
using Parkly_Backend.Models.Response;

namespace Parkly_Backend.Services.Interfaces
{
   public interface IAccountService
    {
        public Task<ApiResponse> Register(RegisterDTO newUser);
        public Task<ApiResponse> RegisterOwner(OwnerRegisterDTO newOwner);
        public Task<ApiResponse<LoginResponseDTO>> LogIn(LoginDTO login);
        public Task<ApiResponse> ForgotPasswordAsync(ForgotPasswordDTO forgotPassword);
        public Task<ApiResponse> ResetPasswordAsync(ResetPasswordDTO resetPassword);
    }
}
