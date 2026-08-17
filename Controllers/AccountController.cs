using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Parkly_Backend.Models;
using Parkly_Backend.Models.DTOs;
using Parkly_Backend.Models.Response;
using Parkly_Backend.Services.Interfaces;

namespace Parkly_Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        private readonly IAccountService _service;
        private readonly UserManager<AppUser> _userManager;
        public AccountController(IAccountService service, UserManager<AppUser> userManager)
        {
            _service = service;
            _userManager = userManager;
        }
        [HttpPost("/register")]
        public async Task<IActionResult> Register(RegisterDTO user)
        {
            if(!ModelState.IsValid)
            {
                var response = ApiResponse.FromModelState("Invalid request", ModelState);
                return BadRequest(response);
            }
            var result = await _service.Register(user);
            if (!result.IsSuccess)
            {
                return BadRequest(result);
            }
            
            return Ok(result);
           
        }
        [HttpPost("/login")]
        public async Task<IActionResult> LogIn(LoginDTO login)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse.FromModelState("Invalid request", ModelState));
            }
            var result = await _service.LogIn(login);
            if (!result.IsSuccess)
            {
                return Unauthorized(result);
            }
            return Ok(result);

        }

    }
}
