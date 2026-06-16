using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RapchieuPhim.API.Constants; // 🌟 Gọi thư mục hằng số để xóa bỏ hardcode
using RapchieuPhim.API.Models;
using System.Security.Claims;
using RapchieuPhim.API.DTOs;

namespace RapchieuPhim.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // 🔐 KHÓA TỔNG: Mặc định bảo mật toàn bộ trục Controller
    public class MoviesController : ControllerBase
    {
        private readonly CinemaManagementContext _context;

        public MoviesController(CinemaManagementContext context)
        {
            _context = context;
        }

        // 🔓 1. LẤY TẤT CẢ PHIM (CÔNG KHAI)
        // GET: api/Movies
        [HttpGet]
        [AllowAnonymous] // 🌍 Khách vãng lai chưa đăng nhập vẫn xem được danh sách phim ngoài trang chủ
        public async Task<IActionResult> GetAll()
        {
            var movies = await _context.Movies.ToListAsync();
            return Ok(movies);
        }

        // 🔓 2. XEM CHI TIẾT PHIM THEO ID (CÔNG KHAI)
        // GET: api/Movies/{id}
        [HttpGet("{id}")]
        [AllowAnonymous] // 🌍 Phục vụ trang click xem nội dung chi tiết phim công khai
        public async Task<IActionResult> GetById(int id)
        {
            var movie = await _context.Movies.FindAsync(id);
            if (movie == null)
                return NotFound(new { Message = ValidationMessages.MovieNotFoundWithId(id) }); // ➔ Sạch hardcode

            return Ok(movie);
        }

        // 🔓 3. LỌC DANH SÁCH PHIM THEO TRẠNG THÁI (CÔNG KHAI)
        // GET: api/Movies/ByStatus/{status}
        [HttpGet("ByStatus/{status}")]
        [AllowAnonymous] // 🌍 Khách xem phim đang chiếu hoặc phim sắp chiếu ngoài trang chủ
        public async Task<IActionResult> GetByStatus(string status)
        {
            var movies = await _context.Movies
                .Where(m => m.Status == status)
                .ToListAsync();
            return Ok(movies);
        }

        // 🔓 3b. LẤY DANH SÁCH PHIM ĐANG CHIẾU (MỌI ĐỐI TƯỢNG)
        // GET: api/Movies/NowShowing
        [HttpGet("NowShowing")]
        [AllowAnonymous] // 🌍 Ai vào web cũng xem được
        public async Task<IActionResult> GetNowShowing()
        {
            // Lấy ngày hiện tại ở định dạng DateOnly để so sánh với DB
            var today = DateOnly.FromDateTime(DateTime.Now);

            var movies = await _context.Movies
                .Where(m => m.Status == "suất đang chiếu" && m.ReleaseDate <= today && m.EndDate >= today)
                .ToListAsync();

            return Ok(movies);
        }

        // 🔓 3c. LẤY DANH SÁCH PHIM SẮP CHIẾU (MỌI ĐỐI TƯỢNG)
        // GET: api/Movies/ComingSoon
        [HttpGet("ComingSoon")]
        [AllowAnonymous]
        public async Task<IActionResult> GetComingSoon()
        {
            var today = DateOnly.FromDateTime(DateTime.Now);

            var movies = await _context.Movies
                .Where(m => m.Status == "suất sắp chiếu")
                .ToListAsync();

            return Ok(movies);
        }

        // 🔓 3d. LẤY DANH SÁCH PHIM CÓ SUẤT CHIẾU ĐẶC BIỆT / CHIẾU SỚM (MỌI ĐỐI TƯỢNG)
        // GET: api/Movies/Special
        [HttpGet("Special")]
        [AllowAnonymous]
        public async Task<IActionResult> GetSpecialRequests()
        {
            // Lọc theo trạng thái đặc biệt bạn quy định trong DB (Ví dụ: "Special")
            var movies = await _context.Movies
                .Where(m => m.Status == "suất đặc biệt")
                .ToListAsync();

            return Ok(movies);
        }

        // 👑 4. THÊM PHIM MỚI (CHỈ ADMIN)
        // POST: api/Movies
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([FromBody] CreateMovieRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
                return Unauthorized(new { Message = ValidationMessages.TokenInvalid });

            int currentUserId = int.Parse(userIdClaim);

            var movie = new Movie
            {
                Title = request.Title.Trim(),
                Description = request.Description?.Trim(),
                Duration = request.Duration,
                Director = request.Director?.Trim(),
                Actors = request.Actors?.Trim(),
                Language = request.Language?.Trim(),
                Subtitles = request.Subtitles?.Trim(),
                AgeRating = request.AgeRating?.Trim(),

                ReleaseDate = DateOnly.FromDateTime(request.ReleaseDate),
                EndDate = DateOnly.FromDateTime(request.EndDate),

                PosterUrl = request.PosterUrl?.Trim(),
                TrailerUrl = request.TrailerUrl?.Trim(),
                Status = request.Status.Trim(),

                CreatedAt = DateTime.Now,
                CreatedBy = currentUserId
                // 🌟 ĐÃ XÓA dòng khởi tạo danh sách lỗi cũ vì file Movie.cs của bạn đã tự tạo sẵn 'Categories' rồi!
            };

            // 🌟 VÒNG LẶP THÊM THỂ LOẠI THEO ĐÚNG CẤU TRÚC 'Categories' CỦA BẠN:
            if (request.CategoryIds != null)
            {
                foreach (var catId in request.CategoryIds)
                {
                    // Vì EF Core giấu bảng trung gian, ta tìm thực thể thể loại hiện có trong DB
                    var category = await _context.Moviecategories.FindAsync(catId);
                    if (category != null)
                    {
                        // Ném thẳng thực thể thể loại vào danh sách 'Categories' của phim
                        movie.Categories.Add(category);
                    }
                }
            }

            _context.Movies.Add(movie);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = movie.MovieId }, movie);
        }

        // 👑 5. CẬP NHẬT THÔNG TIN PHIM (CHỈ ADMIN)
        // PUT: api/Movies/{id}
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")] // 🔐 Phân quyền: Chỉ Admin và Nhân viên mới được sửa phim
        public async Task<IActionResult> Update(int id, [FromBody] UpdateMovieRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // 1. Tìm bộ phim gốc đang có trong Database ra
            var movie = await _context.Movies.FindAsync(id);
            if (movie == null)
            {
                return NotFound(new { Message = ValidationMessages.MovieNotFoundWithId(id) });
            }

            // 2. Tiến hành cập nhật thông tin từ DTO vào Entity
            movie.Title = request.Title.Trim();
            movie.Description = request.Description?.Trim();
            movie.Duration = request.Duration;
            movie.Director = request.Director?.Trim();
            movie.Actors = request.Actors?.Trim();
            movie.Language = request.Language?.Trim();
            movie.Subtitles = request.Subtitles?.Trim();
            movie.AgeRating = request.AgeRating?.Trim();

            // 🌟 FIX LỖI ÉP KIỂU: Ép kiểu từ DateTime của DTO sang DateOnly của Database hợp lệ 100%
            movie.ReleaseDate = DateOnly.FromDateTime(request.ReleaseDate);
            movie.EndDate = DateOnly.FromDateTime(request.EndDate);

            movie.PosterUrl = request.PosterUrl?.Trim();
            movie.TrailerUrl = request.TrailerUrl?.Trim();
            movie.Status = request.Status.Trim();


            try
            {
                // lưu xuống SQL Server
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Movies.Any(e => e.MovieId == id))
                    return NotFound(new { Message = ValidationMessages.MovieNotFoundWithId(id) });
                throw;
            }

            // Trả về thông báo thành công kèm data sạch sẽ
            return Ok(new { Message = "Cập nhật thông tin bộ phim thành công!" });
        }

        // 👑 6. XÓA PHIM KHỎI HỆ THỐNG (CHỈ ADMIN CẤP CAO DUY NHẤT)
        // DELETE: api/Movies/{id}
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")] // Cổng thô thắt chặt quyền Admin
        public async Task<IActionResult> Delete(int id)
        {
            var movie = await _context.Movies.FindAsync(id);
            if (movie == null)
                return NotFound(new { Message = ValidationMessages.MovieNotFoundWithId(id) }); // ➔ Sạch hardcode

            // 🌟 LUẬT BẢO MẬT TỐI CAO: Check đúng email gốc quyền lực
            var currentOperatorEmail = User.FindFirst(ClaimTypes.Email)?.Value;
            if (currentOperatorEmail != ValidationMessages.SuperAdminEmail)
            {
                return StatusCode(403, new { Message = ValidationMessages.UnauthorizedDelete }); // ➔ Sạch hardcode
            }

            _context.Movies.Remove(movie);
            await _context.SaveChangesAsync();
            return Ok(new { Message = "Đã xóa phim thành công khỏi hệ thống!" });
        }
    }
}