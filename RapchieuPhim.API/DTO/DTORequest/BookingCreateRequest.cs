using RapchieuPhim.API.Constants;
using System.ComponentModel.DataAnnotations;

namespace RapchieuPhim.API.DTO.DTORequest
{
    public class BookingCreateRequest
    {
        [Required]
        public int ShowTimeId { get; set; }

        // Danh sách ghế muốn đặt — tối thiểu 1, tối đa 10 ghế mỗi lần
        [Required]
        [MinLength(1, ErrorMessage = ValidationMessages.BookingMessages.SeatIdsMinLength)]
        [MaxLength(10, ErrorMessage = ValidationMessages.BookingMessages.SeatIdsMaxLength)]
        public List<int> SeatIds { get; set; } = new();

        [StringLength(50)]
        public string? DiscountCode { get; set; }

        [Required]
        public string BookingType { get; set; } = ValidationMessages.Online; // Online | Counter

        // Dùng khi nhân viên bán vé hộ tại quầy, nếu Online thì tự động bóc từ Token
        public int? TargetUserId { get; set; }

        // Danh sách đồ ăn / combo muốn đặt kèm (không bắt buộc)
        public List<OrderItemRequest>? OrderItems { get; set; }

        public bool IsStudent { get; set; }
        public int? StudentCount { get; set; }
    }

    // ─── Một dòng đồ ăn/combo trong đơn hàng ─────────────────────────────────────
    public class OrderItemRequest
    {
        // Chỉ được điền 1 trong 2: FoodId HOẶC ComboId
        public int? FoodId { get; set; }
        public int? ComboId { get; set; }

        [Range(1, 50, ErrorMessage = ValidationMessages.BookingMessages.OrderItemQuantityInvalid)]
        public int Quantity { get; set; } = 1;
    }
}