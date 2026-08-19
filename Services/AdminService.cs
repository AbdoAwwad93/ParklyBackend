using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Parkly_Backend.Interfaces;
using Parkly_Backend.Models;
using Parkly_Backend.Models.DTOs;
using Parkly_Backend.Models.Enums;
using Parkly_Backend.Models.Response;

namespace Parkly_Backend.Services
{
    public class AdminService : IAdminService
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly IMapper _mapper;

        public AdminService(UserManager<AppUser> userManager, IMapper mapper)
        {
            _userManager = userManager;
            _mapper = mapper;
        }

        public async Task<ApiResponse> RegisterAdmin(RegisterDTO dto)
        {
            var exUser = await _userManager.FindByEmailAsync(dto.Email);
            if (exUser != null)
            {
                return ApiResponse.Failure("This Email is already exists");
            }

            var newUser = _mapper.Map<AppUser>(dto);
            newUser.Role = UserRole.Admin;

            var result = await _userManager.CreateAsync(newUser, dto.Password);
            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => e.Description).ToList();
                return ApiResponse.Failure("Account creation failed", errors);
            }

            return ApiResponse.Success("Admin account created successfully!");
        }
    }
}