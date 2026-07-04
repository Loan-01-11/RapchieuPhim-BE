using Microsoft.EntityFrameworkCore;
using RapchieuPhim.API.Constants;
using RapchieuPhim.API.DTOs.DTORequest;
using RapchieuPhim.API.DTOs.DTOResponse;
using RapchieuPhim.API.Models;

namespace RapchieuPhim.API.Services
{
    // ─── GIAO TIẾP (Interface) ────────────────────────────────────────────────────
    public interface IComboService
    {
        Task<List<ComboResponse>> GetAllAsync();
        Task<ComboResponse?> GetByIdAsync(int id);
        Task<List<ComboResponse>> GetAvailableAsync();
        Task<(bool IsSuccess, string Message, int StatusCode, ComboResponse? Data)> CreateAsync(ComboRequest request);
        Task<(bool IsSuccess, string Message, int StatusCode)> UpdateAsync(int id, ComboRequest request, string currentOperatorEmail);
        Task<(bool IsSuccess, string Message, int StatusCode)> DeleteAsync(int id, string currentOperatorEmail);
        Task<(bool IsSuccess, string Message, int StatusCode)> AddFoodToComboAsync(int comboId, ComboFoodItemRequest request, string currentOperatorEmail);
        Task<(bool IsSuccess, string Message, int StatusCode)> RemoveFoodFromComboAsync(int comboId, int foodId, string currentOperatorEmail);
        Task<(bool IsSuccess, string Message, int StatusCode)> UpdateFoodQuantityAsync(int comboId, int foodId, int newQuantity, string currentOperatorEmail);
    }

    // ─── TRIỂN KHAI (Implementation) ─────────────────────────────────────────────
    public class ComboService : IComboService
    {
        private readonly CinemaManagementContext _context;

        public ComboService(CinemaManagementContext context)
        {
            _context = context;
        }

        // ── Helper: Ánh xạ Entity → Response DTO ─────────────────────────────────
        private static ComboResponse MapToResponse(Combo c) => new()
        {
            ComboId     = c.ComboId,
            ComboName   = c.ComboName,
            Price       = c.Price,
            Description = c.Description,
            ImageUrl    = c.ImageUrl,
            Quantity    = c.Quantity,
            IsAvailable = c.IsAvailable,
            FoodItems   = c.Combofoodmappings.Select(m => new ComboFoodItemResponse
            {
                FoodId    = m.FoodId,
                FoodName  = m.Food.FoodName,
                Category  = m.Food.Category,
                UnitPrice = m.Food.Price,
                Quantity  = m.Quantity
            }).ToList()
        };

        // ── QUERY CÓ INCLUDE EAGER LOADING ────────────────────────────────────────
        private IQueryable<Combo> QueryWithFoods() =>
            _context.Combos
                .Include(c => c.Combofoodmappings)
                    .ThenInclude(m => m.Food);

        /// <summary>
        /// Lấy toàn bộ danh sách combo kèm danh sách món bên trong.
        /// Quyền: Admin, Staff
        /// </summary>
        public async Task<List<ComboResponse>> GetAllAsync()
        {
            var combos = await QueryWithFoods()
                .OrderBy(c => c.ComboName)
                .ToListAsync();
            return combos.Select(MapToResponse).ToList();
        }

        /// <summary>
        /// Lấy chi tiết 1 combo theo ID kèm danh sách món.
        /// </summary>
        public async Task<ComboResponse?> GetByIdAsync(int id)
        {
            var combo = await QueryWithFoods()
                .FirstOrDefaultAsync(c => c.ComboId == id);
            return combo == null ? null : MapToResponse(combo);
        }

        /// <summary>
        /// Lấy danh sách combo đang bán (IsAvailable = true và còn hàng).
        /// Quyền: Tất cả người dùng đã đăng nhập.
        /// </summary>
        public async Task<List<ComboResponse>> GetAvailableAsync()
        {
            var combos = await QueryWithFoods()
                .Where(c => c.IsAvailable && c.Quantity > 0)
                .OrderBy(c => c.ComboName)
                .ToListAsync();
            return combos.Select(MapToResponse).ToList();
        }

