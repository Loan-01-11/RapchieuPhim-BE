using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RapchieuPhim.API.Constants;
using RapchieuPhim.API.DTO.DTORequest;
using RapchieuPhim.API.Services;
using System.Security.Claims;

namespace RapchieuPhim.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // 🔐 Phải đăng nhập mới được gọi API đơn hàng
    public class OrdersController : ControllerBase
    {
        private readonly IOrderService _orderService;

        public OrdersController(IOrderService orderService)
        {
            _orderService = orderService;
        }

        // GET: api/Orders  👑 (Chỉ Admin + Staff xem toàn bộ đơn hàng)
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAll([FromQuery] string? date)
        {
            var orders = await _orderService.GetAllAsync(date);
            return Ok(orders);
        }

        // GET: api/Orders/{id}  👑 (Admin + Staff xem chi tiết 1 đơn hàng)
        [HttpGet("{id}")]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> GetById(int id)
        {
            var order = await _orderService.GetByIdAsync(id);
            if (order == null)
                return NotFound(new { Message = OrderMessages.NotFoundWithId(id) });
            return Ok(order);
        }

        // GET: api/Orders/ByUser/{userId}  🛡️ (Khách xem lịch sử đơn hàng của chính mình)
        [HttpGet("ByUser/{userId}")]
        public async Task<IActionResult> GetByUser(int userId)
        {
            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var currentRole   = User.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;

            // Bảo mật: Khách hàng chỉ được xem đơn của chính họ
            if (currentRole == RoleConstants.Customer && userId != currentUserId)
                return StatusCode(403, new { Message = OrderMessages.UnauthorizedView });

            var orders = await _orderService.GetByUserAsync(userId, currentUserId, currentRole);
            return Ok(orders);
        }

        // GET: api/Orders/ByBooking/{bookingId}  👑 (Admin + Staff tra cứu đồ ăn theo đơn vé)
        [HttpGet("ByBooking/{bookingId}")]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> GetByBooking(int bookingId)
        {
            var orders = await _orderService.GetByBookingAsync(bookingId);
            return Ok(orders);
        }

        // POST: api/Orders  🔓 (Tất cả user đã đăng nhập đều có thể tạo đơn đồ ăn)
        // Luồng: Khách chọn bắp/nước → Gọi endpoint này → Nhận lại tổng tiền để thanh toán
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] OrderCreateRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var currentRole   = User.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;

            var (isSuccess, message, statusCode, data) = await _orderService.CreateAsync(request, currentUserId, currentRole);

            if (!isSuccess)
                return StatusCode(statusCode, new { Message = message });

            return StatusCode(statusCode, new { Message = message, Data = data });
        }

        // PATCH: api/Orders/{id}/Status  👑 (Admin + Staff xác nhận hoặc hủy đơn hàng)
        [HttpPatch("{id}/Status")]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] OrderStatusRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var currentRole = User.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;

            var (isSuccess, message, statusCode) = await _orderService.UpdateStatusAsync(id, request, currentRole);

            if (!isSuccess)
                return StatusCode(statusCode, new { Message = message });

            return Ok(new { Message = message });
        }

        // DELETE: api/Orders/{id}  🛡️ (Khách tự hủy đơn khi còn Pending; Admin/Staff hủy bất kỳ)
        [HttpDelete("{id}")]
        public async Task<IActionResult> Cancel(int id)
        {
            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var currentRole   = User.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;

            var (isSuccess, message, statusCode) = await _orderService.CancelAsync(id, currentUserId, currentRole);

            if (!isSuccess)
                return StatusCode(statusCode, new { Message = message });

            return Ok(new { Message = message });
        }
    }
}
