using Google.Apis.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.IdentityModel.Tokens;
using RapchieuPhim.API.DTOs.Auth;
using RapchieuPhim.API.Models;
using RapchieuPhim.API.Services;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace RapchieuPhim.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly CinemaManagementContext _context;
        private readonly IConfiguration _config;
        private readonly IEmailService _emailService;
        private readonly IMemoryCache _cache;

        public AuthController(CinemaManagementContext context, IConfiguration config,
            IEmailService emailService, IMemoryCache cache)
        {
            _context      = context;
            _config       = config;
            _emailService = emailService;
            _cache        = cache;
        }

        // ── 1. ĐĂNG NHẬP BẰNG TÀI KHOẢN ────────────────────────────────────
        // POST: api/Auth/Login
        // Body: { "email": "...", "password": "..." }
        [HttpPost("Login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { Message = GetFirstError(), Errors = ModelState });

            var email = request.Email.Trim();

            // Tìm user theo email (collation CI_AS nên không cần ToLower)
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email);

            if (user == null || !user.IsActive)
                return Unauthorized(new { Message = "Email hoặc mật khẩu không chính xác." });

            // User đăng ký qua Google sẽ không đăng nhập được bằng password
            if (string.IsNullOrEmpty(user.PasswordHash) ||
                !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                return Unauthorized(new { Message = "Email hoặc mật khẩu không chính xác." });

            return Ok(BuildAuthResponse(user));
        }

        // ── 2. ĐĂNG KÝ BẰNG TÀI KHOẢN ──────────────────────────────────────
        // POST: api/Auth/Register
        // Body: { "fullName", "email", "password", "confirmPassword", "dateOfBirth": "yyyy-MM-dd", "gender", "phone" }
        [HttpPost("Register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { Message = GetFirstError(), Errors = ModelState });

            // Kiểm tra mật khẩu khớp
            if (request.Password != request.ConfirmPassword)
                return BadRequest(new { Message = "Mật khẩu xác nhận không khớp." });

            // Parse DateOfBirth từ string "yyyy-MM-dd"
            if (!DateOnly.TryParseExact(request.DateOfBirth, "yyyy-MM-dd",
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out var dob))
                return BadRequest(new { Message = "Ngày sinh không đúng định dạng. Vui lòng dùng định dạng yyyy-MM-dd (ví dụ: 2000-01-15)." });

            var email = request.Email.Trim();
            var phone = request.Phone.Trim();

            // Kiểm tra email đã tồn tại (so sánh case-insensitive nhờ DB collation)
            if (await _context.Users.AnyAsync(u => u.Email == email))
                return Conflict(new { Message = "Email này đã được đăng ký. Vui lòng sử dụng email khác hoặc đăng nhập." });

            var user = new User
            {
                FullName     = request.FullName.Trim(),
                Email        = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                Phone        = phone,
                Gender       = string.IsNullOrWhiteSpace(request.Gender) ? null : request.Gender.Trim(),
                DateOfBirth  = dob,
                Role         = "Customer",
                RewardPoint  = 0,
                IsActive     = true,
                CreatedAt    = DateTime.Now
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Đăng ký thành công!", UserId = user.UserId });
        }

        // ── 3. ĐĂNG NHẬP BẰNG GOOGLE ────────────────────────────────────────
        // POST: api/Auth/LoginGoogle
        // Body: { "idToken": "..." }
        // - User đã tồn tại  → 200 + JWT
        // - User chưa tồn tại → 202 + GoogleProfileResponse (FE hiện form thêm thông tin)
        [HttpPost("LoginGoogle")]
        public async Task<IActionResult> LoginGoogle([FromBody] GoogleAuthRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { Message = GetFirstError(), Errors = ModelState });

            var payload = await VerifyGoogleToken(request.IdToken);
            if (payload == null)
                return Unauthorized(new { Message = "Google token không hợp lệ hoặc đã hết hạn." });

            var email = payload.Email.Trim();

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email);

            if (user == null)
            {
                // Chưa có tài khoản → trả thông tin Google để FE điền form
                return StatusCode(202, new GoogleProfileResponse
                {
                    NeedsAdditionalInfo = true,
                    Email               = payload.Email,
                    FullName            = payload.Name ?? "",
                    AvatarUrl           = payload.Picture,
                    Message             = "Tài khoản chưa đăng ký. Vui lòng bổ sung thông tin để hoàn tất."
                });
            }

            if (!user.IsActive)
                return Unauthorized(new { Message = "Tài khoản của bạn đã bị khoá. Vui lòng liên hệ hỗ trợ." });

            // Cập nhật avatar nếu thay đổi
            if (!string.IsNullOrEmpty(payload.Picture) && user.AvatarUrl != payload.Picture)
            {
                user.AvatarUrl = payload.Picture;
                await _context.SaveChangesAsync();
            }

            return Ok(BuildAuthResponse(user));
        }

        // ── 4. HOÀN TẤT ĐĂNG KÝ VỚI GOOGLE ────────────────────────────────
        // POST: api/Auth/RegisterWithGoogle
        // Body: { "idToken", "fullName", "phone", "dateOfBirth": "yyyy-MM-dd", "gender" }
        // Gọi sau khi LoginGoogle trả 202 và FE thu thập đủ thông tin
        [HttpPost("RegisterWithGoogle")]
        public async Task<IActionResult> RegisterWithGoogle([FromBody] GoogleRegisterCompleteRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { Message = GetFirstError(), Errors = ModelState });

            // Xác thực lại Google token
            var payload = await VerifyGoogleToken(request.IdToken);
            if (payload == null)
                return Unauthorized(new { Message = "Google token không hợp lệ hoặc đã hết hạn." });

            // Parse DateOfBirth
            if (!DateOnly.TryParseExact(request.DateOfBirth, "yyyy-MM-dd",
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out var dob))
                return BadRequest(new { Message = "Ngày sinh không đúng định dạng. Vui lòng dùng định dạng yyyy-MM-dd." });

            var email = payload.Email.Trim();
            var phone = request.Phone.Trim();

            // Nếu đã có tài khoản → đăng nhập luôn
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email);

            if (existingUser != null)
            {
                if (!existingUser.IsActive)
                    return Unauthorized(new { Message = "Tài khoản đã bị khoá. Vui lòng liên hệ hỗ trợ." });

                return Ok(BuildAuthResponse(existingUser));
            }

            var newUser = new User
            {
                FullName     = request.FullName.Trim(),
                Email        = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString()), // random – Google user
                AvatarUrl    = payload.Picture,
                Phone        = phone,
                Gender       = string.IsNullOrWhiteSpace(request.Gender) ? null : request.Gender.Trim(),
                DateOfBirth  = dob,
                Role         = "Customer",
                RewardPoint  = 0,
                IsActive     = true,
                CreatedAt    = DateTime.Now
            };

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            // Reload
            var createdUser = await _context.Users
                .FirstAsync(u => u.UserId == newUser.UserId);

            return Ok(BuildAuthResponse(createdUser));
        }

        // ── 5. GỬi OTP QUÊN MẬT KHẨU ───────────────────────────────────
        // POST: api/Auth/ForgotPassword
        // Body: { "email": "..." }
        [HttpPost("ForgotPassword")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { Message = GetFirstError(), Errors = ModelState });

            var email = request.Email.Trim();

            // Tìm user theo email
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email);

            // Bảo mật: luôn trả OK dù email không tồn tại (tránh lộ thông tin)
            if (user == null || !user.IsActive)
                return Ok(new { Message = "Nếu email tồn tại, mã xác nhận đã được gửi. Vui lòng kiểm tra hộp thư." });

            // Sinh OTP 6 chữ số ngỪu nhiên
            var otp = Random.Shared.Next(100000, 999999).ToString();

            // Lưu OTP vào cache với key = "otp_{email}", hết hạn sau 5 phút
            var cacheKey = $"otp_{email.ToLower()}";
            _cache.Set(cacheKey, otp, TimeSpan.FromMinutes(5));

            // Gửi email chứa OTP
            await _emailService.SendOtpAsync(email, user.FullName, otp);

            return Ok(new { Message = "Mã xác nhận đã được gửi đến email của bạn. Vui lòng kiểm tra hộp thư (cả mục Spam)." });
        }

        // ── 6. ĐẶT LẠI MẬT KHẨU ─────────────────────────────────────────
        // POST: api/Auth/ResetPassword
        // Body: { "email", "otpCode", "newPassword", "confirmPassword" }
        [HttpPost("ResetPassword")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { Message = GetFirstError(), Errors = ModelState });

            if (request.NewPassword != request.ConfirmPassword)
                return BadRequest(new { Message = "Mật khẩu xác nhận không khớp." });

            var email    = request.Email.Trim();
            var cacheKey = $"otp_{email.ToLower()}";

            // Kiểm tra OTP trong cache
            if (!_cache.TryGetValue(cacheKey, out string? savedOtp) || savedOtp != request.OtpCode)
                return BadRequest(new { Message = "Mã xác nhận không đúng hoặc đã hết hạn. Vui lòng yêu cầu mã mới." });

            // Tìm user
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email && u.IsActive);

            if (user == null)
                return NotFound(new { Message = "Không tìm thấy tài khoản." });

            // Cập nhật mật khẩu
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            await _context.SaveChangesAsync();

            // Xóa OTP khỏi cache sau khi dùng xong
            _cache.Remove(cacheKey);

            return Ok(new { Message = "Đổi mật khẩu thành công! Vui lòng đăng nhập lại." });
        }

        // ── Private helpers ──────────────────────────────────────────────────

        /// <summary>Xác thực Google ID token, trả null nếu lỗi.</summary>
        private async Task<GoogleJsonWebSignature.Payload?> VerifyGoogleToken(string idToken)
        {
            try
            {
                var settings = new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = new[] { _config["Google:ClientId"] }
                };
                return await GoogleJsonWebSignature.ValidateAsync(idToken, settings);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>Tạo JWT token cho user.</summary>
        private string GenerateJwtToken(User user)
        {
            var key     = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
            var creds   = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expires = DateTime.UtcNow.AddMinutes(
                double.Parse(_config["Jwt:ExpiresInMinutes"] ?? "60"));

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub,   user.UserId.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(ClaimTypes.Name,               user.FullName),
                new Claim(ClaimTypes.Role,               user.Role),
                new Claim(JwtRegisteredClaimNames.Jti,   Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(
                issuer:             _config["Jwt:Issuer"],
                audience:           _config["Jwt:Audience"],
                claims:             claims,
                expires:            expires,
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        /// <summary>Build AuthResponse từ User entity.</summary>
        private AuthResponse BuildAuthResponse(User user)
        {
            return new AuthResponse
            {
                Token     = GenerateJwtToken(user),
                ExpiresAt = DateTime.UtcNow.AddMinutes(
                    double.Parse(_config["Jwt:ExpiresInMinutes"] ?? "60")),
                User = new UserInfo
                {
                    UserId          = user.UserId,
                    FullName        = user.FullName,
                    Email           = user.Email,
                    Phone           = user.Phone,
                    AvatarUrl       = user.AvatarUrl,
                    MembershipLevel = user.MembershipLevel,
                    RewardPoint     = user.RewardPoint,
                    Role            = user.Role
                }
            };
        }

        /// <summary>Lấy lỗi đầu tiên từ ModelState để trả về message thân thiện.</summary>
        private string GetFirstError()
        {
            foreach (var state in ModelState.Values)
                foreach (var error in state.Errors)
                    if (!string.IsNullOrEmpty(error.ErrorMessage))
                        return error.ErrorMessage;
            return "Dữ liệu không hợp lệ.";
        }
    }
}
