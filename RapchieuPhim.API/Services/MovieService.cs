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

        // 🔓 3b. PHIM ĐANG CHIẾU
        public async Task<List<Movie>> GetNowShowingAsync()
        {
            var today = DateOnly.FromDateTime(DateTime.Now);
            return await _context.Movies
                .Include(m => m.Categories)
                .Where(m => m.Status == "suất đang chiếu" && m.ReleaseDate <= today && m.EndDate >= today)
                .ToListAsync();
        }

        // 🔓 3c. PHIM SẮP CHIẾU
        public async Task<List<Movie>> GetComingSoonAsync()
        {
            return await _context.Movies
                .Include(m => m.Categories)
                .Where(m => m.Status == "suất sắp chiếu")
                .ToListAsync();
        }

        // 🔓 3d. PHIM ĐẶC BIỆT
        public async Task<List<Movie>> GetSpecialAsync()
        {
            return await _context.Movies
                .Include(m => m.Categories)
                .Where(m => m.Status == "suất đặc biệt")
                .ToListAsync();
        }

        // 👑 4. THÊM PHIM MỚI (ADMIN)
        public async Task<(bool IsSuccess, string Message, int StatusCode, object? Data)> CreateAsync(CreateMovieRequest request, int createdByUserId)
        {
            var movie = new Movie
            {
                Title       = request.Title.Trim(),
                Description = request.Description?.Trim(),
                Duration    = request.Duration,
                Director    = request.Director?.Trim(),
                Actors      = request.Actors?.Trim(),
                Language    = request.Language?.Trim(),
                Subtitles   = request.Subtitles?.Trim(),
                AgeRating   = request.AgeRating?.Trim(),
                ReleaseDate = DateOnly.FromDateTime(request.ReleaseDate),
                EndDate     = DateOnly.FromDateTime(request.EndDate),
                PosterUrl   = request.PosterUrl?.Trim(),
                TrailerUrl  = request.TrailerUrl?.Trim(),
                Status      = request.Status.Trim(),
                CreatedAt   = DateTime.Now,
                CreatedBy   = createdByUserId
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
            var movie = await _context.Movies.FindAsync(id);
            if (movie == null)
                return (false, ValidationMessages.MovieNotFoundWithId(id), 404, null);

            movie.Title       = request.Title.Trim();
            movie.Description = request.Description?.Trim();
            movie.Duration    = request.Duration;
            movie.Director    = request.Director?.Trim();
            movie.Actors      = request.Actors?.Trim();
            movie.Language    = request.Language?.Trim();
            movie.Subtitles   = request.Subtitles?.Trim();
            movie.AgeRating   = request.AgeRating?.Trim();
            movie.ReleaseDate = DateOnly.FromDateTime(request.ReleaseDate);
            movie.EndDate     = DateOnly.FromDateTime(request.EndDate);
            movie.PosterUrl   = request.PosterUrl?.Trim();
            movie.TrailerUrl  = request.TrailerUrl?.Trim();
            movie.Status      = request.Status.Trim();

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

            return (true, "Cập nhật thông tin bộ phim thành công!", 200, null);
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

            return (true, "Đã xóa phim thành công khỏi hệ thống!", 200, null);
        }
    }
}
