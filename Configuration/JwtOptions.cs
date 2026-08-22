namespace Parkly_Backend.Configuration
{
    public class JwtOptions
    {
        public string SecretKey { get; set; } = string.Empty;
        public string Issuer { get; set; } = string.Empty;
        public string Audience { get; set; } = string.Empty;
        public int JwtExpiresInMinutes { get; set; } = 15;
        public int RefreshTokenExpiresInMonths { get; set; } = 6;
    }
}
