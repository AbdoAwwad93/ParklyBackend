using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Parkly_Backend.Interfaces;
using Parkly_Backend.Models.DTOs;
using Parkly_Backend.Models.Response;
using System.Security.Claims;
using Parkly_Backend.Common.Extensions;

namespace Parkly_Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Produces("application/json")]
    public class ParkingsController : ControllerBase
    {
        private readonly IParkingsService _service;
        private readonly IAvailabilityService _availabilityService;
        private readonly IMapper _mapper;

        public ParkingsController(IParkingsService service, IAvailabilityService availabilityService, IMapper mapper)
        {
            _service = service;
            _availabilityService = availabilityService;
            _mapper = mapper;
        }

        /// <summary>Returns all parking facilities.</summary>
        /// <returns>An <see cref="ApiResponse{T}"/> containing the list of parking facilities.</returns>
        /// <response code="200">Parkings retrieved successfully.</response>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<List<ParkingResponseDTO>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll()
        {
            var result = await _service.GetAllAsync();
            return Ok(result);
        }

        /// <summary>Returns the parking facilities owned by the authenticated parking owner.</summary>
        /// <returns>An <see cref="ApiResponse{T}"/> containing the owner's parking facilities.</returns>
        /// <response code="200">Owned parkings retrieved successfully.</response>
        /// <response code="401">Missing or invalid JWT token.</response>
        /// <response code="403">The authenticated user is not a parking owner.</response>
        [HttpGet("mine")]
        [Authorize(Roles = "ParkingOwner,Admin")]
        [ProducesResponseType(typeof(ApiResponse<List<ParkingResponseDTO>>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetMine()
        {
            var ownerId = User.GetRequiredUserId();
            var result = await _service.GetOwnedAsync(ownerId);
            return Ok(result);
        }

        /// <summary>Returns a single parking facility by id with optional distance calculation relative to user coordinates.</summary>
        /// <param name="id">The id of the parking facility.</param>
        /// <param name="latitude">Optional user latitude coordinate for distance calculation (-90 to 90).</param>
        /// <param name="longitude">Optional user longitude coordinate for distance calculation (-180 to 180).</param>
        /// <returns>An <see cref="ApiResponse{T}"/> containing the parking facility.</returns>
        /// <response code="200">Parking retrieved successfully.</response>
        /// <response code="400">Invalid coordinates.</response>
        /// <response code="404">Parking not found.</response>
        [HttpGet("{id:guid}")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse<ParkingResponseDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(Guid id, [FromQuery] decimal? latitude = null, [FromQuery] decimal? longitude = null)
        {
            if (latitude.HasValue && (latitude < -90 || latitude > 90))
            {
                return BadRequest(ApiResponse.Failure("Latitude must be between -90 and 90."));
            }
            if (longitude.HasValue && (longitude < -180 || longitude > 180))
            {
                return BadRequest(ApiResponse.Failure("Longitude must be between -180 and 180."));
            }
            if ((latitude.HasValue && !longitude.HasValue) || (!latitude.HasValue && longitude.HasValue))
            {
                return BadRequest(ApiResponse.Failure("Both latitude and longitude must be provided together."));
            }

            var result = await _service.GetByIdAsync(id, latitude, longitude);
            return result.IsSuccess ? Ok(result) : NotFound(result);
        }

        /// <summary>Returns aggregated details for a single parking location owned by the authenticated parking owner (Location Details modal).</summary>
        /// <param name="id">The id of the parking facility.</param>
        /// <returns>An <see cref="ApiResponse{T}"/> containing total/available/occupied/reserved counts, base price, rating, monthly revenue and status.</returns>
        /// <response code="200">Location details retrieved successfully.</response>
        /// <response code="400">Parking not found or no permission.</response>
        /// <response code="401">Missing or invalid JWT token.</response>
        /// <response code="403">The authenticated user is not a parking owner.</response>
        [HttpGet("{id:guid}/details")]
        [Authorize(Roles = "ParkingOwner,Admin")]
        [ProducesResponseType(typeof(ApiResponse<LocationDetailsDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetDetails(Guid id)
        {
            var ownerId = User.GetRequiredUserId();
            var result = await _service.GetDetailsAsync(ownerId, id);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        /// <summary>Returns the parking spaces available in a parking facility for a given time window.</summary>
        /// <param name="parkingId">The id of the parking facility.</param>
        /// <param name="arrival">The arrival time.</param>
        /// <param name="departure">The departure time.</param>
        /// <returns>An <see cref="ApiResponse{T}"/> containing the list of available spaces.</returns>
        /// <response code="200">Available spaces retrieved successfully.</response>
        /// <response code="400">Departure time must be after arrival time.</response>
        [HttpGet("{parkingId:guid}/available")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse<List<ParkingSpaceResponseDTO>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetAvailableSpaces(Guid parkingId, [FromQuery] DateTime arrival, [FromQuery] DateTime departure)
        {
            if (arrival >= departure)
            {
                return BadRequest(ApiResponse.Failure("Departure time must be after arrival time."));
            }

            var spaces = await _availabilityService.GetAvailableSpacesAsync(parkingId, arrival, departure);
            var response = _mapper.Map<List<ParkingSpaceResponseDTO>>(spaces);
            return Ok(ApiResponse<List<ParkingSpaceResponseDTO>>.Success("Available spaces retrieved successfully.", response));
        }

        /// <summary>Searches for parking facilities with optional filtering and availability counts.</summary>
        /// <param name="query">The search filter parameters.</param>
        /// <returns>An <see cref="ApiResponse{T}"/> containing the matching parkings with availability details.</returns>
        /// <response code="200">Search completed successfully.</response>
        /// <response code="400">Invalid search parameters.</response>
        [HttpGet("search")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse<SearchParkingPageDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Search([FromQuery] SearchParkingQuery query)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse.FromModelState("Invalid request parameters.", ModelState));
            }

            var result = await _service.SearchAsync(query);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        /// <summary>Finds nearby parking facilities relative to user coordinates, sorted by proximity.</summary>
        /// <param name="query">The nearby location and filter parameters.</param>
        /// <returns>An <see cref="ApiResponse{T}"/> containing nearby parking facilities ordered by distance or price.</returns>
        /// <response code="200">Nearby parkings retrieved successfully.</response>
        /// <response code="400">Invalid parameters or coordinates.</response>
        [HttpGet("nearby")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse<NearbyParkingPageDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetNearby([FromQuery] NearbyParkingQuery query)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse.FromModelState("Invalid request parameters.", ModelState));
            }

            var result = await _service.GetNearbyAsync(query);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        /// <summary>Recommends personalized parking facilities for the user.</summary>
        /// <param name="query">The recommendation parameters.</param>
        /// <returns>An <see cref="ApiResponse{T}"/> containing recommended parking facilities.</returns>
        /// <response code="200">Recommendations retrieved successfully.</response>
        /// <response code="400">Invalid parameters.</response>
        /// <response code="401">User is not authenticated.</response>
        [HttpGet("recommend")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse<RecommendParkingPageDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Recommend([FromQuery] RecommendParkingQuery query)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse.FromModelState("Invalid request parameters.", ModelState));
            }

            var userId = User.GetRequiredUserId();
            var result = await _service.GetRecommendationsAsync(userId, query);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        /// <summary>Creates a new parking facility for the authenticated parking owner.</summary>
        /// <param name="dto">The parking details.</param>
        /// <returns>An <see cref="ApiResponse{T}"/> containing the created parking facility.</returns>
        /// <response code="200">Parking created successfully.</response>
        /// <response code="400">Validation failed.</response>
        /// <response code="401">Missing or invalid JWT token.</response>
        /// <response code="403">The authenticated user is not a parking owner.</response>
        [HttpPost]
        [Authorize(Roles = "ParkingOwner,Admin")]
        [ProducesResponseType(typeof(ApiResponse<ParkingResponseDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> Create(CreateParkingDTO dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse.FromModelState("Invalid request", ModelState));
            }

            var ownerId = User.GetRequiredUserId();
            var result = await _service.CreateAsync(ownerId, dto);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        /// <summary>Updates an existing parking facility belonging to the authenticated parking owner.</summary>
        /// <param name="id">The id of the parking facility to update.</param>
        /// <param name="dto">The new parking details.</param>
        /// <returns>An <see cref="ApiResponse{T}"/> containing the updated parking facility.</returns>
        /// <response code="200">Parking updated successfully.</response>
        /// <response code="400">Validation failed or the parking cannot be updated.</response>
        /// <response code="401">Missing or invalid JWT token.</response>
        /// <response code="403">The authenticated user is not a parking owner.</response>
        /// <response code="404">Parking not found.</response>
        [HttpPut("{id:guid}")]
        [Authorize(Roles = "ParkingOwner,Admin")]
        [ProducesResponseType(typeof(ApiResponse<ParkingResponseDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Update(Guid id, UpdateParkingDTO dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse.FromModelState("Invalid request", ModelState));
            }

            var ownerId = User.GetRequiredUserId();
            var result = await _service.UpdateAsync(ownerId, id, dto);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        /// <summary>Deletes an existing parking facility belonging to the authenticated parking owner.</summary>
        /// <param name="id">The id of the parking facility to delete.</param>
        /// <returns>An <see cref="ApiResponse"/> indicating the deletion result.</returns>
        /// <response code="200">Parking deleted successfully.</response>
        /// <response code="400">The parking could not be deleted.</response>
        /// <response code="401">Missing or invalid JWT token.</response>
        /// <response code="403">The authenticated user is not a parking owner.</response>
        /// <response code="404">Parking not found.</response>
        [HttpDelete("{id:guid}")]
        [Authorize(Roles = "ParkingOwner,Admin")]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(Guid id)
        {
            var ownerId = User.GetRequiredUserId();
            var result = await _service.DeleteAsync(ownerId, id);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }
    }
}