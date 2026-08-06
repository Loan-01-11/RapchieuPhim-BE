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
        Task<List<TicketPricingResponse>> GetByRoomAsync(int roomId);
        Task<(bool IsSuccess, string Message, int StatusCode, List<TicketPricingResponse>? Data)> UpdateRoomPricesAsync(int roomId, RoomTicketPricingBulkRequest request, int operatorId);
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
                    RoomId = p.RoomId,
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
                    RoomId = p.RoomId,
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
                .OrderByDescending(p => p.RoomId.HasValue)
                .ThenBy(p => p.RoomId)
                .ThenBy(p => p.SeatType)
                .ThenBy(p => p.DayType)
                .Select(p => new TicketPricingResponse
                {
                    PricingId = p.PricingId,
                    RoomId = p.RoomId,
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

        public async Task<List<TicketPricingResponse>> GetByRoomAsync(int roomId)
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            return await _context.Ticketpricings.AsNoTracking()
                .Where(p => p.RoomId == roomId && p.IsActive && p.EffectFrom <= today && (p.EffectTo == null || p.EffectTo >= today))
                .OrderBy(p => p.SeatType).ThenBy(p => p.DayType)
                .Select(p => new TicketPricingResponse
                {
                    PricingId = p.PricingId, RoomId = p.RoomId, RoomType = p.RoomType,
                    SeatType = p.SeatType, DayType = p.DayType, Price = p.Price,
                    EffectFrom = p.EffectFrom, EffectTo = p.EffectTo,
                    IsActive = p.IsActive, CreatedBy = p.CreatedBy
                }).ToListAsync();
        }

        public async Task<(bool IsSuccess, string Message, int StatusCode, List<TicketPricingResponse>? Data)> UpdateRoomPricesAsync(
            int roomId, RoomTicketPricingBulkRequest request, int operatorId)
        {
            var room = await _context.Rooms.FindAsync(roomId);
            if (room == null) return (false, "Không tìm thấy phòng chiếu.", 404, null);

            var allowedSeats = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Standard", "VIP", "Couple" };
            var allowedDays = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Weekday", "Weekend" };
            var normalized = request.Prices.Select(x => new
            {
                SeatType = allowedSeats.FirstOrDefault(s => s.Equals(x.SeatType?.Trim(), StringComparison.OrdinalIgnoreCase)),
                DayType = allowedDays.FirstOrDefault(d => d.Equals(x.DayType?.Trim(), StringComparison.OrdinalIgnoreCase)),
                x.Price
            }).ToList();

            if (normalized.Any(x => x.SeatType == null || x.DayType == null || x.Price <= 0))
                return (false, "SeatType, DayType không hợp lệ hoặc giá phải lớn hơn 0.", 400, null);
            if (normalized.GroupBy(x => $"{x.SeatType}:{x.DayType}", StringComparer.OrdinalIgnoreCase).Any(g => g.Count() > 1))
                return (false, "Bảng giá có dòng bị trùng.", 400, null);
            var required = allowedSeats.SelectMany(s => allowedDays.Select(d => $"{s}:{d}")).ToHashSet(StringComparer.OrdinalIgnoreCase);
            if (!required.SetEquals(normalized.Select(x => $"{x.SeatType}:{x.DayType}")))
                return (false, "Phải gửi đủ 6 mức giá Standard/VIP/Couple cho Weekday/Weekend.", 400, null);

            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var today = DateOnly.FromDateTime(DateTime.Today);
                var current = await _context.Ticketpricings
                    .Where(p => p.RoomId == roomId && p.IsActive)
                    .ToListAsync();

                foreach (var item in normalized)
                {
                    var matches = current.Where(p =>
                        string.Equals(p.SeatType, item.SeatType, StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(p.DayType, item.DayType, StringComparison.OrdinalIgnoreCase)).ToList();
                    var pricing = matches.OrderByDescending(p => p.EffectFrom).FirstOrDefault();
                    foreach (var duplicate in matches.Where(p => p != pricing))
                        duplicate.IsActive = false;
                    var oldPrice = pricing?.Price ?? 0m;
                    if (pricing == null)
                    {
                        pricing = new Ticketpricing
                        {
                            RoomId = roomId, RoomType = room.RoomType, SeatType = item.SeatType,
                            DayType = item.DayType, Price = item.Price, EffectFrom = today,
                            IsActive = true, CreatedBy = operatorId
                        };
                        _context.Ticketpricings.Add(pricing);
                        await _context.SaveChangesAsync();
                    }
                    else
                    {
                        pricing.RoomType = room.RoomType;
                        pricing.Price = item.Price;
                        pricing.EffectFrom = today;
                        pricing.EffectTo = null;
                    }

                    if (oldPrice != item.Price)
                    {
                        _context.TicketPricingHistories.Add(new TicketPricingHistory
                        {
                            PricingId = pricing.PricingId, RoomId = roomId,
                            SeatType = item.SeatType!, DayType = item.DayType!,
                            OldPrice = oldPrice, NewPrice = item.Price,
                            ChangedBy = operatorId, ChangedAt = DateTime.UtcNow
                        });
                    }
                }

                await _context.SaveChangesAsync();

                // Giá mới áp dụng cho booking chưa thanh toán; vé Active là snapshot đã mua và không được sửa.
                var priceMap = normalized.ToDictionary(
                    x => $"{x.SeatType}:{x.DayType}", x => x.Price, StringComparer.OrdinalIgnoreCase);
                var unpaidBookings = await _context.Bookings
                    .Where(b => b.ShowTime.RoomId == roomId &&
                        !b.Tickets.Any(t => t.Status == "Active") &&
                        !b.Payments.Any(p => p.PaymentStatus == PaymentMessages.StatusSuccess))
                    .Include(b => b.ShowTime)
                    .Include(b => b.Seat)
                    .Include(b => b.Tickets)
                    .ToListAsync();

                foreach (var booking in unpaidBookings)
                {
                    var dayType = booking.ShowTime.StartTime.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday
                        ? "Weekend" : "Weekday";
                    if (!priceMap.TryGetValue($"{booking.Seat.SeatType}:{dayType}", out var newUnitPrice))
                        continue;
                    booking.TicketPrice = newUnitPrice;
                    booking.TotalAmount = Math.Max(0, newUnitPrice - booking.DiscountAmt);
                    foreach (var ticket in booking.Tickets)
                        ticket.Price = booking.TotalAmount;
                }

                var pendingPayments = await _context.Payments
                    .Where(p => p.BookingId.HasValue && p.Booking!.ShowTime.RoomId == roomId &&
                        p.PaymentStatus != PaymentMessages.StatusSuccess)
                    .Include(p => p.Booking)
                    .Include(p => p.Order)
                    .ToListAsync();
                foreach (var payment in pendingPayments)
                {
                    var root = payment.Booking!;
                    var group = unpaidBookings.Where(b => b.UserId == root.UserId &&
                        b.ShowTimeId == root.ShowTimeId && b.BookingDate == root.BookingDate).ToList();
                    if (group.Count == 0) continue;
                    var foodTotal = payment.Order?.TotalAmount ?? 0m;
                    payment.SubTotal = group.Sum(b => b.TicketPrice) + foodTotal;
                    payment.DiscountAmt = group.Sum(b => b.DiscountAmt);
                    payment.TotalAmount = group.Sum(b => b.TotalAmount) + foodTotal;
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return (true, "Cập nhật bảng giá phòng chiếu thành công.", 200, await GetByRoomAsync(roomId));
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        /// <summary>
        /// Thêm mới một quy tắc cấu hình giá vé (Quyền Admin trở lên)
        /// </summary>
        public async Task<TicketPricingResponse> CreateAsync(TicketPricingRequest request, int creatorId)
        {
            var pricing = new Ticketpricing
            {
                RoomId = request.RoomId,
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
                RoomId = pricing.RoomId,
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
            pricing.RoomId = request.RoomId;
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
