using System.ComponentModel.DataAnnotations;
using RapchieuPhim.API.Constants;

namespace RapchieuPhim.API.DTOs.Auth
{
    // ── Quên mật khẩu: Bước 2 – Đặt lại mật khẩu ────────────────────────
    public class ResetPasswordRequest
    {
        [Required(ErrorMessage = ValidationMessages.EmailRequired)]
        [EmailAddress(ErrorMessage = ValidationMessages.EmailInvalid)]
        public string Email { get; set; } = null!;

        [Required(ErrorMessage = ValidationMessages.OtpRequired)]
        [StringLength(6, MinimumLength = 6, ErrorMessage = ValidationMessages.OtpLength)]
        public string OtpCode { get; set; } = null!;

        [Required(ErrorMessage = ValidationMessages.NewPasswordRequired)]
        [MinLength(6, ErrorMessage = ValidationMessages.PasswordMinLength)]
        public string NewPassword { get; set; } = null!;

        [Required(ErrorMessage = ValidationMessages.ConfirmPasswordRequired)]
        public string ConfirmPassword { get; set; } = null!;
    }
}