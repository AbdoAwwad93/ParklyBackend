using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using Parkly_Backend.Models;
using Parkly_Backend.Models.DTOs;
using Parkly_Backend.Services.Interfaces;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Parkly_Backend.Services.Implemention
{
    public class AccountService:IAccountService
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly IConfiguration _configuration;
        public AccountService(UserManager<AppUser> userManager, IConfiguration configuration)
        {
            _userManager = userManager;
            _configuration = configuration;
        }
        public string GenerateJwtToken(AppUser user)
        {
            var claims = new List<Claim>()
            {
              new Claim(ClaimTypes.Name,user.UserName),
              new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
              new Claim(ClaimTypes.Email,user.Email),
              new Claim(JwtRegisteredClaimNames.Jti,Guid.NewGuid().ToString())
            };

            //SigningCredentials
            var Key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes( _configuration["JWT:SecretKey"]));
            var sc = new SigningCredentials(Key, SecurityAlgorithms.HmacSha256 );
            var token = new JwtSecurityToken(
                claims: claims,
                issuer: _configuration["JWT:Issuer"],
                audience: _configuration["JWT:Audience"],
                expires: DateTime.Now.AddHours(1),
                signingCredentials: sc
                
                );
            return new JwtSecurityTokenHandler().WriteToken(token);

        }

        public async Task<(bool success, string message)> Register(RegisterDTO user)
        {
            var exUser = await _userManager.FindByEmailAsync(user.Email);
            if(exUser!=null)
            {
                return (false, "This Email is already exists");
            }
            var newUser = new AppUser
            {
                FullName = $"{user.FirstName} {user.LastName}",
                Email= user.Email,
                UserName= user.UserName,
                PhoneNumber=user.Phone,
            };
            IdentityResult result = await _userManager.CreateAsync(newUser,user.Password);
            if (!result.Succeeded) {

                var errors = string.Join(",",result.Errors.Select(e => e.Description));
                return (false, errors);
            
            }
            return (true, "Account is created successfully!");
        }
        public async Task<(bool success, string message, string? Token)> LogIn(LoginDTO login)
        {
           var user= await _userManager.FindByEmailAsync(login.Email);
            if (user == null) {

                return (false, "Invalid Email or Password", null);
            }
            var isPasswordValid = await _userManager.CheckPasswordAsync(user, login.Password);
            if (!isPasswordValid) {

                return (false, "Invalid Email or Password", null);
            }
            var token = GenerateJwtToken(user);

            return (true, "Login Successful", token);

        }

    }
}
