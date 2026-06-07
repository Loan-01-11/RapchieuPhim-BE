using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace RapchieuPhim.API.DTOs.Auth
{
    // ── Đăng nhập bằng tài khoản Email + Password ─────────────────────────
    public class LoginRequest
    {
        [Required(ErrorMessage = "Email không được để trống.")]
        [EmailAddress(ErrorMessage = "Email không đúng định dạng.")]
        public string Email { get; set; } = null!;

        [Required(ErrorMessage = "Mật khẩu không được để trống.")]
        public string Password { get; set; } = null!;
    }

    // ── Đăng ký tài khoản thường ─────────────────────────────────────────
    public class RegisterRequest
    {
        [Required(ErrorMessage = "Họ tên không được để trống.")]
        public string FullName { get; set; } = null!;

        [Required(ErrorMessage = "Email không được để trống.")]
        [EmailAddress(ErrorMessage = "Email không đúng định dạng.")]
        public string Email { get; set; } = null!;

        [Required(ErrorMessage = "Mật khẩu không được để trống.")]
        [MinLength(6, ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự.")]
        public string Password { get; set; } = null!;

        [Required(ErrorMessage = "Xác nhận mật khẩu không được để trống.")]
        public string ConfirmPassword { get; set; } = null!;

        /// <summary>
        /// Ngày sinh – FE gửi dưới dạng string "yyyy-MM-dd", ví dụ: "2000-01-15"
        /// Dùng string thay vì DateOnly để tránh lỗi deserialize JSON.
        /// </summary>
        [Required(ErrorMessage = "Ngày sinh không được để trống.")]
        public string DateOfBirth { get; set; } = null!;

        public string? Gender { get; set; }

        [Required(ErrorMessage = "Số điện thoại không được để trống.")]
        public string Phone { get; set; } = null!;

        /// <summary>
        /// RoleName dùng để đăng ký theo vai trò.
        /// Cho phép: Admin, Staff, Customer.
        /// Nếu không gửi thì mặc định là Customer.
        /// </summary>
        public string? RoleName { get; set; } = "Customer";
    }

    // ── Đăng nhập / kiểm tra Google ──────────────────────────────────────
    public class GoogleAuthRequest
    {
        [Required(ErrorMessage = "Google ID token không được để trống.")]
        public string IdToken { get; set; } = null!;
    }

    // ── Hoàn tất đăng ký sau khi xác thực Google ─────────────────────────
    public class GoogleRegisterCompleteRequest
    {
        [Required(ErrorMessage = "Google ID token không được để trống.")]
        public string IdToken { get; set; } = null!;

        [Required(ErrorMessage = "Họ tên không được để trống.")]
        public string FullName { get; set; } = null!;

        [Required(ErrorMessage = "Số điện thoại không được để trống.")]
        public string Phone { get; set; } = null!;

        /// <summary>
        /// Ngày sinh – FE gửi string "yyyy-MM-dd".
        /// </summary>
        [Required(ErrorMessage = "Ngày sinh không được để trống.")]
        public string DateOfBirth { get; set; } = null!;

        public string? Gender { get; set; }
    }

    // ── Response khi Google token hợp lệ nhưng chưa có tài khoản ─────────
    public class GoogleProfileResponse
    {
        public bool NeedsAdditionalInfo { get; set; } = true;
        public string Email { get; set; } = null!;
        public string FullName { get; set; } = null!;
        public string? AvatarUrl { get; set; }
        public string Message { get; set; } = "Vui lòng bổ sung thông tin để hoàn tất đăng ký.";
    }

    // ── Quên mật khẩu: Bước 1 – Gửi OTP ─────────────────────────────────
    public class ForgotPasswordRequest
    {
        [Required(ErrorMessage = "Email không được để trống.")]
        [EmailAddress(ErrorMessage = "Email không đúng định dạng.")]
        public string Email { get; set; } = null!;
    }

    // ── Quên mật khẩu: Bước 2 – Đặt lại mật khẩu ────────────────────────
    public class ResetPasswordRequest
    {
        [Required(ErrorMessage = "Email không được để trống.")]
        [EmailAddress(ErrorMessage = "Email không đúng định dạng.")]
        public string Email { get; set; } = null!;

        [Required(ErrorMessage = "Mã xác nhận không được để trống.")]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "Mã xác nhận gồm 6 chữ số.")]
        public string OtpCode { get; set; } = null!;

        [Required(ErrorMessage = "Mật khẩu mới không được để trống.")]
        [MinLength(6, ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự.")]
        public string NewPassword { get; set; } = null!;

        [Required(ErrorMessage = "Xác nhận mật khẩu không được để trống.")]
        public string ConfirmPassword { get; set; } = null!;
    }

    // ── Response sau khi đăng nhập / đăng ký thành công ─────────────────
    public class AuthResponse
    {
        public string Token { get; set; } = null!;
        public string TokenType { get; set; } = "Bearer";
        public DateTime ExpiresAt { get; set; }
        public UserInfo User { get; set; } = null!;
    }

    public class UserInfo
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string? Phone { get; set; }
        public string? AvatarUrl { get; set; }
        public string? MembershipLevel { get; set; }
        public int RewardPoint { get; set; }
        public string Role { get; set; } = null!;
    }
}
