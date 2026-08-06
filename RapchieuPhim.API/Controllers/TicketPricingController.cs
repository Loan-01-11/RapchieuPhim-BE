using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RapchieuPhim.API.Constants;      // Dùng hệ thống hằng hố sạch
using RapchieuPhim.API.DTOs.DTORequest; // Khay hứng dữ liệu vào sạch rác Swagger
using RapchieuPhim.API.DTOs.DTOResponse;// Khay trả dữ liệu ra
using RapchieuPhim.API.Services;      // Giao tiếp Service lớp nghiệp vụ
using System.Security.Claims;

namespace RapchieuPhim.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // 🔐 KHÓA TỔNG: Bắt buộc phải đăng nhập (có Token) mới được tương tác với cấu hình giá vé
    public class TicketPricingController : ControllerBase
    {
        private readonly ITicketPricingService _pricingService;

        // Tiêm Service thông qua hàm khởi tạo Constructor
        public TicketPricingController(ITicketPricingService pricingService)
        {
            _pricingService = pricingService;
        }

        // GET: api/TicketPricing 👑 (CHỈ ADMIN VÀ STAFF MỚI ĐƯỢC XEM TOÀN BỘ MA TRẬN GIÁ)
        [HttpGet]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> GetAll()
        {
            var pricing = await _pricingService.GetAllAsync();
            return Ok(pricing);
        }

        // GET: api/TicketPricing/{id} 👑 (CHỈ ADMIN VÀ STAFF MỚI ĐƯỢC XEM CHI TIẾT)
        [HttpGet("{id}")]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> GetById(int id)
        {
            var pricing = await _pricingService.GetByIdAsync(id);
            if (pricing == null)
                return NotFound(new { Message = ValidationMessages.PricingNotFoundWithId(id) });

            return Ok(pricing);
        }

        // GET: api/TicketPricing/Active 🔓 (TẤT CẢ MỌI NGƯỜI ĐÃ ĐĂNG NHẬP ĐỀU XEM ĐƯỢC ĐỂ TÍNH TIỀN VÉ)
        [HttpGet("Active")]
        public async Task<IActionResult> GetActive()
        {
            var pricing = await _pricingService.GetActiveAsync();
            return Ok(pricing);
        }

        [HttpGet("Room/{roomId:int}")]
        public async Task<IActionResult> GetByRoom(int roomId)
            => Ok(await _pricingService.GetByRoomAsync(roomId));

        [HttpPut("Room/{roomId:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateRoomPrices(int roomId, [FromBody] RoomTicketPricingBulkRequest request)
        {
            var idValue = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
            if (!int.TryParse(idValue, out var operatorId)) return Unauthorized();
            var result = await _pricingService.UpdateRoomPricesAsync(roomId, request, operatorId);
            return result.IsSuccess
                ? Ok(new { message = result.Message, data = result.Data })
                : StatusCode(result.StatusCode, new { message = result.Message });
        }

        // POST: api/TicketPricing 👑 (CHỈ ADMIN MỚI ĐƯỢC TẠO QUY TẮC GIÁ MỚI)
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([FromBody] TicketPricingRequest request)
        {
            // Bóc tách Token lấy ID của ông Admin đang ngồi click tạo bảng giá để gán vào cột CreatedBy dưới DB
            var creatorIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
            int creatorId = string.IsNullOrEmpty(creatorIdClaim) ? 0 : int.Parse(creatorIdClaim);

            var result = await _pricingService.CreateAsync(request, creatorId);
            return CreatedAtAction(nameof(GetById), new { id = result.PricingId }, result);
        }

        // PUT: api/TicketPricing/{id} 👑 (CHỈ ADMIN TỐI CAO ĐƯỢC PHÉP CHỈNH SỬA)
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")] // Admin thường lọt cửa 1 nhưng dính bẫy email SuperAdmin ở tầng Service
        public async Task<IActionResult> Update(int id, [FromBody] TicketPricingRequest request)
        {
            // Bóc Token lấy Email của tài khoản đang bấm nút Lưu cấu hình
            var currentOperatorEmail = User.FindFirst(ClaimTypes.Email)?.Value;

            var result = await _pricingService.UpdateAsync(id, request, currentOperatorEmail ?? string.Empty);

            if (!result.IsSuccess)
                return StatusCode(result.StatusCode, new { Message = result.Message });

            return Ok(new { Message = result.Message });
        }

        // DELETE: api/TicketPricing/{id} 👑 (CHỈ ADMIN TỐI CAO ĐƯỢC PHÉP XÓA)
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            // Bóc Token lấy Email của tài khoản đang bấm nút Xóa cấu hình
            var currentOperatorEmail = User.FindFirst(ClaimTypes.Email)?.Value;

            var result = await _pricingService.DeleteAsync(id, currentOperatorEmail ?? string.Empty);

            if (!result.IsSuccess)
                return StatusCode(result.StatusCode, new { Message = result.Message });

            return Ok(new { Message = result.Message });
        }
    }
}
