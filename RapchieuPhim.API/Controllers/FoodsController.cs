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
    public class FoodsController : ControllerBase
    {
        private readonly IFoodService _foodService;

        public FoodsController(IFoodService foodService)
        {
            _foodService = foodService;
        }

        // GET: api/Foods  👑 (Admin + Staff xem toàn bộ kể cả hàng đã ngưng bán)
        [HttpGet]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> GetAll()
        {
            var foods = await _foodService.GetAllAsync();
            return Ok(foods);
        }

        // GET: api/Foods/{id}
        [HttpGet("{id}")]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> GetById(int id)
        {
            var food = await _foodService.GetByIdAsync(id);
            if (food == null)
                return NotFound(new { Message = FoodMessages.NotFoundWithId(id) });
            return Ok(food);
        }

        // GET: api/Foods/Available  🔓 (Tất cả user đăng nhập xem menu để chọn order)
        [HttpGet("Available")]
        public async Task<IActionResult> GetAvailable()
        {
            var foods = await _foodService.GetAvailableAsync();
            return Ok(foods);
        }

        // GET: api/Foods/ByCategory/{category}  🔓 (Lọc theo danh mục: Food, Drink, Combo...)
        [HttpGet("ByCategory/{category}")]
        public async Task<IActionResult> GetByCategory(string category)
        {
            var foods = await _foodService.GetByCategoryAsync(category);
            return Ok(foods);
        }

        // POST: api/Foods  👑 (Chỉ Admin mới được thêm món)
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([FromBody] FoodRequest request)
        {
            var (isSuccess, message, statusCode, data) = await _foodService.CreateAsync(request);

            if (!isSuccess)
                return StatusCode(statusCode, new { Message = message });

            return StatusCode(statusCode, new { Message = message, Data = data });
        }

        // PUT: api/Foods/{id}  👑 (Chỉ Super Admin mới được sửa)
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(int id, [FromBody] FoodRequest request)
        {
            var email = User.FindFirst(ClaimTypes.Email)?.Value ?? string.Empty;

            var (isSuccess, message, statusCode) = await _foodService.UpdateAsync(id, request, email);

            if (!isSuccess)
                return StatusCode(statusCode, new { Message = message });

            return Ok(new { Message = message });
        }

        // DELETE: api/Foods/{id}  👑 (Chỉ Super Admin mới được xóa)
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var email = User.FindFirst(ClaimTypes.Email)?.Value ?? string.Empty;

            var (isSuccess, message, statusCode) = await _foodService.DeleteAsync(id, email);

            if (!isSuccess)
                return StatusCode(statusCode, new { Message = message });

            return Ok(new { Message = message });
        }
    }
}
