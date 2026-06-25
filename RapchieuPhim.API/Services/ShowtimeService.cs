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
        Task<List<VwShowtimeDetail>> SearchAsync(int? movieId, string? showDate, int? cinemaId);
        Task<object> GetSeatsByShowtimeAsync(int showtimeId);
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

        // ────────────────────────────────────────────────────────────────────────
        // HELPER: Parse ngày + giờ từ string, trả về lỗi nếu sai định dạng
        // ────────────────────────────────────────────────────────────────────────
        private static (bool Ok, string Error, DateTime Value) ParseDateTime(
            string showDate, string time, string formatErrorMsg)
        {
            if (!DateOnly.TryParseExact(showDate, "yyyy-MM-dd",
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None, out var date))
                return (false, ShowtimeMessages.ShowDateInvalidFormat, default);

            if (!TimeOnly.TryParseExact(time, new[] { "HH:mm", "H:mm" },
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None, out var t))
                return (false, formatErrorMsg, default);

            return (true, string.Empty, date.ToDateTime(t));
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
                .Where(s => s.MovieId == movieId && s.Status == "Active")
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

        // 🔓 5b. TìM KIẾM SUẤT CHIẾU (Showtime Search)
        // Lọc theo: phim, ngày chiếu, rạp
        public async Task<List<VwShowtimeDetail>> SearchAsync(int? movieId, string? showDate, int? cinemaId)
        {
            var query = _context.VwShowtimeDetails
                .Where(s => s.Status == ShowtimeMessages.StatusActive)
                .AsQueryable();

            if (movieId.HasValue)
                query = query.Where(s => s.MovieId == movieId.Value);

            if (!string.IsNullOrWhiteSpace(showDate))
            {
                if (!DateOnly.TryParseExact(showDate, "yyyy-MM-dd",
                        System.Globalization.CultureInfo.InvariantCulture,
                        System.Globalization.DateTimeStyles.None, out var date))
                    return new List<VwShowtimeDetail>();

                var dayStart = date.ToDateTime(TimeOnly.MinValue);
                var dayEnd   = date.ToDateTime(TimeOnly.MaxValue);
                query = query.Where(s => s.StartTime >= dayStart && s.StartTime <= dayEnd);
            }

            if (cinemaId.HasValue)
                query = query.Where(s => s.CinemaId == cinemaId.Value);

            return await query.OrderBy(s => s.StartTime).ToListAsync();
        }

        // 🔓 5c. SƠ ĐỒ GHỪ THEO SUẤT CHIẾU (Seat Selection)
        // Trả về toàn bộ ghế của phòng + trạng thái: Available | Booked | Held
        public async Task<object> GetSeatsByShowtimeAsync(int showtimeId)
        {
            var showtime = await _context.Showtimes
                .Include(s => s.Room)
                .FirstOrDefaultAsync(s => s.ShowTimeId == showtimeId);

            if (showtime == null)
                return new { Error = ShowtimeMessages.NotFoundWithId(showtimeId) };

            // Lấy tất cả ghế của phòng (active)
            var seats = await _context.Seats
                .Where(s => s.RoomId == showtime.RoomId && s.IsActive)
                .OrderBy(s => s.SeatRow)
                .ThenBy(s => s.SeatNumber)
                .ToListAsync();

            // Lấy danh sách SeatId đã đặt thành công cho suất này
            var bookedSeatIds = await _context.Bookings
                .Where(b => b.ShowTimeId == showtimeId &&
                            b.Status != ShowtimeMessages.StatusCancelled)
                .Select(b => b.SeatId)
                .ToListAsync();

            // Nhóm ghế theo hàng, gắn trạng thái
            var layout = seats
                .GroupBy(s => s.SeatRow.Trim())
                .OrderBy(g => g.Key)
                .Select(g => new
                {
                    Row = g.Key,
                    Seats = g.Select(s => new
                    {
                        s.SeatId,
                        s.SeatNumber,
                        s.SeatType,
                        SeatStatus = bookedSeatIds.Contains(s.SeatId)
                            ? ShowtimeMessages.SeatStatusBooked
                            : ShowtimeMessages.SeatStatusAvailable
                    })
                });

            return new
            {
                ShowTimeId   = showtimeId,
                RoomId       = showtime.RoomId,
                RoomName     = showtime.Room.RoomName,
                StartTime    = showtime.StartTime,
                EndTime      = showtime.EndTime,
                BasePrice    = showtime.BasePrice,
                TotalSeats   = seats.Count,
                BookedSeats  = bookedSeatIds.Count,
                Layout       = layout
            };
        }

        // 👑 6. TẠO SUẤT CHIẾU MỚI
        public async Task<(bool IsSuccess, string Message, int StatusCode, object? Data)> CreateAsync(CreateShowtimeRequest request)
        {
            // ── Parse ngày + giờ bắt đầu ─────────────────────────────────────────
            var (startOk, startErr, startTime) = ParseDateTime(
                request.ShowDate, request.StartTime, ShowtimeMessages.StartTimeInvalidFormat);
            if (!startOk) return (false, startErr, 400, null);

            // ── Parse giờ kết thúc ───────────────────────────────────────────────
            var (endOk, endErr, endTime) = ParseDateTime(
                request.ShowDate, request.EndTime, ShowtimeMessages.EndTimeInvalidFormat);
            if (!endOk) return (false, endErr, 400, null);

            // ── Validate: giờ kết thúc phải sau giờ bắt đầu ─────────────────────
            if (endTime <= startTime)
                return (false, ShowtimeMessages.EndTimeBeforeStart, 400, null);

            // ── Validate: không được trong quá khứ ──────────────────────────────
            if (startTime < DateTime.Now)
                return (false, ShowtimeMessages.StartTimePast, 400, null);

            // ── Validate phim tồn tại và còn hoạt động ───────────────────────────
            var movie = await _context.Movies.FindAsync(request.MovieId);
            if (movie == null)
                return (false, ShowtimeMessages.MovieNotFound, 404, null);

            if (ShowtimeMessages.InactiveMovieStatuses.Contains(movie.Status))
                return (false, ShowtimeMessages.MovieNotActive, 409, null);

            // ── Validate phòng tồn tại và đang hoạt động ─────────────────────────
            var room = await _context.Rooms.FindAsync(request.RoomId);
            if (room == null || !room.IsActive)
                return (false, ShowtimeMessages.RoomNotFound, 404, null);

            // ── Kiểm tra xung đột lịch chiếu trong phòng ─────────────────────────
            //    Xung đột khi: startTime_mới < (EndTime_cũ + buffer) VÀ endTime_mới > StartTime_cũ
            var bufferEnd = endTime.AddMinutes(CleaningBufferMinutes);
            var conflict = await _context.Showtimes.AnyAsync(s =>
                s.RoomId == request.RoomId &&
                s.Status != ShowtimeMessages.StatusCancelled &&
                startTime < s.EndTime.AddMinutes(CleaningBufferMinutes) &&
                bufferEnd > s.StartTime);

            if (conflict)
                return (false, ShowtimeMessages.RoomConflict, 409, null);

            // ── Tạo suất chiếu ────────────────────────────────────────────────────
            var showtime = new Showtime
            {
                MovieId   = request.MovieId,
                RoomId    = request.RoomId,
                StartTime = startTime,
                EndTime   = endTime,
                BasePrice = request.BasePrice,
                Status    = ShowtimeMessages.StatusActive
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

            // ── Parse ngày + giờ bắt đầu ─────────────────────────────────────────
            var (startOk, startErr, startTime) = ParseDateTime(
                request.ShowDate, request.StartTime, ShowtimeMessages.StartTimeInvalidFormat);
            if (!startOk) return (false, startErr, 400, null);

            // ── Parse giờ kết thúc ───────────────────────────────────────────────
            var (endOk, endErr, endTime) = ParseDateTime(
                request.ShowDate, request.EndTime, ShowtimeMessages.EndTimeInvalidFormat);
            if (!endOk) return (false, endErr, 400, null);

            // ── Validate: giờ kết thúc phải sau giờ bắt đầu ─────────────────────
            if (endTime <= startTime)
                return (false, ShowtimeMessages.EndTimeBeforeStart, 400, null);

            // ── Validate: không được trong quá khứ (chỉ với suất đang Active) ────
            if (startTime < DateTime.Now && showtime.Status == "Active")
                return (false, ShowtimeMessages.StartTimePast, 400, null);

            // ── Validate phim ─────────────────────────────────────────────────────
            var movie = await _context.Movies.FindAsync(request.MovieId);
            if (movie == null)
                return (false, ShowtimeMessages.MovieNotFound, 404, null);

            // ── Validate phòng ────────────────────────────────────────────────────
            var room = await _context.Rooms.FindAsync(request.RoomId);
            if (room == null || !room.IsActive)
                return (false, ShowtimeMessages.RoomNotFound, 404, null);

            // ── Kiểm tra xung đột (loại trừ chính suất chiếu đang sửa) ──────────
            var bufferEnd = endTime.AddMinutes(CleaningBufferMinutes);
            var conflict = await _context.Showtimes.AnyAsync(s =>
                s.ShowTimeId != id &&
                s.RoomId == request.RoomId &&
                s.Status != ShowtimeMessages.StatusCancelled &&
                startTime < s.EndTime.AddMinutes(CleaningBufferMinutes) &&
                bufferEnd > s.StartTime);

            if (conflict)
                return (false, ShowtimeMessages.RoomConflict, 409, null);

            // ── Áp dụng thay đổi ──────────────────────────────────────────────────
            showtime.MovieId   = request.MovieId;
            showtime.RoomId    = request.RoomId;
            showtime.StartTime = startTime;
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

            if (showtime.Status == ShowtimeMessages.StatusCancelled)
                return (false, ShowtimeMessages.AlreadyCancelled, 409, null);

            showtime.Status = ShowtimeMessages.StatusCancelled;
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
