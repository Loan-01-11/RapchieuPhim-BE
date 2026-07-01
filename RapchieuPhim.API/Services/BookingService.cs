using Microsoft.EntityFrameworkCore;
using RapchieuPhim.API.Constants;
using RapchieuPhim.API.DTO.DTORequest;
using RapchieuPhim.API.DTOs.DTOResponse;
using RapchieuPhim.API.Models;

namespace RapchieuPhim.API.Services
{
    public interface IBookingService
    {
        Task<List<BookingDetailResponse>> GetAllDetailsAsync();
        Task<BookingDetailResponse?> GetDetailByIdAsync(int id);
        Task<(bool IsSuccess, string Message, List<BookingDetailResponse>? Data)> GetHistoryByUserAsync(int userId, int currentUserId, string currentRole);
        Task<List<AvailableSeatResponse>> GetAvailableSeatsAsync(int showTimeId);
        Task<(bool IsSuccess, string Message, List<int> BookingIds)> CreateBookingAsync(BookingCreateRequest request, int currentUserId, string currentRole);
        Task<(bool IsSuccess, string Message)> CancelBookingAsync(int bookingId, int currentUserId, string currentRole);
    }


    public class BookingService : IBookingService
    {
        private readonly CinemaManagementContext _context;

        public BookingService(CinemaManagementContext context)
        {
            _context = context;
        }

        // 🌟 Tận dụng View VW_BOOKING_DETAIL để hốt toàn bộ lịch sử sạch sẽ
        public async Task<List<BookingDetailResponse>> GetAllDetailsAsync()
        {
            return await _context.VwBookingDetails
                .Select(b => new BookingDetailResponse
                {
                    BookingId    = b.BookingId,
                    CustomerName = b.CustomerName,
                    Email        = b.Email,
                    MovieTitle   = b.MovieTitle,
                    AreaName     = b.AreaName,
                    CinemaName   = b.CinemaName,
                    RoomName     = b.RoomName,
                    RoomType     = b.RoomType,
                    SeatNumber   = b.SeatNumber,
                    SeatType     = b.SeatType,
                    StartTime    = b.StartTime,
                    TicketPrice  = b.TicketPrice,
                    DiscountAmt  = b.DiscountAmt,
                    TotalAmount  = b.TotalAmount,
                    BookingType  = b.BookingType,
                    Status       = b.Status,
                    BookingDate  = b.BookingDate
                }).ToListAsync();
        }

        // Lấy chi tiết lịch sử một đơn vé qua View
        public async Task<BookingDetailResponse?> GetDetailByIdAsync(int id)
        {
            return await _context.VwBookingDetails
                .Where(b => b.BookingId == id)
                .Select(b => new BookingDetailResponse
                {
                    BookingId    = b.BookingId,
                    CustomerName = b.CustomerName,
                    Email        = b.Email,
                    MovieTitle   = b.MovieTitle,
                    AreaName     = b.AreaName,
                    CinemaName   = b.CinemaName,
                    RoomName     = b.RoomName,
                    RoomType     = b.RoomType,
                    SeatNumber   = b.SeatNumber,
                    SeatType     = b.SeatType,
                    StartTime    = b.StartTime,
                    TicketPrice  = b.TicketPrice,
                    DiscountAmt  = b.DiscountAmt,
                    TotalAmount  = b.TotalAmount,
                    BookingType  = b.BookingType,
                    Status       = b.Status,
                    BookingDate  = b.BookingDate
                }).FirstOrDefaultAsync();
        }

