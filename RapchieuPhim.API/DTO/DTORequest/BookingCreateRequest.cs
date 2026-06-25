using System.ComponentModel.DataAnnotations;

namespace RapchieuPhim.API.DTO.DTORequest
{
    public class BookingCreateRequest
    {
        [Required]
        public int ShowTimeId { get; set; }

        [Required]
        public int SeatId { get; set; }

        [StringLength(50)]
        public string? DiscountCode { get; set; }

        [Required]
        public string BookingType { get; set; } = "Online"; // Online | Counter

        // Dùng khi nhân viên bán vé hộ tại quầy, nếu Online thì tự động bóc từ Token
        public int? TargetUserId { get; set; }
    }
}