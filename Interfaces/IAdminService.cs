using Parkly_Backend.Models.DTOs;
using Parkly_Backend.Models.Response;

namespace Parkly_Backend.Interfaces
{
    public interface IAdminService
    {
        Task<ApiResponse> RegisterAdmin(RegisterDTO dto);
    }
}