        // 🛡️ Khách thường chỉ được xem vé của chính mình, Admin/Staff xem hết
        public async Task<(bool IsSuccess, string Message, List<BookingDetailResponse>? Data)> GetHistoryByUserAsync(int userId, int currentUserId, string currentRole)
        {
            if (currentRole == RoleConstants.Customer && userId != currentUserId)
                return (false, ValidationMessages.UnauthorizedBookingView, null);

            var data = await _context.VwBookingDetails
                .Where(b => _context.Bookings.Any(realBk => realBk.BookingId == b.BookingId && realBk.UserId == userId))
                .Select(b => new BookingDetailResponse
                {
                    BookingId    = b.BookingId,
                    CustomerName = b.CustomerName,
                    Email        = b.Email,
                    MovieTitle   = b.MovieTitle,
                    AreaName     = b.AreaName,
                    CinemaName   = b.CinemaName,
                    RoomName     = b.RoomName,
                    RoomType     = b.RoomType,
                    SeatNumber   = b.SeatNumber,
                    SeatType     = b.SeatType,
                    StartTime    = b.StartTime,
                    TicketPrice  = b.TicketPrice,
                    DiscountAmt  = b.DiscountAmt,
                    TotalAmount  = b.TotalAmount,
                    BookingType  = b.BookingType,
                    Status       = b.Status,
                    BookingDate  = b.BookingDate
                }).ToListAsync();

            return (true, ValidationMessages.GetHistorySuccess, data);
        }

        // 🌟 Lọc danh sách ghế trống qua View VW_AVAILABLE_SEATS
        public async Task<List<AvailableSeatResponse>> GetAvailableSeatsAsync(int showTimeId)
        {
            return await _context.VwAvailableSeats
                .Where(s => s.ShowTimeId == showTimeId)
                .Select(s => new AvailableSeatResponse
                {
                    ShowTimeId = s.ShowTimeId,
                    MovieTitle = s.MovieTitle,
                    StartTime  = s.StartTime,
                    SeatId     = s.SeatId,
                    SeatNumber = s.SeatNumber,
                    SeatType   = s.SeatType,
                    RoomId     = s.RoomId,
                    RoomName   = s.RoomName
                }).ToListAsync();
        }

