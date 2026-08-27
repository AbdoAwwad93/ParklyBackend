namespace Parkly_Backend.Models.Enums
{
    /// <summary>
    /// Sorting options for nearby parking results.
    /// </summary>
    public enum NearbySortBy
    {
        /// <summary>Sort by distance ascending (closest first).</summary>
        Distance,

        /// <summary>Sort by lowest hourly rate ascending (cheapest first).</summary>
        Price
    }
}
