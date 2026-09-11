using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Parkly_Backend.Interfaces;
using Parkly_Backend.Models.DTOs;
using Parkly_Backend.Models.Response;
using System.Threading.Tasks;

namespace Parkly_Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    [Produces("application/json")]
    public class AccessController : ControllerBase
    {
        private readonly IAccessService _accessService;

        public AccessController(IAccessService accessService)
        {
            _accessService = accessService;
        }

        /// <summary>Processes a physical gate scan using a 6-digit QR / access code.</summary>
        /// <param name="dto">The scan details including QrToken (6-digit code) and ScanType.</param>
        /// <returns>An <see cref="ApiResponse"/> indicating success or failure of the scan.</returns>
        /// <response code="200">Scan processed successfully.</response>
        /// <response code="400">Invalid scan request or state transition.</response>
        /// <response code="401">Missing or invalid JWT token.</response>
        [HttpPost("scan")]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Scan([FromBody] AccessScanDTO dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse.FromModelState("Invalid request", ModelState));
            }

            var result = await _accessService.ProcessScanAsync(dto);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        /// <summary>Performs check-in using a 6-digit QR access code.</summary>
        /// <param name="qrToken">The 6-digit access code.</param>
        /// <returns>An <see cref="ApiResponse{T}"/> containing the check-in and active parking details.</returns>
        /// <response code="200">Checked in successfully.</response>
        /// <response code="400">Invalid code or state transition.</response>
        /// <response code="401">Missing or invalid JWT token.</response>
        [HttpPost("check-in")]
        [ProducesResponseType(typeof(ApiResponse<CheckInResponseDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> CheckIn([FromQuery] string qrToken)
        {
            if (string.IsNullOrWhiteSpace(qrToken))
            {
                return BadRequest(ApiResponse.Failure("QR code cannot be empty."));
            }

            var result = await _accessService.CheckInAsync(qrToken);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        /// <summary>Performs check-out using a 6-digit QR access code.</summary>
        /// <param name="qrToken">The 6-digit access code.</param>
        /// <returns>An <see cref="ApiResponse{T}"/> containing the check-out receipt and billing summary.</returns>
        /// <response code="200">Checked out successfully.</response>
        /// <response code="400">Invalid code or state transition.</response>
        /// <response code="401">Missing or invalid JWT token.</response>
        [HttpPost("check-out")]
        [ProducesResponseType(typeof(ApiResponse<CheckOutResponseDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> CheckOut([FromQuery] string qrToken)
        {
            if (string.IsNullOrWhiteSpace(qrToken))
            {
                return BadRequest(ApiResponse.Failure("QR code cannot be empty."));
            }

            var result = await _accessService.CheckOutAsync(qrToken);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }
    }
}