        /// <summary>
        /// Tạo combo mới, có thể kèm danh sách món ngay khi tạo.
        /// Quyền: Admin
        /// </summary>
        public async Task<(bool IsSuccess, string Message, int StatusCode, ComboResponse? Data)> CreateAsync(ComboRequest request)
        {
            // 1. Kiểm tra trùng tên combo
            bool exists = await _context.Combos.AnyAsync(c =>
                c.ComboName.ToLower() == request.ComboName.ToLower().Trim());
            if (exists)
                return (false, ComboMessages.ComboNameAlreadyExists, 409, null);

            // 2. Tạo Combo
            var combo = new Combo
            {
                ComboName   = request.ComboName.Trim(),
                Price       = request.Price,
                Description = request.Description?.Trim(),
                ImageUrl    = request.ImageUrl?.Trim(),
                Quantity    = request.Quantity,
                IsAvailable = request.IsAvailable
            };
            _context.Combos.Add(combo);
            await _context.SaveChangesAsync(); // Lấy ComboId mới

            // 3. Thêm mapping các món vào combo (nếu có)
            if (request.FoodItems != null && request.FoodItems.Any())
            {
                foreach (var item in request.FoodItems)
                {
                    // Kiểm tra Food tồn tại
                    bool foodExists = await _context.Foods.AnyAsync(f => f.FoodId == item.FoodId);
                    if (!foodExists)
                        return (false, FoodMessages.NotFoundWithId(item.FoodId), 404, null);

                    _context.Combofoodmappings.Add(new Combofoodmapping
                    {
                        ComboId  = combo.ComboId,
                        FoodId   = item.FoodId,
                        Quantity = item.Quantity
                    });
                }
                await _context.SaveChangesAsync();
            }

            // 4. Load lại combo đầy đủ để trả về response
            var created = await QueryWithFoods().FirstOrDefaultAsync(c => c.ComboId == combo.ComboId);
            return (true, ComboMessages.CreateSuccess, 201, created == null ? null : MapToResponse(created));
        }

        /// <summary>
        /// Cập nhật thông tin combo (không bao gồm danh sách món — dùng endpoint riêng).
        /// Quyền: Chỉ Super Admin.
        /// </summary>
        public async Task<(bool IsSuccess, string Message, int StatusCode)> UpdateAsync(
            int id, ComboRequest request, string currentOperatorEmail)
        {
            var combo = await _context.Combos.FindAsync(id);
            if (combo == null)
                return (false, ComboMessages.NotFoundWithId(id), 404);

            if (currentOperatorEmail != ValidationMessages.SuperAdminEmail)
                return (false, ComboMessages.UnauthorizedModify, 403);

            // Kiểm tra trùng tên với bản ghi KHÁC
            bool nameConflict = await _context.Combos.AnyAsync(c =>
                c.ComboName.ToLower() == request.ComboName.ToLower().Trim() &&
                c.ComboId != id);
            if (nameConflict)
                return (false, ComboMessages.ComboNameAlreadyExists, 409);

            combo.ComboName   = request.ComboName.Trim();
            combo.Price       = request.Price;
            combo.Description = request.Description?.Trim();
            combo.ImageUrl    = request.ImageUrl?.Trim();
            combo.Quantity    = request.Quantity;
            combo.IsAvailable = request.IsAvailable;

            // Nếu request có kèm FoodItems → thay thế toàn bộ danh sách món
            if (request.FoodItems != null)
            {
                var oldMappings = _context.Combofoodmappings.Where(m => m.ComboId == id);
                _context.Combofoodmappings.RemoveRange(oldMappings);

                foreach (var item in request.FoodItems)
                {
                    bool foodExists = await _context.Foods.AnyAsync(f => f.FoodId == item.FoodId);
                    if (!foodExists)
                        return (false, FoodMessages.NotFoundWithId(item.FoodId), 404);

                    _context.Combofoodmappings.Add(new Combofoodmapping
                    {
                        ComboId  = id,
                        FoodId   = item.FoodId,
                        Quantity = item.Quantity
                    });
                }
            }

            try
            {
                await _context.SaveChangesAsync();
                return (true, ComboMessages.UpdateSuccess, 200);
            }
            catch (DbUpdateConcurrencyException)
            {
                return (false, ComboMessages.ConcurrencyError, 409);
            }
        }

