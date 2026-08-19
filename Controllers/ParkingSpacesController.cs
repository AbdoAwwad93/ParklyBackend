using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Parkly_Backend.Interfaces;
using Parkly_Backend.Models.DTOs;
using Parkly_Backend.Models.Response;
using System.Security.Claims;

namespace Parkly_Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Produces("application/json")]
    public class ParkingSpacesController : ControllerBase
    {
        private readonly IParkingSpacesService _service;

        public ParkingSpacesController(IParkingSpacesService service)
        {
            _service = service;
        }

        /// <summary>Returns all parking spaces across all parking facilities.</summary>
        /// <returns>An <see cref="ApiResponse{T}"/> containing the list of parking spaces.</returns>
        /// <response code="200">Parking spaces retrieved successfully.</response>
        [HttpGet]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse<List<ParkingSpaceResponseDTO>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll()
        {
            var result = await _service.GetAllAsync();
            return Ok(result);
        }

        /// <summary>Returns the spaces belonging to a specific parking facility.</summary>
        /// <param name="parkingId">The id of the parking facility.</param>
        /// <returns>An <see cref="ApiResponse{T}"/> containing the list of parking spaces.</returns>
        /// <response code="200">Parking spaces retrieved successfully.</response>
        /// <response code="400">The parking was not found.</response>
        [HttpGet("parking/{parkingId:guid}")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse<List<ParkingSpaceResponseDTO>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetByParkingId(Guid parkingId)
        {
            var result = await _service.GetByParkingIdAsync(parkingId);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        /// <summary>Returns a single parking space by id.</summary>
        /// <param name="spaceId">The id of the parking space.</param>
        /// <returns>An <see cref="ApiResponse{T}"/> containing the parking space.</returns>
        /// <response code="200">Parking space retrieved successfully.</response>
        /// <response code="400">The parking space was not found.</response>
        [HttpGet("{spaceId:guid}")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse<ParkingSpaceResponseDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetById(Guid spaceId)
        {
            var result = await _service.GetByIdAsync(spaceId);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        /// <summary>Creates a new parking space for a parking owned by the authenticated parking owner.</summary>
        /// <param name="dto">The parking space details.</param>
        /// <returns>An <see cref="ApiResponse{T}"/> containing the created parking space.</returns>
        /// <response code="200">Parking space created successfully.</response>
        /// <response code="400">Validation failed or ownership cannot be verified.</response>
        /// <response code="401">Missing or invalid JWT token.</response>
        /// <response code="403">The authenticated user is not a parking owner.</response>
        [HttpPost]
        [Authorize(Roles = "ParkingOwner")]
        [ProducesResponseType(typeof(ApiResponse<ParkingSpaceResponseDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> Create(CreateParkingSpaceDTO dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse.FromModelState("Invalid request", ModelState));
            }

            var ownerId = GetUserId();
            var result = await _service.CreateAsync(ownerId, dto);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        /// <summary>Updates an existing parking space belonging to a parking owned by the authenticated parking owner.</summary>
        /// <param name="spaceId">The id of the parking space to update.</param>
        /// <param name="dto">The new parking space details.</param>
        /// <returns>An <see cref="ApiResponse{T}"/> containing the updated parking space.</returns>
        /// <response code="200">Parking space updated successfully.</response>
        /// <response code="400">Validation failed or the space cannot be updated.</response>
        /// <response code="401">Missing or invalid JWT token.</response>
        /// <response code="403">The authenticated user is not a parking owner.</response>
        [HttpPut("{spaceId:guid}")]
        [Authorize(Roles = "ParkingOwner")]
        [ProducesResponseType(typeof(ApiResponse<ParkingSpaceResponseDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> Update(Guid spaceId, UpdateParkingSpaceDTO dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse.FromModelState("Invalid request", ModelState));
            }

            var ownerId = GetUserId();
            var result = await _service.UpdateAsync(ownerId, spaceId, dto);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        /// <summary>Deletes an existing parking space belonging to a parking owned by the authenticated parking owner.</summary>
        /// <param name="spaceId">The id of the parking space to delete.</param>
        /// <returns>An <see cref="ApiResponse"/> indicating the deletion result.</returns>
        /// <response code="200">Parking space deleted successfully.</response>
        /// <response code="400">The space could not be deleted or has active reservations.</response>
        /// <response code="401">Missing or invalid JWT token.</response>
        /// <response code="403">The authenticated user is not a parking owner.</response>
        [HttpDelete("{spaceId:guid}")]
        [Authorize(Roles = "ParkingOwner")]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> Delete(Guid spaceId)
        {
            var ownerId = GetUserId();
            var result = await _service.DeleteAsync(ownerId, spaceId);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        private Guid GetUserId()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.Parse(userId);
        }
    }
}