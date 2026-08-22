using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Parkly_Backend.Models;
using Parkly_Backend.Models.DTOs;
using Parkly_Backend.Models.Response;
using Parkly_Backend.Services.Interfaces;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using System;

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
        /// <summary>Registers a new parking owner account in the system.</summary>
        /// <param name="user">The owner registration details.</param>
        /// <returns>An <see cref="ApiResponse"/> indicating the result of the registration.</returns>
        /// <response code="200">Registration succeeded.</response>
        /// <response code="400">Validation failed or the email is already registered.</response>
        [HttpPost("register-owner")]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> RegisterOwner(OwnerRegisterDTO user)
        {
            if (!ModelState.IsValid)
            {
                var response = ApiResponse.FromModelState("Invalid request", ModelState);
                return BadRequest(response);
            }
            var result = await _service.RegisterOwner(user);
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

        /// <summary>Retrieves the profile of the currently authenticated user.</summary>
        /// <returns>An <see cref="ApiResponse{T}"/> containing the user's profile data.</returns>
        /// <response code="200">Profile retrieved successfully.</response>
        /// <response code="401">Missing or invalid JWT token.</response>
        [HttpGet("profile")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse<ProfileDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetProfile()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out Guid parsedId))
            {
                return Unauthorized();
            }

            var result = await _service.GetProfileAsync(parsedId);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        /// <summary>Updates the profile of the currently authenticated user.</summary>
        /// <param name="updateProfile">The new profile details.</param>
        /// <returns>An <see cref="ApiResponse{T}"/> containing the updated profile data.</returns>
        /// <response code="200">Profile updated successfully.</response>
        /// <response code="400">Validation failed or profile update failed.</response>
        /// <response code="401">Missing or invalid JWT token.</response>
        [HttpPut("profile")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse<ProfileDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> UpdateProfile(UpdateProfileDTO updateProfile)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse.FromModelState("Invalid request", ModelState));
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out Guid parsedId))
            {
                return Unauthorized();
            }

            var result = await _service.UpdateProfileAsync(parsedId, updateProfile);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        /// <summary>Logs out the current user by revoking their refresh token.</summary>
        /// <param name="tokenRequest">The token payload containing the access token and the refresh token.</param>
        /// <returns>An <see cref="ApiResponse"/> indicating success.</returns>
        /// <response code="200">Logout succeeded.</response>
        [HttpPost("logout")]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> Logout([FromBody] TokenRequestDTO tokenRequest)
        {
            var result = await _service.LogoutAsync(tokenRequest);
            return Ok(result);
        }

        /// <summary>Refreshes an expired access token using a valid refresh token.</summary>
        /// <param name="tokenRequest">The token payload containing the expired access token and the refresh token.</param>
        /// <returns>An <see cref="ApiResponse{T}"/> containing the new tokens.</returns>
        /// <response code="200">Tokens refreshed successfully.</response>
        /// <response code="400">Tokens are invalid or expired.</response>
        [HttpPost("refresh-token")]
        [ProducesResponseType(typeof(ApiResponse<LoginResponseDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<LoginResponseDTO>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> RefreshToken([FromBody] TokenRequestDTO tokenRequest)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse.FromModelState("Invalid request", ModelState));
            }
            var result = await _service.RefreshTokenAsync(tokenRequest);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

    }
}
