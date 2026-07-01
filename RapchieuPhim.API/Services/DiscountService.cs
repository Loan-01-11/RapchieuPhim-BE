using Microsoft.EntityFrameworkCore;
using RapchieuPhim.API.Constants;
using RapchieuPhim.API.DTOs.DTORequest;
using RapchieuPhim.API.DTOs.DTOResponse;
using RapchieuPhim.API.Models;

namespace RapchieuPhim.API.Services
{
    // ─── GIAO TIẾP (Interface) ───────────────────────────────────────────────────
    public interface IDiscountService
    {
        Task<List<DiscountResponse>> GetAllAsync();
        Task<DiscountResponse?> GetByIdAsync(int id);
        Task<DiscountResponse?> GetByCodeAsync(string code);
        Task<(bool IsSuccess, string Message, int StatusCode, DiscountResponse? Data)> CreateAsync(DiscountRequest request, int createdByUserId);
        Task<(bool IsSuccess, string Message, int StatusCode)> UpdateAsync(int id, DiscountRequest request, string currentOperatorEmail);
        Task<(bool IsSuccess, string Message, int StatusCode)> DeleteAsync(int id, string currentOperatorEmail);
    }

    // ─── TRIỂN KHAI (Implementation) ─────────────────────────────────────────────
    public class DiscountService : IDiscountService
    {
        private readonly CinemaManagementContext _context;

        public DiscountService(CinemaManagementContext context)
        {
            _context = context;
        }

        // ── Ánh xạ Entity → Response DTO ────────────────────────────────────────
        private static DiscountResponse MapToResponse(Discount d) => new()
        {
            DiscountId      = d.DiscountId,
            DiscountCode    = d.DiscountCode,
            Description     = d.Description,
            DiscountType    = d.DiscountType,
            DiscountValue   = d.DiscountValue,
            MinOrderAmount  = d.MinOrderAmount,
            MaxUsageTotal   = d.MaxUsageTotal,
            MaxUsagePerUser = d.MaxUsagePerUser,
            UsedCount       = d.UsedCount,
            StartDate       = d.StartDate,
            EndDate         = d.EndDate,
            IsActive        = d.IsActive,
            CreatedBy       = d.CreatedBy
        };

        /// <summary>
        /// Lấy toàn bộ danh sách mã giảm giá (kể cả hết hạn / vô hiệu hóa).
        /// </summary>
        public async Task<List<DiscountResponse>> GetAllAsync()
        {
            return await _context.Discounts
                .OrderByDescending(d => d.DiscountId)
                .Select(d => new DiscountResponse
                {
                    DiscountId      = d.DiscountId,
                    DiscountCode    = d.DiscountCode,
                    Description     = d.Description,
                    DiscountType    = d.DiscountType,
                    DiscountValue   = d.DiscountValue,
                    MinOrderAmount  = d.MinOrderAmount,
                    MaxUsageTotal   = d.MaxUsageTotal,
                    MaxUsagePerUser = d.MaxUsagePerUser,
                    UsedCount       = d.UsedCount,
                    StartDate       = d.StartDate,
                    EndDate         = d.EndDate,
                    IsActive        = d.IsActive,
                    CreatedBy       = d.CreatedBy
                })
                .ToListAsync();
        }

        /// <summary>
        /// Lấy chi tiết 1 mã giảm giá theo ID.
        /// </summary>
        public async Task<DiscountResponse?> GetByIdAsync(int id)
        {
            var d = await _context.Discounts.FindAsync(id);
            return d == null ? null : MapToResponse(d);
        }

        /// <summary>
        /// Tìm kiếm và xác thực mã giảm giá theo Code (chỉ trả về mã còn hiệu lực).
        /// </summary>
        public async Task<DiscountResponse?> GetByCodeAsync(string code)
        {
            var d = await _context.Discounts
                .Where(x => x.DiscountCode == code
                         && x.IsActive
                         && x.StartDate <= DateTime.Now
                         && (x.EndDate == null || x.EndDate >= DateTime.Now))
                .FirstOrDefaultAsync();

            return d == null ? null : MapToResponse(d);
        }

