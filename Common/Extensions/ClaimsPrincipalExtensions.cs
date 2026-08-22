using System.Security.Claims;

namespace Parkly_Backend.Common.Extensions
{
    public static class ClaimsPrincipalExtensions
    {
        public static Guid? GetUserId(this ClaimsPrincipal? user)
        {
            if (user == null) return null;

            var claimValue = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(claimValue) || !Guid.TryParse(claimValue, out var userId))
            {
                return null;
            }

            return userId;
        }

        public static Guid GetRequiredUserId(this ClaimsPrincipal? user)
        {
            var userId = user.GetUserId();
            if (!userId.HasValue)
            {
                throw new UnauthorizedAccessException("User is not authenticated or user identifier claim is missing or invalid.");
            }

            return userId.Value;
        }

        public static string? GetUserEmail(this ClaimsPrincipal? user)
        {
            return user?.FindFirstValue(ClaimTypes.Email);
        }

        public static string? GetUserRole(this ClaimsPrincipal? user)
        {
            return user?.FindFirstValue(ClaimTypes.Role);
        }
    }
}
