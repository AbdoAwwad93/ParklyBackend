using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Parkly_Backend.Models;
using Parkly_Backend.Models.DTOs;
using Parkly_Backend.Models.Response;
using Parkly_Backend.Services.Interfaces;

namespace Parkly_Backend.Controllers
{
    [Route("api/auth")]
    [ApiController]
    [Produces("application/json")]
    public class AccountController : ControllerBase
    {
        private readonly IAccountService _service;
        private readonly UserManager<AppUser> _userManager;
        public AccountController(IAccountService service, UserManager<AppUser> userManager)
        {
            _service = service;
            _userManager = userManager;
        }
        /// <summary>Registers a new user account in the system.</summary>
        /// <param name="user">The registration details.</param>
        /// <returns>An <see cref="ApiResponse"/> indicating the result of the registration.</returns>
        /// <response code="200">Registration succeeded.</response>
        /// <response code="400">Validation failed or the email is already registered.</response>
        [HttpPost("register")]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
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
        /// <summary>Authenticates a user and returns a JWT token.</summary>
        /// <param name="login">The login credentials.</param>
        /// <returns>An <see cref="ApiResponse{T}"/> whose <c>Data</c> is the JWT token on success.</returns>
        /// <response code="200">Login succeeded; a JWT token is returned.</response>
        /// <response code="400">Validation failed.</response>
        /// <response code="401">Invalid credentials.</response>
        [HttpPost("login")]
        [ProducesResponseType(typeof(ApiResponse<LoginResponseDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<LoginResponseDTO>), StatusCodes.Status401Unauthorized)]
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
        /// <summary>Requests a password reset OTP sent to the user's email.</summary>
        /// <param name="forgotPassword">The email of the account.</param>
        /// <returns>An <see cref="ApiResponse"/> indicating the result.</returns>
        /// <response code="200">The request was processed (generic response, enumeration-safe).</response>
        /// <response code="400">Validation failed.</response>
        [HttpPost("forgot-password")]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordDTO forgotPassword)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse.FromModelState("Invalid request", ModelState));
            }
            var result = await _service.ForgotPasswordAsync(forgotPassword);
            return Ok(result);
        }

        /// <summary>Resets the user's password using the OTP received by email.</summary>
        /// <param name="resetPassword">The email, OTP and new password.</param>
        /// <returns>An <see cref="ApiResponse"/> indicating the result.</returns>
        /// <response code="200">Password reset succeeded.</response>
        /// <response code="400">Validation failed or the OTP is invalid/expired.</response>
        [HttpPost("reset-password")]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ResetPassword(ResetPasswordDTO resetPassword)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse.FromModelState("Invalid request", ModelState));
            }
            var result = await _service.ResetPasswordAsync(resetPassword);
            if (!result.IsSuccess)
            {
                return BadRequest(result);
            }
            return Ok(result);
        }

    }
}
