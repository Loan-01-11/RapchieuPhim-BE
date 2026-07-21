using Microsoft.EntityFrameworkCore;
using RapchieuPhim.API.Constants;
using RapchieuPhim.API.DTOs.DTORequest;
using RapchieuPhim.API.DTOs.DTOResponse;
using RapchieuPhim.API.Models;

namespace RapchieuPhim.API.Services
{
    // ─── GIAO TIẾP (Interface) ───────────────────────────────────────────────────
    public interface IFoodService
    {
        Task<List<FoodResponse>> GetAllAsync();
        Task<FoodResponse?> GetByIdAsync(int id);
        Task<List<FoodResponse>> GetAvailableAsync();
        Task<List<FoodResponse>> GetByCategoryAsync(string category);
        Task<(bool IsSuccess, string Message, int StatusCode, FoodResponse? Data)> CreateAsync(FoodRequest request);
        Task<(bool IsSuccess, string Message, int StatusCode)> UpdateAsync(int id, FoodRequest request, string currentOperatorEmail);
        Task<(bool IsSuccess, string Message, int StatusCode)> DeleteAsync(int id, string currentOperatorEmail);
    }

    // ─── TRIỂN KHAI (Implementation) ─────────────────────────────────────────────
    public class FoodService : IFoodService
    {
        private readonly CinemaManagementContext _context;

        public FoodService(CinemaManagementContext context)
        {
            _context = context;
        }

        // ── Ánh xạ Entity → Response DTO ────────────────────────────────────────
        private static FoodResponse MapToResponse(Food f) => new()
        {
            FoodId = f.FoodId,
            FoodName = f.FoodName,
            Category = f.Category,
            Price = f.Price,
            Quantity = f.Quantity,
            ImageUrl = f.ImageUrl,
            IsAvailable = f.IsAvailable,
            SoldThisMonth = f.Orderitems != null ? f.Orderitems.Where(oi => oi.Order.OrderDate.Month == DateTime.Now.Month && oi.Order.OrderDate.Year == DateTime.Now.Year).Sum(oi => (int?)oi.Quantity) ?? 0 : 0,
            RevenueThisMonth = f.Orderitems != null ? f.Orderitems.Where(oi => oi.Order.OrderDate.Month == DateTime.Now.Month && oi.Order.OrderDate.Year == DateTime.Now.Year).Sum(oi => (decimal?)oi.Subtotal) ?? 0m : 0m,
            SoldToday = f.Orderitems != null ? f.Orderitems.Where(oi => oi.Order.OrderDate.Date == DateTime.Now.Date).Sum(oi => (int?)oi.Quantity) ?? 0 : 0,
            RevenueToday = f.Orderitems != null ? f.Orderitems.Where(oi => oi.Order.OrderDate.Date == DateTime.Now.Date).Sum(oi => (decimal?)oi.Subtotal) ?? 0m : 0m,
            SoldThisWeek = f.Orderitems != null ? f.Orderitems.Where(oi => oi.Order.OrderDate >= DateTime.Now.AddDays(-7)).Sum(oi => (int?)oi.Quantity) ?? 0 : 0,
            RevenueThisWeek = f.Orderitems != null ? f.Orderitems.Where(oi => oi.Order.OrderDate >= DateTime.Now.AddDays(-7)).Sum(oi => (decimal?)oi.Subtotal) ?? 0m : 0m
        };

