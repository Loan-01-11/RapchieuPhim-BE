using System.ComponentModel.DataAnnotations;
using RapchieuPhim.API.Constants;

namespace RapchieuPhim.API.DTO.DTORequest
{
    // ── Đổi mật khẩu khi đang đăng nhập ─────────────────────────────────────
    public class ChangePasswordRequest
    {
        [Required(ErrorMessage = ValidationMessages.PasswordRequired)]
        public string CurrentPassword { get; set; } = null!;

        [Required(ErrorMessage = ValidationMessages.NewPasswordRequired)]
        [MinLength(6, ErrorMessage = ValidationMessages.PasswordMinLength)]
        public string NewPassword { get; set; } = null!;

        [Required(ErrorMessage = ValidationMessages.ConfirmPasswordRequired)]
        public string ConfirmPassword { get; set; } = null!;
    }
}
