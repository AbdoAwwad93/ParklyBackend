using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Parkly_Backend.Models;
using Parkly_Backend.Models.DTOs;
using Parkly_Backend.Services.Interfaces;
using Superpower.Parsers;

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
                return BadRequest(ModelState);
            }
            var result = await _service.Register(user);
            if (!result.success)
            {
                return BadRequest("An error occurred or email is exists");
            }
            
            return Ok("Account Created Successfully");
           
        }
        [HttpPost("/login")]
        public async Task<IActionResult> LogIn(LoginDTO login)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            var result = await _service.LogIn(login);
            if (!result.success)
            {
                return Unauthorized("Invalid Email or PassWord");
            }
            return Ok($"token:result.Token");

        }

    }
}
