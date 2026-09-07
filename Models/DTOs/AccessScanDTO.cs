using System.ComponentModel.DataAnnotations;
using Parkly_Backend.Models.Enums;

namespace Parkly_Backend.Models.DTOs
{
    public class AccessScanDTO
    {
        /// <summary>The 6-digit QR code or access PIN.</summary>
        [Required]
        public string QrToken { get; set; } = string.Empty;    
        [Required]
        public ScanType ScanType { get; set; }
    }
}
