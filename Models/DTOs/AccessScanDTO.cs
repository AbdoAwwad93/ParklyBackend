using System.ComponentModel.DataAnnotations;
using Parkly_Backend.Models.Enums;

namespace Parkly_Backend.Models.DTOs
{
    public class AccessScanDTO
    {
        [Required]
        public string QrToken { get; set; } = string.Empty;    
        [Required]
        public ScanType ScanType { get; set; }
    }
}
