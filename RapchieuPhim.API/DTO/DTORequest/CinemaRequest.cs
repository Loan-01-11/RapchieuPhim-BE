using RapchieuPhim.API.Constants;
using System.ComponentModel.DataAnnotations;

namespace RapchieuPhim.API.DTOs.DTORequest
{
    public class CinemaRequest
    {
        // 1. Xác thực Tên rạp (Bắt buộc nhập + Không quá 150 ký tự như DB)
        [Required(ErrorMessage = ValidationMessages.CinemaNameRequired)]
        [StringLength(150, ErrorMessage = ValidationMessages.CinemaNameMaxLength)]
        public string CinemaName { get; set; } = null!;

        // 2. Xác thực Địa chỉ (Vì dưới DB của bạn là NOT NULL nên ta bắt buộc nhập + Không quá 255 ký tự)
        [Required(ErrorMessage = ValidationMessages.UnauthorizedCinemaUpdate)]
        [StringLength(255, ErrorMessage = ValidationMessages.CinemaAddressMaxLength)]
        public string? Address { get; set; }

        // 3. Xác thực Mã khu vực (Bắt buộc phải chọn vùng có ID từ 1 trở lên)
        [Range(1, int.MaxValue, ErrorMessage = ValidationMessages.CinemaAreaRequired)]
        public int AreaId { get; set; }

        // 4. Trạng thái hoạt động (Trường cũ giữ nguyên)
        public bool IsActive { get; set; }

        // 5. Số điện thoại 
        [StringLength(20, ErrorMessage = ValidationMessages.CinemaPhoneMaxLength)]
        public string? Phone { get; set; }
    }
}