using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RapchieuPhim.API.Constants;
using RapchieuPhim.API.DTOs.DTORequest;
using RapchieuPhim.API.Services;
using System.Security.Claims;

namespace RapchieuPhim.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // 🔐 Toàn bộ controller yêu cầu đăng nhập
    public class CombosController : ControllerBase
    {
        private readonly IComboService _comboService;

        public CombosController(IComboService comboService)
        {
            _comboService = comboService;
        }

        // ─────────────────────────────────────────────────────────────────────────
        // PHẦN 1 — QUẢN LÝ COMBO
        // ─────────────────────────────────────────────────────────────────────────

        // GET: api/Combos  👑 (Admin + Staff xem tất cả kể cả combo ngưng bán)
        [HttpGet]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> GetAll()
        {
            var combos = await _comboService.GetAllAsync();
            return Ok(combos);
        }

        // GET: api/Combos/{id}  👑 (Admin + Staff xem chi tiết)
        [HttpGet("{id}")]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> GetById(int id)
        {
            var combo = await _comboService.GetByIdAsync(id);
            if (combo == null)
                return NotFound(new { Message = ComboMessages.NotFoundWithId(id) });
            return Ok(combo);
        }

        // GET: api/Combos/Available  🔓 (Khách hàng xem combo đang bán để chọn order)
        [HttpGet("Available")]
        public async Task<IActionResult> GetAvailable()
        {
            var combos = await _comboService.GetAvailableAsync();
            return Ok(combos);
        }

        // POST: api/Combos  👑 (Admin tạo combo mới, có thể kèm danh sách món ngay)
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([FromBody] ComboRequest request)
        {
            var (isSuccess, message, statusCode, data) = await _comboService.CreateAsync(request);

            if (!isSuccess)
                return StatusCode(statusCode, new { Message = message });

            return StatusCode(statusCode, new { Message = message, Data = data });
        }

        // PUT: api/Combos/{id}  👑 (Super Admin cập nhật thông tin + có thể thay toàn bộ danh sách món)
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(int id, [FromBody] ComboRequest request)
        {
            var email = User.FindFirst(ClaimTypes.Email)?.Value ?? string.Empty;

            var (isSuccess, message, statusCode) = await _comboService.UpdateAsync(id, request, email);

            if (!isSuccess)
                return StatusCode(statusCode, new { Message = message });

            return Ok(new { Message = message });
        }

        // DELETE: api/Combos/{id}  👑 (Super Admin xóa combo — bảo vệ nếu đã có đơn hàng)
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var email = User.FindFirst(ClaimTypes.Email)?.Value ?? string.Empty;

            var (isSuccess, message, statusCode) = await _comboService.DeleteAsync(id, email);

            if (!isSuccess)
                return StatusCode(statusCode, new { Message = message });

            return Ok(new { Message = message });
        }

        // ─────────────────────────────────────────────────────────────────────────
        // PHẦN 2 — QUẢN LÝ COMBOFOODMAPPING (Các món trong Combo)
        // ─────────────────────────────────────────────────────────────────────────

        // POST: api/Combos/{comboId}/Foods  👑 (Thêm 1 món vào combo đã tồn tại)
        [HttpPost("{comboId}/Foods")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AddFood(int comboId, [FromBody] ComboFoodItemRequest request)
        {
            var email = User.FindFirst(ClaimTypes.Email)?.Value ?? string.Empty;

            var (isSuccess, message, statusCode) = await _comboService.AddFoodToComboAsync(comboId, request, email);

            if (!isSuccess)
                return StatusCode(statusCode, new { Message = message });

            return Ok(new { Message = message });
        }

        // DELETE: api/Combos/{comboId}/Foods/{foodId}  👑 (Xóa 1 món khỏi combo)
        [HttpDelete("{comboId}/Foods/{foodId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RemoveFood(int comboId, int foodId)
        {
            var email = User.FindFirst(ClaimTypes.Email)?.Value ?? string.Empty;

            var (isSuccess, message, statusCode) = await _comboService.RemoveFoodFromComboAsync(comboId, foodId, email);

            if (!isSuccess)
                return StatusCode(statusCode, new { Message = message });

            return Ok(new { Message = message });
        }

        // PATCH: api/Combos/{comboId}/Foods/{foodId}/Quantity  👑 (Cập nhật số lượng của 1 món trong combo)
        [HttpPatch("{comboId}/Foods/{foodId}/Quantity")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateFoodQuantity(int comboId, int foodId, [FromQuery] int quantity)
        {
            var email = User.FindFirst(ClaimTypes.Email)?.Value ?? string.Empty;

            var (isSuccess, message, statusCode) = await _comboService.UpdateFoodQuantityAsync(comboId, foodId, quantity, email);

            if (!isSuccess)
                return StatusCode(statusCode, new { Message = message });

            return Ok(new { Message = message });
        }
    }
}
