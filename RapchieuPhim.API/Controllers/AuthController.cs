using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RapchieuPhim.API.Constants;
using RapchieuPhim.API.DTO.DTORequest;
using RapchieuPhim.API.Services;

namespace RapchieuPhim.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        // ── 1. ĐĂNG NHẬP BẰNG TÀI KHOẢN ────────────────────────────────────
        // POST: api/Auth/Login
        // Body: { "email": "...", "password": "..." }
        [HttpPost("Login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { Message = GetFirstError() });

            var result = await _authService.LoginAsync(request);
            return StatusCode(result.StatusCode,
                result.IsSuccess ? result.Data : new { result.Message });
        }

        // ── 2a. ĐĂNG KÝ TÀI KHOẢN CUSTOMER ─────────────────────────────────
        // POST: api/Auth/Register
        // Body: { "fullName", "email", "password", "confirmPassword", "dateOfBirth", "gender", "phone" }
        [HttpPost("Register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { Message = GetFirstError() });

            var result = await _authService.RegisterAsync(request);
            return StatusCode(result.StatusCode,
                result.IsSuccess ? result.Data : new { result.Message });
        }

        // ── 2b. TẠO TÀI KHOẢN NỘI BỘ (CHỈ ADMIN) ───────────────────────────
        // POST: api/Auth/CreateInternalAccount
        // Body: { "fullName", "email", "password", "confirmPassword", "dateOfBirth", "gender", "phone", "roleName" }
        [HttpPost("CreateInternalAccount")]
        [Authorize(Roles = RoleConstants.Admin)]
        public async Task<IActionResult> CreateInternalAccount([FromBody] RegisterRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { Message = GetFirstError() });

            var result = await _authService.CreateInternalAccountAsync(request);
            return StatusCode(result.StatusCode,
                result.IsSuccess ? result.Data : new { result.Message });
        }

        // ── 3. ĐĂNG NHẬP BẰNG GOOGLE ────────────────────────────────────────
        // POST: api/Auth/LoginGoogle
        // Body: { "idToken": "..." }
        // - User đã tồn tại  → 200 + JWT
        // - User chưa tồn tại → 202 + GoogleProfileResponse
        [HttpPost("LoginGoogle")]
        public async Task<IActionResult> LoginGoogle([FromBody] GoogleAuthRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { Message = GetFirstError() });

            var result = await _authService.LoginGoogleAsync(request);
            return StatusCode(result.StatusCode,
                result.IsSuccess ? result.Data : new { result.Message });
        }

        // ── 4. HOÀN TẤT ĐĂNG KÝ VỚI GOOGLE ────────────────────────────────
        // POST: api/Auth/RegisterWithGoogle
        // Body: { "idToken", "fullName", "phone", "dateOfBirth": "yyyy-MM-dd", "gender" }
        [HttpPost("RegisterWithGoogle")]
        public async Task<IActionResult> RegisterWithGoogle([FromBody] GoogleRegisterCompleteRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { Message = GetFirstError() });

            var result = await _authService.RegisterWithGoogleAsync(request);
            return StatusCode(result.StatusCode,
                result.IsSuccess ? result.Data : new { result.Message });
        }

        // ── 5. GỬI OTP QUÊN MẬT KHẨU ───────────────────────────────────────
        // POST: api/Auth/ForgotPassword
        // Body: { "email": "..." }
        [HttpPost("ForgotPassword")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { Message = GetFirstError() });

            var result = await _authService.ForgotPasswordAsync(request);
            return StatusCode(result.StatusCode, new { result.Message });
        }

        // ── 6. ĐẶT LẠI MẬT KHẨU ────────────────────────────────────────────
        // POST: api/Auth/ResetPassword
        // Body: { "email", "otpCode", "newPassword", "confirmPassword" }
        [HttpPost("ResetPassword")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { Message = GetFirstError() });

            var result = await _authService.ResetPasswordAsync(request);
            return StatusCode(result.StatusCode, new { result.Message });
        }

        // ── 7. XÁC MINH MÃ OTP ──────────────────────────────────────────────
        // POST: api/Auth/VerifyResetCode
        // Body: { "email": "...", "otpCode": "123456" }
        [HttpPost("VerifyResetCode")]
        public async Task<IActionResult> VerifyResetCode([FromBody] VerifyResetCodeRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { Message = GetFirstError() });

            var result = await _authService.VerifyResetCodeAsync(request);
            return StatusCode(result.StatusCode, new { result.Message });
        }

        // ── Private Helpers ──────────────────────────────────────────────────

        /// <summary>Lấy lỗi đầu tiên từ ModelState để trả về message thân thiện.</summary>
        private string GetFirstError()
        {
            foreach (var state in ModelState.Values)
                foreach (var error in state.Errors)
                    if (!string.IsNullOrEmpty(error.ErrorMessage))
                        return error.ErrorMessage;
            return ValidationMessages.DataInvalid;
        }
    }
}
