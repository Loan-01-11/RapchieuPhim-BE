using Microsoft.EntityFrameworkCore;
using RapchieuPhim.API.Constants;
using RapchieuPhim.API.DTO.DTORequest;
using RapchieuPhim.API.Models;

namespace RapchieuPhim.API.Services
{
    public interface IMovieService
    {
        Task<List<Movie>> GetAllAsync();
        Task<Movie?> GetByIdAsync(int id);
        Task<List<Movie>> GetByStatusAsync(string status);
        Task<List<Movie>> GetNowShowingAsync();
        Task<List<Movie>> GetComingSoonAsync();
        Task<List<Movie>> GetSpecialAsync();
        Task<List<object>> GetWithShowtimesAsync();
        Task<(bool IsSuccess, string Message, int StatusCode, object? Data)> CreateAsync(CreateMovieRequest request, int createdByUserId);
        Task<(bool IsSuccess, string Message, int StatusCode, object? Data)> UpdateAsync(int id, UpdateMovieRequest request);
        Task<(bool IsSuccess, string Message, int StatusCode, object? Data)> DeleteAsync(int id, string currentOperatorEmail);
    }

    public class MovieService : IMovieService
    {
        private readonly CinemaManagementContext _context;

        public MovieService(CinemaManagementContext context)
        {
            _context = context;
        }

        // 🔓 1. LẤY TẤT CẢ PHIM
        public async Task<List<Movie>> GetAllAsync()
        {
            return await _context.Movies
                .Include(m => m.Categories)
                .ToListAsync();
        }

        // 🔓 2. XEM CHI TIẾT PHIM THEO ID
        public async Task<Movie?> GetByIdAsync(int id)
        {
            return await _context.Movies
                .Include(m => m.Categories)
                .FirstOrDefaultAsync(m => m.MovieId == id);
        }

        // 🔓 3. LỌC PHIM THEO TRẠNG THÁI
        public async Task<List<Movie>> GetByStatusAsync(string status)
        {
            return await _context.Movies
                .Include(m => m.Categories)
                .Where(m => m.Status == status)
                .ToListAsync();
        }

        // 🎬 3a. PHIM ĐANG CHIẾU
        public async Task<List<Movie>> GetNowShowingAsync()
        {
            var today = DateOnly.FromDateTime(DateTime.Now);
            return await _context.Movies
                .Include(m => m.Categories)
                .Where(m => m.Status == MovieStatus.NowShowing && m.ReleaseDate <= today && m.EndDate >= today)
                .ToListAsync();
        }

        // 🎬 3b. PHIM SẮP CHIẾU
        public async Task<List<Movie>> GetComingSoonAsync()
        {
            return await _context.Movies
                .Include(m => m.Categories)
                .Where(m => m.Status == MovieStatus.ComingSoon)
                .ToListAsync();
        }

        // 🎬 3c. PHIM ĐẶC BIỆT
        public async Task<List<Movie>> GetSpecialAsync()
        {
            return await _context.Movies
                .Include(m => m.Categories)
                .Where(m => m.Status == MovieStatus.Special) // 🌟 Dùng hằng số
                .ToListAsync();
        }

        // 👑 4. THÊM PHIM MỚI (ADMIN)
        public async Task<(bool IsSuccess, string Message, int StatusCode, object? Data)> CreateAsync(CreateMovieRequest request, int createdByUserId)
        {
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
                CreatedBy = createdByUserId
            };

            // Gắn thể loại cho phim
            if (request.CategoryIds != null)
            {
                foreach (var catId in request.CategoryIds)
                {
                    var category = await _context.Moviecategories.FindAsync(catId);
                    if (category != null)
                        movie.Categories.Add(category);
                }
            }

            _context.Movies.Add(movie);
            await _context.SaveChangesAsync();

            return (true, string.Empty, 201, movie);
        }

