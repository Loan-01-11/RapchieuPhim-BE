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
            return await _context.Seats
                .Where(s => s.RoomId == roomId && s.IsActive)
                .OrderBy(s => s.SeatRow)
                .ThenBy(s => s.SeatNumber)
                .ToListAsync();
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
                .OrderBy(s => s.SeatRow)
                .ThenBy(s => s.SeatNumber)
                .ToListAsync();

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
                        s.IsActive
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

            seat.SeatRow    = request.SeatRow.ToUpper().Trim();
            seat.SeatNumber = request.SeatNumber.Trim();
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

            var room = await _context.Rooms.FindAsync(seat.RoomId);
            if (room != null && room.TotalSeats > 0) room.TotalSeats -= 1;

            _context.Seats.Remove(seat);
            await _context.SaveChangesAsync();

            return (true, SeatMessages.DeleteSeatSuccess, 200, null);
        }
    }
}
