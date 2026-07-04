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
    public class PaymentsController : ControllerBase
    {
        private readonly IPaymentService _paymentService;

        public PaymentsController(IPaymentService paymentService)
        {
            _paymentService = paymentService;
        }

        // GET: api/Payments  👑 (Chỉ Admin + Staff xem toàn bộ lịch sử giao dịch)
        [HttpGet]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> GetAll()
        {
            var payments = await _paymentService.GetAllAsync();
            return Ok(payments);
        }

        // GET: api/Payments/{id}  👑 (Admin + Staff xem chi tiết 1 giao dịch)
        [HttpGet("{id}")]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> GetById(int id)
        {
            var payment = await _paymentService.GetByIdAsync(id);
            if (payment == null)
                return NotFound(new { Message = PaymentMessages.NotFoundWithId(id) });
            return Ok(payment);
        }

        // GET: api/Payments/ByUser/{userId}  🛡️ (Khách xem lịch sử thanh toán của chính mình)
        [HttpGet("ByUser/{userId}")]
        public async Task<IActionResult> GetByUser(int userId)
        {
            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var currentRole   = User.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;

            if (currentRole == RoleConstants.Customer && userId != currentUserId)
                return StatusCode(403, new { Message = PaymentMessages.UnauthorizedPayment });

            var payments = await _paymentService.GetByUserAsync(userId, currentUserId, currentRole);
            return Ok(payments);
        }

        // GET: api/Payments/ByBooking/{bookingId}  👑 (Admin + Staff tra cứu thanh toán theo đơn vé)
        [HttpGet("ByBooking/{bookingId}")]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> GetByBooking(int bookingId)
        {
            var payments = await _paymentService.GetByBookingAsync(bookingId);
            return Ok(payments);
        }

        // GET: api/Payments/RevenueByMovie  👑 (Admin + Staff xem doanh thu theo phim từ View)
        [HttpGet("RevenueByMovie")]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> GetRevenueByMovie()
        {
            // Vẫn giữ lại tính năng cũ dùng View VW_REVENUE_BY_MOVIE
            // (Sẽ chuyển vào PaymentService nếu cần)
            return Ok();
        }

        // POST: api/Payments  🔓 (Tất cả user đã đăng nhập tạo thanh toán)
        // Luồng: Đặt vé xong → gọi endpoint này để hoàn tất thanh toán
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] PaymentRequest request)
        {
            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var currentRole   = User.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;

            var (isSuccess, message, statusCode, data) = await _paymentService.CreateAsync(request, currentUserId, currentRole);

            if (!isSuccess)
                return StatusCode(statusCode, new { Message = message });

            return StatusCode(statusCode, new { Message = message, Data = data });
        }

        // PATCH: api/Payments/{id}/Status  👑 (Admin + Staff xác nhận / huỷ giao dịch)
        [HttpPatch("{id}/Status")]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] PaymentStatusRequest request)
        {
            var currentRole = User.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;

            var (isSuccess, message, statusCode) = await _paymentService.UpdateStatusAsync(id, request, currentRole);

            if (!isSuccess)
                return StatusCode(statusCode, new { Message = message });

            return Ok(new { Message = message });
        }
    }
}
