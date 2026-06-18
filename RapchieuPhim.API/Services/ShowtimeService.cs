using Microsoft.EntityFrameworkCore;
using RapchieuPhim.API.Constants;
using RapchieuPhim.API.DTO.DTORequest;
using RapchieuPhim.API.Models;

namespace RapchieuPhim.API.Services
{
    public interface IShowtimeService
    {
        Task<List<Showtime>> GetAllAsync();
        Task<Showtime?> GetByIdAsync(int id);
        Task<List<Showtime>> GetByMovieAsync(int movieId);
        Task<List<Showtime>> GetByRoomAsync(int roomId);
        Task<List<VwShowtimeDetail>> GetDetailAsync();
        Task<(bool IsSuccess, string Message, int StatusCode, object? Data)> CreateAsync(CreateShowtimeRequest request);
        Task<(bool IsSuccess, string Message, int StatusCode, object? Data)> UpdateAsync(int id, UpdateShowtimeRequest request);
        Task<(bool IsSuccess, string Message, int StatusCode, object? Data)> CancelAsync(int id);
        Task<(bool IsSuccess, string Message, int StatusCode, object? Data)> DeleteAsync(int id);
    }

    public class ShowtimeService : IShowtimeService
    {
        private readonly CinemaManagementContext _context;
        // Thời gian dọn phòng giữa các suất chiếu (phút)
        private const int CleaningBufferMinutes = 15;

        public ShowtimeService(CinemaManagementContext context)
        {
            _context = context;
        }

        // 🔓 1. LẤY TẤT CẢ SUẤT CHIẾU
        public async Task<List<Showtime>> GetAllAsync()
        {
            return await _context.Showtimes
                .OrderBy(s => s.StartTime)
                .ToListAsync();
        }

        // 🔓 2. XEM CHI TIẾT SUẤT CHIẾU THEO ID
        public async Task<Showtime?> GetByIdAsync(int id)
        {
            return await _context.Showtimes.FindAsync(id);
        }

        // 🔓 3. SUẤT CHIẾU THEO PHIM (chỉ Active)
        public async Task<List<Showtime>> GetByMovieAsync(int movieId)
        {
            return await _context.Showtimes
                .Where(s => s.MovieId == movieId && s.Status == ShowtimeMessages.ValidStatuses[0]) // "Active"
                .OrderBy(s => s.StartTime)
                .ToListAsync();
        }

        // 🔓 4. SUẤT CHIẾU THEO PHÒNG
        public async Task<List<Showtime>> GetByRoomAsync(int roomId)
        {
            return await _context.Showtimes
                .Where(s => s.RoomId == roomId)
                .OrderBy(s => s.StartTime)
                .ToListAsync();
        }

        // 🔓 5. SUẤT CHIẾU CHI TIẾT (qua View VW_SHOWTIME_DETAIL)
        public async Task<List<VwShowtimeDetail>> GetDetailAsync()
        {
            return await _context.VwShowtimeDetails.ToListAsync();
        }

        // 👑 6. TẠO SUẤT CHIẾU MỚI
        public async Task<(bool IsSuccess, string Message, int StatusCode, object? Data)> CreateAsync(CreateShowtimeRequest request)
        {
            // ── Validate phim tồn tại và còn hoạt động ──────────────────────────
            var movie = await _context.Movies.FindAsync(request.MovieId);
            if (movie == null)
                return (false, ShowtimeMessages.MovieNotFound, 404, null);

            // ── Chỉ từ chối nếu phim bị "Inactive" (ngừng hoạt động) ──────────────────────
            if (movie.Status == ValidationMessages.MovieStatusInactive)
                return (false, ShowtimeMessages.MovieNotActive, 409, null);

            // ── Validate phòng tồn tại và đang hoạt động ─────────────────────────
            var room = await _context.Rooms.FindAsync(request.RoomId);
            if (room == null || !room.IsActive)
                return (false, ShowtimeMessages.RoomNotFound, 404, null);

            // ── Validate thời gian bắt đầu không trong quá khứ ──────────────────
            if (request.StartTime < DateTime.Now)
                return (false, ShowtimeMessages.StartTimePast, 400, null);

            // ── Tính thời gian kết thúc = StartTime + Duration phim ──────────────
            var endTime = request.StartTime.AddMinutes(movie.Duration);

            // ── Kiểm tra xung đột lịch chiếu trong phòng ─────────────────────────
            //    Xung đột khi: StartTime_mới < (EndTime_cũ + buffer) VÀ EndTime_mới > StartTime_cũ
            var bufferEnd = endTime.AddMinutes(CleaningBufferMinutes);
            var conflict = await _context.Showtimes.AnyAsync(s =>
                s.RoomId == request.RoomId &&
                s.Status != "Cancelled" &&
                request.StartTime < s.EndTime.AddMinutes(CleaningBufferMinutes) &&
                bufferEnd > s.StartTime);

            if (conflict)
                return (false, ShowtimeMessages.RoomConflict, 409, null);

            // ── Tạo suất chiếu ────────────────────────────────────────────────────
            var showtime = new Showtime
            {
                MovieId   = request.MovieId,
                RoomId    = request.RoomId,
                StartTime = request.StartTime,
                EndTime   = endTime,
                BasePrice = request.BasePrice,
                Status    = "Active"
            };

            _context.Showtimes.Add(showtime);
            await _context.SaveChangesAsync();

            return (true, ShowtimeMessages.CreateSuccess, 201, showtime);
        }

