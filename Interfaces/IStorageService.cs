using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace Parkly_Backend.Interfaces
{
    public interface IStorageService
    {
        Task<string> UploadFileAsync(IFormFile file, string bucketName, string fileName);
        Task DeleteFileAsync(string bucketName, string fileName);
    }
}
