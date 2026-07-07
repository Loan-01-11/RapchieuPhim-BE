using Microsoft.EntityFrameworkCore;
using RapchieuPhim.API.DTO.DTORequest;
using RapchieuPhim.API.Models;

namespace RapchieuPhim.API.Services
{
    public interface IStaffShiftService
    {
        Task<List<Staffshift>> GetAllAsync();
        Task<Staffshift?> GetByIdAsync(int id);
        Task<List<Staffshift>> GetByStaffAsync(int staffId);
        Task<List<Staffshift>> GetByCinemaAsync(int cinemaId);
        Task<(bool IsSuccess, string Message, int StatusCode, object? Data)> CreateAsync(CreateStaffShiftRequest request);
        Task<(bool IsSuccess, string Message, int StatusCode, object? Data)> CloseShiftAsync(int id, CloseStaffShiftRequest request);
        Task<(bool IsSuccess, string Message, int StatusCode, object? Data)> DeleteAsync(int id);
    }

    public class StaffShiftService : IStaffShiftService
    {
        private readonly CinemaManagementContext _context;

        public StaffShiftService(CinemaManagementContext context)
        {
            _context = context;
        }

        // 🔓 1. LẤY TẤT CẢ CA LÀM VIỆC
        public async Task<List<Staffshift>> GetAllAsync()
        {
            return await _context.Staffshifts
                .OrderByDescending(s => s.ShiftDate)
                .ToListAsync();
        }

        // 🔓 2. XEM CHI TIẾT CA LÀM VIỆC THEO ID
        public async Task<Staffshift?> GetByIdAsync(int id)
        {
            return await _context.Staffshifts.FindAsync(id);
        }

        // 🔓 3. LẤY DANH SÁCH CA THEO NHÂN VIÊN
        public async Task<List<Staffshift>> GetByStaffAsync(int staffId)
        {
            return await _context.Staffshifts
                .Where(s => s.StaffId == staffId)
                .OrderByDescending(s => s.ShiftDate)
                .ToListAsync();
        }

        // 🔓 4. LẤY DANH SÁCH CA THEO RẠP
        public async Task<List<Staffshift>> GetByCinemaAsync(int cinemaId)
        {
            return await _context.Staffshifts
                .Where(s => s.CinemaId == cinemaId)
                .OrderByDescending(s => s.ShiftDate)
                .ToListAsync();
        }

        // 👑 5. MỞ CA LÀM VIỆC MỚI
        // Tự động gán ShiftStart = Now, ShiftDate = hôm nay, Status = "Open"
        public async Task<(bool IsSuccess, string Message, int StatusCode, object? Data)> CreateAsync(CreateStaffShiftRequest request)
        {
            // Kiểm tra nhân viên tồn tại
            var staffExists = await _context.Users.AnyAsync(u => u.UserId == request.StaffId && u.IsActive);
            if (!staffExists)
                return (false, $"Không tìm thấy nhân viên với id {request.StaffId}.", 404, null);

            // Kiểm tra rạp tồn tại
            var cinemaExists = await _context.Cinemas.AnyAsync(c => c.CinemaId == request.CinemaId);
            if (!cinemaExists)
                return (false, $"Không tìm thấy rạp với id {request.CinemaId}.", 404, null);

            // Chặn mở 2 ca cùng lúc cho cùng 1 nhân viên
            var openShiftExists = await _context.Staffshifts.AnyAsync(s =>
                s.StaffId == request.StaffId && s.Status == "Open");
            if (openShiftExists)
                return (false, "Nhân viên này đang có ca chưa kết thúc. Vui lòng đóng ca trước.", 409, null);

            var shift = new Staffshift
            {
                StaffId    = request.StaffId,
                CinemaId   = request.CinemaId,
                ShiftDate  = DateOnly.FromDateTime(DateTime.Now),
                ShiftStart = DateTime.Now,
                ShiftEnd   = null,
                Status     = "Open",
                TotalBookings = 0,
                TotalOrders   = 0,
                TotalRevenue  = 0
            };

            _context.Staffshifts.Add(shift);
            await _context.SaveChangesAsync();

            return (true, "Mở ca làm việc thành công.", 201, shift);
        }

        // 👑 6. ĐÓNG CA LÀM VIỆC – GHI NHẬN KẾT QUẢ
        // Tự động gán ShiftEnd = Now, Status = "Closed"
        public async Task<(bool IsSuccess, string Message, int StatusCode, object? Data)> CloseShiftAsync(int id, CloseStaffShiftRequest request)
        {
            var shift = await _context.Staffshifts.FindAsync(id);
            if (shift == null)
                return (false, $"Không tìm thấy ca làm việc với id {id}.", 404, null);

            // Chỉ đóng được ca đang Open
            if (shift.Status == "Closed")
                return (false, "Ca làm việc này đã được đóng trước đó.", 409, null);

            shift.ShiftEnd      = DateTime.Now;
            shift.TotalBookings = request.TotalBookings;
            shift.TotalOrders   = request.TotalOrders;
            shift.TotalRevenue  = request.TotalRevenue;
            shift.Summary       = request.Summary;
            shift.Status        = "Closed";

            await _context.SaveChangesAsync();

            return (true, "Đóng ca làm việc thành công.", 200, shift);
        }

        // 👑 7. XÓA CA LÀM VIỆC (chỉ Admin, chỉ ca đã đóng)
        public async Task<(bool IsSuccess, string Message, int StatusCode, object? Data)> DeleteAsync(int id)
        {
            var shift = await _context.Staffshifts.FindAsync(id);
            if (shift == null)
                return (false, $"Không tìm thấy ca làm việc với id {id}.", 404, null);

            if (shift.Status == "Open")
                return (false, "Không thể xóa ca đang hoạt động. Vui lòng đóng ca trước.", 409, null);

            _context.Staffshifts.Remove(shift);
            await _context.SaveChangesAsync();

            return (true, "Xóa ca làm việc thành công.", 200, null);
        }
    }
}
