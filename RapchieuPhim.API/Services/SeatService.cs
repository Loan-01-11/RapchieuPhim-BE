using Microsoft.EntityFrameworkCore;
using RapchieuPhim.API.Constants;
using RapchieuPhim.API.DTO.DTORequest;
using RapchieuPhim.API.Models;

namespace RapchieuPhim.API.Services
{
    public interface ISeatService
    {
        Task<List<Seat>> GetAllAsync();
        Task<Seat?> GetByIdAsync(int id);
        Task<List<Seat>> GetByRoomAsync(int roomId);
        Task<object> GetLayoutByRoomAsync(int roomId);
        Task<List<VwAvailableSeat>> GetAvailableByShowtimeAsync(int showtimeId);
        Task<(bool IsSuccess, string Message, int StatusCode, object? Data)> CreateAsync(CreateSeatRequest request);
        Task<(bool IsSuccess, string Message, int StatusCode, object? Data)> CreateBatchAsync(CreateSeatBatchRequest request);
        Task<(bool IsSuccess, string Message, int StatusCode, object? Data)> UpdateAsync(int id, UpdateSeatRequest request);
        Task<(bool IsSuccess, string Message, int StatusCode, object? Data)> UpdateTypeBatchAsync(UpdateSeatTypeBatchRequest request);
        Task<(bool IsSuccess, string Message, int StatusCode, object? Data)> ToggleStatusBatchAsync(ToggleSeatStatusBatchRequest request);
        Task<(bool IsSuccess, string Message, int StatusCode, object? Data)> CreateRangeAsync(CreateSeatRangeRequest request);
        Task<(bool IsSuccess, string Message, int StatusCode, object? Data)> UpdateLayoutAsync(int roomId, UpdateSeatLayoutRequest request);
        Task<(bool IsSuccess, string Message, int StatusCode, object? Data)> DeleteAsync(int id);
    }

    public class SeatService : ISeatService
    {
        private readonly CinemaManagementContext _context;

        public SeatService(CinemaManagementContext context)
        {
            _context = context;
        }

        // 🔓 1. LẤY TẤT CẢ GHẾ
        public async Task<List<Seat>> GetAllAsync()
        {
            return await _context.Seats
                .OrderBy(s => s.RoomId)
                .ThenBy(s => s.SeatRow)
                .ThenBy(s => s.SeatNumber)
                .ToListAsync();
        }

        // 🔓 2. XEM CHI TIẾT GHẾ THEO ID
        public async Task<Seat?> GetByIdAsync(int id)
        {
            return await _context.Seats.FindAsync(id);
        }

        // 🔓 3. LẤY DANH SÁCH GHẾ THEO PHÒNG
        public async Task<List<Seat>> GetByRoomAsync(int roomId)
        {
            var seats = await _context.Seats.AsNoTracking()
                .Where(s => s.RoomId == roomId)
                .ToListAsync();
            return seats.OrderBy(s => s.SeatRow).ThenBy(SeatOrdinal).ToList();
        }

        // 🔓 4. LẤY SƠ ĐỒ GHẾ THEO PHÒNG (nhóm theo hàng)
        public async Task<object> GetLayoutByRoomAsync(int roomId)
        {
            var room = await _context.Rooms
                .Where(r => r.RoomId == roomId)
                .Select(r => new { r.RoomId, r.RoomName, r.RoomType, r.TotalSeats })
                .FirstOrDefaultAsync();

            var seats = await _context.Seats
                .Where(s => s.RoomId == roomId)
                .ToListAsync();
            seats = seats.OrderBy(s => s.SeatRow).ThenBy(SeatOrdinal).ToList();

            // Nhóm ghế theo hàng để FE dễ vẽ sơ đồ
            var layout = seats
                .GroupBy(s => s.SeatRow.Trim())
                .OrderBy(g => g.Key)
                .Select(g => new
                {
                    Row   = g.Key,
                    Seats = g.Select(s => new
                    {
                        s.SeatId,
                        s.SeatNumber,
                        s.SeatType,
                        s.IsActive,
                        s.CoupleGroupId
                    }).ToList()
                }).ToList();

            return new { Room = room, Layout = layout };
        }

        // 🔓 5. LẤY GHẾ CÒN TRỐNG THEO SUẤT CHIẾU (dùng View)
        public async Task<List<VwAvailableSeat>> GetAvailableByShowtimeAsync(int showtimeId)
        {
            return await _context.VwAvailableSeats
                .Where(v => v.ShowTimeId == showtimeId)
                .OrderBy(v => v.SeatNumber)
                .ToListAsync();
        }

