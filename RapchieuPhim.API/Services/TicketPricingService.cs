using Microsoft.EntityFrameworkCore;
using RapchieuPhim.API.Constants;
using RapchieuPhim.API.DTOs.DTORequest;
using RapchieuPhim.API.DTOs.DTOResponse;
using RapchieuPhim.API.Models;

namespace RapchieuPhim.API.Services
{
    public interface ITicketPricingService
    {
        Task<List<TicketPricingResponse>> GetAllAsync();
        Task<TicketPricingResponse?> GetByIdAsync(int id);
        Task<List<TicketPricingResponse>> GetActiveAsync();
        Task<TicketPricingResponse> CreateAsync(TicketPricingRequest request, int creatorId);
        Task<(bool IsSuccess, string Message, int StatusCode)> UpdateAsync(int id, TicketPricingRequest request, string currentOperatorEmail);
        Task<(bool IsSuccess, string Message, int StatusCode)> DeleteAsync(int id, string currentOperatorEmail);
    }


    /// <summary>
    /// Lớp xử lý toàn bộ logic nghiệp vụ (Business Logic) liên quan đến Cấu hình giá vé.
    /// Giúp tách biệt mã nguồn xử lý Database ra khỏi Controller và đảm bảo an toàn doanh thu.
    /// </summary>
    public class TicketPricingService : ITicketPricingService
    {
        private readonly CinemaManagementContext _context;

