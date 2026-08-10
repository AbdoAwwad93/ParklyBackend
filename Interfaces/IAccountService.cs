using Parkly_Backend.Data;
using Parkly_Backend.Models.DTOs;

namespace Parkly_Backend.Services.Interfaces
{
   public interface IAccountService
    {
        public Task<(bool success, string message)> Register(RegisterDTO newUser);
        public Task<(bool success, string message, string? Token)> LogIn(LoginDTO login);

    }
}
