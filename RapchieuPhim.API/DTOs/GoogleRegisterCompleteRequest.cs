using System.ComponentModel.DataAnnotations;
using RapchieuPhim.API.Constants;

namespace RapchieuPhim.API.DTOs.Auth
{
    // ── Hoàn tất đăng ký sau khi xác thực Google ─────────────────────────
    public class GoogleRegisterCompleteRequest
    {
        [Required(ErrorMessage = ValidationMessages.GoogleIdTokenRequired)]
        public string IdToken { get; set; } = null!;

        [Required(ErrorMessage = ValidationMessages.FullNameRequired)]
        public string FullName { get; set; } = null!;

        [Required(ErrorMessage = ValidationMessages.PhoneRequired)]
        public string Phone { get; set; } = null!;

        /// <summary>
        /// Ngày sinh – FE gửi string "yyyy-MM-dd".
        /// </summary>
        [Required(ErrorMessage = ValidationMessages.DateOfBirthRequired)]
        public string DateOfBirth { get; set; } = null!;

        public string? Gender { get; set; }
    }
}