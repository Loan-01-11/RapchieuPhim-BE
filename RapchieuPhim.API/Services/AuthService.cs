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
    }

    public class AuthService : IAuthService
    {
        private readonly CinemaManagementContext _context;
        private readonly IConfiguration _config;
        private readonly IEmailService _emailService;
        private readonly IMemoryCache _cache;

        public AuthService(CinemaManagementContext context, IConfiguration config,
            IEmailService emailService, IMemoryCache cache)
        {
            _context      = context;
            _config       = config;
            _emailService = emailService;
            _cache        = cache;
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

        // ── Private Helpers ──────────────────────────────────────────────────

        /// <summary>Xác thực Google ID token, trả null nếu lỗi.</summary>
        private async Task<GoogleJsonWebSignature.Payload?> VerifyGoogleToken(string idToken)
        {
            // CỔNG PHỤ: Nếu token là "mock-google-test", trả về thông tin giả lập để test trên Swagger
            if (idToken == "mock-google-test")
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
                var settings = new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = new[] { _config["Google:ClientId"] }
                };
                return await GoogleJsonWebSignature.ValidateAsync(idToken, settings);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Google Auth Error]: {ex.Message}");
                if (ex.InnerException != null)
                    Console.WriteLine($"[Inner Exception]: {ex.InnerException.Message}");
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