        // Hàm khởi tạo: Nhận Context kết nối DB từ hệ thống .NET bơm vào thông qua cơ chế DI
        public TicketPricingService(CinemaManagementContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Lấy toàn bộ danh sách cấu hình ma trận giá vé (Dành cho Admin quản trị)
        /// </summary>
        public async Task<List<TicketPricingResponse>> GetAllAsync()
        {
            return await _context.Ticketpricings
                .Select(p => new TicketPricingResponse
                {
                    PricingId = p.PricingId,
                    RoomType = p.RoomType,
                    SeatType = p.SeatType,
                    DayType = p.DayType,
                    Price = p.Price,
                    EffectFrom = p.EffectFrom,
                    EffectTo = p.EffectTo,
                    IsActive = p.IsActive,
                    CreatedBy = p.CreatedBy
                }).ToListAsync();
        }

        /// <summary>
        /// Lấy chi tiết quy tắc tính giá vé theo ID cấu hình
        /// </summary>
        public async Task<TicketPricingResponse?> GetByIdAsync(int id)
        {
            return await _context.Ticketpricings
                .Where(p => p.PricingId == id)
                .Select(p => new TicketPricingResponse
                {
                    PricingId = p.PricingId,
                    RoomType = p.RoomType,
                    SeatType = p.SeatType,
                    DayType = p.DayType,
                    Price = p.Price,
                    EffectFrom = p.EffectFrom,
                    EffectTo = p.EffectTo,
                    IsActive = p.IsActive,
                    CreatedBy = p.CreatedBy
                }).FirstOrDefaultAsync();
        }

        /// <summary>
        /// Lọc ra các quy tắc tính giá vé đang có hiệu lực ở thời điểm hiện tại (Dành cho Khách hàng/App bán vé)
        /// </summary>
        public async Task<List<TicketPricingResponse>> GetActiveAsync()
        {
            var today = DateOnly.FromDateTime(DateTime.Today); // Lấy ngày hôm nay chuẩn định dạng DateOnly

            return await _context.Ticketpricings
                // Điều kiện lọc: Quy tắc đang bật VÀ ngày hiện tại nằm trong khoảng EffectFrom -> EffectTo
                .Where(p => p.IsActive && p.EffectFrom <= today && (p.EffectTo == null || p.EffectTo >= today))
                .Select(p => new TicketPricingResponse
                {
                    PricingId = p.PricingId,
                    RoomType = p.RoomType,
                    SeatType = p.SeatType,
                    DayType = p.DayType,
                    Price = p.Price,
                    EffectFrom = p.EffectFrom,
                    EffectTo = p.EffectTo,
                    IsActive = p.IsActive,
                    CreatedBy = p.CreatedBy
                }).ToListAsync();
        }

        /// <summary>
        /// Thêm mới một quy tắc cấu hình giá vé (Quyền Admin trở lên)
        /// </summary>
        public async Task<TicketPricingResponse> CreateAsync(TicketPricingRequest request, int creatorId)
        {
            var pricing = new Ticketpricing
            {
                RoomType = request.RoomType?.Trim(),
                SeatType = request.SeatType?.Trim(),
                DayType = request.DayType?.Trim(),
                Price = request.Price,
                EffectFrom = request.EffectFrom,
                EffectTo = request.EffectTo,
                IsActive = request.IsActive,
                CreatedBy = creatorId // 🌟 Ghi nhận ID của vị Admin vừa tạo ra bộ luật giá này
            };

            _context.Ticketpricings.Add(pricing);
            await _context.SaveChangesAsync(); // Lưu vĩnh viễn xuống SQL Server

            return new TicketPricingResponse
            {
                PricingId = pricing.PricingId,
                RoomType = pricing.RoomType,
                SeatType = pricing.SeatType,
                DayType = pricing.DayType,
                Price = pricing.Price,
                EffectFrom = pricing.EffectFrom,
                EffectTo = pricing.EffectTo,
                IsActive = pricing.IsActive,
                CreatedBy = pricing.CreatedBy
            };
        }

        /// <summary>
        /// Sửa đổi ma trận tính giá vé (👑 CHỈ SUPER ADMIN MỚI ĐƯỢC PHÉP CHẠY)
        /// </summary>
        public async Task<(bool IsSuccess, string Message, int StatusCode)> UpdateAsync(int id, TicketPricingRequest request, string currentOperatorEmail)
        {
            var pricing = await _context.Ticketpricings.FindAsync(id);
            if (pricing == null)
                return (false, ValidationMessages.PricingNotFoundWithId(id), 404);

            // 🛡️ CHỐT CHẶN BẢO MẬT HẠT NHÂN: Kiểm tra email sếp tổng
            if (currentOperatorEmail != ValidationMessages.SuperAdminEmail)
                return (false, ValidationMessages.UnauthorizedPricingUpdate, 403);

            // Tiến hành cập nhật thông tin mới đè lên thực thể cũ
            pricing.RoomType = request.RoomType?.Trim();
            pricing.SeatType = request.SeatType?.Trim();
            pricing.DayType = request.DayType?.Trim();
            pricing.Price = request.Price;
            pricing.EffectFrom = request.EffectFrom;
            pricing.EffectTo = request.EffectTo;
            pricing.IsActive = request.IsActive;

            try
            {
                await _context.SaveChangesAsync();
                return (true, ValidationMessages.PricingUpdateSuccess, 200);
            }
            catch (DbUpdateConcurrencyException)
            {
                // Bẫy lỗi đồng thời nếu có 2 luồng cùng ghi đè bảng giá tại một phần triệu giây
                return (false, ValidationMessages.PricingConcurrencyError, 409);
            }
        }

        /// <summary>
        /// Xóa bỏ cấu hình giá vé khỏi hệ thống (👑 CHỈ SUPER ADMIN MỚI ĐƯỢC PHÉP XÓA)
        /// </summary>
        public async Task<(bool IsSuccess, string Message, int StatusCode)> DeleteAsync(int id, string currentOperatorEmail)
        {
            var pricing = await _context.Ticketpricings.FindAsync(id);
            if (pricing == null)
                return (false, ValidationMessages.PricingNotFoundWithId(id), 404);

            // 🛡️ CHỐT CHẶN BẢO MẬT HẠT NHÂN: Chỉ duy nhất Sếp tổng mới được xóa cấu hình doanh thu
            if (currentOperatorEmail != ValidationMessages.SuperAdminEmail)
                return (false, ValidationMessages.UnauthorizedDelete, 403);

            _context.Ticketpricings.Remove(pricing);
            await _context.SaveChangesAsync();

            return (true, ValidationMessages.PricingDeleteSuccess, 200);
        }
    }
}