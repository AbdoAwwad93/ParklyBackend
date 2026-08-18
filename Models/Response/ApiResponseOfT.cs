namespace Parkly_Backend.Models.Response
{
    /// <summary>Standard wrapper carrying typed payload data on success.</summary>
    public class ApiResponse<T> : ApiResponse
    {
        /// <summary>The payload returned on success.</summary>
        public T? Data { get; set; }

        public static ApiResponse<T> Success(string message, T data)
        {
            return new ApiResponse<T>
            {
                IsSuccess = true,
                Message = message,
                Data = data,
            };
        }

        public new static ApiResponse<T> Failure(string message, List<string>? errors = null)
        {
            return new ApiResponse<T>
            {
                IsSuccess = false,
                Message = message,
                Errors = errors,
            };
        }
    }
}