        // 👑 6. TẠO MỘT GHẾ MỚI
        public async Task<(bool IsSuccess, string Message, int StatusCode, object? Data)> CreateAsync(CreateSeatRequest request)
        {
            // Kiểm tra phòng tồn tại
            var roomExists = await _context.Rooms.AnyAsync(r => r.RoomId == request.RoomId && r.IsActive);
            if (!roomExists)
                return (false, SeatMessages.RoomNotFound, 404, null);

            // Kiểm tra loại ghế hợp lệ
            if (!SeatMessages.ValidSeatTypes.Contains(request.SeatType))
                return (false, SeatMessages.InvalidSeatType(request.SeatType), 400, null);

            // Kiểm tra ghế trùng
            var exists = await _context.Seats.AnyAsync(s =>
                s.RoomId == request.RoomId &&
                s.SeatRow == request.SeatRow.ToUpper() &&
                s.SeatNumber == request.SeatNumber);
            if (exists)
                return (false, string.Format(SeatMessages.SeatAlreadyExists, request.SeatRow.ToUpper(), request.SeatNumber), 409, null);

            var seat = new Seat
            {
                RoomId     = request.RoomId,
                SeatRow    = request.SeatRow.ToUpper().Trim(),
                SeatNumber = request.SeatNumber.Trim(),
                SeatType   = request.SeatType.Trim(),
                IsActive   = request.IsActive
            };

            _context.Seats.Add(seat);
            var room = await _context.Rooms.FindAsync(request.RoomId);
            if (room != null) room.TotalSeats += 1;
            await _context.SaveChangesAsync();

            return (true, SeatMessages.CreateSeatSuccess, 201, seat);
        }

        // 👑 7. TẠO HÀNG LOẠT GHẾ (Batch)
        public async Task<(bool IsSuccess, string Message, int StatusCode, object? Data)> CreateBatchAsync(CreateSeatBatchRequest request)
        {
            // Kiểm tra phòng tồn tại
            var roomExists = await _context.Rooms.AnyAsync(r => r.RoomId == request.RoomId && r.IsActive);
            if (!roomExists)
                return (false, SeatMessages.RoomNotFound, 404, null);

            // Kiểm tra loại ghế hợp lệ
            if (!SeatMessages.ValidSeatTypes.Contains(request.SeatType))
                return (false, SeatMessages.InvalidSeatType(request.SeatType), 400, null);

            var newSeats = new List<Seat>();
            var skipped  = new List<string>();

            foreach (var row in request.Rows)
            {
                for (int num = 1; num <= request.SeatsPerRow; num++)
                {
                    var rowUpper   = row.ToUpper().Trim();
                    var seatNumber = $"{rowUpper}{num}";

                    // Bỏ qua nếu đã tồn tại
                    var exists = await _context.Seats.AnyAsync(s =>
                        s.RoomId == request.RoomId &&
                        s.SeatRow == rowUpper &&
                        s.SeatNumber == seatNumber);

                    if (exists)
                    {
                        skipped.Add($"{rowUpper}{seatNumber}");
                        continue;
                    }

                    newSeats.Add(new Seat
                    {
                        RoomId     = request.RoomId,
                        SeatRow    = rowUpper,
                        SeatNumber = seatNumber,
                        SeatType   = request.SeatType.Trim(),
                        IsActive   = true
                    });
                }
            }

            if (newSeats.Count > 0)
            {
                _context.Seats.AddRange(newSeats);
                var room = await _context.Rooms.FindAsync(request.RoomId);
                if (room != null) room.TotalSeats += newSeats.Count;
                await _context.SaveChangesAsync();
            }

            return (true, SeatMessages.CreateBatchSuccess, 201, new
            {
                Created = newSeats.Count,
                Skipped = skipped.Count,
                SkippedSeats = skipped
            });
        }

        // 👑 8. CẬP NHẬT THÔNG TIN GHẾ
        public async Task<(bool IsSuccess, string Message, int StatusCode, object? Data)> UpdateAsync(int id, UpdateSeatRequest request)
        {
            var seat = await _context.Seats.FindAsync(id);
            if (seat == null)
                return (false, ValidationMessages.SeatNotFoundWithId(id), 404, null);

            // Kiểm tra loại ghế hợp lệ
            if (!SeatMessages.ValidSeatTypes.Contains(request.SeatType))
                return (false, SeatMessages.InvalidSeatType(request.SeatType), 400, null);

            // Kiểm tra ghế trùng (ngoại trừ chính ghế đang sửa)
            var duplicate = await _context.Seats.AnyAsync(s =>
                s.RoomId == seat.RoomId &&
                s.SeatRow == request.SeatRow.ToUpper() &&
                s.SeatNumber == request.SeatNumber &&
                s.SeatId != id);
            if (duplicate)
                return (false, string.Format(SeatMessages.SeatAlreadyExists, request.SeatRow.ToUpper(), request.SeatNumber), 409, null);

            var hasBooking = await _context.Bookings.AnyAsync(b => b.SeatId == id);
            var newRow = request.SeatRow.ToUpper().Trim();
            var newNumber = NormalizeSeatCode(newRow, request.SeatNumber);
            if (hasBooking && (seat.SeatRow != newRow || seat.SeatNumber != newNumber))
                return (false, "Ghế đã có booking/vé nên không được đổi mã; chỉ có thể chuyển Inactive.", 409, null);

            seat.SeatRow    = newRow;
            seat.SeatNumber = newNumber;
            seat.SeatType   = request.SeatType.Trim();
            seat.IsActive   = request.IsActive;

            await _context.SaveChangesAsync();

            return (true, SeatMessages.UpdateSeatSuccess, 200, seat);
        }

