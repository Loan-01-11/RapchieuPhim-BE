using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RapchieuPhim.API.Constants;
using RapchieuPhim.API.DTOs;
using RapchieuPhim.API.Services;
using System.Security.Claims;

namespace RapchieuPhim.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;

        public UsersController(IUserService userService)
        {
            _userService = userService;
        }

        // 👑 1. LẤY TẤT CẢ USER (CHỈ ADMIN)
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAll()
        {
            var users = await _userService.GetAllAsync();
            return Ok(users);
        }

        // 👑 2. XEM CHI TIẾT USER THEO ID (CHỈ ADMIN)
        [HttpGet("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetById(int id)
        {
            var user = await _userService.GetByIdAsync(id);
            if (user == null)
                return NotFound(new { Message = ValidationMessages.UserNotFoundWithId(id) });

            return Ok(user);
        }

        // 🔓 3a. TẤT CẢ USER TỰ LẤY THÔNG TIN CÁ NHÂN CỦA MÌNH
        [HttpGet("GetProfile")]
        public async Task<IActionResult> GetProfile()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
                return Unauthorized(new { Message = ValidationMessages.TokenInvalidOrExpired });

            int userId = int.Parse(userIdClaim);
            var userProfile = await _userService.GetProfileAsync(userId);

            if (userProfile == null)
                return NotFound(new { Message = ValidationMessages.UserNotFoundInSystem });

            return Ok(userProfile);
        }

        // 🔓 3b. TẤT CẢ USER TỰ CẬP NHẬT THÔNG TIN CHÍNH MÌNH
        [HttpPut("UpdateProfile")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileUserRequest request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
                return Unauthorized(new { Message = ValidationMessages.TokenInvalid });

            int userId = int.Parse(userIdClaim);
            var result = await _userService.UpdateProfileAsync(userId, request);

            if (!result.IsSuccess)
                return StatusCode(result.StatusCode, new { Message = result.Message });

            return Ok(new { Message = result.Message });
        }

        // 👑 4. ADMIN CẬP NHẬT NGƯỜI KHÁC (CHỈ ADMIN TỐI CAO ĐƯỢC ĐỔI ROLE)
        [HttpPut("AdminUpdate/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminUpdate(int id, [FromBody] AdminUpdateUserRequest request)
        {
            var currentOperatorEmail = User.FindFirst(ClaimTypes.Email)?.Value;
            var result = await _userService.AdminUpdateAsync(id, request, currentOperatorEmail ?? string.Empty);

            if (!result.IsSuccess)
                return StatusCode(result.StatusCode, new { Message = result.Message });

            return Ok(new { Message = result.Message });
        }

        // 👑 5. XÓA TÀI KHOẢN (CHỈ SUPER ADMIN)
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var currentOperatorEmail = User.FindFirst(ClaimTypes.Email)?.Value;
            var result = await _userService.DeleteAsync(id, currentOperatorEmail ?? string.Empty);

            if (!result.IsSuccess)
                return StatusCode(result.StatusCode, new { Message = result.Message });

            return Ok(new { Message = result.Message });
        }

        // 👑 6. LỌC DANH SÁCH USER THEO QUYỀN (CHỈ ADMIN)
        [HttpGet("ByRole/{role}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetByRole(string role)
        {
            var users = await _userService.GetByRoleAsync(role);
            return Ok(users);
        }
    }
}