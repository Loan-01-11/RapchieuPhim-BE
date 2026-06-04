using System.ComponentModel.DataAnnotations;

namespace RapchieuPhim.API.DTOs.Auth
{
    public class GoogleLoginRequest
    {
        [Required]
        public string IdToken { get; set; } = null!;  // ID token received from Google SDK on the FE
    }

    public class RegisterRequest
    {
        [Required]
        public string FullName { get; set; } = null!;           // Họ tên *

        [Required]
        [EmailAddress]
        public string Email { get; set; } = null!;              // Email *

        [Required]
        [MinLength(6)]
        public string Password { get; set; } = null!;           // Mật khẩu *

        [Required]
        public string ConfirmPassword { get; set; } = null!;    // Xác nhận lại mật khẩu *

        [Required]
        public DateOnly DateOfBirth { get; set; }               // Ngày sinh *

        public string? Gender { get; set; }                     // Giới tính (optional)

        [Required]
        public string Phone { get; set; } = null!;              // Số điện thoại *

        public string? OtpCode { get; set; }                    // Mã xác thực (optional for now)
    }

    public class LoginRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = null!;              // Email

        [Required]
        public string Password { get; set; } = null!;           // Mật khẩu
    }

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
        public int RoleId { get; set; }
        public string RoleName { get; set; } = null!;
    }
}
