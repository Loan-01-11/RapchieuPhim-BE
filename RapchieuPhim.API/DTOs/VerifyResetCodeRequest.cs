
using System.ComponentModel.DataAnnotations;
using RapchieuPhim.API.Constants;

namespace RapchieuPhim.API.DTOs.Auth
{
    // Verify OTP trước khi cho phép hiện form đổi mật khẩu
    public class VerifyResetCodeRequest
    {
        [Required(ErrorMessage = ValidationMessages.EmailRequired)]
        [EmailAddress(ErrorMessage = ValidationMessages.EmailInvalid)]
        public string Email { get; set; } = null!;

        [Required(ErrorMessage = ValidationMessages.OtpRequired)]
        [StringLength(6, MinimumLength = 6, ErrorMessage = ValidationMessages.OtpLength)]
        public string OtpCode { get; set; } = null!;
    }
}