        /// <summary>
        /// Tạo mã giảm giá mới.
        /// Quy tắc: Mã Code không được trùng + DiscountType hợp lệ + EndDate > StartDate.
        /// </summary>
        public async Task<(bool IsSuccess, string Message, int StatusCode, DiscountResponse? Data)> CreateAsync(
            DiscountRequest request, int createdByUserId)
        {
            // 1. Kiểm tra loại giảm giá hợp lệ
            if (!DiscountMessages.ValidDiscountTypes.Contains(request.DiscountType))
                return (false, DiscountMessages.InvalidDiscountType, 400, null);

            // 2. Nếu Percent thì DiscountValue phải <= 100
            if (request.DiscountType == DiscountMessages.TypePercent && request.DiscountValue > 100)
                return (false, DiscountMessages.PercentValueExceeds100, 400, null);

            // 3. Kiểm tra EndDate phải > StartDate (nếu có)
            if (request.EndDate.HasValue && request.EndDate.Value <= request.StartDate)
                return (false, DiscountMessages.EndDateBeforeStartDate, 400, null);

            // 4. Kiểm tra Code không được trùng (case-insensitive)
            var exists = await _context.Discounts
                .AnyAsync(d => d.DiscountCode.ToLower() == request.DiscountCode.ToLower().Trim());
            if (exists)
                return (false, DiscountMessages.DiscountCodeAlreadyExists, 409, null);

            // 5. Kiểm tra người tạo có tồn tại trong hệ thống không
            var userExists = await _context.Users.AnyAsync(u => u.UserId == createdByUserId);
            if (!userExists)
                return (false, ValidationMessages.UserNotFoundInSystem, 404, null);

            // 6. Tạo entity mới và lưu DB
            var discount = new Discount
            {
                DiscountCode    = request.DiscountCode.Trim().ToUpper(),
                Description     = request.Description?.Trim(),
                DiscountType    = request.DiscountType,
                DiscountValue   = request.DiscountValue,
                MinOrderAmount  = request.MinOrderAmount,
                MaxUsageTotal   = request.MaxUsageTotal,
                MaxUsagePerUser = request.MaxUsagePerUser,
                UsedCount       = 0, // Mặc định khi tạo mới
                StartDate       = request.StartDate,
                EndDate         = request.EndDate,
                IsActive        = request.IsActive,
                CreatedBy       = createdByUserId
            };

            _context.Discounts.Add(discount);
            await _context.SaveChangesAsync();

            return (true, DiscountMessages.CreateSuccess, 201, MapToResponse(discount));
        }

        /// <summary>
        /// Cập nhật mã giảm giá.
        /// Quyền hạn: Chỉ Super Admin mới được sửa.
        /// </summary>
        public async Task<(bool IsSuccess, string Message, int StatusCode)> UpdateAsync(
            int id, DiscountRequest request, string currentOperatorEmail)
        {
            // 1. Tìm mã giảm giá trong DB
            var discount = await _context.Discounts.FindAsync(id);
            if (discount == null)
                return (false, DiscountMessages.NotFoundWithId(id), 404);

            // 2. CHỐT CHẶN: Chỉ Super Admin mới được cập nhật
            if (currentOperatorEmail != ValidationMessages.SuperAdminEmail)
                return (false, DiscountMessages.UnauthorizedUpdate, 403);

            // 3. Kiểm tra loại giảm giá hợp lệ
            if (!DiscountMessages.ValidDiscountTypes.Contains(request.DiscountType))
                return (false, DiscountMessages.InvalidDiscountType, 400);

            // 4. Nếu Percent thì DiscountValue phải <= 100
            if (request.DiscountType == DiscountMessages.TypePercent && request.DiscountValue > 100)
                return (false, DiscountMessages.PercentValueExceeds100, 400);

            // 5. Kiểm tra EndDate phải > StartDate (nếu có)
            if (request.EndDate.HasValue && request.EndDate.Value <= request.StartDate)
                return (false, DiscountMessages.EndDateBeforeStartDate, 400);

            // 6. Kiểm tra Code trùng với bản ghi KHÁC (không phải chính nó)
            var codeConflict = await _context.Discounts
                .AnyAsync(d => d.DiscountCode.ToLower() == request.DiscountCode.ToLower().Trim()
                            && d.DiscountId != id);
            if (codeConflict)
                return (false, DiscountMessages.DiscountCodeAlreadyExists, 409);

            // 7. Áp dụng thay đổi
            discount.DiscountCode    = request.DiscountCode.Trim().ToUpper();
            discount.Description     = request.Description?.Trim();
            discount.DiscountType    = request.DiscountType;
            discount.DiscountValue   = request.DiscountValue;
            discount.MinOrderAmount  = request.MinOrderAmount;
            discount.MaxUsageTotal   = request.MaxUsageTotal;
            discount.MaxUsagePerUser = request.MaxUsagePerUser;
            discount.StartDate       = request.StartDate;
            discount.EndDate         = request.EndDate;
            discount.IsActive        = request.IsActive;

            try
            {
                await _context.SaveChangesAsync();
                return (true, DiscountMessages.UpdateSuccess, 200);
            }
            catch (DbUpdateConcurrencyException)
            {
                return (false, DiscountMessages.ConcurrencyError, 409);
            }
        }

        /// <summary>
        /// Xóa mã giảm giá.
        /// Quyền hạn: Chỉ Super Admin mới được xóa.
        /// Bảo vệ: Không cho xóa nếu mã đã được dùng (UsedCount > 0).
        /// </summary>
        public async Task<(bool IsSuccess, string Message, int StatusCode)> DeleteAsync(
            int id, string currentOperatorEmail)
        {
            // 1. Tìm mã giảm giá trong DB
            var discount = await _context.Discounts.FindAsync(id);
            if (discount == null)
                return (false, DiscountMessages.NotFoundWithId(id), 404);

            // 2. CHỐT CHẶN: Chỉ Super Admin mới được xóa
            if (currentOperatorEmail != ValidationMessages.SuperAdminEmail)
                return (false, ValidationMessages.UnauthorizedDelete, 403);

            // 3. BẢO VỆ DỮ LIỆU: Không cho xóa mã đã được khách hàng sử dụng
            if (discount.UsedCount > 0)
                return (false, DiscountMessages.CannotDeleteUsedDiscount, 409);

            // 4. Tiến hành xóa
            _context.Discounts.Remove(discount);
            await _context.SaveChangesAsync();

            return (true, DiscountMessages.DeleteSuccess, 200);
        }
    }
}
