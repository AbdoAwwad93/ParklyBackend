namespace Parkly_Backend.Models.Enums
{
    /// <summary>
    /// Represents the type/purpose of a parking space.
    /// Distinct from <see cref="VehicleSize"/> which describes physical size.
    /// </summary>
    public enum SpaceType
    {
        Standard,
        EVCharging,
        Accessible,
        Compact
    }
}
