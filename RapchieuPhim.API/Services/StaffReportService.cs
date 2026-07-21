using Microsoft.EntityFrameworkCore;
using RapchieuPhim.API.DTO.DTORequest;
using RapchieuPhim.API.Models;

namespace RapchieuPhim.API.Services
{
    public interface IStaffReportService
    {
        Task<List<Staffreport>> GetAllAsync();
        Task<Staffreport?> GetByIdAsync(int id);
        Task<List<Staffreport>> GetByStaffAsync(int staffId);
        Task<List<Staffreport>> GetByCinemaAsync(int cinemaId);
        Task<(bool IsSuccess, string Message, int StatusCode, object? Data)> CreateAsync(CreateStaffReportRequest request);
        Task<(bool IsSuccess, string Message, int StatusCode, object? Data)> DeleteAsync(int id);
    }

    public class StaffReportService : IStaffReportService
    {
        private readonly CinemaManagementContext _context;

        public StaffReportService(CinemaManagementContext context)
        {
            _context = context;
        }

        // 🔓 1. LẤY TẤT CẢ BÁO CÁO CA LÀM VIỆC
        public async Task<List<Staffreport>> GetAllAsync()
        {
            return await _context.Staffreports
                .Include(r => r.Staff)
                .Include(r => r.Cinema)
                .OrderByDescending(r => r.GeneratedAt)
                .ThenByDescending(r => r.ReportDate)
                .ToListAsync();
        }

        // 🔓 2. XEM CHI TIẾT BÁO CÁO THEO ID
        public async Task<Staffreport?> GetByIdAsync(int id)
        {
            return await _context.Staffreports
                .Include(r => r.Staff)
                .Include(r => r.Cinema)
                .FirstOrDefaultAsync(r => r.ReportId == id);
        }

        // 🔓 3. LẤY DANH SÁCH BÁO CÁO THEO NHÂN VIÊN
        public async Task<List<Staffreport>> GetByStaffAsync(int staffId)
        {
            return await _context.Staffreports
                .Include(r => r.Staff)
                .Include(r => r.Cinema)
                .Where(r => r.StaffId == staffId)
                .OrderByDescending(r => r.GeneratedAt)
                .ToListAsync();
        }

        // 🔓 4. LẤY DANH SÁCH BÁO CÁO THEO RẠP
        public async Task<List<Staffreport>> GetByCinemaAsync(int cinemaId)
        {
            return await _context.Staffreports
                .Include(r => r.Staff)
                .Include(r => r.Cinema)
                .Where(r => r.CinemaId == cinemaId)
                .OrderByDescending(r => r.GeneratedAt)
                .ToListAsync();
        }

        // 👑 5. TẠO BÁO CÁO CA LÀM VIỆC
        // Tự động gán GeneratedAt = thời điểm hiện tại
        public async Task<(bool IsSuccess, string Message, int StatusCode, object? Data)> CreateAsync(CreateStaffReportRequest request)
        {
            // Kiểm tra nhân viên tồn tại
            var staffExists = await _context.Users.AnyAsync(u => u.UserId == request.StaffId && u.IsActive);
            if (!staffExists)
                return (false, $"Không tìm thấy nhân viên với id {request.StaffId}.", 404, null);

            // Kiểm tra rạp tồn tại
            var cinemaExists = await _context.Cinemas.AnyAsync(c => c.CinemaId == request.CinemaId);
            if (!cinemaExists)
                return (false, $"Không tìm thấy rạp với id {request.CinemaId}.", 404, null);

            // Parse ngày báo cáo
            if (!DateOnly.TryParseExact(request.ReportDate, "yyyy-MM-dd",
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None, out var reportDate))
                return (false, "Định dạng ReportDate không hợp lệ, vui lòng dùng yyyy-MM-dd.", 400, null);

            var report = new Staffreport
            {
                StaffId       = request.StaffId,
                CinemaId      = request.CinemaId,
                ReportDate    = reportDate,
                Summary       = request.Summary,
                TotalBookings = request.TotalBookings,
                TotalOrders   = request.TotalOrders,
                TotalRevenue  = request.TotalRevenue,
                GeneratedAt   = DateTime.Now
            };

            _context.Staffreports.Add(report);
            await _context.SaveChangesAsync();

            return (true, "Tạo báo cáo ca làm việc thành công.", 201, report);
        }

        // 👑 6. XÓA BÁO CÁO (chỉ Admin)
        public async Task<(bool IsSuccess, string Message, int StatusCode, object? Data)> DeleteAsync(int id)
        {
            var report = await _context.Staffreports.FindAsync(id);
            if (report == null)
                return (false, $"Không tìm thấy báo cáo với id {id}.", 404, null);

            _context.Staffreports.Remove(report);
            await _context.SaveChangesAsync();

            return (true, "Xóa báo cáo thành công.", 200, null);
        }
    }
}