        // 👑 5. CẬP NHẬT PHIM (ADMIN)
        public async Task<(bool IsSuccess, string Message, int StatusCode, object? Data)> UpdateAsync(int id, UpdateMovieRequest request)
        {
            var movie = await _context.Movies
                .Include(m => m.Categories)
                .FirstOrDefaultAsync(m => m.MovieId == id);

            if (movie == null)
                return (false, ValidationMessages.MovieNotFoundWithId(id), 404, null);

            movie.Title = request.Title.Trim();
            movie.Description = request.Description?.Trim();
            movie.Duration = request.Duration;
            movie.Director = request.Director?.Trim();
            movie.Actors = request.Actors?.Trim();
            movie.Language = request.Language?.Trim();
            movie.Subtitles = request.Subtitles?.Trim();
            movie.AgeRating = request.AgeRating?.Trim();
            movie.ReleaseDate = DateOnly.FromDateTime(request.ReleaseDate);
            movie.EndDate = DateOnly.FromDateTime(request.EndDate);
            movie.PosterUrl = request.PosterUrl?.Trim();
            movie.TrailerUrl = request.TrailerUrl?.Trim();
            movie.Status = request.Status.Trim();

            // Cập nhật thể loại cho phim
            if (request.CategoryIds != null)
            {
                movie.Categories.Clear();
                foreach (var catId in request.CategoryIds)
                {
                    var category = await _context.Moviecategories.FindAsync(catId);
                    if (category != null)
                        movie.Categories.Add(category);
                }
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Movies.Any(e => e.MovieId == id))
                    return (false, ValidationMessages.MovieNotFoundWithId(id), 404, null);
                throw;
            }

            return (true, ValidationMessages.UpdateMovieSuccess, 200, null);
        }

        // 👑 6. XÓA PHIM (CHỈ SUPER ADMIN)
        public async Task<(bool IsSuccess, string Message, int StatusCode, object? Data)> DeleteAsync(int id, string currentOperatorEmail)
        {
            var movie = await _context.Movies.FindAsync(id);
            if (movie == null)
                return (false, ValidationMessages.MovieNotFoundWithId(id), 404, null);

            if (currentOperatorEmail != ValidationMessages.SuperAdminEmail)
                return (false, ValidationMessages.UnauthorizedDelete, 403, null);

            _context.Movies.Remove(movie);
            await _context.SaveChangesAsync();

            return (true, ValidationMessages.DeleteMovieSuccess, 200, null);
        }

        // ─────────────────────────────────────────────────────────────────────
        // 🔓 7. DANH SÁCH PHIM KÈM SUẤT CHIẾU KHẢ DỤNG (Movie Catalog)
        // Trả về tất cả phim đang hoạt động kèm suất chiếu sắp tới và thể loại.
        // Tách thành 3 query riêng biệt để tránh lỗi MARS (Multiple Active Result Sets)
        // xảy ra khi dùng nhiều .Include() trên cùng một connection SQL Server.
        // ─────────────────────────────────────────────────────────────────────
        public async Task<List<object>> GetWithShowtimesAsync()
        {
            var now = DateTime.Now;

            // Query 1: Lấy danh sách phim đang hoạt động (bỏ Deleted + Archived)
            var movies = await _context.Movies
                .Where(m => !ShowtimeMessages.InactiveMovieStatuses.Contains(m.Status))
                .OrderBy(m => m.Title)
                .ToListAsync();

            var movieIds = movies.Select(m => m.MovieId).ToList();

            // Query 2: Lấy suất chiếu Active có giờ chiếu từ hiện tại trở đi
            var showtimes = await _context.Showtimes
                .Where(s => movieIds.Contains(s.MovieId)
                         && s.Status == ShowtimeMessages.StatusActive
                         && s.StartTime >= now)
                .OrderBy(s => s.StartTime)
                .ToListAsync();

            // Query 3: Lấy thể loại của từng phim qua bảng junction (many-to-many)
            // Dùng projection thay vì Include để tránh MARS
            var movieWithCats = await _context.Movies
                .Where(m => movieIds.Contains(m.MovieId))
                .Select(m => new
                {
                    m.MovieId,
                    CategoryNames = m.Categories.Select(c => c.CategoryName)
                })
                .ToListAsync();

            // Tạo Dictionary để tra cứu thể loại theo MovieId nhanh hơn O(n²)
            var categoryMap = movieWithCats.ToDictionary(x => x.MovieId, x => x.CategoryNames);

            // Ghép kết quả trong bộ nhớ (in-memory join)
            var result = movies.Select(m => (object)new
            {
                m.MovieId,
                m.Title,
                m.Duration,
                m.AgeRating,
                m.PosterUrl,
                m.TrailerUrl,
                m.Status,
                m.ReleaseDate,
                m.EndDate,
                // Lấy danh sách tên thể loại, trả về rỗng nếu chưa có thể loại nào
                Categories = categoryMap.TryGetValue(m.MovieId, out var cats) ? cats : Enumerable.Empty<string>(),
                // Lọc suất chiếu thuộc đúng phim này
                UpcomingShowtimes = showtimes
                    .Where(s => s.MovieId == m.MovieId)
                    .Select(s => new
                    {
                        s.ShowTimeId,
                        s.RoomId,
                        s.StartTime,
                        s.EndTime,
                        s.BasePrice
                    })
            }).ToList();

            return result;
        }
    }
}
