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
    [Authorize]
    [Produces("application/json")]
    public class ReservationsController : ControllerBase
    {
        private readonly IReservationsService _service;

        public ReservationsController(IReservationsService service)
        {
            _service = service;
        }

        /// <summary>Creates a new parking reservation for the authenticated user.</summary>
        /// <param name="dto">The reservation details.</param>
        /// <returns>An <see cref="ApiResponse{T}"/> containing the created reservation.</returns>
        /// <response code="200">Reservation created successfully.</response>
        /// <response code="400">Validation failed or the space/booking is unavailable.</response>
        /// <response code="401">Missing or invalid JWT token.</response>
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<ReservationResponseDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Create(CreateReservationDTO dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse.FromModelState("Invalid request", ModelState));
            }

            var userId = GetUserId();
            var result = await _service.CreateAsync(userId, dto);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        /// <summary>Updates the times of an existing reservation belonging to the authenticated user.</summary>
        /// <param name="id">The id of the reservation to update.</param>
        /// <param name="dto">The new arrival and departure times.</param>
        /// <returns>An <see cref="ApiResponse{T}"/> containing the updated reservation.</returns>
        /// <response code="200">Reservation updated successfully.</response>
        /// <response code="400">Validation failed or the reservation cannot be updated.</response>
        /// <response code="401">Missing or invalid JWT token.</response>
        [HttpPut("{id:guid}")]
        [ProducesResponseType(typeof(ApiResponse<ReservationResponseDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Update(Guid id, UpdateReservationDTO dto)
        {
            if (!ModelState.IsValid)
            {
                var response = ApiResponse.FromModelState("Invalid request", ModelState);
                return BadRequest(response);
            }

            var userId = GetUserId();
            var result = await _service.UpdateAsync(userId, id, dto);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        /// <summary>Cancels an existing reservation belonging to the authenticated user.</summary>
        /// <param name="id">The id of the reservation to cancel.</param>
        /// <returns>An <see cref="ApiResponse"/> indicating the cancellation result.</returns>
        /// <response code="200">Reservation cancelled successfully.</response>
        /// <response code="400">The reservation could not be cancelled.</response>
        /// <response code="401">Missing or invalid JWT token.</response>
        [HttpDelete("{id:guid}")]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Cancel(Guid id)
        {
            var userId = GetUserId();
            var result = await _service.CancelAsync(userId, id);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        /// <summary>Generates a QR code token for an existing reservation.</summary>
        /// <param name="id">The id of the reservation.</param>
        /// <returns>An <see cref="ApiResponse{T}"/> containing the QR code token.</returns>
        /// <response code="200">QR code generated successfully.</response>
        /// <response code="400">The QR code could not be generated.</response>
        /// <response code="401">Missing or invalid JWT token.</response>
        [HttpGet("{id:guid}/qr")]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetQrCode(Guid id)
        {
            var userId = GetUserId();
            var result = await _service.GetQrCodeAsync(userId, id);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        private Guid GetUserId()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.Parse(userId);
        }
    }
}