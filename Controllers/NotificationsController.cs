using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Parkly_Backend.Common.Extensions;
using Parkly_Backend.Interfaces;
using Parkly_Backend.Models.DTOs;
using Parkly_Backend.Models.Enums;
using Parkly_Backend.Models.Response;

namespace Parkly_Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "ParkingOwner")]
    [Produces("application/json")]
    public class NotificationsController : ControllerBase
    {
        private readonly INotificationService _service;
        public NotificationsController(INotificationService service) => _service = service;

        /// <summary>Returns paginated notifications for the authenticated owner.</summary>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<NotificationPageDTO>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll([FromQuery] NotificationType? type, [FromQuery] bool? isRead, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
            => Ok(await _service.GetForOwnerAsync(User.GetRequiredUserId(), type, isRead, page, pageSize));

        /// <summary>Returns total and unread notification counts for the notification bell and category cards.</summary>
        [HttpGet("summary")]
        [ProducesResponseType(typeof(ApiResponse<NotificationSummaryDTO>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetSummary()
            => Ok(await _service.GetSummaryAsync(User.GetRequiredUserId()));

        [HttpPatch("{id:guid}/read")]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> MarkRead(Guid id)
        {
            var result = await _service.MarkReadAsync(User.GetRequiredUserId(), id);
            return result.IsSuccess ? Ok(result) : NotFound(result);
        }

        [HttpPatch("read-all")]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> MarkAllRead()
            => Ok(await _service.MarkAllReadAsync(User.GetRequiredUserId()));
    }
}
