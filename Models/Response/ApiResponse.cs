using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Text.Json.Serialization;

namespace Parkly_Backend.Models.Response
{
    public class ApiResponse
    {
        public bool IsSuccess { get; set; }
        public string? Message { get; set; }

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
