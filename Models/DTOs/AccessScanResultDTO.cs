using Parkly_Backend.Models.Enums;

namespace Parkly_Backend.Models.DTOs
{
    /// <summary>Polymorphic container returned by gate scan operations.</summary>
    public class AccessScanResultDTO
    {
        /// <summary>The type of scan executed (Entry or Exit).</summary>
        public ScanType ScanType { get; set; }

        /// <summary>Informative message describing the outcome.</summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>Populated if ScanType is Entry.</summary>
        public CheckInResponseDTO? CheckIn { get; set; }

        /// <summary>Populated if ScanType is Exit.</summary>
        public CheckOutResponseDTO? CheckOut { get; set; }
    }
}
