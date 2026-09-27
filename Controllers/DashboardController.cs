using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Parkly_Backend.Common.Extensions;
using Parkly_Backend.Interfaces;
using Parkly_Backend.Models.DTOs;
using Parkly_Backend.Models.Response;

namespace Parkly_Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "ParkingOwner,Admin")]
    [Produces("application/json")]
    public class DashboardController : ControllerBase
    {
        private readonly IDashboardService _service;

        public DashboardController(IDashboardService service)
        {
            _service = service;
        }

        /// <summary>Returns aggregated statistics for the dashboard header banner and four stats cards.</summary>
        /// <returns>An <see cref="ApiResponse{T}"/> containing the dashboard summary.</returns>
        /// <response code="200">Summary retrieved successfully.</response>
        /// <response code="401">Missing or invalid JWT token.</response>
        /// <response code="403">Authenticated user is not a parking owner.</response>
        [HttpGet("summary")]
        [ProducesResponseType(typeof(ApiResponse<DashboardSummaryDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetSummary()
        {
            var ownerId = User.GetRequiredUserId();
            var result = await _service.GetSummaryAsync(ownerId);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        /// <summary>Returns revenue chart data aggregated by the specified period.</summary>
        /// <param name="period">Aggregation period: "daily" (last 7 days), "weekly" (last 4 weeks), or "monthly" (last 12 months). Defaults to "daily".</param>
        /// <returns>An <see cref="ApiResponse{T}"/> containing the revenue overview with data points.</returns>
        /// <response code="200">Revenue overview retrieved successfully.</response>
        /// <response code="400">Invalid period value.</response>
        /// <response code="401">Missing or invalid JWT token.</response>
        /// <response code="403">Authenticated user is not a parking owner.</response>
        [HttpGet("revenue")]
        [ProducesResponseType(typeof(ApiResponse<RevenueOverviewDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetRevenueOverview([FromQuery] string period = "daily")
        {
            var ownerId = User.GetRequiredUserId();
            var result = await _service.GetRevenueOverviewAsync(ownerId, period);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        /// <summary>Returns occupancy status and progress bar data for each of the owner's parking locations.</summary>
        /// <returns>An <see cref="ApiResponse{T}"/> containing the list of location statuses.</returns>
        /// <response code="200">Locations status retrieved successfully.</response>
        /// <response code="401">Missing or invalid JWT token.</response>
        /// <response code="403">Authenticated user is not a parking owner.</response>
        [HttpGet("locations")]
        [ProducesResponseType(typeof(ApiResponse<List<LocationStatusDTO>>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetLocationsStatus()
        {
            var ownerId = User.GetRequiredUserId();
            var result = await _service.GetLocationsStatusAsync(ownerId);
            return Ok(result);
        }

        /// <summary>Returns a paginated list of today's reservations across all of the owner's parking locations.</summary>
        /// <param name="page">1-based page number (default: 1).</param>
        /// <param name="pageSize">Number of items per page, clamped to 1–100 (default: 10).</param>
        /// <returns>An <see cref="ApiResponse{T}"/> containing the list of today's reservations.</returns>
        /// <response code="200">Today's reservations retrieved successfully.</response>
        /// <response code="401">Missing or invalid JWT token.</response>
        /// <response code="403">Authenticated user is not a parking owner.</response>
        [HttpGet("reservations/today")]
        [ProducesResponseType(typeof(ApiResponse<List<OwnerReservationDTO>>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetTodaysReservations(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            var ownerId = User.GetRequiredUserId();
            var result = await _service.GetTodaysReservationsAsync(ownerId, page, pageSize);
            return Ok(result);
        }

        /// <summary>Returns the most recent activity events (check-ins, bookings, cancellations) across all owner locations.</summary>
        /// <param name="limit">Maximum number of activity items to return, clamped to 1–50 (default: 10).</param>
        /// <returns>An <see cref="ApiResponse{T}"/> containing the list of activity feed items.</returns>
        /// <response code="200">Recent activity retrieved successfully.</response>
        /// <response code="401">Missing or invalid JWT token.</response>
        /// <response code="403">Authenticated user is not a parking owner.</response>
        [HttpGet("activity")]
        [ProducesResponseType(typeof(ApiResponse<List<ActivityFeedItemDTO>>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetRecentActivity([FromQuery] int limit = 10)
        {
            var ownerId = User.GetRequiredUserId();
            var result = await _service.GetRecentActivityAsync(ownerId, limit);
            return Ok(result);
        }
    }
}
