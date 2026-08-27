using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Parkly_Backend.Common.Extensions;
using Parkly_Backend.Interfaces;
using Parkly_Backend.Models.DTOs;
using Parkly_Backend.Models.Response;

namespace Parkly_Backend.Controllers
{
    /// <summary>
    /// Manages saved/favorite locations for the authenticated user (Home, Work).
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    [Produces("application/json")]
    [Tags("Saved Places")]
    public class SavedPlacesController : ControllerBase
    {
        private readonly ISavedPlacesService _service;

        public SavedPlacesController(ISavedPlacesService service)
        {
            _service = service;
        }

        /// <summary>Returns all saved favorite places for the authenticated user.</summary>
        /// <returns>An <see cref="ApiResponse{T}"/> containing the list of saved places.</returns>
        /// <response code="200">Saved places retrieved successfully.</response>
        /// <response code="401">Missing or invalid JWT token.</response>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<List<SavedPlaceResponseDTO>>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetAll()
        {
            var userId = User.GetRequiredUserId();
            var result = await _service.GetUserSavedPlacesAsync(userId);
            return Ok(result);
        }

        /// <summary>Returns a specific saved place by ID belonging to the authenticated user.</summary>
        /// <param name="id">The unique identifier of the saved place.</param>
        /// <returns>An <see cref="ApiResponse{T}"/> containing the saved place details.</returns>
        /// <response code="200">Saved place retrieved successfully.</response>
        /// <response code="401">Missing or invalid JWT token.</response>
        /// <response code="404">Saved place not found.</response>
        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(ApiResponse<SavedPlaceResponseDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(Guid id)
        {
            var userId = User.GetRequiredUserId();
            var result = await _service.GetByIdAsync(userId, id);
            return result.IsSuccess ? Ok(result) : NotFound(result);
        }

        /// <summary>Creates a new saved favorite place for the authenticated user.</summary>
        /// <param name="dto">The saved place details (title, address, coordinates, place type).</param>
        /// <returns>An <see cref="ApiResponse{T}"/> containing the created saved place.</returns>
        /// <response code="200">Saved place created successfully.</response>
        /// <response code="400">Validation failed or maximum limit reached.</response>
        /// <response code="401">Missing or invalid JWT token.</response>
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<SavedPlaceResponseDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Create([FromBody] CreateSavedPlaceDTO dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse.FromModelState("Invalid request", ModelState));
            }

            var userId = User.GetRequiredUserId();
            var result = await _service.CreateAsync(userId, dto);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        /// <summary>Updates an existing saved favorite place belonging to the authenticated user.</summary>
        /// <param name="id">The unique identifier of the saved place to update.</param>
        /// <param name="dto">The updated saved place details.</param>
        /// <returns>An <see cref="ApiResponse{T}"/> containing the updated saved place.</returns>
        /// <response code="200">Saved place updated successfully.</response>
        /// <response code="400">Validation failed or conflict with existing type.</response>
        /// <response code="401">Missing or invalid JWT token.</response>
        /// <response code="404">Saved place not found.</response>
        [HttpPut("{id:guid}")]
        [ProducesResponseType(typeof(ApiResponse<SavedPlaceResponseDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateSavedPlaceDTO dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse.FromModelState("Invalid request", ModelState));
            }

            var userId = User.GetRequiredUserId();
            var result = await _service.UpdateAsync(userId, id, dto);
            if (!result.IsSuccess && result.Message == "Saved place not found.")
            {
                return NotFound(result);
            }
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        /// <summary>Deletes an existing saved favorite place belonging to the authenticated user.</summary>
        /// <param name="id">The unique identifier of the saved place to delete.</param>
        /// <returns>An <see cref="ApiResponse"/> indicating success.</returns>
        /// <response code="200">Saved place deleted successfully.</response>
        /// <response code="401">Missing or invalid JWT token.</response>
        /// <response code="404">Saved place not found.</response>
        [HttpDelete("{id:guid}")]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(Guid id)
        {
            var userId = User.GetRequiredUserId();
            var result = await _service.DeleteAsync(userId, id);
            return result.IsSuccess ? Ok(result) : NotFound(result);
        }
    }
}
