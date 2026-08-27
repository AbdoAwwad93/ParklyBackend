using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;

using Parkly_Backend.Data.Repositories;
using Parkly_Backend.Interfaces;
using Parkly_Backend.Models;
using Parkly_Backend.Models.DTOs;
using Parkly_Backend.Models.Enums;
using Parkly_Backend.Models.Response;

namespace Parkly_Backend.Services
{
    public class SavedPlacesService : ISavedPlacesService
    {
        public const int MaxSavedPlacesPerUser = 20;

        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public SavedPlacesService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<ApiResponse<List<SavedPlaceResponseDTO>>> GetUserSavedPlacesAsync(Guid userId)
        {
            var places = await _unitOfWork.SavedPlaces.GetUserSavedPlacesOrderedAsync(userId);

            var response = _mapper.Map<List<SavedPlaceResponseDTO>>(places);
            return ApiResponse<List<SavedPlaceResponseDTO>>.Success("Saved places retrieved successfully.", response);
        }

        public async Task<ApiResponse<SavedPlaceResponseDTO>> GetByIdAsync(Guid userId, Guid placeId)
        {
            var place = await _unitOfWork.SavedPlaces.FirstOrDefaultAsync(p => p.PlaceId == placeId && p.UserId == userId);

            if (place == null)
            {
                return ApiResponse<SavedPlaceResponseDTO>.Failure("Saved place not found.");
            }

            var response = _mapper.Map<SavedPlaceResponseDTO>(place);
            return ApiResponse<SavedPlaceResponseDTO>.Success("Saved place retrieved successfully.", response);
        }

        public async Task<ApiResponse<SavedPlaceResponseDTO>> CreateAsync(Guid userId, CreateSavedPlaceDTO dto)
        {
            if (dto.Latitude < -90 || dto.Latitude > 90)
            {
                return ApiResponse<SavedPlaceResponseDTO>.Failure("Latitude must be between -90 and 90.");
            }
            if (dto.Longitude < -180 || dto.Longitude > 180)
            {
                return ApiResponse<SavedPlaceResponseDTO>.Failure("Longitude must be between -180 and 180.");
            }

            var userPlaces = await _unitOfWork.SavedPlaces.GetUserSavedPlacesOrderedAsync(userId);
            var count = userPlaces.Count;

            if (count >= MaxSavedPlacesPerUser)
            {
                return ApiResponse<SavedPlaceResponseDTO>.Failure($"You cannot save more than {MaxSavedPlacesPerUser} favorite places.");
            }

            // Enforce unique Home and Work locations per user
            if (dto.PlaceType == PlaceType.Home || dto.PlaceType == PlaceType.Work)
            {
                var existing = await _unitOfWork.SavedPlaces.AnyAsync(p =>
                    p.UserId == userId && p.PlaceType == dto.PlaceType);

                if (existing)
                {
                    return ApiResponse<SavedPlaceResponseDTO>.Failure($"You already have a saved {dto.PlaceType} place. Please update the existing one.");
                }
            }

            var place = _mapper.Map<SavedPlace>(dto);
            place.UserId = userId;
            place.CreatedAt = DateTime.UtcNow;

            await _unitOfWork.SavedPlaces.AddAsync(place);
            await _unitOfWork.SaveChangesAsync();

            var response = _mapper.Map<SavedPlaceResponseDTO>(place);
            return ApiResponse<SavedPlaceResponseDTO>.Success("Saved place created successfully.", response);
        }

        public async Task<ApiResponse<SavedPlaceResponseDTO>> UpdateAsync(Guid userId, Guid placeId, UpdateSavedPlaceDTO dto)
        {
            if (dto.Latitude < -90 || dto.Latitude > 90)
            {
                return ApiResponse<SavedPlaceResponseDTO>.Failure("Latitude must be between -90 and 90.");
            }
            if (dto.Longitude < -180 || dto.Longitude > 180)
            {
                return ApiResponse<SavedPlaceResponseDTO>.Failure("Longitude must be between -180 and 180.");
            }

            var place = await _unitOfWork.SavedPlaces.FirstOrDefaultAsync(p => p.PlaceId == placeId && p.UserId == userId);

            if (place == null)
            {
                return ApiResponse<SavedPlaceResponseDTO>.Failure("Saved place not found.");
            }

            // If updating to Home or Work, ensure another entry does not already occupy that type
            if ((dto.PlaceType == PlaceType.Home || dto.PlaceType == PlaceType.Work) && place.PlaceType != dto.PlaceType)
            {
                var conflict = await _unitOfWork.SavedPlaces.AnyAsync(p =>
                    p.UserId == userId && p.PlaceId != placeId && p.PlaceType == dto.PlaceType);

                if (conflict)
                {
                    return ApiResponse<SavedPlaceResponseDTO>.Failure($"You already have a saved {dto.PlaceType} place.");
                }
            }

            _mapper.Map(dto, place);
            await _unitOfWork.SaveChangesAsync();

            var response = _mapper.Map<SavedPlaceResponseDTO>(place);
            return ApiResponse<SavedPlaceResponseDTO>.Success("Saved place updated successfully.", response);
        }

        public async Task<ApiResponse> DeleteAsync(Guid userId, Guid placeId)
        {
            var place = await _unitOfWork.SavedPlaces.FirstOrDefaultAsync(p => p.PlaceId == placeId && p.UserId == userId);

            if (place == null)
            {
                return ApiResponse.Failure("Saved place not found.");
            }

            _unitOfWork.SavedPlaces.Delete(place);
            await _unitOfWork.SaveChangesAsync();

            return ApiResponse.Success("Saved place deleted successfully.");
        }
    }
}
