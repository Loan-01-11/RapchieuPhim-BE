using System.ComponentModel.DataAnnotations;
using RapchieuPhim.API.Constants;

namespace RapchieuPhim.API.DTOs.Auth
{
    // ── Đăng ký tài khoản thường ─────────────────────────────────────────
    public class RegisterRequest
    {
        [Required(ErrorMessage = ValidationMessages.FullNameRequired)]
        public string FullName { get; set; } = null!;

        [Required(ErrorMessage = ValidationMessages.EmailRequired)]
        [EmailAddress(ErrorMessage = ValidationMessages.EmailInvalid)]
        public string Email { get; set; } = null!;

        [Required(ErrorMessage = ValidationMessages.PasswordRequired)]
        [MinLength(6, ErrorMessage = ValidationMessages.PasswordMinLength)]
        public string Password { get; set; } = null!;

        [Required(ErrorMessage = ValidationMessages.ConfirmPasswordRequired)]
        public string ConfirmPassword { get; set; } = null!;

        /// <summary>
        /// Ngày sinh – FE gửi dưới dạng string "yyyy-MM-dd", ví dụ: "2000-01-15"
        /// Dùng string thay vì DateOnly để tránh lỗi deserialize JSON.
        /// </summary>
        [Required(ErrorMessage = ValidationMessages.DateOfBirthRequired)]
        public string DateOfBirth { get; set; } = null!;

        public string? Gender { get; set; }

        [Required(ErrorMessage = ValidationMessages.PhoneRequired)]
        public string Phone { get; set; } = null!;

        /// <summary>
        /// RoleName dùng để đăng ký theo vai trò.
        /// Cho phép: Admin, Staff, Customer.
        /// Nếu không gửi thì mặc định là Customer.
        /// </summary>
        public string? RoleName { get; set; } = "Customer";
    }
}
