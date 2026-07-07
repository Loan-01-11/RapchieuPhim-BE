using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RapchieuPhim.API.Constants;
using RapchieuPhim.API.DTO.DTORequest;
using RapchieuPhim.API.Services;

namespace RapchieuPhim.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // 🔐 Mặc định toàn bộ Controller yêu cầu đăng nhập
    public class StaffShiftsController : ControllerBase
    {
        private readonly IStaffShiftService _staffShiftService;

        public StaffShiftsController(IStaffShiftService staffShiftService)
        {
            _staffShiftService = staffShiftService;
        }

        // 👑 1. LẤY TẤT CẢ CA LÀM VIỆC (CHỈ ADMIN & STAFF)
        // GET: api/StaffShifts
        [HttpGet]
        [Authorize(Roles = $"{RoleConstants.Admin},{RoleConstants.Staff}")]
        public async Task<IActionResult> GetAll()
        {
            var shifts = await _staffShiftService.GetAllAsync();
            return Ok(shifts);
        }

        // 👑 2. XEM CHI TIẾT CA LÀM VIỆC THEO ID (CHỈ ADMIN & STAFF)
        // GET: api/StaffShifts/{id}
        [HttpGet("{id}")]
        [Authorize(Roles = $"{RoleConstants.Admin},{RoleConstants.Staff}")]
        public async Task<IActionResult> GetById(int id)
        {
            var shift = await _staffShiftService.GetByIdAsync(id);
            if (shift == null)
                return NotFound(new { Message = $"Không tìm thấy ca làm việc với id {id}." });

            return Ok(shift);
        }

        // 👑 3. LẤY DANH SÁCH CA THEO NHÂN VIÊN (CHỈ ADMIN & STAFF)
        // GET: api/StaffShifts/ByStaff/{staffId}
        [HttpGet("ByStaff/{staffId}")]
        [Authorize(Roles = $"{RoleConstants.Admin},{RoleConstants.Staff}")]
        public async Task<IActionResult> GetByStaff(int staffId)
        {
            var shifts = await _staffShiftService.GetByStaffAsync(staffId);
            return Ok(shifts);
        }

        // 👑 4. LẤY DANH SÁCH CA THEO RẠP (CHỈ ADMIN & STAFF)
        // GET: api/StaffShifts/ByCinema/{cinemaId}
        [HttpGet("ByCinema/{cinemaId}")]
        [Authorize(Roles = $"{RoleConstants.Admin},{RoleConstants.Staff}")]
        public async Task<IActionResult> GetByCinema(int cinemaId)
        {
            var shifts = await _staffShiftService.GetByCinemaAsync(cinemaId);
            return Ok(shifts);
        }

        // 👑 5. MỞ CA LÀM VIỆC MỚI (CHỈ ADMIN & STAFF)
        // POST: api/StaffShifts
        // Body: { "staffId": 1, "cinemaId": 1 } — hệ thống tự gán ShiftStart và Status = "Open"
        [HttpPost]
        [Authorize(Roles = $"{RoleConstants.Admin},{RoleConstants.Staff}")]
        public async Task<IActionResult> Create([FromBody] CreateStaffShiftRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { Message = GetFirstError() });

            var result = await _staffShiftService.CreateAsync(request);
            return StatusCode(result.StatusCode,
                result.IsSuccess ? result.Data : new { result.Message });
        }

        // 👑 6. ĐÓNG CA LÀM VIỆC – GHI NHẬN KẾT QUẢ (CHỈ ADMIN & STAFF)
        // PUT: api/StaffShifts/{id}/Close
        // Body: { "totalBookings": 5, "totalOrders": 3, "totalRevenue": 500000, "summary": "..." }
        // Hệ thống tự gán ShiftEnd = Now và Status = "Closed"
        [HttpPut("{id}/Close")]
        [Authorize(Roles = $"{RoleConstants.Admin},{RoleConstants.Staff}")]
        public async Task<IActionResult> CloseShift(int id, [FromBody] CloseStaffShiftRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { Message = GetFirstError() });

            var result = await _staffShiftService.CloseShiftAsync(id, request);
            return StatusCode(result.StatusCode,
                result.IsSuccess ? result.Data : new { result.Message });
        }

        // 👑 7. XÓA CA LÀM VIỆC (CHỈ ADMIN)
        // DELETE: api/StaffShifts/{id}
        // Chỉ cho phép xóa ca đã đóng (Status = "Closed")
        [HttpDelete("{id}")]
        [Authorize(Roles = RoleConstants.Admin)]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _staffShiftService.DeleteAsync(id);
            return StatusCode(result.StatusCode, new { result.Message });
        }

        // ── Private Helpers ──────────────────────────────────────────────────

        /// <summary>Lấy lỗi đầu tiên từ ModelState để trả về message thân thiện.</summary>
        private string GetFirstError()
        {
            foreach (var state in ModelState.Values)
                foreach (var error in state.Errors)
                    if (!string.IsNullOrEmpty(error.ErrorMessage))
                        return error.ErrorMessage;
            return "Dữ liệu không hợp lệ.";
        }
    }
}
