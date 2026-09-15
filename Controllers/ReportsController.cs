using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Parkly_Backend.Common.Extensions;
using Parkly_Backend.Interfaces;
using Parkly_Backend.Models.DTOs;
using Parkly_Backend.Models.Response;

namespace Parkly_Backend.Controllers
{
    [Route("api/reports")]
    [ApiController]
    [Authorize(Roles = "ParkingOwner")]
    [Produces("application/json")]
    public class ReportsController : ControllerBase
    {
        private readonly IReportsService _service;

        public ReportsController(IReportsService service)
        {
            _service = service;
        }

        [HttpGet("revenue")]
        [ProducesResponseType(typeof(ApiResponse<RevenueReportsDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetRevenueReport(
            [FromQuery] string? period = "monthly",
            [FromQuery] DateTime? from = null,
            [FromQuery] DateTime? to = null)
        {
            var ownerId = User.GetRequiredUserId();
            var result = await _service.GetRevenueReportAsync(ownerId, period, from, to);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        [HttpGet("revenue/export")]
        [Produces("text/csv")]
        [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> ExportRevenueReport(
            [FromQuery] string? period = "monthly",
            [FromQuery] DateTime? from = null,
            [FromQuery] DateTime? to = null)
        {
            var ownerId = User.GetRequiredUserId();
            var result = await _service.ExportRevenueByLocationCsvAsync(ownerId, period, from, to);
            if (!result.IsSuccess || result.Data == null)
            {
                return BadRequest(result);
            }

            var bytes = Encoding.UTF8.GetBytes(result.Data);
            return File(bytes, "text/csv", $"parkly-revenue-{DateTime.UtcNow:yyyyMMddHHmmss}.csv");
        }
    }
}
