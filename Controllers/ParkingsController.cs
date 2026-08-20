using AutoMapper;
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

        /// <summary>Returns a single parking facility by id.</summary>
        /// <param name="id">The id of the parking facility.</param>
        /// <returns>An <see cref="ApiResponse{T}"/> containing the parking facility.</returns>
        /// <response code="200">Parking retrieved successfully.</response>
        /// <response code="404">Parking not found.</response>
        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(ApiResponse<ParkingResponseDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _service.GetByIdAsync(id);
            return result.IsSuccess ? Ok(result) : NotFound(result);
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
        [HttpGet("search")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse<List<SearchParkingDTO>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Search([FromQuery] SearchParkingQuery query)
        {
            var result = await _service.SearchAsync(query);
            return Ok(result);
        }

        /// <summary>Creates a new parking facility for the authenticated parking owner.</summary>
        /// <param name="dto">The parking details.</param>
        /// <returns>An <see cref="ApiResponse{T}"/> containing the created parking facility.</returns>
        /// <response code="200">Parking created successfully.</response>
        /// <response code="400">Validation failed.</response>
        /// <response code="401">Missing or invalid JWT token.</response>
        /// <response code="403">The authenticated user is not a parking owner.</response>
        [HttpPost]
        [Authorize(Roles = "ParkingOwner")]
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

            var ownerId = GetUserId();
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
        [Authorize(Roles = "ParkingOwner")]
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

            var ownerId = GetUserId();
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
        [Authorize(Roles = "ParkingOwner")]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(Guid id)
        {
            var ownerId = GetUserId();
            var result = await _service.DeleteAsync(ownerId, id);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        private Guid GetUserId()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.Parse(userId);
        }
    }
}