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
    public class StaffReportsController : ControllerBase
    {
        private readonly IStaffReportService _staffReportService;

        public StaffReportsController(IStaffReportService staffReportService)
        {
            _staffReportService = staffReportService;
        }

        // 👑 1. LẤY TẤT CẢ BÁO CÁO CA LÀM VIỆC (CHỈ ADMIN & STAFF)
        // GET: api/StaffReports
        [HttpGet]
        [Authorize(Roles = $"{RoleConstants.Admin},{RoleConstants.Staff}")]
        public async Task<IActionResult> GetAll()
        {
            var reports = await _staffReportService.GetAllAsync();
            return Ok(reports);
        }

        // 👑 2. XEM CHI TIẾT BÁO CÁO THEO ID (CHỈ ADMIN & STAFF)
        // GET: api/StaffReports/{id}
        [HttpGet("{id}")]
        [Authorize(Roles = $"{RoleConstants.Admin},{RoleConstants.Staff}")]
        public async Task<IActionResult> GetById(int id)
        {
            var report = await _staffReportService.GetByIdAsync(id);
            if (report == null)
                return NotFound(new { Message = $"Không tìm thấy báo cáo với id {id}." });

            return Ok(report);
        }

        // 👑 3. LẤY DANH SÁCH BÁO CÁO THEO NHÂN VIÊN (CHỈ ADMIN & STAFF)
        // GET: api/StaffReports/ByStaff/{staffId}
        [HttpGet("ByStaff/{staffId}")]
        [Authorize(Roles = $"{RoleConstants.Admin},{RoleConstants.Staff}")]
        public async Task<IActionResult> GetByStaff(int staffId)
        {
            var reports = await _staffReportService.GetByStaffAsync(staffId);
            return Ok(reports);
        }

        // 👑 4. LẤY DANH SÁCH BÁO CÁO THEO RẠP (CHỈ ADMIN & STAFF)
        // GET: api/StaffReports/ByCinema/{cinemaId}
        [HttpGet("ByCinema/{cinemaId}")]
        [Authorize(Roles = $"{RoleConstants.Admin},{RoleConstants.Staff}")]
        public async Task<IActionResult> GetByCinema(int cinemaId)
        {
            var reports = await _staffReportService.GetByCinemaAsync(cinemaId);
            return Ok(reports);
        }

        // 👑 5. TẠO BÁO CÁO CA LÀM VIỆC MỚI (CHỈ ADMIN & STAFF)
        // POST: api/StaffReports
        // Body: { "staffId": 1, "cinemaId": 1, "reportDate": "2026-07-07", "summary": "...", "totalBookings": 10, "totalOrders": 5, "totalRevenue": 1500000 }
        [HttpPost]
        [Authorize(Roles = $"{RoleConstants.Admin},{RoleConstants.Staff}")]
        public async Task<IActionResult> Create([FromBody] CreateStaffReportRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { Message = GetFirstError() });

            var result = await _staffReportService.CreateAsync(request);
            return StatusCode(result.StatusCode,
                result.IsSuccess ? result.Data : new { result.Message });
        }

        // 👑 6. XÓA BÁO CÁO (CHỈ ADMIN)
        // DELETE: api/StaffReports/{id}
        [HttpDelete("{id}")]
        [Authorize(Roles = RoleConstants.Admin)]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _staffReportService.DeleteAsync(id);
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
