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
    [Authorize] // 🔐 Mặc định toàn bộ Controller yêu cầu đăng nhập
    public class MoviesController : ControllerBase
    {
        private readonly IMovieService _movieService;

        public MoviesController(IMovieService movieService)
        {
            _movieService = movieService;
        }

        // 🔓 1. LẤY TẤT CẢ PHIM (CÔNG KHAI)
        // GET: api/Movies
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAll()
        {
            var movies = await _movieService.GetAllAsync();
            return Ok(movies);
        }

        // 🔓 2. XEM CHI TIẾT PHIM THEO ID (CÔNG KHAI)
        // GET: api/Movies/{id}
        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetById(int id)
        {
            var movie = await _movieService.GetByIdAsync(id);
            if (movie == null)
                return NotFound(new { Message = ValidationMessages.MovieNotFoundWithId(id) });

            return Ok(movie);
        }

        // 🔓 3. LỌC DANH SÁCH PHIM THEO TRẠNG THÁI (CÔNG KHAI)
        // GET: api/Movies/ByStatus/{status}
        [HttpGet("ByStatus/{status}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetByStatus(string status)
        {
            var movies = await _movieService.GetByStatusAsync(status);
            return Ok(movies);
        }

        // 🔓 3b. PHIM ĐANG CHIẾU (CÔNG KHAI)
        // GET: api/Movies/NowShowing
        [HttpGet("NowShowing")]
        [AllowAnonymous]
        public async Task<IActionResult> GetNowShowing()
        {
            var movies = await _movieService.GetNowShowingAsync();
            return Ok(movies);
        }

        // 🔓 3c. PHIM SẮP CHIẾU (CÔNG KHAI)
        // GET: api/Movies/ComingSoon
        [HttpGet("ComingSoon")]
        [AllowAnonymous]
        public async Task<IActionResult> GetComingSoon()
        {
            var movies = await _movieService.GetComingSoonAsync();
            return Ok(movies);
        }

        // 🔓 3d. PHIM ĐẶC BIỆT (CÔNG KHAI)
        // GET: api/Movies/Special
        [HttpGet("Special")]
        [AllowAnonymous]
        public async Task<IActionResult> GetSpecial()
        {
            var movies = await _movieService.GetSpecialAsync();
            return Ok(movies);
        }

        // 🔓 3e. DANH SÁCH PHIM KÈM SUẤT CHIẾU KHẢ DỤNG (CATALOG - CÔNG KHAI)
        // GET: api/Movies/WithShowtimes
        [HttpGet("WithShowtimes")]
        [AllowAnonymous]
        public async Task<IActionResult> GetWithShowtimes()
        {
            var result = await _movieService.GetWithShowtimesAsync();
            return Ok(result);
        }

        // 👑 4. THÊM PHIM MỚI (CHỈ ADMIN)
        // POST: api/Movies
        [HttpPost]
        [Authorize(Roles = RoleConstants.Admin)]
        public async Task<IActionResult> Create([FromBody] CreateMovieRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                           ?? User.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
                return Unauthorized(new { Message = ValidationMessages.TokenInvalid });

            var result = await _movieService.CreateAsync(request, int.Parse(userIdClaim));
            return StatusCode(result.StatusCode,
                result.IsSuccess ? result.Data : new { result.Message });
        }

        // 👑 5. CẬP NHẬT PHIM (CHỈ ADMIN)
        // PUT: api/Movies/{id}
        [HttpPut("{id}")]
        [Authorize(Roles = RoleConstants.Admin)]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateMovieRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _movieService.UpdateAsync(id, request);
            return StatusCode(result.StatusCode,
                result.IsSuccess ? new { result.Message } : new { result.Message });
        }

        // 👑 6. XÓA PHIM (CHỈ SUPER ADMIN)
        // DELETE: api/Movies/{id}
        [HttpDelete("{id}")]
        [Authorize(Roles = RoleConstants.Admin)]
        public async Task<IActionResult> Delete(int id)
        {
            var email = User.FindFirst(ClaimTypes.Email)?.Value ?? string.Empty;
            var result = await _movieService.DeleteAsync(id, email);
            return StatusCode(result.StatusCode, new { result.Message });
        }
    }
}