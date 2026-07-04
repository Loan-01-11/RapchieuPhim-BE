using RapchieuPhim.API.Constants;
using System.ComponentModel.DataAnnotations;

namespace RapchieuPhim.API.DTOs.DTORequest
{
    /// <summary>
    /// Request tạo thanh toán — khách gửi lên sau khi đặt vé thành công.
    /// </summary>
    public class PaymentRequest
    {
        // BookingId đầu tiên trong danh sách (hoặc bất kỳ BookingId nào trong đơn)
        // Hệ thống sẽ tự tổng hợp tất cả booking + order cùng session
        [Required(ErrorMessage = PaymentMessages.BookingIdRequired)]
        public int BookingId { get; set; }

        // OrderId nếu có đặt đồ ăn kèm (nullable)
        public int? OrderId { get; set; }

        [Required(ErrorMessage = PaymentMessages.PaymentMethodRequired)]
        [StringLength(50, ErrorMessage = PaymentMessages.PaymentMethodMaxLength)]
        public string PaymentMethod { get; set; } = null!; // Cash | BankTransfer | Momo 

        // Mã giao dịch từ cổng thanh toán (nếu thanh toán online)
        [StringLength(100)]
        public string? TransactionId { get; set; }

        [StringLength(500)]
        public string? Notes { get; set; }
    }

    /// <summary>
    /// Request cập nhật trạng thái thanh toán (dùng cho Admin/Staff xác nhận).
    /// </summary>
    public class PaymentStatusRequest
    {
        [Required(ErrorMessage = PaymentMessages.StatusRequired)]
        public string Status { get; set; } = null!; // Pending | Success | Failed | Refunded

        public string? Notes { get; set; }
    }
}
