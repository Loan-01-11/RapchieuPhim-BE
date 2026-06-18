using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RapchieuPhim.API.Constants; // Sử dụng ValidationMessages hằng số sạch
using RapchieuPhim.API.DTO.DTORequest;
using RapchieuPhim.API.DTOs;      // Sử dụng AreaRequest và AreaResponse DTO
using RapchieuPhim.API.Services;  // Sử dụng giao tiếp IAreaService
using System.Security.Claims;

namespace RapchieuPhim.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // 🔐 KHÓA TỔNG: Bắt buộc tất cả các hàm ở dưới phải có Token đăng nhập mới được gọi
    public class AreasController : ControllerBase
    {
        private readonly IAreaService _areaService;

        // Tiêm (Inject) Service vào thông qua hàm khởi tạo Constructor
        public System.Security.Claims.ClaimsPrincipal currentUser => User;

        public AreasController(IAreaService areaService)
        {
            _areaService = areaService;
        }

        // GET: api/Areas (Tất cả thành viên đã đăng nhập đều có quyền xem)
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var areas = await _areaService.GetAllAsync();
            return Ok(areas);
        }

        // GET: api/Areas/{id} (Tất cả thành viên đã đăng nhập đều có quyền xem chi tiết)
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var area = await _areaService.GetByIdAsync(id);
            if (area == null)
                return NotFound(new { Message = ValidationMessages.AreaNotFoundWithId(id) });

            return Ok(area);
        }

        // POST: api/Areas 👑 (CHỈ ADMIN MỚI CÓ QUYỀN TẠO MỚI)
        [HttpPost]
        [Authorize(Roles = "Admin")] // Chốt chặn phân quyền theo vai trò (Role-Based Authorization)
        public async Task<IActionResult> Create([FromBody] AreaRequest request)
        {
            var result = await _areaService.CreateAsync(request);

            // Nếu Service báo thất bại (ví dụ: trùng tên khu vực)
            if (!result.IsSuccess)
                return StatusCode(result.StatusCode, new { Message = result.Message });

            // Trả về mã thành công RESTful chuẩn 201 Created kèm dữ liệu sạch
            return CreatedAtAction(nameof(GetById), new { id = result.Data!.AreaId }, result.Data);
        }

        // PUT: api/Areas/{id} 👑 (CHỈ ADMIN MỚI CÓ QUYỀN SỬA)
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")] // Chốt chặn phân quyền
        public async Task<IActionResult> Update(int id, [FromBody] AreaRequest request)
        {
            var result = await _areaService.UpdateAsync(id, request);

            // Nếu có lỗi (404 không thấy hoặc 400 dính tên trùng lặp với thằng khác)
            if (!result.IsSuccess)
                return StatusCode(result.StatusCode, new { Message = result.Message });

            return Ok(new { Message = result.Message });
        }

        // DELETE: api/Areas/{id} 👑 (CHỈ ADMIN TỐI CAO MỚI ĐƯỢC XÓA)
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")] // Admin thường lọt qua cửa này, nhưng sẽ bị khóa chặt ở Service dưới
        public async Task<IActionResult> Delete(int id)
        {
            // 1. Bóc Token lấy Email của ông Admin đang ngồi click bấm nút xóa
            var currentOperatorEmail = User.FindFirst(ClaimTypes.Email)?.Value;

            // 2. Chuyển ID và Email xuống Service thẩm định quyền lực tối cao
            var result = await _areaService.DeleteAsync(id, currentOperatorEmail ?? string.Empty);

            // 3. Nếu không phải Super Admin (Email không khớp hằng số) ➔ Service trả lỗi 403 Forbidden
            if (!result.IsSuccess)
                return StatusCode(result.StatusCode, new { Message = result.Message });

            return Ok(new { Message = result.Message });
        }
    }
}