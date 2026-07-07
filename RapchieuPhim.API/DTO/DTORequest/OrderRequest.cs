using System.ComponentModel.DataAnnotations;
using RapchieuPhim.API.Constants;

namespace RapchieuPhim.API.DTO.DTORequest
{
    // ─────────────────────────────────────────────────────────────────────────────
    // DTO TẠO MỚI ĐƠN HÀNG ĐỒ ĂN (Customer / Staff tạo đơn bắp nước)
    // ─────────────────────────────────────────────────────────────────────────────
    public class OrderCreateRequest
    {
        // BookingId là tuỳ chọn: Nếu khách mua đồ ăn trong lúc đặt vé thì có,
        // nếu mua tách riêng tại quầy thì có thể để trống
        public int? BookingId { get; set; }

        // Mã giảm giá cho đơn đồ ăn (tuỳ chọn)
        public int? DiscountId { get; set; }

        [Required(ErrorMessage = OrderMessages.OrderTypeRequired)]
        [MaxLength(50, ErrorMessage = OrderMessages.OrderTypeMaxLength)]
        public string OrderType { get; set; } = null!; // "DineIn" | "Takeaway" | "Online"

        // Danh sách các món trong đơn (bắt buộc phải có ít nhất 1 món)
        [MinLength(1, ErrorMessage = OrderMessages.ItemsRequired)]
        public List<OrderCreateItemRequest> Items { get; set; } = new();
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // DTO CHO 1 DÒNG MÓN TRONG ĐƠN (1 Food hoặc 1 Combo)
    // ─────────────────────────────────────────────────────────────────────────────
    public class OrderCreateItemRequest
    {
        // Chỉ được điền FoodId HOẶC ComboId (không điền cả hai cùng lúc)
        public int? FoodId  { get; set; }
        public int? ComboId { get; set; }

        [Range(1, 100, ErrorMessage = OrderMessages.QuantityInvalid)]
        public int Quantity { get; set; }
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // DTO CẬP NHẬT TRẠNG THÁI ĐƠN HÀNG (Admin / Staff duyệt/hủy đơn)
    // ─────────────────────────────────────────────────────────────────────────────
    public class OrderStatusRequest
    {
        [Required(ErrorMessage = OrderMessages.StatusRequired)]
        public string Status { get; set; } = null!; // "Pending" | "Confirmed" | "Cancelled"
    }
}