        /// <summary>
        /// Lấy toàn bộ danh sách món ăn / đồ uống (kể cả đã tạm ngưng).
        /// Quyền: Admin, Staff
        /// </summary>
        public async Task<List<FoodResponse>> GetAllAsync()
        {
            return await _context.Foods
                .OrderBy(f => f.Category)
                .ThenBy(f => f.FoodName)
                .Select(f => new FoodResponse
                {
                    FoodId = f.FoodId,
                    FoodName = f.FoodName,
                    Category = f.Category,
                    Price = f.Price,
                    Quantity = f.Quantity,
                    ImageUrl = f.ImageUrl,
                    IsAvailable = f.IsAvailable,
                    SoldThisMonth = f.Orderitems != null ? f.Orderitems.Where(oi => oi.Order.OrderDate.Month == DateTime.Now.Month && oi.Order.OrderDate.Year == DateTime.Now.Year).Sum(oi => (int?)oi.Quantity) ?? 0 : 0,
                    RevenueThisMonth = f.Orderitems != null ? f.Orderitems.Where(oi => oi.Order.OrderDate.Month == DateTime.Now.Month && oi.Order.OrderDate.Year == DateTime.Now.Year).Sum(oi => (decimal?)oi.Subtotal) ?? 0m : 0m,
                    SoldToday = f.Orderitems != null ? f.Orderitems.Where(oi => oi.Order.OrderDate.Date == DateTime.Now.Date).Sum(oi => (int?)oi.Quantity) ?? 0 : 0,
                    RevenueToday = f.Orderitems != null ? f.Orderitems.Where(oi => oi.Order.OrderDate.Date == DateTime.Now.Date).Sum(oi => (decimal?)oi.Subtotal) ?? 0m : 0m,
                    SoldThisWeek = f.Orderitems != null ? f.Orderitems.Where(oi => oi.Order.OrderDate >= DateTime.Now.AddDays(-7)).Sum(oi => (int?)oi.Quantity) ?? 0 : 0,
                    RevenueThisWeek = f.Orderitems != null ? f.Orderitems.Where(oi => oi.Order.OrderDate >= DateTime.Now.AddDays(-7)).Sum(oi => (decimal?)oi.Subtotal) ?? 0m : 0m
                })
                .ToListAsync();
        }

        /// <summary>
        /// Lấy chi tiết 1 món theo ID.
        /// </summary>
        public async Task<FoodResponse?> GetByIdAsync(int id)
        {
            var f = await _context.Foods
                .Include(f => f.Orderitems)
                .ThenInclude(oi => oi.Order)
                .FirstOrDefaultAsync(f => f.FoodId == id);
            return f == null ? null : MapToResponse(f);
        }

        /// <summary>
        /// Lấy danh sách món đang được bán (IsAvailable = true và còn hàng).
        /// Quyền: Tất cả người dùng đã đăng nhập (để hiển thị menu order)
        /// </summary>
        public async Task<List<FoodResponse>> GetAvailableAsync()
        {
            return await _context.Foods
                .Where(f => f.IsAvailable && f.Quantity > 0)
                .OrderBy(f => f.Category)
                .ThenBy(f => f.FoodName)
                .Select(f => new FoodResponse
                {
                    FoodId = f.FoodId,
                    FoodName = f.FoodName,
                    Category = f.Category,
                    Price = f.Price,
                    Quantity = f.Quantity,
                    ImageUrl = f.ImageUrl,
                    IsAvailable = f.IsAvailable,
                    SoldThisMonth = 0,
                    RevenueThisMonth = 0m,
                    SoldToday = 0,
                    RevenueToday = 0m,
                    SoldThisWeek = 0,
                    RevenueThisWeek = 0m
                })
                .ToListAsync();
        }

        /// <summary>
        /// Lọc danh sách món theo danh mục (Food / Drink / Combo...).
        /// </summary>
        public async Task<List<FoodResponse>> GetByCategoryAsync(string category)
        {
            return await _context.Foods
                .Where(f => f.Category == category && f.IsAvailable)
                .OrderBy(f => f.FoodName)
                .Select(f => new FoodResponse
                {
                    FoodId = f.FoodId,
                    FoodName = f.FoodName,
                    Category = f.Category,
                    Price = f.Price,
                    Quantity = f.Quantity,
                    ImageUrl = f.ImageUrl,
                    IsAvailable = f.IsAvailable,
                    SoldThisMonth = 0,
                    RevenueThisMonth = 0m,
                    SoldToday = 0,
                    RevenueToday = 0m,
                    SoldThisWeek = 0,
                    RevenueThisWeek = 0m
                })
                .ToListAsync();
        }

