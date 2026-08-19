namespace Parkly_Backend.Interfaces
{
    public interface IPricingService
    {
        Task<decimal> CalculateTotalPriceAsync(Guid spaceId, DateTime arrival, DateTime departure);
    }
}