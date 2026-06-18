using System.ComponentModel.DataAnnotations;
using RapchieuPhim.API.Constants;

namespace RapchieuPhim.API.DTO.DTORequest
{
    // ── Quên mật khẩu: Bước 1 – Gửi OTP ────────────────────────────────
    public class ForgotPasswordRequest
    {
        [Required(ErrorMessage = ValidationMessages.EmailRequired)]
        [EmailAddress(ErrorMessage = ValidationMessages.EmailInvalid)]
        public string Email { get; set; } = null!;
    }
}
