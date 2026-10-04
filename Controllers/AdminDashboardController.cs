using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Parkly_Backend.Interfaces;
using Parkly_Backend.Models.DTOs;
using Parkly_Backend.Models.Response;

namespace Parkly_Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    [Produces("application/json")]
    public class AdminDashboardController : ControllerBase
    {
        private readonly IAdminDashboardService _service;

        public AdminDashboardController(IAdminDashboardService service)
        {
            _service = service;
        }

        /// <summary>Returns platform-wide aggregated statistics for the admin dashboard stats cards.</summary>
        /// <returns>An <see cref="ApiResponse{T}"/> containing the admin dashboard summary.</returns>
        /// <response code="200">Summary retrieved successfully.</response>
        /// <response code="401">Missing or invalid JWT token.</response>
        /// <response code="403">Authenticated user is not an admin.</response>
        [HttpGet("summary")]
        [ProducesResponseType(typeof(ApiResponse<AdminDashboardSummaryDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetSummary()
        {
            var result = await _service.GetSummaryAsync();
            return Ok(result);
        }

        /// <summary>Returns platform-wide revenue chart data aggregated by the specified period.</summary>
        /// <param name="period">Aggregation period: "daily" (last 7 days), "weekly" (last 4 weeks), or "monthly" (last 12 months). Defaults to "monthly".</param>
        /// <returns>An <see cref="ApiResponse{T}"/> containing the revenue overview with data points.</returns>
        /// <response code="200">Revenue overview retrieved successfully.</response>
        /// <response code="400">Invalid period value.</response>
        /// <response code="401">Missing or invalid JWT token.</response>
        /// <response code="403">Authenticated user is not an admin.</response>
        [HttpGet("revenue")]
        [ProducesResponseType(typeof(ApiResponse<RevenueOverviewDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetRevenueOverview([FromQuery] string period = "monthly")
        {
            var result = await _service.GetRevenueOverviewAsync(period);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        /// <summary>Returns booking volume chart data aggregated by the specified period.</summary>
        /// <param name="period">Aggregation period: "daily" (last 7 days) or "weekly" (last 4 weeks). Defaults to "weekly".</param>
        /// <returns>An <see cref="ApiResponse{T}"/> containing the bookings chart data.</returns>
        /// <response code="200">Bookings chart retrieved successfully.</response>
        /// <response code="400">Invalid period value.</response>
        /// <response code="401">Missing or invalid JWT token.</response>
        /// <response code="403">Authenticated user is not an admin.</response>
        [HttpGet("bookings-chart")]
        [ProducesResponseType(typeof(ApiResponse<BookingsChartDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetBookingsChart([FromQuery] string period = "weekly")
        {
            var result = await _service.GetBookingsChartAsync(period);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        /// <summary>Returns the most recent platform-wide activity events (new accounts, bookings, occupancy alerts, etc.).</summary>
        /// <param name="limit">Maximum number of activity items to return, clamped to 1–50 (default: 10).</param>
        /// <returns>An <see cref="ApiResponse{T}"/> containing the list of activity feed items.</returns>
        /// <response code="200">Recent activity retrieved successfully.</response>
        /// <response code="401">Missing or invalid JWT token.</response>
        /// <response code="403">Authenticated user is not an admin.</response>
        [HttpGet("activity")]
        [ProducesResponseType(typeof(ApiResponse<List<AdminActivityFeedItemDTO>>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetRecentActivity([FromQuery] int limit = 10)
        {
            var result = await _service.GetRecentActivityAsync(limit);
            return Ok(result);
        }

        /// <summary>Returns a paginated list of parking owners awaiting admin approval.</summary>
        /// <param name="page">1-based page number (default: 1).</param>
        /// <param name="pageSize">Number of items per page, clamped to 1–50 (default: 10).</param>
        /// <returns>An <see cref="ApiResponse{T}"/> containing the list of pending approvals.</returns>
        /// <response code="200">Pending approvals retrieved successfully.</response>
        /// <response code="401">Missing or invalid JWT token.</response>
        /// <response code="403">Authenticated user is not an admin.</response>
        [HttpGet("pending-approvals")]
        [ProducesResponseType(typeof(ApiResponse<List<PendingApprovalDTO>>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetPendingApprovals(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            var result = await _service.GetPendingApprovalsAsync(page, pageSize);
            return Ok(result);
        }

        /// <summary>Approves or rejects a pending parking owner application.</summary>
        /// <param name="ownerId">The owner's user ID.</param>
        /// <param name="dto">The approval action details.</param>
        /// <returns>An <see cref="ApiResponse"/> indicating the result.</returns>
        /// <response code="200">Approval action processed successfully.</response>
        /// <response code="400">Invalid action or owner not in pending state.</response>
        /// <response code="401">Missing or invalid JWT token.</response>
        /// <response code="403">Authenticated user is not an admin.</response>
        /// <response code="404">Owner not found.</response>
        [HttpPut("approvals/{ownerId}")]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ProcessApproval(Guid ownerId, [FromBody] ApprovalActionDTO dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse.FromModelState("Invalid request", ModelState));
            }

            var result = await _service.ProcessApprovalAsync(ownerId, dto.Action);
            if (!result.IsSuccess && result.Message == "Owner not found.")
            {
                return NotFound(result);
            }
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }
    }
}
