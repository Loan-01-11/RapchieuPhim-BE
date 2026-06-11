using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RapchieuPhim.API.Constants; // 🌟 Gọi thư mục hằng số ra dùng
using RapchieuPhim.API.DTOs;
using RapchieuPhim.API.Models;
using System.Globalization;
using System.Security.Claims;

namespace RapchieuPhim.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly CinemaManagementContext _context;

        public UsersController(CinemaManagementContext context)
        {
            _context = context;
        }

        // 👑 1. LẤY TẤT CẢ USER (CHỈ ADMIN)
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAll()
        {
            var users = await _context.Users
                .Select(u => new { u.UserId, u.FullName, u.Email, u.Phone, u.Role, u.IsActive, u.CreatedAt, u.DateOfBirth, u.Gender })
                .ToListAsync();
            return Ok(users);
        }

        // 👑 2. XEM CHI TIẾT USER THEO ID (CHỈ ADMIN)
        [HttpGet("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetById(int id)
        {
            var user = await _context.Users
                .Where(u => u.UserId == id)
                .Select(u => new { u.UserId, u.FullName, u.Email, u.Phone, u.Role, u.IsActive })
                .FirstOrDefaultAsync();

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
                return Unauthorized(new { Message = ValidationMessages.TokenInvalidOrExpired }); // ➔ Sạch hằng số

            int userId = int.Parse(userIdClaim);
            var userProfile = await _context.Users
                .Where(u => u.UserId == userId)
                .Select(u => new { u.UserId, u.FullName, u.Email, u.Phone, u.DateOfBirth, u.Gender, u.Role, u.CreatedAt })
                .FirstOrDefaultAsync();

            if (userProfile == null)
                return NotFound(new { Message = ValidationMessages.UserNotFoundInSystem }); // ➔ Sạch hằng số

            return Ok(userProfile);
        }

        // 🔓 3b. TẤT CẢ USER TỰ CẬP NHẬT THÔNG TIN CHÍNH MÌNH
        [HttpPut("UpdateProfile")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileUserRequest request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
                return Unauthorized(new { Message = ValidationMessages.TokenInvalid }); // ➔ Sạch hằng số

            int userId = int.Parse(userIdClaim);
            var user = await _context.Users.FindAsync(userId);
            if (user == null || !user.IsActive)
                return NotFound(new { Message = ValidationMessages.AccountNotFoundOrLocked }); // ➔ Sạch hằng số

            if (!DateOnly.TryParseExact(request.DateOfBirth, "yyyy-MM-dd",
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out var dob))
                return BadRequest(new { Message = ValidationMessages.DateOfBirthInvalidFormatSimple }); // ➔ Sạch hằng số

            user.FullName = request.FullName.Trim();
            user.Phone = request.Phone.Trim();
            user.DateOfBirth = dob;
            user.Gender = string.IsNullOrWhiteSpace(request.Gender) ? null : request.Gender.Trim();

            await _context.SaveChangesAsync();
            return Ok(new { Message = "Cập nhật hồ sơ cá nhân thành công!" });
        }

        // 👑 4. ADMIN CẬP NHẬT NGƯỜI KHÁC (CHỈ ADMIN TỐI CAO ĐƯỢC ĐỔI ROLE)
        [HttpPut("AdminUpdate/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminUpdate(int id, [FromBody] AdminUpdateUserRequest request)
        {
            var targetUser = await _context.Users.FindAsync(id);
            if (targetUser == null)
                return NotFound(new { Message = ValidationMessages.UserNotFoundWithId(id) });

            var currentOperatorEmail = User.FindFirst(ClaimTypes.Email)?.Value;
            var newRole = request.Role.Trim();

            if (targetUser.Role != newRole)
            {
                if (currentOperatorEmail != ValidationMessages.SuperAdminEmail) // ➔ Dùng hằng số email chuẩn
                {
                    return StatusCode(403, new { Message = ValidationMessages.UnauthorizedRoleChange }); // ➔ Sạch hằng số
                }
            }

            if (!DateOnly.TryParseExact(request.DateOfBirth, "yyyy-MM-dd",
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out var dob))
                return BadRequest(new { Message = ValidationMessages.DateOfBirthInvalidFormatSimple }); // ➔ Sạch hằng số

            var allowedRoles = new[] { "Admin", "Staff", "Customer" };
            if (!allowedRoles.Contains(newRole))
                return BadRequest(new { Message = ValidationMessages.RoleSelectionInvalid }); // ➔ Sạch hằng số

            targetUser.FullName = request.FullName.Trim();
            targetUser.Phone = request.Phone.Trim();
            targetUser.DateOfBirth = dob;
            targetUser.Gender = string.IsNullOrWhiteSpace(request.Gender) ? null : request.Gender.Trim();
            targetUser.Role = newRole;

            await _context.SaveChangesAsync();
            return Ok(new { Message = ValidationMessages.UserUpdateSuccessWithId(id) });
        }

        // 👑 5. XÓA TÀI KHOẢN (CHỈ SUPER ADMIN)
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return NotFound(new { Message = ValidationMessages.UserNotFoundWithId(id) });

            var currentOperatorEmail = User.FindFirst(ClaimTypes.Email)?.Value;

            if (currentOperatorEmail != ValidationMessages.SuperAdminEmail) // ➔ Dùng hằng số email chuẩn
            {
                return StatusCode(403, new { Message = ValidationMessages.UnauthorizedDelete }); // ➔ Sạch hằng số
            }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            return Ok(new { Message = ValidationMessages.UserUpdateSuccessWithId(id) });
        }

        // 👑 6. LỌC DANH SÁCH USER THEO QUYỀN (CHỈ ADMIN)
        [HttpGet("ByRole/{role}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetByRole(string role)
        {
            var users = await _context.Users
                .Where(u => u.Role == role && u.IsActive)
                .Select(u => new { u.UserId, u.FullName, u.Email, u.Role })
                .ToListAsync();
            return Ok(users);
        }
    }
}