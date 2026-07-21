using Google.Apis.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.IdentityModel.Tokens;
using RapchieuPhim.API.Constants;
using RapchieuPhim.API.DTO.DTORequest;
using RapchieuPhim.API.DTO.DTOResponse;
using RapchieuPhim.API.Models;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace RapchieuPhim.API.Services
{
    public interface IAuthService
    {
        Task<(bool IsSuccess, string Message, int StatusCode, object? Data)> LoginAsync(LoginRequest request);
        Task<(bool IsSuccess, string Message, int StatusCode, object? Data)> RegisterAsync(RegisterRequest request);
        Task<(bool IsSuccess, string Message, int StatusCode, object? Data)> CreateInternalAccountAsync(RegisterRequest request);
        Task<(bool IsSuccess, string Message, int StatusCode, object? Data)> LoginGoogleAsync(GoogleAuthRequest request);
        Task<(bool IsSuccess, string Message, int StatusCode, object? Data)> RegisterWithGoogleAsync(GoogleRegisterCompleteRequest request);
        Task<(bool IsSuccess, string Message, int StatusCode, object? Data)> ForgotPasswordAsync(ForgotPasswordRequest request);
        Task<(bool IsSuccess, string Message, int StatusCode, object? Data)> ResetPasswordAsync(ResetPasswordRequest request);
        Task<(bool IsSuccess, string Message, int StatusCode, object? Data)> VerifyResetCodeAsync(VerifyResetCodeRequest request);
        Task<(bool IsSuccess, string Message, int StatusCode, object? Data)> ChangePasswordAsync(int userId, ChangePasswordRequest request);
    }

    public class AuthService : IAuthService
    {
        private readonly CinemaManagementContext _context;
        private readonly IConfiguration _config;
        private readonly IEmailService _emailService;
        private readonly IMemoryCache _cache;
        private readonly IHostEnvironment _env;

        public AuthService(CinemaManagementContext context, IConfiguration config,
            IEmailService emailService, IMemoryCache cache, IHostEnvironment env)
        {
            _context      = context;
            _config       = config;
            _emailService = emailService;
            _cache        = cache;
            _env          = env;
        }

        // ── 1. ĐĂNG NHẬP BẰNG TÀI KHOẢN ────────────────────────────────────
        public async Task<(bool IsSuccess, string Message, int StatusCode, object? Data)> LoginAsync(LoginRequest request)
        {
            var email = request.Email.Trim();

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email);

            if (user == null || !user.IsActive)
                return (false, ValidationMessages.InvalidCredentials, 401, null);

            // User đăng ký qua Google sẽ không đăng nhập được bằng password
            if (string.IsNullOrEmpty(user.PasswordHash) ||
                !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                return (false, ValidationMessages.InvalidCredentials, 401, null);

            return (true, string.Empty, 200, BuildAuthResponse(user));
        }

        // ── 2a. ĐĂNG KÝ TÀI KHOẢN (CHỈ CUSTOMER) ───────────────────────────
        public async Task<(bool IsSuccess, string Message, int StatusCode, object? Data)> RegisterAsync(RegisterRequest request)
        {
            if (request.Password != request.ConfirmPassword)
                return (false, ValidationMessages.ConfirmPasswordMismatch, 400, null);

            if (!DateOnly.TryParseExact(request.DateOfBirth, "yyyy-MM-dd",
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out var dob))
                return (false, ValidationMessages.DateOfBirthInvalidFormat, 400, null);

            var email = request.Email.Trim();
            var phone = request.Phone.Trim();

            if (await _context.Users.AnyAsync(u => u.Email == email))
                return (false, ValidationMessages.EmailAlreadyRegistered, 409, null);

            // Chỉ cho phép đăng ký Customer qua endpoint công khai này
            if (!string.IsNullOrWhiteSpace(request.RoleName) &&
                !string.Equals(request.RoleName.Trim(), "Customer", StringComparison.OrdinalIgnoreCase))
                return (false, ValidationMessages.OnlyCustomerRegistrationAllowed, 400, null);

            var user = new User
            {
                FullName     = request.FullName.Trim(),
                Email        = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                Phone        = phone,
                Gender       = string.IsNullOrWhiteSpace(request.Gender) ? null : request.Gender.Trim(),
                DateOfBirth  = dob,
                Role         = RoleConstants.Customer,
                RewardPoint  = 0,
                IsActive     = true,
                CreatedAt    = DateTime.Now
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return (true, string.Empty, 200, BuildAuthResponse(user));
        }

        // ── 2b. TẠO TÀI KHOẢN NỘI BỘ (CHỈ ADMIN) ──────────────────────────
        public async Task<(bool IsSuccess, string Message, int StatusCode, object? Data)> CreateInternalAccountAsync(RegisterRequest request)
        {
            if (request.Password != request.ConfirmPassword)
                return (false, ValidationMessages.ConfirmPasswordMismatch, 400, null);

            if (!DateOnly.TryParseExact(request.DateOfBirth, "yyyy-MM-dd",
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out var dob))
                return (false, ValidationMessages.DateOfBirthInvalidFormat, 400, null);

            var roleName = string.IsNullOrWhiteSpace(request.RoleName) ? "" : request.RoleName.Trim();
            var allowedRoles = new[] { RoleConstants.Admin, RoleConstants.Staff };

            if (!allowedRoles.Contains(roleName))
                return (false, ValidationMessages.InvalidInternalRole, 400, null);

            var email = request.Email.Trim();
            var phone = request.Phone.Trim();

            if (await _context.Users.AnyAsync(u => u.Email == email))
                return (false, ValidationMessages.EmailAlreadyRegistered, 409, null);

            var user = new User
            {
                FullName     = request.FullName.Trim(),
                Email        = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                Phone        = phone,
                Gender       = string.IsNullOrWhiteSpace(request.Gender) ? null : request.Gender.Trim(),
                DateOfBirth  = dob,
                Role         = roleName,
                RewardPoint  = 0,
                IsActive     = true,
                CreatedAt    = DateTime.Now
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return (true, $"{roleName} account created successfully.", 200,
                new { user.UserId, user.FullName, user.Email, user.Role });
        }

        // ── 3. ĐĂNG NHẬP BẰNG GOOGLE ────────────────────────────────────────
        public async Task<(bool IsSuccess, string Message, int StatusCode, object? Data)> LoginGoogleAsync(GoogleAuthRequest request)
        {
            var payload = await VerifyGoogleToken(request.IdToken);
            if (payload == null)
                return (false, ValidationMessages.GoogleTokenInvalid, 401, null);

            var email = payload.Email.Trim();
            var user  = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

            // Chưa có tài khoản → trả thông tin Google để FE điền form thêm
            if (user == null)
            {
                var profile = new GoogleProfileResponse
                {
                    NeedsAdditionalInfo = true,
                    Email               = payload.Email,
                    FullName            = payload.Name ?? "",
                    AvatarUrl           = payload.Picture,
                    Message             = ValidationMessages.GoogleAccountNotRegistered
                };
                return (true, string.Empty, 202, profile);
            }

            if (!user.IsActive)
                return (false, ValidationMessages.UserLocked, 401, null);

            // Cập nhật avatar nếu thay đổi
            if (!string.IsNullOrEmpty(payload.Picture) && user.AvatarUrl != payload.Picture)
            {
                user.AvatarUrl = payload.Picture;
                await _context.SaveChangesAsync();
            }

            return (true, string.Empty, 200, BuildAuthResponse(user));
        }

        // ── 4. HOÀN TẤT ĐĂNG KÝ VỚI GOOGLE ────────────────────────────────
        public async Task<(bool IsSuccess, string Message, int StatusCode, object? Data)> RegisterWithGoogleAsync(GoogleRegisterCompleteRequest request)
        {
            var payload = await VerifyGoogleToken(request.IdToken);
            if (payload == null)
                return (false, ValidationMessages.GoogleTokenInvalid, 401, null);

            if (!DateOnly.TryParseExact(request.DateOfBirth, "yyyy-MM-dd",
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out var dob))
                return (false, ValidationMessages.DateOfBirthInvalidFormat, 400, null);

            var email = payload.Email.Trim();
            var phone = request.Phone.Trim();

            // Nếu đã có tài khoản → đăng nhập luôn
            var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (existingUser != null)
            {
                if (!existingUser.IsActive)
                    return (false, ValidationMessages.UserLocked, 401, null);

                return (true, string.Empty, 200, BuildAuthResponse(existingUser));
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
                Role         = RoleConstants.Customer,
                RewardPoint  = 0,
                IsActive     = true,
                CreatedAt    = DateTime.Now
            };

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            return (true, string.Empty, 200, BuildAuthResponse(newUser));
        }

        // ── 5. GỬI OTP QUÊN MẬT KHẨU ───────────────────────────────────────
        public async Task<(bool IsSuccess, string Message, int StatusCode, object? Data)> ForgotPasswordAsync(ForgotPasswordRequest request)
        {
            var email = request.Email.Trim();
            var user  = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

            // Bảo mật: luôn trả OK dù email không tồn tại (tránh lộ thông tin)
            if (user == null || !user.IsActive)
                return (true, ValidationMessages.IfEmailExistsOtpSent, 200, null);

            var otp      = Random.Shared.Next(100000, 999999).ToString();
            var cacheKey = $"otp_{email.ToLower()}";
            _cache.Set(cacheKey, otp, TimeSpan.FromMinutes(5));

            await _emailService.SendOtpAsync(email, user.FullName, otp);

            return (true, ValidationMessages.OtpSentSuccess, 200, null);
        }

        // ── 6. ĐẶT LẠI MẬT KHẨU ────────────────────────────────────────────
        public async Task<(bool IsSuccess, string Message, int StatusCode, object? Data)> ResetPasswordAsync(ResetPasswordRequest request)
        {
            if (request.NewPassword != request.ConfirmPassword)
                return (false, ValidationMessages.ConfirmPasswordMismatch, 400, null);

            var email    = request.Email.Trim();
            var cacheKey = $"otp_{email.ToLower()}";

            if (!_cache.TryGetValue(cacheKey, out string? savedOtp) || savedOtp != request.OtpCode)
                return (false, ValidationMessages.OtpInvalidOrExpired, 400, null);

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email && u.IsActive);
            if (user == null)
                return (false, ValidationMessages.UserNotFound, 404, null);

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            await _context.SaveChangesAsync();

            _cache.Remove(cacheKey);

            return (true, ValidationMessages.ResetPasswordSuccess, 200, null);
        }

        // ── 7. XÁC MINH MÃ OTP ──────────────────────────────────────────────
        public async Task<(bool IsSuccess, string Message, int StatusCode, object? Data)> VerifyResetCodeAsync(VerifyResetCodeRequest request)
        {
            var email    = request.Email.Trim();
            var cacheKey = $"otp_{email.ToLower()}";

            if (!_cache.TryGetValue(cacheKey, out string? savedOtp) || savedOtp != request.OtpCode)
                return (false, ValidationMessages.OtpInvalidOrExpired, 400, null);

            return await Task.FromResult((true, ValidationMessages.OtpValid, 200, (object?)null));
        }

        // ── 8. ĐỔI MẬT KHẨU KHI ĐANG ĐĂNG NHẬP ─────────────────────────────
        // Yêu cầu: user phải cung cấp đúng mật khẩu hiện tại trước khi đổi
        public async Task<(bool IsSuccess, string Message, int StatusCode, object? Data)> ChangePasswordAsync(int userId, ChangePasswordRequest request)
        {
            // Xác nhận mật khẩu mới và mật khẩu xác nhận phải khớp nhau
            if (request.NewPassword != request.ConfirmPassword)
                return (false, ValidationMessages.ConfirmPasswordMismatch, 400, null);

            // Không cho phép đặt mật khẩu mới trùng với mật khẩu cũ
            if (request.NewPassword == request.CurrentPassword)
                return (false, "Mật khẩu mới không được trùng với mật khẩu hiện tại.", 400, null);

            var user = await _context.Users.FindAsync(userId);
            if (user == null || !user.IsActive)
                return (false, ValidationMessages.UserNotFound, 404, null);

            // User đăng ký qua Google không có mật khẩu thật → không cho đổi theo luồng này
            if (string.IsNullOrEmpty(user.PasswordHash))
                return (false, "Tài khoản Google không thể đổi mật khẩu theo cách này.", 400, null);

            // Xác minh mật khẩu hiện tại nhập vào có đúng không
            if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
                return (false, "Mật khẩu hiện tại không chính xác.", 400, null);

            // Băm mật khẩu mới và lưu xuống DB
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            await _context.SaveChangesAsync();

            return (true, "Đổi mật khẩu thành công! Vui lòng đăng nhập lại.", 200, null);
        }

        // ── Private Helpers ──────────────────────────────────────────────────

        /// <summary>Xác thực Google ID token hoặc Access Token, trả null nếu lỗi.</summary>
        private async Task<GoogleJsonWebSignature.Payload?> VerifyGoogleToken(string idToken)
        {
            // CỔNG PHỤ: Chỉ cho phép dùng token giả lập "mock-google-test" khi ở môi trường Development
            if (_env.IsDevelopment() && idToken == "mock-google-test")
            {
                return new GoogleJsonWebSignature.Payload
                {
                    Email = "mockuser@gmail.com",
                    Name  = "User Test Google",
                    Picture = "https://lh3.googleusercontent.com/a/default-user"
                };
            }

            try
            {
                var clientId = _config["Google:ClientId"];
                var audiences = new List<string>();
                if (!string.IsNullOrWhiteSpace(clientId) && clientId != "YOUR_GOOGLE_CLIENT_ID_HERE")
                {
                    audiences.Add(clientId);
                }

                // Thêm Client ID OAuth Playground chỉ khi ở môi trường Development để test
                if (_env.IsDevelopment())
                {
                    audiences.Add("407408718192.apps.googleusercontent.com");
                }

                var settings = new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = audiences
                };
                return await GoogleJsonWebSignature.ValidateAsync(idToken, settings);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Google Auth Error]: {ex.Message}");
                if (ex.InnerException != null)
                    Console.WriteLine($"[Inner Exception]: {ex.InnerException.Message}");

                // Fallback: Thử xác thực nếu token truyền vào là Access Token (ya29...) hoặc API userinfo Google
                try
                {
                    using var client = new HttpClient();
                    var req = new HttpRequestMessage(HttpMethod.Get, "https://www.googleapis.com/oauth2/v3/userinfo");
                    req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", idToken);
                    var res = await client.SendAsync(req);
                    if (res.IsSuccessStatusCode)
                    {
                        var json = await res.Content.ReadAsStringAsync();
                        using var doc = System.Text.Json.JsonDocument.Parse(json);
                        var root = doc.RootElement;
                        return new GoogleJsonWebSignature.Payload
                        {
                            Email = root.TryGetProperty("email", out var e) ? e.GetString()! : "",
                            Name = root.TryGetProperty("name", out var n) ? n.GetString()! : "",
                            Picture = root.TryGetProperty("picture", out var p) ? p.GetString()! : ""
                        };
                    }
                }
                catch (Exception fallbackEx)
                {
                    Console.WriteLine($"[Google Auth Fallback Error]: {fallbackEx.Message}");
                }

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
    }
}
