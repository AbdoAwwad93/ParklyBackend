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
    [Route("api/[controller]")]
    [ApiController]
    [Produces("application/json")]
    [Tags("Ratings & Reviews")]
    public class ReviewsController : ControllerBase
    {
        private readonly IReviewsService _reviewsService;

        public ReviewsController(IReviewsService reviewsService)
        {
            _reviewsService = reviewsService;
        }

        /// <summary>Submits a review for a completed reservation.</summary>
        /// <param name="dto">The review details containing Rating (1-5) and optional comment.</param>
        /// <returns>An <see cref="ApiResponse{T}"/> containing the created review.</returns>
        /// <response code="200">Review submitted successfully.</response>
        /// <response code="400">Validation failed, reservation not completed, or already reviewed.</response>
        /// <response code="401">Missing or invalid JWT token.</response>
        [HttpPost]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse<ReviewResponseDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Create([FromBody] CreateReviewDTO dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse.FromModelState("Invalid request", ModelState));
            }

            var userId = User.GetRequiredUserId();
            var result = await _reviewsService.CreateAsync(userId, dto);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        /// <summary>Updates an existing review.</summary>
        /// <param name="id">The unique identifier of the review.</param>
        /// <param name="dto">The updated rating and comment.</param>
        /// <returns>An <see cref="ApiResponse{T}"/> containing the updated review.</returns>
        /// <response code="200">Review updated successfully.</response>
        /// <response code="400">Validation failed.</response>
        /// <response code="401">Missing or invalid JWT token.</response>
        /// <response code="404">Review not found.</response>
        [HttpPut("{id:guid}")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse<ReviewResponseDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateReviewDTO dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse.FromModelState("Invalid request", ModelState));
            }

            var userId = User.GetRequiredUserId();
            var result = await _reviewsService.UpdateAsync(userId, id, dto);
            
            if (!result.IsSuccess && result.Message == "Review not found.")
            {
                return NotFound(result);
            }
            
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        /// <summary>Gets all reviews for a specific parking facility.</summary>
        /// <param name="parkingId">The unique identifier of the parking facility.</param>
        /// <param name="page">The page number for pagination.</param>
        /// <param name="pageSize">The number of reviews per page.</param>
        /// <returns>An <see cref="ApiResponse{T}"/> containing the parking reviews summary and list.</returns>
        /// <response code="200">Reviews retrieved successfully.</response>
        /// <response code="404">Parking facility not found.</response>
        [HttpGet("parking/{parkingId:guid}")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse<ParkingReviewsSummaryDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetParkingReviews(Guid parkingId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var result = await _reviewsService.GetParkingReviewsAsync(parkingId, page, pageSize);
            return result.IsSuccess ? Ok(result) : NotFound(result);
        }

        /// <summary>Gets all reviews submitted by the authenticated user.</summary>
        /// <returns>An <see cref="ApiResponse{T}"/> containing the list of user reviews.</returns>
        /// <response code="200">Reviews retrieved successfully.</response>
        /// <response code="401">Missing or invalid JWT token.</response>
        [HttpGet("my-reviews")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse<List<ReviewResponseDTO>>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetUserReviews()
        {
            var userId = User.GetRequiredUserId();
            var result = await _reviewsService.GetUserReviewsAsync(userId);
            return Ok(result);
        }
    }
}
