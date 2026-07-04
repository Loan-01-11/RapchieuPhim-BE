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
        Task<(bool IsSuccess, string Message, BookingSummaryResponse? Data)> CreateBookingAsync(BookingCreateRequest request, int currentUserId, string currentRole);
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

        // ─── TẠO ĐƠN ĐẶT VÉ NHIỀU GHẾ + ĐỒ ĂN/COMBO + ÁP MÃ GIẢM GIÁ ──────────────
        public async Task<(bool IsSuccess, string Message, BookingSummaryResponse? Data)> CreateBookingAsync(
            BookingCreateRequest request, int currentUserId, string currentRole)
        {
            // ── BƯỚC 1: Kiểm tra danh sách ghế không trùng nhau ──────────────────
            if (request.SeatIds.Distinct().Count() != request.SeatIds.Count)
                return (false, ValidationMessages.BookingMessages.DuplicateSeatIds, null);

            // ── BƯỚC 2: Xác định UserId & StaffId ────────────────────────────────
            int  finalUserId = currentUserId;
            int? staffId     = null;

            if (request.BookingType.Trim() == ValidationMessages.Counter)
            {
                if (currentRole != RoleConstants.Admin && currentRole != RoleConstants.Staff)
                    return (false, ValidationMessages.OnlyStaffCanCreateCounterBooking, null);

                finalUserId = (request.TargetUserId == null || request.TargetUserId == 0)
                              ? currentUserId
                              : request.TargetUserId.Value;
                staffId = currentUserId;
            }

            // ── BƯỚC 3: Validate & Load dữ liệu OrderItems trước khi bắt đầu Transaction
            var preparedOrderItems = new List<(int? FoodId, int? ComboId, string Name, decimal UnitPrice, int Qty)>();

            if (request.OrderItems != null && request.OrderItems.Any())
            {
                foreach (var item in request.OrderItems)
                {
                    // Phải có đúng 1 trong 2: FoodId hoặc ComboId
                    if (item.FoodId == null && item.ComboId == null)
                        return (false, ValidationMessages.BookingMessages.FoodOrComboRequired, null);
                    if (item.FoodId != null && item.ComboId != null)
                        return (false, ValidationMessages.BookingMessages.FoodOrComboNotBoth, null);

                    if (item.FoodId != null)
                    {
                        var food = await _context.Foods.FindAsync(item.FoodId.Value);
                        if (food == null)
                            return (false, FoodMessages.NotFoundWithId(item.FoodId.Value), null);
                        if (!food.IsAvailable || food.Quantity < item.Quantity)
                            return (false, ValidationMessages.BookingMessages.FoodOrComboUnavailable, null);

                        preparedOrderItems.Add((item.FoodId, null, food.FoodName, food.Price, item.Quantity));
                    }
                    else
                    {
                        var combo = await _context.Combos.FindAsync(item.ComboId!.Value);
                        if (combo == null)
                            return (false, ComboMessages.NotFoundWithId(item.ComboId.Value), null);
                        if (!combo.IsAvailable || combo.Quantity < item.Quantity)
                            return (false, ValidationMessages.BookingMessages.FoodOrComboUnavailable, null);

                        preparedOrderItems.Add((null, item.ComboId, combo.ComboName, combo.Price, item.Quantity));
                    }
                }
            }

            // ── BƯỚC 4: Kiểm tra Suất chiếu & Ghế ───────────────────────────────
            var showtime = await _context.Showtimes
                .Include(s => s.Room)
                .FirstOrDefaultAsync(s => s.ShowTimeId == request.ShowTimeId);
            if (showtime == null)
                return (false, ShowtimeMessages.NotFoundWithId(request.ShowTimeId), null);

            var seats = await _context.Seats
                .Where(s => request.SeatIds.Contains(s.SeatId))
                .ToListAsync();

            if (seats.Count != request.SeatIds.Count)
            {
                var foundIds  = seats.Select(s => s.SeatId).ToHashSet();
                var missingId = request.SeatIds.First(id => !foundIds.Contains(id));
                return (false, ValidationMessages.SeatNotFoundWithId(missingId), null);
            }

            // ── BƯỚC 5: Kiểm tra không có ghế nào đã bị đặt ─────────────────────
            var takenSeatIds = await _context.Bookings
                .Where(b => b.ShowTimeId == request.ShowTimeId
                         && request.SeatIds.Contains(b.SeatId)
                         && b.Status != "Cancelled")
                .Select(b => b.SeatId)
                .ToListAsync();

            if (takenSeatIds.Any())
                return (false, ValidationMessages.BookingMessages.SeatsAlreadyBooked(takenSeatIds), null);

            // ── BƯỚC 6: Lấy giá vé từ TicketPricing ─────────────────────────────
            var    today   = DateOnly.FromDateTime(DateTime.Now);
            string dayType = DateTime.Now.DayOfWeek == DayOfWeek.Saturday ||
                             DateTime.Now.DayOfWeek == DayOfWeek.Sunday
                             ? "Weekend" : "Weekday";

            // ── BƯỚC 7: Xử lý mã giảm giá ────────────────────────────────────────
            decimal   totalDiscountAmt = 0;
            int?      discountId       = null;
            Discount? discount         = null;

            if (!string.IsNullOrWhiteSpace(request.DiscountCode))
            {
                discount = await _context.Discounts
                    .FirstOrDefaultAsync(d =>
                        d.DiscountCode == request.DiscountCode.Trim().ToUpper() &&
                        d.IsActive &&
                        d.StartDate <= DateTime.Now &&
                        (d.EndDate == null || d.EndDate >= DateTime.Now));

                if (discount == null)
                    return (false, DiscountMessages.InvalidOrExpiredCode, null);

                if (discount.MaxUsageTotal.HasValue && discount.UsedCount >= discount.MaxUsageTotal.Value)
                    return (false, ValidationMessages.BookingMessages.DiscountMaxUsageReached, null);

                var userUsage    = await _context.Userdiscountusages
                    .FirstOrDefaultAsync(u => u.UserId == finalUserId && u.DiscountId == discount.DiscountId);
                int userUsedCount = userUsage?.UsedCount ?? 0;
                if (userUsedCount >= discount.MaxUsagePerUser)
                    return (false, ValidationMessages.BookingMessages.DiscountUserLimitReached, null);

                discountId = discount.DiscountId;
            }

            // ── BƯỚC 8: Lưu tất cả trong 1 Transaction ───────────────────────────
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var newBookingIds     = new List<int>();
                decimal ticketTotal   = 0; // Tổng tiền vé (trước giảm giá)
                var createdBookings   = new List<(Booking Booking, decimal TicketPrice)>();

                // 8a. Tính giá và tạo Booking cho từng ghế
                foreach (var seat in seats)
                {
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
                        return (false, ValidationMessages.BookingMessages.PricingNotFound, null);

                    ticketTotal += pricing.Price;
                    createdBookings.Add((new Booking
                    {
                        UserId      = finalUserId,
                        ShowTimeId  = request.ShowTimeId,
                        SeatId      = seat.SeatId,
                        DiscountId  = null,
                        BookingDate = DateTime.Now,
                        TicketPrice = pricing.Price,
                        DiscountAmt = 0,
                        TotalAmount = pricing.Price,
                        BookingType = request.BookingType.Trim(),
                        StaffId     = staffId,
                        Status      = ValidationMessages.StutusComfirmed
                    }, pricing.Price));
                }

                // 8b. Tính và phân bổ discount vào từng vé
                if (discount != null)
                {
                    if (ticketTotal < discount.MinOrderAmount)
                        return (false, ValidationMessages.BookingMessages.OrderBelowMinAmount(discount.MinOrderAmount), null);

                    totalDiscountAmt = discount.DiscountType == DiscountMessages.TypePercent
                        ? Math.Round(ticketTotal * discount.DiscountValue / 100, 0)
                        : discount.DiscountValue;
                    totalDiscountAmt = Math.Min(totalDiscountAmt, ticketTotal);

                    decimal discountPerSeat = Math.Floor(totalDiscountAmt / seats.Count);
                    decimal remainder       = totalDiscountAmt - discountPerSeat * seats.Count;

                    for (int i = 0; i < createdBookings.Count; i++)
                    {
                        decimal thisDiscount     = discountPerSeat + (i == createdBookings.Count - 1 ? remainder : 0);
                        var (booking, price)     = createdBookings[i];
                        booking.DiscountId       = discountId;
                        booking.DiscountAmt      = thisDiscount;
                        booking.TotalAmount      = price - thisDiscount;
                    }
                }

                // 8c. Lưu từng Booking + Ticket vào DB
                foreach (var (booking, _) in createdBookings)
                {
                    _context.Bookings.Add(booking);
                    await _context.SaveChangesAsync();

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

                // 8d. Cập nhật thống kê mã giảm giá
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

                // ── BƯỚC 9: Tạo Order + OrderItems nếu khách có đặt đồ ăn ───────
                decimal foodTotal  = 0;
                int?    newOrderId = null;
                var     orderItemResponses = new List<OrderItemSummary>();

                if (preparedOrderItems.Any())
                {
                    // Lấy BookingId đầu tiên để liên kết Order với Booking
                    var order = new Order
                    {
                        UserId      = finalUserId,
                        BookingId   = newBookingIds.FirstOrDefault(),
                        StaffId     = staffId,
                        OrderDate   = DateTime.Now,
                        TotalAmount = 0,        // Cập nhật sau khi tính xong
                        OrderType   = request.BookingType.Trim(),
                        Status      = ValidationMessages.StutusComfirmed
                    };
                    _context.Orders.Add(order);
                    await _context.SaveChangesAsync(); // Lấy OrderId

                    foreach (var (foodId, comboId, name, unitPrice, qty) in preparedOrderItems)
                    {
                        decimal subtotal = unitPrice * qty;
                        foodTotal += subtotal;

                        _context.Orderitems.Add(new Orderitem
                        {
                            OrderId   = order.OrderId,
                            FoodId    = foodId,
                            ComboId   = comboId,
                            Quantity  = qty,
                            UnitPrice = unitPrice,
                            Subtotal  = subtotal
                        });

                        // Giảm tồn kho
                        if (foodId != null)
                        {
                            var food = await _context.Foods.FindAsync(foodId.Value);
                            if (food != null) food.Quantity -= qty;
                        }
                        else if (comboId != null)
                        {
                            var combo = await _context.Combos.FindAsync(comboId.Value);
                            if (combo != null) combo.Quantity -= qty;
                        }

                        orderItemResponses.Add(new OrderItemSummary
                        {
                            Name      = name,
                            FoodId    = foodId,
                            ComboId   = comboId,
                            UnitPrice = unitPrice,
                            Quantity  = qty,
                            Subtotal  = subtotal
                        });
                    }

                    order.TotalAmount = foodTotal;
                    await _context.SaveChangesAsync();
                    newOrderId = order.OrderId;
                }

                await transaction.CommitAsync(); // ✅ Commit toàn bộ

                // ── BƯỚC 10: Tổng hợp response ────────────────────────────────────
                decimal ticketAfterDiscount = ticketTotal - totalDiscountAmt;
                decimal grandTotal          = ticketAfterDiscount + foodTotal;

                var summary = new BookingSummaryResponse
                {
                    BookingIds      = newBookingIds,
                    OrderId         = newOrderId,

                    TicketTotal     = ticketTotal,
                    DiscountAmt     = totalDiscountAmt,
                    TicketAfterDiscount = ticketAfterDiscount,

                    FoodTotal       = foodTotal,
                    OrderItems      = orderItemResponses,

                    GrandTotal      = grandTotal
                };

                return (true, ValidationMessages.CreateBookingSuccess, summary);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                Console.WriteLine(ValidationMessages.ErrorAutoTicket + ex.Message);
                return (false, ValidationMessages.BookingMessages.CreateBookingFailed, null);
            }
        }

        // Hủy đơn đặt vé — xóa vật lý, ghế tự động được giải phóng
        public async Task<(bool IsSuccess, string Message)> CancelBookingAsync(int bookingId, int currentUserId, string currentRole)
        {
            var booking = await _context.Bookings.FindAsync(bookingId);
            if (booking == null)
                return (false, ValidationMessages.BookingNotFoundWithId(bookingId));

            if (currentRole == RoleConstants.Customer && booking.UserId != currentUserId)
                return (false, ValidationMessages.UnauthorizedBookingCancel);

            _context.Bookings.Remove(booking);
            await _context.SaveChangesAsync();

            return (true, ValidationMessages.CancelBookingSuccess);
        }
    }
}
