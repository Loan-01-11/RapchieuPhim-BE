using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RapchieuPhim.API.Constants;
using RapchieuPhim.API.DTO.DTORequest;
using RapchieuPhim.API.Services;

namespace RapchieuPhim.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ShowtimesController : ControllerBase
    {
        private readonly IShowtimeService _showtimeService;

        public ShowtimesController(IShowtimeService showtimeService)
        {
            _showtimeService = showtimeService;
        }

        // 🔓 1. LẤY TẤT CẢ SUẤT CHIẾU (CÔNG KHAI)
        // GET: api/Showtimes
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAll()
        {
            var showtimes = await _showtimeService.GetAllAsync();
            return Ok(showtimes);
        }

        // 🔓 2. XEM CHI TIẾT SUẤT CHIẾU THEO ID (CÔNG KHAI)
        // GET: api/Showtimes/{id}
        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetById(int id)
        {
            var showtime = await _showtimeService.GetByIdAsync(id);
            if (showtime == null)
                return NotFound(new { Message = ShowtimeMessages.NotFoundWithId(id) });
            return Ok(showtime);
        }

        // 🔓 3. LẤY SUẤT CHIẾU THEO PHIM (CÔNG KHAI)
        // GET: api/Showtimes/ByMovie/{movieId}
        [HttpGet("ByMovie/{movieId}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetByMovie(int movieId)
        {
            var showtimes = await _showtimeService.GetByMovieAsync(movieId);
            return Ok(showtimes);
        }

        // 🔓 4. LẤY SUẤT CHIẾU THEO PHÒNG (CÔNG KHAI)
        // GET: api/Showtimes/ByRoom/{roomId}
        [HttpGet("ByRoom/{roomId}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetByRoom(int roomId)
        {
            var showtimes = await _showtimeService.GetByRoomAsync(roomId);
            return Ok(showtimes);
        }

        // 🔓 5. XEM SUẤT CHIẾU CHI TIẾT (qua VW_SHOWTIME_DETAIL – CÔNG KHAI)
        // GET: api/Showtimes/Detail
        [HttpGet("Detail")]
        [AllowAnonymous]
        public async Task<IActionResult> GetDetail()
        {
            var detail = await _showtimeService.GetDetailAsync();
            return Ok(detail);
        }

        // 👑 6. TẠO SUẤT CHIẾU MỚI (CHỈ ADMIN)
        // POST: api/Showtimes
        [HttpPost]
        [Authorize(Roles = RoleConstants.Admin)]
        public async Task<IActionResult> Create([FromBody] CreateShowtimeRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _showtimeService.CreateAsync(request);
            return StatusCode(result.StatusCode,
                result.IsSuccess ? result.Data : new { result.Message });
        }

        // 👑 7. CẬP NHẬT SUẤT CHIẾU (CHỈ ADMIN)
        // PUT: api/Showtimes/{id}
        [HttpPut("{id}")]
        [Authorize(Roles = RoleConstants.Admin)]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateShowtimeRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _showtimeService.UpdateAsync(id, request);
            return StatusCode(result.StatusCode,
                result.IsSuccess ? result.Data : new { result.Message });
        }

        // 👑 8. HUỶ SUẤT CHIẾU (CHỈ ADMIN)
        // PATCH: api/Showtimes/{id}/Cancel
        [HttpPatch("{id}/Cancel")]
        [Authorize(Roles = RoleConstants.Admin)]
        public async Task<IActionResult> Cancel(int id)
        {
            var result = await _showtimeService.CancelAsync(id);
            return StatusCode(result.StatusCode, new { result.Message });
        }

        // 👑 9. XOÁ SUẤT CHIẾU (CHỈ ADMIN – chỉ khi chưa có vé đặt)
        // DELETE: api/Showtimes/{id}
        [HttpDelete("{id}")]
        [Authorize(Roles = RoleConstants.Admin)]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _showtimeService.DeleteAsync(id);
            return StatusCode(result.StatusCode, new { result.Message });
        }
    }
}
