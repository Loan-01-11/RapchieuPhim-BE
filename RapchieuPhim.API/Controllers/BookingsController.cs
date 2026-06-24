using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RapchieuPhim.API.Constants;      // Hằng số sạch
using RapchieuPhim.API.DTOs.DTORequest; // Khay hứng đầu vào gọn gàng
using RapchieuPhim.API.Services;      // Tầng giao tiếp nghiệp vụ
using System.Security.Claims;

namespace RapchieuPhim.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // 🔐 KHÓA TỔNG: Phải có Token đăng nhập mới được sờ vào nghiệp vụ Đặt vé
    public class BookingsController : ControllerBase
    {
        private readonly IBookingService _bookingService;

        public BookingsController(IBookingService bookingService)
        {
            _bookingService = bookingService;
        }

        // GET: api/Bookings 👑 (CHỈ ADMIN VÀ STAFF MỚI ĐƯỢC XEM TOÀN BỘ DANH SÁCH ĐƠN VÉ)
        [HttpGet]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> GetAll()
        {
            var bookings = await _bookingService.GetAllDetailsAsync();
            return Ok(bookings);
        }

        // GET: api/Bookings/{id} 👑 (CHỈ ADMIN VÀ STAFF MỚI ĐƯỢC XEM CHI TIẾT THEO ID ĐƠN THÔ)
        [HttpGet("{id}")]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> GetById(int id)
        {
            var booking = await _bookingService.GetDetailByIdAsync(id);
            if (booking == null)
                return NotFound(new { Message = ValidationMessages.BookingNotFoundWithId(id) });
            return Ok(booking);
        }

        // GET: api/Bookings/ByUser/{userId} 🛡️ (XEM LỊCH SỬ MUA VÉ - Khách chỉ tự xem của mình, Admin xem hết)
        [HttpGet("ByUser/{userId}")]
        public async Task<IActionResult> GetByUser(int userId)
        {
            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var currentRole = User.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;

            var result = await _bookingService.GetHistoryByUserAsync(userId, currentUserId, currentRole);
            if (!result.IsSuccess)
                return StatusCode(403, new { Message = result.Message });

            return Ok(result.Data);
        }

        // GET: api/Bookings/Detail 👑 (CHỈ ADMIN/STAFF MỚI ĐƯỢC QUÉT VIEW DOANH THU)
        [HttpGet("Detail")]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> GetDetail()
        {
            var detail = await _bookingService.GetAllDetailsAsync();
            return Ok(detail);
        }

        // GET: api/Bookings/AvailableSeats/{showTimeId} 🔓 (TẤT CẢ USER ĐÃ ĐĂNG NHẬP ĐỀU XEM ĐƯỢC ĐỂ CHỌN GHẾ)
        [HttpGet("AvailableSeats/{showTimeId}")]
        public async Task<IActionResult> GetAvailableSeats(int showTimeId)
        {
            var seats = await _bookingService.GetAvailableSeatsAsync(showTimeId);
            return Ok(seats);
        }

        // POST: api/Bookings 🔓 (TẤT CẢ USER ĐỀU ĐẶT ĐƯỢC - Tự động nhận diện Online/Quầy và tự bóc ID)
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] BookingCreateRequest request)
        {
            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var currentRole = User.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;

            // 🌟 SỬA ĐÒNG NÀY: Đổi .CreateAsync thành .CreateBookingAsync
            var result = await _bookingService.CreateBookingAsync(request, currentUserId, currentRole);

            if (!result.IsSuccess)
                return BadRequest(new { Message = result.Message });

            return Ok(new { Message = result.Message, BookingId = result.BookingId });
        }

        // DELETE: api/Bookings/{id} 🛡️ (HỦY ĐƠN VÉ - Sử dụng Stored Procedure an toàn đa tầng)
        [HttpDelete("{id}")]
        public async Task<IActionResult> CancelBooking(int id)
        {
            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var currentRole = User.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;

            var result = await _bookingService.CancelBookingAsync(id, currentUserId, currentRole);

            if (!result.IsSuccess)
                return BadRequest(new { Message = result.Message });

            return Ok(new { Message = result.Message });
        }
    }
}