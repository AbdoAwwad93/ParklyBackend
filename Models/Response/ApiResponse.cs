using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Text.Json.Serialization;

namespace Parkly_Backend.Models.Response
{
    /// <summary>Standard envelope wrapping all API responses.</summary>
    public class ApiResponse
    {
        /// <summary>Indicates whether the request succeeded.</summary>
        public bool IsSuccess { get; set; }
        /// <summary>A human-readable message describing the result.</summary>
        public string? Message { get; set; }

        /// <summary>List of validation or error messages. Only present on failure.</summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<string>? Errors { get; set; }

        public static ApiResponse Success(string message)
        {
            return new ApiResponse
            {
                IsSuccess = true,
                Message = message,
            };
        }

        public static ApiResponse Failure(string message, List<string>? errors = null)
        {
            return new ApiResponse
            {
                IsSuccess = false,
                Message = message,
                Errors = errors,
            };
        }

        public static ApiResponse FromModelState(string message, ModelStateDictionary modelState)
        {
            var errors = modelState.Values
                .SelectMany(v => v.Errors.Select(e => e.ErrorMessage))
                .ToList();

            return Failure(message, errors);
        }
    }
}
