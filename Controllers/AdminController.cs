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
    public class AdminController : ControllerBase
    {
        private readonly IAdminService _adminService;

        public AdminController(IAdminService adminService)
        {
            _adminService = adminService;
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
        public async Task<IActionResult> Register([FromBody] RegisterDTO user)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse.FromModelState("Invalid request", ModelState));
            }

            var result = await _adminService.RegisterAdmin(user);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        /// <summary>Returns platform-wide aggregated statistics for the admin dashboard stats cards.</summary>
        /// <returns>An <see cref="ApiResponse{T}"/> containing the admin dashboard summary.</returns>
        /// <response code="200">Summary retrieved successfully.</response>
        /// <response code="401">Missing or invalid JWT token.</response>
        /// <response code="403">Authenticated user is not an admin.</response>
        [HttpGet("dashboard/summary")]
        [ProducesResponseType(typeof(ApiResponse<AdminDashboardSummaryDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetSummary()
        {
            var result = await _adminService.GetSummaryAsync();
            return Ok(result);
        }

        /// <summary>Returns platform-wide revenue chart data aggregated by the specified period.</summary>
        /// <param name="period">Aggregation period: "daily" (last 7 days), "weekly" (last 4 weeks), or "monthly" (last 12 months). Defaults to "monthly".</param>
        /// <returns>An <see cref="ApiResponse{T}"/> containing the revenue overview with data points.</returns>
        /// <response code="200">Revenue overview retrieved successfully.</response>
        /// <response code="400">Invalid period value.</response>
        /// <response code="401">Missing or invalid JWT token.</response>
        /// <response code="403">Authenticated user is not an admin.</response>
        [HttpGet("dashboard/revenue")]
        [ProducesResponseType(typeof(ApiResponse<RevenueOverviewDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetRevenueOverview([FromQuery] string period = "monthly")
        {
            var result = await _adminService.GetRevenueOverviewAsync(period);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        /// <summary>Returns booking volume chart data aggregated by the specified period.</summary>
        /// <param name="period">Aggregation period: "daily" (last 7 days) or "weekly" (last 4 weeks). Defaults to "weekly".</param>
        /// <returns>An <see cref="ApiResponse{T}"/> containing the bookings chart data.</returns>
        /// <response code="200">Bookings chart retrieved successfully.</response>
        /// <response code="400">Invalid period value.</response>
        /// <response code="401">Missing or invalid JWT token.</response>
        /// <response code="403">Authenticated user is not an admin.</response>
        [HttpGet("dashboard/bookings-chart")]
        [ProducesResponseType(typeof(ApiResponse<BookingsChartDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetBookingsChart([FromQuery] string period = "weekly")
        {
            var result = await _adminService.GetBookingsChartAsync(period);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        /// <summary>Returns the most recent platform-wide activity events (new accounts, bookings, occupancy alerts, etc.).</summary>
        /// <param name="limit">Maximum number of activity items to return, clamped to 1–50 (default: 10).</param>
        /// <returns>An <see cref="ApiResponse{T}"/> containing the list of activity feed items.</returns>
        /// <response code="200">Recent activity retrieved successfully.</response>
        /// <response code="401">Missing or invalid JWT token.</response>
        /// <response code="403">Authenticated user is not an admin.</response>
        [HttpGet("dashboard/activity")]
        [ProducesResponseType(typeof(ApiResponse<List<AdminActivityFeedItemDTO>>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetRecentActivity([FromQuery] int limit = 10)
        {
            var result = await _adminService.GetRecentActivityAsync(limit);
            return Ok(result);
        }

        /// <summary>Returns a paginated list of parking owners awaiting admin approval.</summary>
        /// <param name="page">1-based page number (default: 1).</param>
        /// <param name="pageSize">Number of items per page, clamped to 1–50 (default: 10).</param>
        /// <returns>An <see cref="ApiResponse{T}"/> containing the list of pending approvals.</returns>
        /// <response code="200">Pending approvals retrieved successfully.</response>
        /// <response code="401">Missing or invalid JWT token.</response>
        /// <response code="403">Authenticated user is not an admin.</response>
        [HttpGet("dashboard/pending-approvals")]
        [ProducesResponseType(typeof(ApiResponse<List<PendingApprovalDTO>>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetPendingApprovals(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            var result = await _adminService.GetPendingApprovalsAsync(page, pageSize);
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
        [HttpPut("dashboard/approvals/{ownerId}")]
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

            var result = await _adminService.ProcessApprovalAsync(ownerId, dto.Action);
            if (!result.IsSuccess && result.Message == "Owner not found.")
            {
                return NotFound(result);
            }
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        /// <summary>Returns aggregated count statistics for the 4 header cards on the Users Management page.</summary>
        /// <returns>An <see cref="ApiResponse{T}"/> containing the driver statistics.</returns>
        /// <response code="200">Statistics retrieved successfully.</response>
        /// <response code="401">Missing or invalid JWT token.</response>
        /// <response code="403">Authenticated user is not an admin.</response>
        [HttpGet("users/stats")]
        [ProducesResponseType(typeof(ApiResponse<AdminDriverStatsDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetUserStats()
        {
            var result = await _adminService.GetDriverStatsAsync();
            return Ok(result);
        }

        /// <summary>Returns a paginated, filterable, and searchable list of drivers for the Users Management table.</summary>
        /// <param name="status">Optional status filter: "all", "active", "pending", or "suspended" (default: "all").</param>
        /// <param name="search">Optional search term to filter by full name, email, or phone number.</param>
        /// <param name="page">1-based page number (default: 1).</param>
        /// <param name="pageSize">Number of items per page, clamped between 1 and 100 (default: 10).</param>
        /// <returns>An <see cref="ApiResponse{T}"/> containing the paginated list of drivers.</returns>
        /// <response code="200">Drivers retrieved successfully.</response>
        /// <response code="401">Missing or invalid JWT token.</response>
        /// <response code="403">Authenticated user is not an admin.</response>
        [HttpGet("users")]
        [ProducesResponseType(typeof(ApiResponse<PagedResult<AdminDriverListItemDTO>>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetDrivers(
            [FromQuery] string? status = "all",
            [FromQuery] string? search = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            var result = await _adminService.GetDriversAsync(status, search, page, pageSize);
            return Ok(result);
        }

        /// <summary>Returns detailed profile information, summary statistics, and recent reservations for a specific driver.</summary>
        /// <param name="userId">The driver's user ID.</param>
        /// <returns>An <see cref="ApiResponse{T}"/> containing the driver details.</returns>
        /// <response code="200">Driver details retrieved successfully.</response>
        /// <response code="401">Missing or invalid JWT token.</response>
        /// <response code="403">Authenticated user is not an admin.</response>
        /// <response code="404">Driver not found.</response>
        [HttpGet("users/{userId}")]
        [ProducesResponseType(typeof(ApiResponse<AdminDriverDetailDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetDriverById(Guid userId)
        {
            var result = await _adminService.GetDriverByIdAsync(userId);
            if (!result.IsSuccess)
            {
                return NotFound(result);
            }
            return Ok(result);
        }

        /// <summary>Updates the status of a driver account (e.g. suspend or activate).</summary>
        /// <param name="userId">The driver's user ID.</param>
        /// <param name="dto">The target status: "Active" or "Suspended".</param>
        /// <returns>An <see cref="ApiResponse"/> indicating the result of the update.</returns>
        /// <response code="200">Status updated successfully.</response>
        /// <response code="400">Invalid status value or request body.</response>
        /// <response code="401">Missing or invalid JWT token.</response>
        /// <response code="403">Authenticated user is not an admin.</response>
        /// <response code="404">Driver not found.</response>
        [HttpPut("users/{userId}/status")]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateUserStatus(Guid userId, [FromBody] UpdateUserStatusDTO dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse.FromModelState("Invalid request", ModelState));
            }

            var result = await _adminService.UpdateDriverStatusAsync(userId, dto.Status);
            if (!result.IsSuccess)
            {
                if (result.Message == "Driver not found.")
                {
                    return NotFound(result);
                }
                return BadRequest(result);
            }

            return Ok(result);
        }

        /// <summary>Returns aggregated statistics for the 4 header cards on the Parking Owners page (Total Owners, Active, Pending, Total Revenue).</summary>
        /// <returns>An <see cref="ApiResponse{T}"/> containing the parking owner metrics.</returns>
        /// <response code="200">Statistics retrieved successfully.</response>
        /// <response code="401">Missing or invalid JWT token.</response>
        /// <response code="403">Authenticated user is not an admin.</response>
        [HttpGet("owners/stats")]
        [ProducesResponseType(typeof(ApiResponse<AdminOwnerStatsDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetOwnerStats()
        {
            var result = await _adminService.GetOwnerStatsAsync();
            return Ok(result);
        }

        /// <summary>Returns a paginated, filterable, and searchable list of parking owners for the Parking Owners management table.</summary>
        /// <param name="status">Optional status filter: "all", "active", "pending", or "suspended" (default: "all").</param>
        /// <param name="search">Optional search term to filter by business name, owner name, email, or phone number.</param>
        /// <param name="page">1-based page number (default: 1).</param>
        /// <param name="pageSize">Number of items per page, clamped between 1 and 100 (default: 10).</param>
        /// <returns>An <see cref="ApiResponse{T}"/> containing the paginated list of parking owners.</returns>
        /// <response code="200">Parking owners retrieved successfully.</response>
        /// <response code="401">Missing or invalid JWT token.</response>
        /// <response code="403">Authenticated user is not an admin.</response>
        [HttpGet("owners")]
        [ProducesResponseType(typeof(ApiResponse<PagedResult<AdminOwnerListItemDTO>>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetOwners(
            [FromQuery] string? status = "all",
            [FromQuery] string? search = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            var result = await _adminService.GetOwnersAsync(status, search, page, pageSize);
            return Ok(result);
        }

        /// <summary>Returns detailed profile information, business verification, and parking locations breakdown for a specific parking owner.</summary>
        /// <param name="ownerId">The parking owner's user ID.</param>
        /// <returns>An <see cref="ApiResponse{T}"/> containing the owner details.</returns>
        /// <response code="200">Owner details retrieved successfully.</response>
        /// <response code="401">Missing or invalid JWT token.</response>
        /// <response code="403">Authenticated user is not an admin.</response>
        /// <response code="404">Parking owner not found.</response>
        [HttpGet("owners/{ownerId}")]
        [ProducesResponseType(typeof(ApiResponse<AdminOwnerDetailDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetOwnerById(Guid ownerId)
        {
            var result = await _adminService.GetOwnerByIdAsync(ownerId);
            if (!result.IsSuccess)
            {
                return NotFound(result);
            }
            return Ok(result);
        }

        /// <summary>Updates the verification status of a parking owner ("Active", "Pending", or "Suspended").</summary>
        /// <param name="ownerId">The parking owner's user ID.</param>
        /// <param name="dto">The target status: "Active", "Pending", or "Suspended".</param>
        /// <returns>An <see cref="ApiResponse"/> indicating the result of the update.</returns>
        /// <response code="200">Status updated successfully.</response>
        /// <response code="400">Invalid status value or request body.</response>
        /// <response code="401">Missing or invalid JWT token.</response>
        /// <response code="403">Authenticated user is not an admin.</response>
        /// <response code="404">Parking owner not found.</response>
        [HttpPut("owners/{ownerId}/status")]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateOwnerStatus(Guid ownerId, [FromBody] UpdateOwnerStatusDTO dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse.FromModelState("Invalid request", ModelState));
            }

            var result = await _adminService.UpdateOwnerStatusAsync(ownerId, dto.Status);
            if (!result.IsSuccess)
            {
                if (result.Message == "Parking owner not found.")
                {
                    return NotFound(result);
                }
                return BadRequest(result);
            }

            return Ok(result);
        }
    }
}