        /// <summary>
        /// Thêm món ăn / đồ uống mới.
        /// Quyền: Admin
        /// </summary>
        public async Task<(bool IsSuccess, string Message, int StatusCode, FoodResponse? Data)> CreateAsync(FoodRequest request)
        {
            // Kiểm tra trùng tên trong cùng danh mục
            bool exists = await _context.Foods.AnyAsync(f =>
                f.FoodName.ToLower() == request.FoodName.ToLower().Trim() &&
                f.Category == request.Category);
            if (exists)
                return (false, FoodMessages.FoodNameAlreadyExists, 409, null);

            var food = new Food
            {
                FoodName = request.FoodName.Trim(),
                Category = request.Category?.Trim(),
                Price = request.Price,
                Quantity = request.Quantity,
                ImageUrl = request.ImageUrl?.Trim(),
                IsAvailable = request.IsAvailable
            };

            _context.Foods.Add(food);
            await _context.SaveChangesAsync();

            return (true, FoodMessages.CreateSuccess, 201, MapToResponse(food));
        }

        /// <summary>
        /// Cập nhật thông tin món ăn / đồ uống.
        /// Quyền: Chỉ Super Admin mới được sửa.
        /// </summary>
        public async Task<(bool IsSuccess, string Message, int StatusCode)> UpdateAsync(
            int id, FoodRequest request, string currentOperatorEmail)
        {
            // 1. Tìm món trong DB
            var food = await _context.Foods.FindAsync(id);
            if (food == null)
                return (false, FoodMessages.NotFoundWithId(id), 404);

            // 2. CHỐT CHẶN: Chỉ Super Admin mới được sửa
            if (currentOperatorEmail != ValidationMessages.SuperAdminEmail)
                return (false, FoodMessages.UnauthorizedUpdate, 403);

            // 3. Kiểm tra trùng tên với bản ghi KHÁC
            bool nameConflict = await _context.Foods.AnyAsync(f =>
                f.FoodName.ToLower() == request.FoodName.ToLower().Trim() &&
                f.Category == request.Category &&
                f.FoodId != id);
            if (nameConflict)
                return (false, FoodMessages.FoodNameAlreadyExists, 409);

            // 4. Áp dụng thay đổi
            food.FoodName = request.FoodName.Trim();
            food.Category = request.Category?.Trim();
            food.Price = request.Price;
            food.Quantity = request.Quantity;
            food.ImageUrl = request.ImageUrl?.Trim();
            food.IsAvailable = request.IsAvailable;

            try
            {
                await _context.SaveChangesAsync();
                return (true, FoodMessages.UpdateSuccess, 200);
            }
            catch (DbUpdateConcurrencyException)
            {
                return (false, FoodMessages.ConcurrencyError, 409);
            }
        }

        /// <summary>
        /// Xóa món ăn / đồ uống.
        /// Quyền: Chỉ Super Admin mới được xóa.
        /// Bảo vệ: Không xóa nếu món đã được dùng trong OrderItem.
        /// </summary>
        public async Task<(bool IsSuccess, string Message, int StatusCode)> DeleteAsync(
            int id, string currentOperatorEmail)
        {
            // 1. Tìm món trong DB
            var food = await _context.Foods.FindAsync(id);
            if (food == null)
                return (false, FoodMessages.NotFoundWithId(id), 404);

            // 2. CHỐT CHẶN: Chỉ Super Admin mới được xóa
            if (currentOperatorEmail != ValidationMessages.SuperAdminEmail)
                return (false, ValidationMessages.UnauthorizedDelete, 403);

            // 3. BẢO VỆ DỮ LIỆU: Không xóa nếu món đã có lịch sử được đặt
            bool hasOrders = await _context.Orderitems.AnyAsync(o => o.FoodId == id);
            if (hasOrders)
                return (false, FoodMessages.CannotDeleteUsedFood, 409);

            // 4. Tiến hành xóa
            _context.Foods.Remove(food);
            await _context.SaveChangesAsync();

            return (true, FoodMessages.DeleteSuccess, 200);
        }
        
    }
}
