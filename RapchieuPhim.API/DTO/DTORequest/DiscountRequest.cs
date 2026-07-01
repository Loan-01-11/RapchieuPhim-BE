using RapchieuPhim.API.Constants;
using System.ComponentModel.DataAnnotations;

namespace RapchieuPhim.API.DTOs.DTORequest
{
    public class DiscountRequest
    {
        // 1. Mã giảm giá (Bắt buộc, tối đa 50 ký tự)
        [Required(ErrorMessage = DiscountMessages.DiscountCodeRequired)]
        [StringLength(50, ErrorMessage = DiscountMessages.DiscountCodeMaxLength)]
        public string DiscountCode { get; set; } = null!;

        // 2. Mô tả khuyến mãi (Không bắt buộc)
        public string? Description { get; set; }

        // 3. Loại giảm giá (Percent | Fixed - Bắt buộc)
        [Required(ErrorMessage = DiscountMessages.DiscountTypeRequired)]
        public string DiscountType { get; set; } = null!;

        // 4. Giá trị giảm (Bắt buộc, phải > 0)
        [Range(0.01, double.MaxValue, ErrorMessage = DiscountMessages.DiscountValueInvalid)]
        public decimal DiscountValue { get; set; }

        // 5. Giá trị đơn hàng tối thiểu để áp mã (>= 0)
        [Range(0, double.MaxValue, ErrorMessage = DiscountMessages.MinOrderAmountInvalid)]
        public decimal MinOrderAmount { get; set; }

        // 6. Số lần dùng tối đa toàn hệ thống (null = không giới hạn)
        public int? MaxUsageTotal { get; set; }

        // 7. Số lần dùng tối đa mỗi người dùng (phải >= 1)
        [Range(1, int.MaxValue, ErrorMessage = DiscountMessages.MaxUsagePerUserInvalid)]
        public int MaxUsagePerUser { get; set; }

        // 8. Ngày bắt đầu hiệu lực (Bắt buộc)
        [Required(ErrorMessage = DiscountMessages.StartDateRequired)]
        public DateTime StartDate { get; set; }

        // 9. Ngày hết hạn (Không bắt buộc, null = vô thời hạn)
        public DateTime? EndDate { get; set; }

        // 10. Trạng thái kích hoạt
        public bool IsActive { get; set; } = true;
    }
}