        // 👑 9. ĐỔI LOẠI GHẾ HÀNG LOẠT
        public async Task<(bool IsSuccess, string Message, int StatusCode, object? Data)> UpdateTypeBatchAsync(UpdateSeatTypeBatchRequest request)
        {
            if (request.SeatIds == null || request.SeatIds.Count == 0)
                return (false, SeatMessages.SeatIdsRequired, 400, null);

            if (!SeatMessages.ValidSeatTypes.Contains(request.SeatType))
                return (false, SeatMessages.InvalidSeatType(request.SeatType), 400, null);

            var seats = await _context.Seats
                .Where(s => request.SeatIds.Contains(s.SeatId))
                .ToListAsync();

            foreach (var seat in seats)
                seat.SeatType = request.SeatType.Trim();

            await _context.SaveChangesAsync();

            return (true, SeatMessages.UpdateTypeSuccess, 200, new { Updated = seats.Count });
        }

        // 👑 10. BẬT/TẮT TRẠNG THÁI GHẾ HÀNG LOẠT
        public async Task<(bool IsSuccess, string Message, int StatusCode, object? Data)> ToggleStatusBatchAsync(ToggleSeatStatusBatchRequest request)
        {
            if (request.SeatIds == null || request.SeatIds.Count == 0)
                return (false, SeatMessages.SeatIdsRequired, 400, null);

            var seats = await _context.Seats
                .Where(s => request.SeatIds.Contains(s.SeatId))
                .ToListAsync();

            foreach (var seat in seats)
                seat.IsActive = request.IsActive;

            await _context.SaveChangesAsync();

            return (true, SeatMessages.UpdateStatusSuccess, 200, new { Updated = seats.Count });
        }

        // 👑 11. XÓA GHẾ
        public async Task<(bool IsSuccess, string Message, int StatusCode, object? Data)> DeleteAsync(int id)
        {
            var seat = await _context.Seats.FindAsync(id);
            if (seat == null)
                return (false, ValidationMessages.SeatNotFoundWithId(id), 404, null);

            if (await _context.Bookings.AnyAsync(b => b.SeatId == id))
            {
                seat.IsActive = false;
                await _context.SaveChangesAsync();
                return (true, "Ghế đã có booking/vé nên được chuyển sang Inactive thay vì xóa.", 200, seat);
            }

            var room = await _context.Rooms.FindAsync(seat.RoomId);
            _context.Seats.Remove(seat);
            await _context.SaveChangesAsync();
            if (room != null) room.TotalSeats = await _context.Seats.CountAsync(s => s.RoomId == seat.RoomId);
            await _context.SaveChangesAsync();

            return (true, SeatMessages.DeleteSeatSuccess, 200, null);
        }

