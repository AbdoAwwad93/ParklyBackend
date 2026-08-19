using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Parkly_Backend.Interfaces;
using Parkly_Backend.Models.DTOs;
using Parkly_Backend.Models.Response;

namespace Parkly_Backend.Controllers
{
    [Route("api/admin")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    [Produces("application/json")]
    public class AdminController : ControllerBase
    {
        private readonly IAdminService _service;

        public AdminController(IAdminService service)
        {
            _service = service;
        }

        /// <summary>Registers a new admin account. Only existing admins can perform this action.</summary>
        /// <param name="user">The new admin's registration details.</param>
        /// <returns>An <see cref="ApiResponse"/> indicating the result of the registration.</returns>
        /// <response code="200">Registration succeeded.</response>
        /// <response code="400">Validation failed or the email is already registered.</response>
        /// <response code="401">Missing or invalid JWT token.</response>
        /// <response code="403">The authenticated user is not an admin.</response>
        [HttpPost("register")]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> Register(RegisterDTO user)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse.FromModelState("Invalid request", ModelState));
            }

            var result = await _service.RegisterAdmin(user);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }
    }
}