        // ─── TẠO ĐƠN ĐẶT VÉ NHIỀU GHẾ + ÁP MÃ GIẢM GIÁ (xử lý trong C# với Transaction) ───
        public async Task<(bool IsSuccess, string Message, List<int> BookingIds)> CreateBookingAsync(
            BookingCreateRequest request, int currentUserId, string currentRole)
        {
            var emptyList = new List<int>();

            // ── BƯỚC 1: Kiểm tra danh sách ghế không được trùng nhau ──────────────
            if (request.SeatIds.Distinct().Count() != request.SeatIds.Count)
                return (false, ValidationMessages.BookingMessages.DuplicateSeatIds, emptyList);

            // ── BƯỚC 2: Xác định UserId & StaffId ────────────────────────────────
            int  finalUserId = currentUserId;
            int? staffId     = null;

            if (request.BookingType.Trim() == ValidationMessages.Counter)
            {
                if (currentRole != RoleConstants.Admin && currentRole != RoleConstants.Staff)
                    return (false, ValidationMessages.OnlyStaffCanCreateCounterBooking, emptyList);

                finalUserId = (request.TargetUserId == null || request.TargetUserId == 0)
                              ? currentUserId
                              : request.TargetUserId.Value;
                staffId = currentUserId;
            }

            // ── BƯỚC 3: Kiểm tra Suất chiếu tồn tại ─────────────────────────────
            var showtime = await _context.Showtimes
                .Include(s => s.Room)
                .FirstOrDefaultAsync(s => s.ShowTimeId == request.ShowTimeId);
            if (showtime == null)
                return (false, ShowtimeMessages.NotFoundWithId(request.ShowTimeId), emptyList);

            // ── BƯỚC 4: Kiểm tra tất cả ghế tồn tại ─────────────────────────────
            var seats = await _context.Seats
                .Where(s => request.SeatIds.Contains(s.SeatId))
                .ToListAsync();

            if (seats.Count != request.SeatIds.Count)
            {
                // Tìm ghế nào không tồn tại để báo lỗi cụ thể
                var foundIds  = seats.Select(s => s.SeatId).ToHashSet();
                var missingId = request.SeatIds.First(id => !foundIds.Contains(id));
                return (false, ValidationMessages.SeatNotFoundWithId(missingId), emptyList);
            }

            // ── BƯỚC 5: Kiểm tra không có ghế nào đã bị đặt ──────────────────────
            var takenSeatIds = await _context.Bookings
                .Where(b => b.ShowTimeId == request.ShowTimeId
                         && request.SeatIds.Contains(b.SeatId)
                         && b.Status != "Cancelled")
                .Select(b => b.SeatId)
                .ToListAsync();

            if (takenSeatIds.Any())
                return (false, ValidationMessages.BookingMessages.SeatsAlreadyBooked(takenSeatIds), emptyList);

            // ── BƯỚC 6: Lấy giá vé từ TicketPricing ─────────────────────────────
            var    today   = DateOnly.FromDateTime(DateTime.Now);
            string dayType = DateTime.Now.DayOfWeek == DayOfWeek.Saturday ||
                             DateTime.Now.DayOfWeek == DayOfWeek.Sunday
                             ? "Weekend" : "Weekday";

            // ── BƯỚC 7: Xử lý mã giảm giá (áp 1 lần cho toàn bộ đơn) ────────────
            decimal   totalDiscountAmt = 0;
            int?      discountId       = null;
            Discount? discount         = null;

            if (!string.IsNullOrWhiteSpace(request.DiscountCode))
            {
                // 7a. Tìm mã trong DB
                discount = await _context.Discounts
                    .FirstOrDefaultAsync(d =>
                        d.DiscountCode == request.DiscountCode.Trim().ToUpper() &&
                        d.IsActive &&
                        d.StartDate <= DateTime.Now &&
                        (d.EndDate == null || d.EndDate >= DateTime.Now));

                if (discount == null)
                    return (false, DiscountMessages.InvalidOrExpiredCode, emptyList);

                // 7b. Kiểm tra mã còn lượt dùng tổng
                if (discount.MaxUsageTotal.HasValue && discount.UsedCount >= discount.MaxUsageTotal.Value)
                    return (false, ValidationMessages.BookingMessages.DiscountMaxUsageReached, emptyList);

                // 7c. Kiểm tra lượt dùng của riêng user này
                var userUsage    = await _context.Userdiscountusages
                    .FirstOrDefaultAsync(u => u.UserId == finalUserId && u.DiscountId == discount.DiscountId);
                int userUsedCount = userUsage?.UsedCount ?? 0;
                if (userUsedCount >= discount.MaxUsagePerUser)
                    return (false, ValidationMessages.BookingMessages.DiscountUserLimitReached, emptyList);

                discountId = discount.DiscountId;
            }

            // ── BƯỚC 8: Lưu tất cả trong 1 Transaction ───────────────────────────
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var newBookingIds = new List<int>();

                // Tổng giá trước giảm (dùng để kiểm tra MinOrderAmount và tính giảm theo %)
                decimal grandTotal = 0;

                // Danh sách booking đã tạo (để tính discount sau)
                var createdBookings = new List<(Booking Booking, decimal TicketPrice)>();

                foreach (var seat in seats)
                {
                    // Lấy giá vé riêng cho từng ghế (mỗi ghế có SeatType khác nhau)
                    var pricing = await _context.Ticketpricings
                        .Where(p => p.IsActive
                                 && (p.RoomType == null || p.RoomType == showtime.Room.RoomType)
                                 && (p.SeatType == null || p.SeatType == seat.SeatType)
                                 && (p.DayType  == null || p.DayType  == dayType)
                                 && p.EffectFrom <= today
                                 && (p.EffectTo  == null || p.EffectTo >= today))
                        .OrderByDescending(p => p.EffectFrom)
                        .FirstOrDefaultAsync();

                    if (pricing == null)
                        return (false, ValidationMessages.BookingMessages.PricingNotFound, emptyList);

                    grandTotal += pricing.Price;
                    createdBookings.Add((new Booking
                    {
                        UserId      = finalUserId,
                        ShowTimeId  = request.ShowTimeId,
                        SeatId      = seat.SeatId,
                        DiscountId  = null,          // Gán discount sau khi tính xong
                        BookingDate = DateTime.Now,
                        TicketPrice = pricing.Price,
                        DiscountAmt = 0,             // Cập nhật sau
                        TotalAmount = pricing.Price, // Cập nhật sau
                        BookingType = request.BookingType.Trim(),
                        StaffId     = staffId,
                        Status      = ValidationMessages.StutusComfirmed
                    }, pricing.Price));
                }

                // Tính discount sau khi biết tổng giá
                if (discount != null)
                {
                    // 8a. Kiểm tra tổng đơn đạt MinOrderAmount
                    if (grandTotal < discount.MinOrderAmount)
                        return (false, ValidationMessages.BookingMessages.OrderBelowMinAmount(discount.MinOrderAmount), emptyList);

                    // 8b. Tính tổng số tiền giảm cho toàn bộ đơn
                    totalDiscountAmt = discount.DiscountType == DiscountMessages.TypePercent
                        ? Math.Round(grandTotal * discount.DiscountValue / 100, 0)
                        : discount.DiscountValue;

                    totalDiscountAmt = Math.Min(totalDiscountAmt, grandTotal);

                    // 8c. Phân bổ discount đều ra các booking (ghế cuối cùng nhận phần dư lẻ)
                    decimal discountPerSeat = Math.Floor(totalDiscountAmt / seats.Count);
                    decimal remainder       = totalDiscountAmt - discountPerSeat * seats.Count;

                    for (int i = 0; i < createdBookings.Count; i++)
                    {
                        decimal thisDiscount  = discountPerSeat + (i == createdBookings.Count - 1 ? remainder : 0);
                        var (booking, price)  = createdBookings[i];
                        booking.DiscountId    = discountId;
                        booking.DiscountAmt   = thisDiscount;
                        booking.TotalAmount   = price - thisDiscount;
                    }
                }

                // 8d. Lưu tất cả Booking vào DB
                foreach (var (booking, _) in createdBookings)
                {
                    _context.Bookings.Add(booking);
                    await _context.SaveChangesAsync(); // Cần BookingId để tạo Ticket

                    // Cấp Ticket tự động kèm QR Code
                    string ticketCode = "TIC" + Guid.NewGuid().ToString().Replace("-", "").Substring(0, 7).ToUpper();
                    _context.Tickets.Add(new Ticket
                    {
                        BookingId  = booking.BookingId,
                        TicketCode = ticketCode,
                        QrCodeUrl  = "https://api.qrserver.com/v1/create-qr-code/?size=250x250&data=" + ticketCode,
                        Price      = booking.TotalAmount,
                        IssuedAt   = DateTime.Now,
                        Status     = ShowtimeMessages.StatusActive
                    });
                    await _context.SaveChangesAsync();

                    newBookingIds.Add(booking.BookingId);
                }

                // 8e. Cập nhật thống kê sử dụng mã giảm giá (1 lần duy nhất)
                if (discount != null)
                {
                    discount.UsedCount++;

                    var userUsage = await _context.Userdiscountusages
                        .FirstOrDefaultAsync(u => u.UserId == finalUserId && u.DiscountId == discount.DiscountId);

                    if (userUsage != null)
                    {
                        userUsage.UsedCount++;
                        userUsage.LastUsedAt = DateTime.Now;
                    }
                    else
                    {
                        _context.Userdiscountusages.Add(new Userdiscountusage
                        {
                            UserId     = finalUserId,
                            DiscountId = discount.DiscountId,
                            UsedCount  = 1,
                            LastUsedAt = DateTime.Now
                        });
                    }
                    await _context.SaveChangesAsync();
                }

                await transaction.CommitAsync(); // ✅ Commit toàn bộ khi không có lỗi

                return (true, ValidationMessages.CreateBookingSuccess, newBookingIds);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(); // ❌ Rollback nếu bất kỳ bước nào lỗi
                Console.WriteLine(ValidationMessages.ErrorAutoTicket + ex.Message);
                return (false, ValidationMessages.BookingMessages.CreateBookingFailed, emptyList);
            }
        }

        // Hủy đơn đặt vé — xóa vật lý, ghế tự động được giải phóng
        public async Task<(bool IsSuccess, string Message)> CancelBookingAsync(int bookingId, int currentUserId, string currentRole)
        {
            var booking = await _context.Bookings.FindAsync(bookingId);
            if (booking == null)
                return (false, ValidationMessages.BookingNotFoundWithId(bookingId));

            // Bảo mật: Khách chỉ được tự hủy đơn của chính mình
            if (currentRole == RoleConstants.Customer && booking.UserId != currentUserId)
                return (false, ValidationMessages.UnauthorizedBookingCancel);

            _context.Bookings.Remove(booking);
            await _context.SaveChangesAsync();

            return (true, ValidationMessages.CancelBookingSuccess);
        }
    }
}
