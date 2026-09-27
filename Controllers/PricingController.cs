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
    public class PricingController : ControllerBase
    {
        private readonly IPricingService _service;
        public PricingController(IPricingService service) => _service = service;

        /// <summary>Returns the owner's parking locations for the Pricing Management location tabs.</summary>
        [HttpGet("locations")]
        [ProducesResponseType(typeof(ApiResponse<List<PricingLocationDTO>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetLocations() => Ok(await _service.GetOwnerLocationsAsync(User.GetRequiredUserId()));

        /// <summary>Returns hourly, daily, and weekly rates for all space types at an owner location.</summary>
        [HttpGet("parking/{parkingId:guid}")]
        [ProducesResponseType(typeof(ApiResponse<ParkingPricingDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetParkingPricing(Guid parkingId)
        {
            var result = await _service.GetParkingPricingAsync(User.GetRequiredUserId(), parkingId);
            return result.IsSuccess ? Ok(result) : NotFound(result);
        }

        /// <summary>Creates or updates the rates for one space type at an owner location.</summary>
        [HttpPut("parking/{parkingId:guid}/space-type")]
        [ProducesResponseType(typeof(ApiResponse<SpaceTypePricingDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpsertSpaceTypePricing(Guid parkingId, UpsertSpaceTypePricingDTO dto)
        {
            if (!ModelState.IsValid) return BadRequest(ApiResponse.FromModelState("Invalid request.", ModelState));
            var result = await _service.UpsertSpaceTypePricingAsync(User.GetRequiredUserId(), parkingId, dto);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }
    }
}