        public async Task<(bool IsSuccess, string Message, int StatusCode, object? Data)> CreateRangeAsync(CreateSeatRangeRequest request)
        {
            var row = request.SeatRow.Trim().ToUpperInvariant();
            var type = NormalizeSeatType(request.SeatType);
            if (request.FromSeat > request.ToSeat)
                return (false, "Từ số phải nhỏ hơn hoặc bằng Đến số.", 400, null);
            if (type == null) return (false, SeatMessages.InvalidSeatType(request.SeatType), 400, null);

            var room = await _context.Rooms.SingleOrDefaultAsync(r => r.RoomId == request.RoomId);
            if (room == null) return (false, SeatMessages.RoomNotFound, 404, null);
            var count = request.ToSeat - request.FromSeat + 1;
            if (type == "Couple" && (count % 2 != 0 || request.FromSeat % 2 == 0 || request.ToSeat % 2 != 0))
                return (false, "Ghế Couple phải tạo theo cặp liên tiếp: bắt đầu số lẻ, kết thúc số chẵn.", 400, null);

            var currentCount = await _context.Seats.CountAsync(s => s.RoomId == request.RoomId);
            if (room.TotalSeats > 0 && currentCount + count > room.TotalSeats)
                return (false, $"Số ghế mới vượt sức chứa phòng ({room.TotalSeats}).", 409, null);

            var codes = Enumerable.Range(request.FromSeat, count).Select(n => $"{row}{n}").ToHashSet(StringComparer.OrdinalIgnoreCase);
            var existingInRow = await _context.Seats.AsNoTracking()
                .Where(s => s.RoomId == request.RoomId && s.SeatRow == row)
                .Select(s => s.SeatNumber).ToListAsync();
            var duplicates = existingInRow
                .Select(number => NormalizeSeatCode(row, number))
                .Where(codes.Contains).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            if (duplicates.Count > 0)
                return (false, $"Mã ghế đã tồn tại: {string.Join(", ", duplicates)}.", 409, null);

            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var seats = new List<Seat>();
                Guid? coupleGroup = null;
                for (var number = request.FromSeat; number <= request.ToSeat; number++)
                {
                    if (type == "Couple" && (number - request.FromSeat) % 2 == 0) coupleGroup = Guid.NewGuid();
                    seats.Add(new Seat
                    {
                        RoomId = request.RoomId, SeatRow = row, SeatNumber = $"{row}{number}",
                        SeatType = type, IsActive = request.IsActive,
                        CoupleGroupId = type == "Couple" ? coupleGroup : null
                    });
                }
                _context.Seats.AddRange(seats);
                await _context.SaveChangesAsync();
                room.TotalSeats = await _context.Seats.CountAsync(s => s.RoomId == request.RoomId);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return (true, $"Đã thêm {seats.Count} ghế.", 201, seats);
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<(bool IsSuccess, string Message, int StatusCode, object? Data)> UpdateLayoutAsync(int roomId, UpdateSeatLayoutRequest request)
        {
            var room = await _context.Rooms.FindAsync(roomId);
            if (room == null) return (false, SeatMessages.RoomNotFound, 404, null);
            if (request.Changes.Select(c => c.SeatId).Distinct().Count() != request.Changes.Count)
                return (false, "Danh sách thay đổi có ghế bị trùng.", 400, null);

            var seats = await _context.Seats.Where(s => s.RoomId == roomId).ToListAsync();
            var seatMap = seats.ToDictionary(s => s.SeatId);
            foreach (var change in request.Changes)
            {
                if (!seatMap.TryGetValue(change.SeatId, out var seat))
                    return (false, $"Ghế {change.SeatId} không thuộc phòng.", 404, null);
                var type = NormalizeSeatType(change.SeatType);
                if (type == null) return (false, SeatMessages.InvalidSeatType(change.SeatType), 400, null);
                seat.SeatType = type;
                seat.IsActive = change.IsActive;
            }

            foreach (var seat in seats.Where(s => s.SeatType != "Couple")) seat.CoupleGroupId = null;
            foreach (var rowGroup in seats.Where(s => s.SeatType == "Couple").GroupBy(s => s.SeatRow))
            {
                var couples = rowGroup.OrderBy(SeatOrdinal).ToList();
                if (couples.Count % 2 != 0)
                    return (false, $"Hàng {rowGroup.Key} có số ghế Couple lẻ.", 400, null);
                for (var i = 0; i < couples.Count; i += 2)
                {
                    var first = SeatOrdinal(couples[i]);
                    var second = SeatOrdinal(couples[i + 1]);
                    if (first % 2 == 0 || second != first + 1)
                        return (false, $"Ghế Couple hàng {rowGroup.Key} phải ghép theo cặp số lẻ-chẵn liên tiếp.", 400, null);
                    var groupId = couples[i].CoupleGroupId ?? couples[i + 1].CoupleGroupId ?? Guid.NewGuid();
                    couples[i].CoupleGroupId = groupId;
                    couples[i + 1].CoupleGroupId = groupId;
                }
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                room.TotalSeats = seats.Count;
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return (true, "Lưu sơ đồ ghế thành công.", 200, new { Updated = request.Changes.Count });
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        private static int SeatOrdinal(Seat seat)
        {
            var digits = new string((seat.SeatNumber ?? "").Where(char.IsDigit).ToArray());
            return int.TryParse(digits, out var value) ? value : int.MaxValue;
        }

        private static string NormalizeSeatCode(string row, string seatNumber)
        {
            var digits = new string((seatNumber ?? "").Where(char.IsDigit).ToArray());
            return $"{row}{digits}";
        }

        private static string? NormalizeSeatType(string? value)
        {
            if (string.Equals(value?.Trim(), "Standard", StringComparison.OrdinalIgnoreCase)) return "Standard";
            if (string.Equals(value?.Trim(), "VIP", StringComparison.OrdinalIgnoreCase)) return "VIP";
            if (string.Equals(value?.Trim(), "Couple", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value?.Trim(), "Sweetbox", StringComparison.OrdinalIgnoreCase)) return "Couple";
            return null;
        }
    }
}
