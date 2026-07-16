using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RapchieuPhim.API.Constants;      // Dùng hệ thống hằng số sạch
using RapchieuPhim.API.DTOs.DTORequest; // Khay hứng dữ liệu vào
using RapchieuPhim.API.DTOs.DTOResponse;// Khay trả dữ liệu ra
using RapchieuPhim.API.Services;      // Tầng giao tiếp Service

namespace RapchieuPhim.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // 🔐 KHÓA TỔNG: Tất cả mọi người bắt buộc phải đăng nhập mới được sờ vào Vé
    public class TicketsController : ControllerBase
    {
        private readonly ITicketService _ticketService;

        public TicketsController(ITicketService ticketService)
        {
            _ticketService = ticketService;
        }

        // GET: api/Tickets 👑 (CHỈ ADMIN VÀ STAFF MỚI ĐƯỢC XEM TOÀN BỘ VÉ)
        [HttpGet]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> GetAll()
        {
            var tickets = await _ticketService.GetAllAsync();
            return Ok(tickets);
        }

        // GET: api/Tickets/{id} 👑 (CHỈ ADMIN VÀ STAFF MỚI ĐƯỢC XEM THEO ID)
        [HttpGet("{id}")]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> GetById(int id)
        {
            var ticket = await _ticketService.GetByIdAsync(id);
            if (ticket == null)
                return NotFound(new { Message = ValidationMessages.TicketNotFoundWithId(id) });

            return Ok(ticket);
        }

        // GET: api/Tickets/ByCode/{ticketCode} 🛡️ (QUÉT MÃ VÉ: Dành cho Admin và Nhân viên tại cửa soát vé)
        [HttpGet("ByCode/{ticketCode}")]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> GetByCode(string ticketCode)
        {
            var ticket = await _ticketService.GetByCodeAsync(ticketCode);
            if (ticket == null)
                return NotFound(new { Message = ValidationMessages.TicketNotFoundWithCode(ticketCode) });

            return Ok(ticket);
        }

        // GET: api/Tickets/ByBooking/{bookingId} 🔓 (Mọi vai trò đều xem được vé của đơn Booking)
        [HttpGet("ByBooking/{bookingId}")]
        public async Task<IActionResult> GetByBooking(int bookingId)
        {
            var tickets = await _ticketService.GetByBookingAsync(bookingId);
            return Ok(tickets);
        }

        // POST: api/Tickets 👑 (CHỈ ADMIN MỚI ĐƯỢC PHÉP IN VÉ THỦ CÔNG)
        [HttpPost]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> Create([FromBody] TicketCreateRequest request)
        {
            // Gọi xuống tầng Service để xử lý logic thêm vé và lưu DB
            var result = await _ticketService.CreateAsync(request);
            return CreatedAtAction(nameof(GetById), new { id = result.TicketId }, result);
        }

        // PUT: api/Tickets/{id}/Status 🛡️ (NHÂN VIÊN SOÁT VÉ bấm đổi trạng thái thành 'Used' khi khách vào phòng)
        [HttpPut("{id}/Status")]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] TicketStatusRequest request)
        {
            var result = await _ticketService.UpdateStatusAsync(id, request);

            if (!result.IsSuccess)
                return StatusCode(result.StatusCode, new { Message = result.Message });

            return Ok(new { Message = result.Message });
        }

        // POST: api/Tickets/Scan 🎫 (NHÂN VIÊN QUÉT MÃ QR SOÁT VÉ TẠI CẬA PHÒNG CHIếu)
        // - Vé Active  → Cho vào → Đổi sang "Used"
        // - Vé Used    → Từ chối (Vé đã dùng rồi)
        // - Vé Pending → Từ chối (Chưa thanh toán)
        // - Vé Cancelled→ Từ chối (Vé bị hủy)
        [HttpPost("Scan")]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> ScanTicket([FromBody] ScanTicketRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.TicketCode))
                return BadRequest(new { Message = ValidationMessages.TicketCodeRequired });

            var result = await _ticketService.ScanTicketAsync(request.TicketCode);

            return StatusCode(result.StatusCode, new
            {
                IsSuccess = result.IsSuccess,
                Message   = result.Message,
                Ticket    = result.Ticket
            });
        }
    }
}