        // 👑 7. CẬP NHẬT SUẤT CHIẾU
        public async Task<(bool IsSuccess, string Message, int StatusCode, object? Data)> UpdateAsync(int id, UpdateShowtimeRequest request)
        {
            var showtime = await _context.Showtimes.FindAsync(id);
            if (showtime == null)
                return (false, ShowtimeMessages.NotFoundWithId(id), 404, null);

            // ── Validate trạng thái hợp lệ ──────────────────────────────────────
            if (!ShowtimeMessages.ValidStatuses.Contains(request.Status))
                return (false, ShowtimeMessages.InvalidStatus(request.Status), 400, null);

            // ── Validate phim ────────────────────────────────────────────────────
            var movie = await _context.Movies.FindAsync(request.MovieId);
            if (movie == null)
                return (false, ShowtimeMessages.MovieNotFound, 404, null);

            // ── Validate phòng ────────────────────────────────────────────────────
            var room = await _context.Rooms.FindAsync(request.RoomId);
            if (room == null || !room.IsActive)
                return (false, ShowtimeMessages.RoomNotFound, 404, null);

            // ── Validate thời gian ────────────────────────────────────────────────
            if (request.StartTime < DateTime.Now && showtime.Status == "Active")
                return (false, ShowtimeMessages.StartTimePast, 400, null);

            // ── Tính lại EndTime nếu phim hoặc StartTime thay đổi ────────────────
            var endTime = request.StartTime.AddMinutes(movie.Duration);

            // ── Kiểm tra xung đột (loại trừ chính suất chiếu đang sửa) ──────────
            var bufferEnd = endTime.AddMinutes(CleaningBufferMinutes);
            var conflict = await _context.Showtimes.AnyAsync(s =>
                s.ShowTimeId != id &&
                s.RoomId == request.RoomId &&
                s.Status != "Cancelled" &&
                request.StartTime < s.EndTime.AddMinutes(CleaningBufferMinutes) &&
                bufferEnd > s.StartTime);

            if (conflict)
                return (false, ShowtimeMessages.RoomConflict, 409, null);

            // ── Áp dụng thay đổi ──────────────────────────────────────────────────
            showtime.MovieId   = request.MovieId;
            showtime.RoomId    = request.RoomId;
            showtime.StartTime = request.StartTime;
            showtime.EndTime   = endTime;
            showtime.BasePrice = request.BasePrice;
            showtime.Status    = request.Status;

            await _context.SaveChangesAsync();

            return (true, ShowtimeMessages.UpdateSuccess, 200, showtime);
        }

        // 👑 8. HUỶ SUẤT CHIẾU (thay đổi Status → Cancelled, không xoá dữ liệu)
        public async Task<(bool IsSuccess, string Message, int StatusCode, object? Data)> CancelAsync(int id)
        {
            var showtime = await _context.Showtimes.FindAsync(id);
            if (showtime == null)
                return (false, ShowtimeMessages.NotFoundWithId(id), 404, null);

            if (showtime.Status == "Cancelled")
                return (false, "Suất chiếu này đã được huỷ trước đó.", 409, null);

            showtime.Status = "Cancelled";
            await _context.SaveChangesAsync();

            return (true, ShowtimeMessages.CancelSuccess, 200, null);
        }

        // 👑 9. XOÁ SUẤT CHIẾU (chỉ khi chưa có vé đặt)
        public async Task<(bool IsSuccess, string Message, int StatusCode, object? Data)> DeleteAsync(int id)
        {
            var showtime = await _context.Showtimes
                .Include(s => s.Bookings)
                .FirstOrDefaultAsync(s => s.ShowTimeId == id);

            if (showtime == null)
                return (false, ShowtimeMessages.NotFoundWithId(id), 404, null);

            // Không cho xoá nếu đã có booking
            if (showtime.Bookings.Any())
                return (false, ShowtimeMessages.HasBookings, 409, null);

            _context.Showtimes.Remove(showtime);
            await _context.SaveChangesAsync();

            return (true, ShowtimeMessages.DeleteSuccess, 200, null);
        }
    }
}
