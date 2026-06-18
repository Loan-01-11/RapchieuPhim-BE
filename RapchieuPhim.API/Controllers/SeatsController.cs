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
    public class SeatsController : ControllerBase
    {
        private readonly ISeatService _seatService;

        public SeatsController(ISeatService seatService)
        {
            _seatService = seatService;
        }

        // 🔓 1. LẤY TẤT CẢ GHẾ (CÔNG KHAI)
        // GET: api/Seats
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAll()
        {
            var seats = await _seatService.GetAllAsync();
            return Ok(seats);
        }

        // 🔓 2. XEM CHI TIẾT GHẾ THEO ID (CÔNG KHAI)
        // GET: api/Seats/{id}
        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetById(int id)
        {
            var seat = await _seatService.GetByIdAsync(id);
            if (seat == null)
                return NotFound(new { Message = ValidationMessages.SeatNotFoundWithId(id) });

            return Ok(seat);
        }

        // 🔓 3. LẤY DANH SÁCH GHẾ THEO PHÒNG (CÔNG KHAI)
        // GET: api/Seats/ByRoom/{roomId}
        [HttpGet("ByRoom/{roomId}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetByRoom(int roomId)
        {
            var seats = await _seatService.GetByRoomAsync(roomId);
            return Ok(seats);
        }

        // 🔓 4. XEM SƠ ĐỒ GHẾ THEO PHÒNG (CÔNG KHAI)
        // GET: api/Seats/Layout/{roomId}
        [HttpGet("Layout/{roomId}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetLayout(int roomId)
        {
            var layout = await _seatService.GetLayoutByRoomAsync(roomId);
            return Ok(layout);
        }

        // 🔓 5. LẤY GHẾ CÒN TRỐNG THEO SUẤT CHIẾU (CÔNG KHAI)
        // GET: api/Seats/Available/{showtimeId}
        [HttpGet("Available/{showtimeId}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetAvailable(int showtimeId)
        {
            var seats = await _seatService.GetAvailableByShowtimeAsync(showtimeId);
            return Ok(seats);
        }

        // 👑 6. TẠO MỘT GHẾ MỚI (CHỈ ADMIN)
        // POST: api/Seats
        [HttpPost]
        [Authorize(Roles = RoleConstants.Admin)]
        public async Task<IActionResult> Create([FromBody] CreateSeatRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { Message = GetFirstError() });

            var result = await _seatService.CreateAsync(request);
            return StatusCode(result.StatusCode,
                result.IsSuccess ? result.Data : new { result.Message });
        }

        // 👑 7. TẠO HÀNG LOẠT GHẾ (CHỈ ADMIN)
        // POST: api/Seats/Batch
        [HttpPost("Batch")]
        [Authorize(Roles = RoleConstants.Admin)]
        public async Task<IActionResult> CreateBatch([FromBody] CreateSeatBatchRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { Message = GetFirstError() });

            var result = await _seatService.CreateBatchAsync(request);
            return StatusCode(result.StatusCode,
                result.IsSuccess ? result.Data : new { result.Message });
        }

        // 👑 8. CẬP NHẬT THÔNG TIN GHẾ (CHỈ ADMIN)
        // PUT: api/Seats/{id}
        [HttpPut("{id}")]
        [Authorize(Roles = RoleConstants.Admin)]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateSeatRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { Message = GetFirstError() });

            var result = await _seatService.UpdateAsync(id, request);
            return StatusCode(result.StatusCode,
                result.IsSuccess ? result.Data : new { result.Message });
        }

        // 👑 9. ĐỔI LOẠI GHẾ HÀNG LOẠT (CHỈ ADMIN)
        // PATCH: api/Seats/BatchType
        [HttpPatch("BatchType")]
        [Authorize(Roles = RoleConstants.Admin)]
        public async Task<IActionResult> UpdateTypeBatch([FromBody] UpdateSeatTypeBatchRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { Message = GetFirstError() });

            var result = await _seatService.UpdateTypeBatchAsync(request);
            return StatusCode(result.StatusCode,
                result.IsSuccess ? result.Data : new { result.Message });
        }

        // 👑 10. BẬT/TẮT TRẠNG THÁI GHẾ HÀNG LOẠT (CHỈ ADMIN)
        // PATCH: api/Seats/BatchStatus
        [HttpPatch("BatchStatus")]
        [Authorize(Roles = RoleConstants.Admin)]
        public async Task<IActionResult> ToggleStatusBatch([FromBody] ToggleSeatStatusBatchRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { Message = GetFirstError() });

            var result = await _seatService.ToggleStatusBatchAsync(request);
            return StatusCode(result.StatusCode,
                result.IsSuccess ? result.Data : new { result.Message });
        }

        // 👑 11. XÓA GHẾ (CHỈ ADMIN)
        // DELETE: api/Seats/{id}
        [HttpDelete("{id}")]
        [Authorize(Roles = RoleConstants.Admin)]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _seatService.DeleteAsync(id);
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
            return ValidationMessages.DataInvalid;
        }
    }
}
