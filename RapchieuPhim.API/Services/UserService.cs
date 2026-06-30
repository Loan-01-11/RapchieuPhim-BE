using Microsoft.EntityFrameworkCore;
using RapchieuPhim.API.Constants;
using RapchieuPhim.API.DTO.DTORequest;
using RapchieuPhim.API.DTO.DTOResponse;
using RapchieuPhim.API.Models;
using System.Globalization;

namespace RapchieuPhim.API.Services
{
    public interface IUserService
    {
        Task<List<UserResponse>> GetAllAsync();
        Task<UserResponse?> GetByIdAsync(int id);
        Task<UserResponse?> GetProfileAsync(int userId);
        Task<(bool IsSuccess, string Message, int StatusCode)> UpdateProfileAsync(int userId, UpdateProfileUserRequest request);
        Task<(bool IsSuccess, string Message, int StatusCode)> AdminUpdateAsync(int id, AdminUpdateUserRequest request, string currentOperatorEmail);
        Task<(bool IsSuccess, string Message, int StatusCode)> DeleteAsync(int id, string currentOperatorEmail);
        Task<List<UserResponse>> GetByRoleAsync(string role);
    }


    public class UserService : IUserService
    {
        private readonly CinemaManagementContext _context;

        public UserService(CinemaManagementContext context)
        {
            _context = context;
        }

        // 👑 1. LẤY TẤT CẢ USER
        public async Task<List<UserResponse>> GetAllAsync()
        {
            return await _context.Users
                .Select(u => new UserResponse
                {
                    UserId = u.UserId,
                    FullName = u.FullName,
                    Email = u.Email,
                    Phone = u.Phone,
                    Role = u.Role,
                    IsActive = u.IsActive,
                    CreatedAt = u.CreatedAt,
                    DateOfBirth = u.DateOfBirth,
                    Gender = u.Gender
                }).ToListAsync();
        }

        // 👑 2. XEM CHI TIẾT USER THEO ID
        public async Task<UserResponse?> GetByIdAsync(int id)
        {
            return await _context.Users
                .Where(u => u.UserId == id)
                .Select(u => new UserResponse
                {
                    UserId = u.UserId,
                    FullName = u.FullName,
                    Email = u.Email,
                    Phone = u.Phone,
                    Role = u.Role,
                    IsActive = u.IsActive
                }).FirstOrDefaultAsync();
        }

        // 🔓 3a. TẤT CẢ USER TỰ LẤY THÔNG TIN CÁ NHÂN CỦA MÌNH
        public async Task<UserResponse?> GetProfileAsync(int userId)
        {
            return await _context.Users
                .Where(u => u.UserId == userId)
                .Select(u => new UserResponse
                {
                    UserId = u.UserId,
                    FullName = u.FullName,
                    Email = u.Email,
                    Phone = u.Phone,
                    DateOfBirth = u.DateOfBirth,
                    Gender = u.Gender,
                    Role = u.Role,
                    CreatedAt = u.CreatedAt
                }).FirstOrDefaultAsync();
        }

        // 🔓 3b. TẤT CẢ USER TỰ CẬP NHẬT THÔNG TIN CHÍNH MÌNH
        public async Task<(bool IsSuccess, string Message, int StatusCode)> UpdateProfileAsync(int userId, UpdateProfileUserRequest request)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null || !user.IsActive)
                return (false, ValidationMessages.AccountNotFoundOrLocked, 404);

            if (!DateOnly.TryParseExact(request.DateOfBirth, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dob))
                return (false, ValidationMessages.DateOfBirthInvalidFormatSimple, 400);

            user.FullName = request.FullName.Trim();
            user.Phone = request.Phone.Trim();
            user.DateOfBirth = dob;
            user.Gender = string.IsNullOrWhiteSpace(request.Gender) ? null : request.Gender.Trim();

            await _context.SaveChangesAsync();
            return (true, ValidationMessages.UpdatedProfileSuccessfully, 200);
        }

        // 👑 4. ADMIN CẬP NHẬT NGƯỜI KHÁC
        public async Task<(bool IsSuccess, string Message, int StatusCode)> AdminUpdateAsync(int id, AdminUpdateUserRequest request, string currentOperatorEmail)
        {
            var targetUser = await _context.Users.FindAsync(id);
            if (targetUser == null)
                return (false, ValidationMessages.UserNotFoundWithId(id), 404);

            var newRole = request.Role.Trim();

            if (targetUser.Role != newRole)
            {
                if (currentOperatorEmail != ValidationMessages.SuperAdminEmail)
                    return (false, ValidationMessages.UnauthorizedRoleChange, 403);
            }

            if (!DateOnly.TryParseExact(request.DateOfBirth, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dob))
                return (false, ValidationMessages.DateOfBirthInvalidFormatSimple, 400);

            var allowedRoles = new[] { RoleConstants.Admin, RoleConstants.Staff, RoleConstants.Customer };
            if (!allowedRoles.Contains(newRole))
                return (false, ValidationMessages.RoleSelectionInvalid, 400);

            targetUser.FullName = request.FullName.Trim();
            targetUser.Phone = request.Phone.Trim();
            targetUser.DateOfBirth = dob;
            targetUser.Gender = string.IsNullOrWhiteSpace(request.Gender) ? null : request.Gender.Trim();
            targetUser.Role = newRole;
            targetUser.IsActive = request.IsActive;

            await _context.SaveChangesAsync();
            return (true, ValidationMessages.UserUpdateSuccessWithId(id), 200);
        }

        // 👑 5. XÓA TÀI KHOẢN
        public async Task<(bool IsSuccess, string Message, int StatusCode)> DeleteAsync(int id, string currentOperatorEmail)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return (false, ValidationMessages.UserNotFoundWithId(id), 404);

            if (currentOperatorEmail != ValidationMessages.SuperAdminEmail)
                return (false, ValidationMessages.UnauthorizedDelete, 403);

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            return (true, ValidationMessages.UserUpdateSuccessWithId(id), 200);
        }

        // 👑 6. LỌC DANH SÁCH USER THEO QUYỀN
        public async Task<List<UserResponse>> GetByRoleAsync(string role)
        {
            return await _context.Users
                .Where(u => u.Role == role)
                .Select(u => new UserResponse
                {
                    UserId = u.UserId,
                    FullName = u.FullName,
                    Email = u.Email,
                    Phone = u.Phone,
                    Role = u.Role,
                    IsActive = u.IsActive,
                    CreatedAt = u.CreatedAt,
                    DateOfBirth = u.DateOfBirth,
                    Gender = u.Gender
                }).ToListAsync();
        }
    }
}