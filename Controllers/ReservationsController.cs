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
    public class ReservationsController : ControllerBase
    {
        private readonly IReservationsService _service;

        public ReservationsController(IReservationsService service)
        {
            _service = service;
        }

        [HttpPost]
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

        [HttpPut("{id:guid}")]
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

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Cancel(Guid id)
        {
            var userId = GetUserId();
            var result = await _service.CancelAsync(userId, id);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        private Guid GetUserId()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.Parse(userId);
        }
    }
}