using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using AutoMapper;
using Microsoft.IdentityModel.Tokens;
using Parkly_Backend.Models;
using Parkly_Backend.Models.DTOs;
using Parkly_Backend.Models.Response;
using Parkly_Backend.Services.Interfaces;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Parkly_Backend.Services.Implemention
{
    public class AccountService:IAccountService
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly IMapper _mapper;
        public AccountService(UserManager<AppUser> userManager, IMapper mapper)
        {
            _userManager = userManager;
            _mapper = mapper;
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

    }
}