        /// <summary>
        /// Xóa combo.
        /// Quyền: Chỉ Super Admin.
        /// Bảo vệ: Không xóa nếu combo đã xuất hiện trong OrderItem.
        /// </summary>
        public async Task<(bool IsSuccess, string Message, int StatusCode)> DeleteAsync(
            int id, string currentOperatorEmail)
        {
            var combo = await _context.Combos.FindAsync(id);
            if (combo == null)
                return (false, ComboMessages.NotFoundWithId(id), 404);

            if (currentOperatorEmail != ValidationMessages.SuperAdminEmail)
                return (false, ValidationMessages.UnauthorizedDelete, 403);

            bool hasOrders = await _context.Orderitems.AnyAsync(o => o.ComboId == id);
            if (hasOrders)
                return (false, ComboMessages.CannotDeleteUsedCombo, 409);

            // Xóa mapping trước, rồi xóa combo (tránh FK constraint)
            var mappings = _context.Combofoodmappings.Where(m => m.ComboId == id);
            _context.Combofoodmappings.RemoveRange(mappings);
            _context.Combos.Remove(combo);
            await _context.SaveChangesAsync();

            return (true, ComboMessages.DeleteSuccess, 200);
        }

        /// <summary>
        /// Thêm 1 món vào combo đã có.
        /// Quyền: Chỉ Super Admin.
        /// </summary>
        public async Task<(bool IsSuccess, string Message, int StatusCode)> AddFoodToComboAsync(
            int comboId, ComboFoodItemRequest request, string currentOperatorEmail)
        {
            if (currentOperatorEmail != ValidationMessages.SuperAdminEmail)
                return (false, ComboMessages.UnauthorizedModify, 403);

            var combo = await _context.Combos.FindAsync(comboId);
            if (combo == null)
                return (false, ComboMessages.NotFoundWithId(comboId), 404);

            bool foodExists = await _context.Foods.AnyAsync(f => f.FoodId == request.FoodId);
            if (!foodExists)
                return (false, FoodMessages.NotFoundWithId(request.FoodId), 404);

            // Kiểm tra món này đã có trong combo chưa
            bool alreadyMapped = await _context.Combofoodmappings.AnyAsync(m =>
                m.ComboId == comboId && m.FoodId == request.FoodId);
            if (alreadyMapped)
                return (false, ComboMessages.FoodAlreadyInCombo, 409);

            _context.Combofoodmappings.Add(new Combofoodmapping
            {
                ComboId  = comboId,
                FoodId   = request.FoodId,
                Quantity = request.Quantity
            });
            await _context.SaveChangesAsync();

            return (true, ComboMessages.AddFoodSuccess, 200);
        }

        /// <summary>
        /// Xóa 1 món khỏi combo.
        /// Quyền: Chỉ Super Admin.
        /// </summary>
        public async Task<(bool IsSuccess, string Message, int StatusCode)> RemoveFoodFromComboAsync(
            int comboId, int foodId, string currentOperatorEmail)
        {
            if (currentOperatorEmail != ValidationMessages.SuperAdminEmail)
                return (false, ComboMessages.UnauthorizedModify, 403);

            var mapping = await _context.Combofoodmappings
                .FirstOrDefaultAsync(m => m.ComboId == comboId && m.FoodId == foodId);
            if (mapping == null)
                return (false, ComboMessages.FoodNotInCombo, 404);

            _context.Combofoodmappings.Remove(mapping);
            await _context.SaveChangesAsync();

            return (true, ComboMessages.RemoveFoodSuccess, 200);
        }

        /// <summary>
        /// Cập nhật số lượng của 1 món trong combo.
        /// Quyền: Chỉ Super Admin.
        /// </summary>
        public async Task<(bool IsSuccess, string Message, int StatusCode)> UpdateFoodQuantityAsync(
            int comboId, int foodId, int newQuantity, string currentOperatorEmail)
        {
            if (currentOperatorEmail != ValidationMessages.SuperAdminEmail)
                return (false, ComboMessages.UnauthorizedModify, 403);

            if (newQuantity < 1)
                return (false, ComboMessages.FoodQuantityInvalid, 400);

            var mapping = await _context.Combofoodmappings
                .FirstOrDefaultAsync(m => m.ComboId == comboId && m.FoodId == foodId);
            if (mapping == null)
                return (false, ComboMessages.FoodNotInCombo, 404);

            mapping.Quantity = newQuantity;
            await _context.SaveChangesAsync();

            return (true, ComboMessages.UpdateFoodQuantitySuccess, 200);
        }
    }
}
