using System.ComponentModel.DataAnnotations;
using RapchieuPhim.API.Constants;

namespace RapchieuPhim.API.DTOs.DTORequest
{
    public class TicketPricingRequest
    {
        public int? RoomId { get; set; }

        [StringLength(50, ErrorMessage = ValidationMessages.PricingRoomTypeMaxLength)]
        public string? RoomType { get; set; } // 2D | 3D | IMAX | 4DX ; NULL = tất cả

        [StringLength(30, ErrorMessage = ValidationMessages.PricingSeatTypeMaxLength)]
        public string? SeatType { get; set; } // Standard | VIP | Couple ; NULL = tất cả

        [StringLength(20, ErrorMessage = ValidationMessages.PricingDayTypeMaxLength)]
        public string? DayType { get; set; }   // Weekday | Weekend | Holiday ; NULL = tất cả

        [Required]
        [Range(typeof(decimal), "0.01", "999999999", ErrorMessage = ValidationMessages.PricingPriceInvalid)]
        public decimal Price { get; set; }

        [Required]
        public DateOnly EffectFrom { get; set; }

        public DateOnly? EffectTo { get; set; } // NULL = vô thời hạn

        public bool IsActive { get; set; }
    }
}
