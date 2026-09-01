using Microsoft.AspNetCore.Http;
using System.IO;
using System.Threading.Tasks;
using Parkly_Backend.Interfaces;
using Supabase;

namespace Parkly_Backend.Services
{
    public class SupabaseStorageService : IStorageService
    {
        private readonly Client _supabaseClient;

        public SupabaseStorageService(Client supabaseClient)
        {
            _supabaseClient = supabaseClient;
        }

        public async Task<string> UploadFileAsync(IFormFile file, string bucketName, string fileName)
        {
            using var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream);
            var bytes = memoryStream.ToArray();

            var bucket = _supabaseClient.Storage.From(bucketName);
            await bucket.Upload(bytes, fileName, new Supabase.Storage.FileOptions
            {
                ContentType = file.ContentType,
                Upsert = true
            });

            return bucket.GetPublicUrl(fileName);
        }

        public async Task DeleteFileAsync(string bucketName, string fileName)
        {
            var bucket = _supabaseClient.Storage.From(bucketName);
            await bucket.Remove(new List<string> { fileName });
        }
    }
}
