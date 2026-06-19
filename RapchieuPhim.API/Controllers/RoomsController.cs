using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RapchieuPhim.API.Constants;      // Dùng ValidationMessages hằng số sạch
using RapchieuPhim.API.DTOs.DTORequest; // DTO đầu vào
using RapchieuPhim.API.DTOs.DTOResponse;// DTO đầu ra
using RapchieuPhim.API.Services;      // Giao tiếp Service
using System.Security.Claims;
using static RapchieuPhim.API.Services.RoomService;

namespace RapchieuPhim.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // 🔐 KHÓA TỔNG: Yêu cầu mọi request bắt buộc phải đăng nhập (có Token) mới được vào
    public class RoomsController : ControllerBase
    {
        private readonly IRoomService _roomService;

        // Tiêm (Inject) Service vào thông qua hàm khởi tạo Constructor
        public RoomsController(IRoomService roomService)
        {
            _roomService = roomService;
        }

        // GET: api/Rooms (Tất cả thành viên đã đăng nhập đều xem được)
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var rooms = await _roomService.GetAllAsync();
            return Ok(rooms);
        }

        // GET: api/Rooms/{id} (Tất cả thành viên đã đăng nhập đều xem được chi tiết)
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var room = await _roomService.GetByIdAsync(id);
            if (room == null)
                return NotFound(new { Message = ValidationMessages.RoomNotFoundWithId(id) });

            return Ok(room);
        }

        // GET: api/Rooms/ByCinema/{cinemaId} (Tất cả thành viên đã đăng nhập đều xem được)
        [HttpGet("ByCinema/{cinemaId}")]
        public async Task<IActionResult> GetByCinema(int cinemaId)
        {
            var rooms = await _roomService.GetByCinemaAsync(cinemaId);
            return Ok(rooms);
        }

        // POST: api/Rooms 👑 (CHỈ ADMIN MỚI ĐƯỢC TẠO PHÒNG CHIẾU)
        [HttpPost]
        [Authorize(Roles = "Admin")] // Chốt chặn nhóm quyền Admin bảo mật vòng 2
        public async Task<IActionResult> Create([FromBody] RoomRequest request)
        {
            var result = await _roomService.CreateAsync(request);
            return CreatedAtAction(nameof(GetById), new { id = result.RoomId }, result);
        }

        // PUT: api/Rooms/{id} 👑 (CHỈ ADMIN TỐI CAO MỚI ĐƯỢC PHÉP CHỈNH SỬA)
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")] // Admin thường lọt qua, nhưng sẽ kẹt ở tầng Service dưới
        public async Task<IActionResult> Update(int id, [FromBody] RoomRequest request)
        {
            // 1. Bóc Token lấy Email của tài khoản đang thực hiện thao tác click nút sửa
            var currentOperatorEmail = User.FindFirst(ClaimTypes.Email)?.Value;

            // 2. Chuyển dữ liệu và Email xuống tầng Service nhờ thẩm tra đặc quyền hột nhân
            var result = await _roomService.UpdateAsync(id, request, currentOperatorEmail ?? string.Empty);

            // 3. Nếu không phải Super Admin (Email không khớp hằng số) -> Trả về lỗi 403 Forbidden chặn đứng câu lệnh
            if (!result.IsSuccess)
                return StatusCode(result.StatusCode, new { Message = result.Message });

            return Ok(new { Message = result.Message });
        }

        // DELETE: api/Rooms/{id} 👑 (CHỈ ADMIN TỐI CAO MỚI ĐƯỢC PHÉP XÓA)
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")] // Chốt chặn bảo mật vòng 2
        public async Task<IActionResult> Delete(int id)
        {
            // 1. Bóc Token lấy Email của tài khoản đang thực hiện thao tác click nút xóa
            var currentOperatorEmail = User.FindFirst(ClaimTypes.Email)?.Value;

            // 2. Chuyển ID phòng chiếu và Email xuống tầng Service thẩm định quyền lực
            var result = await _roomService.DeleteAsync(id, currentOperatorEmail ?? string.Empty);

            // 3. Nếu dính lỗi kiểm tra đặc quyền, lập tức trả lỗi về chặn đứng luồng xử lý
            if (!result.IsSuccess)
                return StatusCode(result.StatusCode, new { Message = result.Message });

            return Ok(new { Message = result.Message });
        }
    }
}