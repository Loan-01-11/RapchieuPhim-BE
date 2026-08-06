using Microsoft.EntityFrameworkCore;
using RapchieuPhim.API.Constants;
using RapchieuPhim.API.DTO.DTORequest;
using RapchieuPhim.API.DTOs.DTOResponse;
using RapchieuPhim.API.Models;
using RapchieuPhim.API.Utilities;

namespace RapchieuPhim.API.Services
{
    public interface IBookingService
    {
        Task<List<BookingDetailResponse>> GetAllDetailsAsync(string? date = null);
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
        public async Task<List<BookingDetailResponse>> GetAllDetailsAsync(string? date = null)
        {
            var query = _context.VwBookingDetails.AsQueryable();

            if (!string.IsNullOrEmpty(date) && DateTime.TryParse(date, out var parsedDate))
            {
                var start = parsedDate.Date;
                var end = start.AddDays(1);
                query = query.Where(b => b.BookingDate >= start && b.BookingDate < end);
            }

            return await query
                .OrderByDescending(b => b.BookingDate)
                .Take(500)
                .Select(b => new BookingDetailResponse
                {
                    BookingId = b.BookingId,
                    CustomerName = b.CustomerName,
                    Email = b.Email,
                    MovieTitle = b.MovieTitle,
                    AreaName = b.AreaName,
                    CinemaName = b.CinemaName,
                    RoomName = b.RoomName,
                    RoomType = b.RoomType,
                    SeatCode = b.SeatNumber,
                    SeatRow = string.Empty,
                    SeatNumber = b.SeatNumber,
                    SeatType = b.SeatType,
                    Price = b.TicketPrice,
                    StartTime = b.StartTime,
                    TicketPrice = b.TicketPrice,
                    DiscountAmt = b.DiscountAmt,
                    TotalAmount = b.TotalAmount,
                    BookingType = b.BookingType,
                    Status = b.Status,
                    BookingDate = b.BookingDate
                }).ToListAsync();
        }

        // Lấy chi tiết lịch sử một đơn vé qua View
        public async Task<BookingDetailResponse?> GetDetailByIdAsync(int id)
        {
            return await _context.VwBookingDetails
                .Where(b => b.BookingId == id)
                .Select(b => new BookingDetailResponse
                {
                    BookingId = b.BookingId,
                    CustomerName = b.CustomerName,
                    Email = b.Email,
                    MovieTitle = b.MovieTitle,
                    AreaName = b.AreaName,
                    CinemaName = b.CinemaName,
                    RoomName = b.RoomName,
                    RoomType = b.RoomType,
                    SeatNumber = b.SeatNumber,
                    SeatType = b.SeatType,
                    StartTime = b.StartTime,
                    TicketPrice = b.TicketPrice,
                    DiscountAmt = b.DiscountAmt,
                    TotalAmount = b.TotalAmount,
                    BookingType = b.BookingType,
                    Status = b.Status,
                    BookingDate = b.BookingDate
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
                    BookingId = b.BookingId,
                    CustomerName = b.CustomerName,
                    Email = b.Email,
                    MovieTitle = b.MovieTitle,
                    AreaName = b.AreaName,
                    CinemaName = b.CinemaName,
                    RoomName = b.RoomName,
                    RoomType = b.RoomType,
                    SeatNumber = b.SeatNumber,
                    SeatType = b.SeatType,
                    StartTime = b.StartTime,
                    TicketPrice = b.TicketPrice,
                    DiscountAmt = b.DiscountAmt,
                    TotalAmount = b.TotalAmount,
                    BookingType = b.BookingType,
                    Status = b.Status,
                    BookingDate = b.BookingDate,
                    TicketCode = _context.Tickets.Where(tk => tk.BookingId == b.BookingId).Select(tk => tk.TicketCode).FirstOrDefault()
                }).ToListAsync();

            var bookingIds = data.Select(b => b.BookingId).ToList();
            var seatsByBooking = await _context.Bookings
                .AsNoTracking()
                .Where(booking => bookingIds.Contains(booking.BookingId))
                .Select(booking => new
                {
                    booking.BookingId,
                    booking.Seat.SeatRow,
                    booking.Seat.SeatNumber,
                    booking.Seat.SeatType
                })
                .ToDictionaryAsync(booking => booking.BookingId);

            foreach (var booking in data)
            {
                if (!seatsByBooking.TryGetValue(booking.BookingId, out var seat))
                    continue;

                var row = seat.SeatRow?.Trim() ?? string.Empty;
                var number = seat.SeatNumber?.Trim() ?? string.Empty;
                var seatCode = !string.IsNullOrEmpty(row) && !number.StartsWith(row, StringComparison.OrdinalIgnoreCase)
                    ? $"{row}{number}"
                    : number;
                booking.SeatCode = seatCode;
                booking.SeatRow = row;
                booking.SeatNumber = number;
                booking.SeatType = seat.SeatType;
                booking.Price = booking.TicketPrice;
            }

            var orders = await _context.Orders
                .Include(o => o.Orderitems)
                .ThenInclude(oi => oi.Food)
                .Include(o => o.Orderitems)
                .ThenInclude(oi => oi.Combo)
                .Include(o => o.Orderitems)
                .ThenInclude(oi => oi.ComboSelections)
                .Where(o => o.BookingId.HasValue && bookingIds.Contains(o.BookingId.Value))
                .ToListAsync();

            foreach (var b in data)
            {
                var order = orders.FirstOrDefault(o => o.BookingId == b.BookingId);
                if (order != null)
                {
                    foreach (var oi in order.Orderitems)
                    {
                        var name = oi.Food?.FoodName ?? oi.Combo?.ComboName ?? "Đồ ăn kèm";
                        var snapshot = RapchieuPhim.API.DTO.DTOResponse.OrderItemSnapshotHelper.Parse(oi.ComboSelectionSnapshot, name);
                        b.Foods.Add(new BookingFoodDetailResponse
                        {
                            FoodOrderDetailId = oi.OrderItemId,
                            FoodId = oi.FoodId,
                            ComboId = oi.ComboId,
                            ItemType = oi.ComboId.HasValue ? "COMBO" : "FOOD",
                            Name = snapshot.ItemNameSnapshot,
                            ItemNameSnapshot = snapshot.ItemNameSnapshot,
                            Quantity = oi.Quantity,
                            Price = oi.UnitPrice,
                            UnitPriceSnapshot = oi.UnitPrice,
                            LineTotal = oi.Subtotal,
                            ComboSelections = oi.ComboSelections.Count > 0
                                ? oi.ComboSelections.Select(selection => new RapchieuPhim.API.DTO.DTOResponse.OrderComboComponentResponse
                                {
                                    FoodId = selection.FoodId,
                                    FoodName = selection.FoodNameSnapshot,
                                    Category = selection.CategorySnapshot,
                                    Quantity = selection.Quantity
                                }).ToList()
                                : snapshot.ComboSelections,
                            ComboSelectionDataUnavailable = oi.ComboId.HasValue && oi.ComboSelections.Count == 0 && snapshot.ComboSelections.Count == 0
                        });
                    }
                }
            }

            return (true, ValidationMessages.GetHistorySuccess, data);
        }

        // 🌟 Lọc danh sách ghế trống qua View VW_AVAILABLE_SEATS
        public async Task<List<AvailableSeatResponse>> GetAvailableSeatsAsync(int showTimeId)
        {
            // The SQL view can be stale or use different status rules. Always
            // exclude seats from the authoritative BOOKINGS table as well.
            var bookedSeatIds = _context.Bookings
                .Where(b => b.ShowTimeId == showTimeId &&
                            b.Status != ShowtimeMessages.StatusCancelled)
                .Select(b => b.SeatId);

            return await _context.VwAvailableSeats
                .AsNoTracking()
                .Where(s => s.ShowTimeId == showTimeId &&
                            _context.Seats.Any(seat => seat.SeatId == s.SeatId && seat.IsActive) &&
                            !bookedSeatIds.Contains(s.SeatId))
                .Select(s => new AvailableSeatResponse
                {
                    ShowTimeId = s.ShowTimeId,
                    MovieTitle = s.MovieTitle,
                    StartTime = s.StartTime,
                    SeatId = s.SeatId,
                    SeatNumber = s.SeatNumber,
                    SeatType = s.SeatType,
                    RoomId = s.RoomId,
                    RoomName = s.RoomName
                }).ToListAsync();
        }

        // ─── TẠO ĐƠN ĐẶT VÉ NHIỀU GHẾ + ĐỒ ĂN/COMBO + ÁP MÃ GIẢM GIÁ ──────────────
        public async Task<(bool IsSuccess, string Message, BookingSummaryResponse? Data)> CreateBookingAsync(
            BookingCreateRequest request, int currentUserId, string currentRole)
        {
            var currentShift = SellingShiftClock.GetCurrentShift();
            if (currentShift == null)
                return (false, SellingShiftClock.ClosedMessage, null);

            // ── BƯỚC 1: Kiểm tra danh sách ghế không trùng nhau ──────────────────
            if (request.SeatIds.Distinct().Count() != request.SeatIds.Count)
                return (false, ValidationMessages.BookingMessages.DuplicateSeatIds, null);

            // ── BƯỚC 2: Xác định UserId & StaffId ────────────────────────────────
            int finalUserId = currentUserId;
            int? staffId = null;

            var isStaffBooking =
                request.BookingType.Trim().Equals(ValidationMessages.Counter, StringComparison.OrdinalIgnoreCase) ||
                request.BookingType.Trim().Equals("Staff", StringComparison.OrdinalIgnoreCase);

            if (isStaffBooking)
            {
                if (currentRole != RoleConstants.Admin && currentRole != RoleConstants.Staff)
                    return (false, ValidationMessages.OnlyStaffCanCreateCounterBooking, null);

                finalUserId = (request.TargetUserId == null || request.TargetUserId == 0)
                              ? currentUserId
                              : request.TargetUserId.Value;
                staffId = currentUserId;
            }

            // ── BƯỚC 3: Validate & Load dữ liệu OrderItems trước khi bắt đầu Transaction
            // [NOTE]: Bước này lọc và kiểm tra tính hợp lệ của các mặt hàng ăn uống (Food/Combo) khách đặt kèm.
            // Đảm bảo không chọn trống cả hai, không chọn cả hai cùng lúc, còn hàng trong kho và đang hoạt động kinh doanh.
            var preparedOrderItems = new List<(int? FoodId, int? ComboId, string Name, decimal UnitPrice, int Qty, string? Snapshot)>();

            if (request.OrderItems != null && request.OrderItems.Any())
            {
                foreach (var item in request.OrderItems)
                {
                    // [NOTE]: Kiểm tra xem item chỉ thuộc 1 trong 2: Hoặc FoodId hoặc ComboId, không thể để trống hoặc chọn cả hai
                    if (item.FoodId == null && item.ComboId == null)
                        return (false, ValidationMessages.BookingMessages.FoodOrComboRequired, null);
                    if (item.FoodId != null && item.ComboId != null)
                        return (false, ValidationMessages.BookingMessages.FoodOrComboNotBoth, null);

                    if (item.FoodId != null)
                    {
                        // [NOTE]: Kiểm tra món ăn đơn lẻ (Food) trong Database
                        var food = await _context.Foods.FindAsync(item.FoodId.Value);
                        if (food == null)
                            return (false, FoodMessages.NotFoundWithId(item.FoodId.Value), null);
                        var bookingCinemaId = await _context.Showtimes.Where(x => x.ShowTimeId == request.ShowTimeId).Select(x => x.Room.CinemaId).SingleAsync();
                        var foodInventory = await _context.CinemaFoodInventories.AsNoTracking()
                            .SingleOrDefaultAsync(x => x.CinemaId == bookingCinemaId && x.FoodId == food.FoodId);
                        if (!food.IsAvailable || foodInventory == null || foodInventory.SaleStatus != "ACTIVE" || foodInventory.Quantity < item.Quantity)
                            return (false, ValidationMessages.BookingMessages.FoodOrComboUnavailable, null);

                        preparedOrderItems.Add((item.FoodId, null, food.FoodName, food.Price, item.Quantity, null));
                    }
                    else
                    {
                        // [NOTE]: Kiểm tra gói Combo trong Database
                        var combo = await _context.Combos.Include(x => x.Combofoodmappings).ThenInclude(x => x.Food).FirstOrDefaultAsync(x => x.ComboId == item.ComboId!.Value);
                        if (combo == null)
                            return (false, ComboMessages.NotFoundWithId(item.ComboId.Value), null);
                        // [NOTE]: Đảm bảo combo còn kinh doanh và đủ số lượng trong kho
                        var bookingCinemaId = await _context.Showtimes.Where(x => x.ShowTimeId == request.ShowTimeId).Select(x => x.Room.CinemaId).SingleAsync();
                        var comboSaleStatus = await _context.CinemaComboSettings.Where(x => x.CinemaId == bookingCinemaId && x.ComboId == combo.ComboId).Select(x => x.SaleStatus).SingleOrDefaultAsync();
                        comboSaleStatus ??= combo.IsAvailable ? "ACTIVE" : "INACTIVE";
                        if (comboSaleStatus != "ACTIVE")
                            return (false, "Combo hiện đã ngừng bán, vui lòng tải lại danh sách.", null);
                        if (item.SelectedComponents == null || item.SelectedComponents.Count == 0)
                            return (false, "Vui lòng chọn đủ thành phần Combo.", null);
                        var selectedIds = item.SelectedComponents.Select(x => x.FoodId).ToList();
                        var selectedFoods = await _context.Foods.Where(x => selectedIds.Contains(x.FoodId)).ToDictionaryAsync(x => x.FoodId);
                        var allowedIds = combo.Combofoodmappings.Select(x => x.FoodId).ToHashSet();
                        if (selectedFoods.Count != selectedIds.Distinct().Count() || selectedIds.Any(x => !allowedIds.Contains(x)))
                            return (false, "Có món không nằm trong danh sách được phép của Combo.", null);
                        static string Group(string? value) { var c=(value??"").ToLower(); return c.Contains("nước") ? "DRINK" : c.Contains("bắp") ? "POPCORN" : "OTHER"; }
                        var groups = item.SelectedComponents.GroupBy(x => Group(selectedFoods[x.FoodId].Category)).ToDictionary(x => x.Key, x => x.Sum(y => y.Quantity));
                        if (groups.GetValueOrDefault("DRINK") != combo.DrinkSlotCount * item.Quantity || groups.GetValueOrDefault("POPCORN") != combo.PopcornSlotCount * item.Quantity || groups.ContainsKey("OTHER"))
                            return (false, "Số lượng nước hoặc bắp không đúng cấu hình Combo.", null);
                        var snapshot = item.SelectedComponents.Select(x => new RapchieuPhim.API.DTO.DTOResponse.OrderComboComponentResponse { FoodId=x.FoodId, FoodName=selectedFoods[x.FoodId].FoodName, Category=selectedFoods[x.FoodId].Category, Quantity=x.Quantity, UnitPriceSnapshot=selectedFoods[x.FoodId].Price }).ToList();
                        preparedOrderItems.Add((null, item.ComboId, combo.ComboName, combo.Price, item.Quantity, System.Text.Json.JsonSerializer.Serialize(snapshot)));
                    }
                }
            }

            // ── BƯỚC 4: Kiểm tra Suất chiếu & Ghế ───────────────────────────────
            // [NOTE]: Lấy thông tin chi tiết của suất chiếu và toàn bộ danh sách ghế mà khách hàng yêu cầu
            var showtime = await _context.Showtimes
                .Include(s => s.Room)
                .FirstOrDefaultAsync(s => s.ShowTimeId == request.ShowTimeId);
            if (showtime == null)
                return (false, ShowtimeMessages.NotFoundWithId(request.ShowTimeId), null);

            var seats = await _context.Seats
                .Where(s => request.SeatIds.Contains(s.SeatId))
                .ToListAsync();

            // [NOTE]: Đảm bảo tìm đủ số lượng ghế trong Database tương ứng với số lượng ID gửi lên
            if (seats.Count != request.SeatIds.Count)
            {
                var foundIds = seats.Select(s => s.SeatId).ToHashSet();
                var missingId = request.SeatIds.First(id => !foundIds.Contains(id));
                return (false, ValidationMessages.SeatNotFoundWithId(missingId), null);
            }

            if (seats.Any(s => !s.IsActive || s.RoomId != showtime.RoomId))
                return (false, "Ghế không hoạt động hoặc không thuộc phòng của suất chiếu.", null);

            // Ghế Couple là một đơn vị bán gồm đúng hai ghế. Client bắt buộc gửi đủ
            // cả hai SeatId thuộc cùng CoupleGroupId; không chấp nhận mua lẻ một nửa.
            var requestedCoupleGroups = seats
                .Where(s => string.Equals(s.SeatType, "Couple", StringComparison.OrdinalIgnoreCase))
                .GroupBy(s => s.CoupleGroupId)
                .ToList();
            if (requestedCoupleGroups.Any(g => g.Key == null || g.Count() != 2))
                return (false, "Ghế Couple phải được chọn đủ một cặp hợp lệ.", null);

            foreach (var group in requestedCoupleGroups)
            {
                var databasePair = await _context.Seats
                    .Where(s => s.RoomId == showtime.RoomId && s.CoupleGroupId == group.Key && s.IsActive)
                    .Select(s => s.SeatId)
                    .ToListAsync();
                if (databasePair.Count != 2 || !databasePair.OrderBy(x => x).SequenceEqual(group.Select(s => s.SeatId).OrderBy(x => x)))
                    return (false, "Cặp ghế Couple không đầy đủ hoặc đã bị khóa.", null);
            }

            // ── BƯỚC 5: Kiểm tra không có ghế nào đã bị đặt ─────────────────────
            // [NOTE]: Kiểm tra xem có ghế nào trong số các ghế yêu cầu đã bị đặt trước đó cho suất chiếu này chưa
            // Chỉ loại trừ các booking có trạng thái là đã hủy "Cancelled"
            var takenSeatIds = await _context.Bookings
                .Where(b => b.ShowTimeId == request.ShowTimeId
                         && request.SeatIds.Contains(b.SeatId)
                         && b.Status != ShowtimeMessages.StatusCancelled)
                .Select(b => b.SeatId)
                .ToListAsync();

            if (takenSeatIds.Any())
                return (false, ValidationMessages.BookingMessages.SeatsAlreadyBooked(takenSeatIds), null);

            // ── BƯỚC 6: Lấy giá vé từ TicketPricing ─────────────────────────────
            // [NOTE]: Xác định ngày đặt vé là Ngày thường (Weekday) hay Cuối tuần (Weekend) để tính giá
            var today = DateOnly.FromDateTime(showtime.StartTime);
            string dayType = showtime.StartTime.DayOfWeek == DayOfWeek.Saturday ||
                             showtime.StartTime.DayOfWeek == DayOfWeek.Sunday
                             ? "Weekend" : "Weekday";

            // ── BƯỚC 7: Xử lý mã giảm giá ────────────────────────────────────────
            // [NOTE]: Xác thực mã giảm giá (DiscountCode) nếu khách hàng có áp dụng
            decimal totalDiscountAmt = 0; //Biến lưu tổng số tiền sẽ được giảm giá (mặc định ban đầu là 0 đồng).
            int? discountId = null;//Biến lưu ID của mã giảm giá (dùng kiểu int? để có thể nhận giá trị null nếu khách không áp mã).
            Discount? discount = null;//Biến dùng để chứa toàn bộ thông tin của mã giảm giá lấy từ Database lên.

            //Kiểm tra xem khách hàng có nhập mã giảm giá hay không.
            //Hàm IsNullOrWhiteSpace sẽ kiểm tra xem chuỗi có bị null, rỗng "" hoặc chỉ toàn dấu cách "   " hay không
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

                // [NOTE]: Kiểm tra xem mã giảm giá đã hết lượt dùng tối đa trên toàn hệ thống chưa
                if (discount.MaxUsageTotal.HasValue && discount.UsedCount >= discount.MaxUsageTotal.Value)
                    return (false, ValidationMessages.BookingMessages.DiscountMaxUsageReached, null);

                // [NOTE]: Kiểm tra xem tài khoản khách hàng đã dùng vượt quá số lần giới hạn cho phép chưa
                var userUsage = await _context.Userdiscountusages
                    .FirstOrDefaultAsync(u => u.UserId == finalUserId && u.DiscountId == discount.DiscountId);
                int userUsedCount = userUsage?.UsedCount ?? 0;//?? 0: Toán tử liên kết null. Nếu kết quả bên trái là null, hệ thống sẽ tự động gán giá trị bằng 0.
                if (userUsedCount >= discount.MaxUsagePerUser)
                    return (false, ValidationMessages.BookingMessages.DiscountUserLimitReached, null);

                discountId = discount.DiscountId;
            }

            // ── BƯỚC 8: Lưu tất cả trong 1 Transaction ───────────────────────────
            // [NOTE]: Bắt đầu một Transaction. Nếu có bất kỳ bước lưu dữ liệu nào phía sau bị lỗi (Exception),
            // toàn bộ các thay đổi trước đó sẽ được tự động rút lại (Rollback), đảm bảo an toàn tuyệt đối cho DB.
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var newBookingIds = new List<int>();
                decimal ticketTotal = 0; // Tổng tiền vé trước mọi ưu đãi
                decimal studentDiscountTotal = 0;
                var createdBookings = new List<(Booking Booking, decimal TicketPrice)>();
                var bookingDate = DateTime.Now; // 🌟 Thời gian đồng nhất cho cả nhóm ghế đặt cùng lúc

                // 8a. Tính giá và tạo Booking cho từng ghế
                int studentDiscountAppliedCount = 0;
                int maxStudentDiscounts = request.StudentCount.HasValue && request.StudentCount.Value > 0
                    ? Math.Min(request.StudentCount.Value, seats.Count)
                    : (request.IsStudent ? seats.Count : 0);

                foreach (var seat in seats)
                {
                    // [NOTE]: Tìm bảng giá vé khớp với loại phòng chiếu, loại ghế, loại ngày thường/cuối tuần và đang trong thời gian hiệu lực
                    var pricing = await _context.Ticketpricings
                        .Where(p => p.IsActive
                                 && (p.RoomId == showtime.RoomId || p.RoomId == null)
                                 && (p.RoomType == null || p.RoomType == showtime.Room.RoomType)
                                 && (p.SeatType == null || p.SeatType == seat.SeatType)
                                 && (p.DayType == null || p.DayType == dayType)
                                 && p.EffectFrom <= today
                                 && (p.EffectTo == null || p.EffectTo >= today))
                        .OrderByDescending(p => p.RoomId == showtime.RoomId)
                        .ThenByDescending(p => p.EffectFrom)
                        .FirstOrDefaultAsync();

                    if (pricing == null)
                        return (false, ValidationMessages.BookingMessages.PricingNotFound, null);

                    bool applyStudentDiscount = request.IsStudent && studentDiscountAppliedCount < maxStudentDiscounts;
                    bool isCoupleSeat = seat.SeatType != null && (
                        seat.SeatType.Contains("Couple", StringComparison.OrdinalIgnoreCase) ||
                        seat.SeatType.Contains("Sweetbox", StringComparison.OrdinalIgnoreCase) ||
                        seat.SeatType.Contains("Đôi", StringComparison.OrdinalIgnoreCase) ||
                        seat.SeatType.Contains("Doi", StringComparison.OrdinalIgnoreCase)
                    );

                    // Nếu là ghế đôi (Couple) và giá ghi nhận cho cả cặp (> 100k), chỉ tính 15% giảm trên 1 vé đơn (1/2 giá cặp)
                    decimal seatUnitPrice = isCoupleSeat ? pricing.Price / 2m : pricing.Price;
                    decimal studentDiscount = applyStudentDiscount ? Math.Round(seatUnitPrice * 0.15m, 0) : 0;
                    if (applyStudentDiscount) studentDiscountAppliedCount++;

                    decimal finalPrice = seatUnitPrice - studentDiscount;

                    ticketTotal += seatUnitPrice;
                    studentDiscountTotal += studentDiscount;
                    createdBookings.Add((new Booking
                    {
                        UserId = finalUserId,
                        ShowTimeId = request.ShowTimeId,
                        SeatId = seat.SeatId,
                        DiscountId = null,
                        BookingDate = bookingDate, // 🌟 Gán thời gian đồng nhất (dùng để gom nhóm khi thanh toán)
                        TicketPrice = seatUnitPrice,
                        DiscountAmt = studentDiscount,
                        TotalAmount = finalPrice,
                        BookingType = request.BookingType.Trim(),
                        StaffId = staffId,
                        ShiftId = currentShift.ShiftId,
                        Status = ValidationMessages.StutusComfirmed
                    }, finalPrice));
                }

                // Ưu đãi sinh viên đã được tính vào từng Booking ở trên.
                totalDiscountAmt = studentDiscountTotal;

                // 8b. Tính và phân bổ discount vào từng vé
                if (discount != null)
                {
                    var ticketAfterStudentDiscount = createdBookings.Sum(x => x.Booking.TotalAmount);
                    // [NOTE]: Đảm bảo tổng giá trị vé gốc lớn hơn hoặc bằng điều kiện giá trị đơn tối thiểu của mã giảm giá (MinOrderAmount)
                    if (ticketAfterStudentDiscount < discount.MinOrderAmount)
                        return (false, ValidationMessages.BookingMessages.OrderBelowMinAmount(discount.MinOrderAmount), null);

                    // [NOTE]: Tính tổng tiền giảm giá (giảm theo % hoặc giảm thẳng số tiền cố định)
                    var promotionDiscountAmt = discount.DiscountType == DiscountMessages.TypePercent
                        ? Math.Round(ticketAfterStudentDiscount * discount.DiscountValue / 100, 0)
                        : discount.DiscountValue;
                    promotionDiscountAmt = Math.Min(promotionDiscountAmt, ticketAfterStudentDiscount);
                    totalDiscountAmt += promotionDiscountAmt;

                    // [NOTE]: Chia đều số tiền được giảm giá cho toàn bộ số ghế đã chọn
                    //chia tổng số tiền giam giá cho từng ghê 
                    decimal discountPerSeat = Math.Floor(promotionDiscountAmt / seats.Count);
                    //Tính số tiền dư (phần lẻ còn sót lại sau khi làm tròn).
                    decimal remainder = promotionDiscountAmt - discountPerSeat * seats.Count;

                    for (int i = 0; i < createdBookings.Count; i++)
                    {
                        decimal thisDiscount = discountPerSeat + (i == createdBookings.Count - 1 ? remainder : 0);
                        var (booking, _) = createdBookings[i];
                        booking.DiscountId = discountId;
                        booking.DiscountAmt += thisDiscount;
                        booking.TotalAmount -= thisDiscount;
                    }
                }

                // 8c. Lưu từng Booking + Ticket vào DB (hoặc cập nhật nếu đã có bản ghi Cancelled trước đó để tránh vi phạm Unique Constraint)
                foreach (var (booking, _) in createdBookings)
                {
                    var existingBooking = await _context.Bookings
                        .FirstOrDefaultAsync(b => b.ShowTimeId == booking.ShowTimeId && b.SeatId == booking.SeatId);

                    if (existingBooking != null)
                    {
                        // Cập nhật lại bản ghi cũ đã bị hủy
                        existingBooking.UserId = booking.UserId;
                        existingBooking.BookingDate = booking.BookingDate;
                        existingBooking.TicketPrice = booking.TicketPrice;
                        existingBooking.DiscountAmt = booking.DiscountAmt;
                        existingBooking.DiscountId = booking.DiscountId;
                        existingBooking.TotalAmount = booking.TotalAmount;
                        existingBooking.BookingType = booking.BookingType;
                        existingBooking.StaffId = booking.StaffId;
                        existingBooking.Status = booking.Status;

                        await _context.SaveChangesAsync();

                        // Cập nhật hoặc tạo mới Ticket đi kèm
                        var existingTicket = await _context.Tickets
                            .FirstOrDefaultAsync(t => t.BookingId == existingBooking.BookingId);

                        string ticketCode = "TIC" + Guid.NewGuid().ToString().Replace("-", "").Substring(0, 7).ToUpper();
                        if (existingTicket != null)
                        {
                            existingTicket.TicketCode = ticketCode;
                            existingTicket.QrCodeUrl = null;
                            existingTicket.Price = booking.TotalAmount;
                            existingTicket.IssuedAt = DateTime.Now;
                            existingTicket.Status = PaymentMessages.StatusPending;
                        }
                        else
                        {
                            _context.Tickets.Add(new Ticket
                            {
                                BookingId = existingBooking.BookingId,
                                TicketCode = ticketCode,
                                QrCodeUrl = $"https://api.qrserver.com/v1/create-qr-code/?size=300x300&data={ticketCode}",
                                Price = booking.TotalAmount,
                                IssuedAt = DateTime.Now,
                                Status = PaymentMessages.StatusPending
                            });
                        }
                        await _context.SaveChangesAsync();
                        newBookingIds.Add(existingBooking.BookingId);
                    }
                    else
                    {
                        // Tạo mới hoàn toàn
                        _context.Bookings.Add(booking);
                        await _context.SaveChangesAsync();

                        // [NOTE]: Sinh TicketCode ngẫu nhiên duy nhất
                        string ticketCode = "TIC" + Guid.NewGuid().ToString().Replace("-", "").Substring(0, 7).ToUpper();
                        _context.Tickets.Add(new Ticket
                        {
                            BookingId = booking.BookingId,
                            TicketCode = ticketCode,
                            QrCodeUrl = $"https://api.qrserver.com/v1/create-qr-code/?size=300x300&data={ticketCode}",
                            Price = booking.TotalAmount,
                            IssuedAt = DateTime.Now,
                            Status = PaymentMessages.StatusPending           // Vé chờ xác nhận thanh toán
                        });
                        await _context.SaveChangesAsync();
                        newBookingIds.Add(booking.BookingId);
                    }
                }

                // 8d. Cập nhật thống kê mã giảm giá
                if (discount != null)
                {
                    discount.UsedCount++; // Tăng lượt dùng trên toàn hệ thống
                    var userUsage = await _context.Userdiscountusages
                        .FirstOrDefaultAsync(u => u.UserId == finalUserId && u.DiscountId == discount.DiscountId);
                    if (userUsage != null)
                    {
                        userUsage.UsedCount++; // Tăng lượt dùng của riêng User này
                        userUsage.LastUsedAt = DateTime.Now;
                    }
                    else
                    {
                        _context.Userdiscountusages.Add(new Userdiscountusage
                        {
                            UserId = finalUserId,
                            DiscountId = discount.DiscountId,
                            UsedCount = 1,
                            LastUsedAt = DateTime.Now
                        });
                    }
                    await _context.SaveChangesAsync();
                }

                // ── BƯỚC 9: Tạo Order + OrderItems nếu khách có đặt đồ ăn ───────
                // [NOTE]: Tạo và tính toán hóa đơn đồ ăn mua kèm nếu danh sách tạm có dữ liệu
                decimal foodTotal = 0;
                int? newOrderId = null;
                var orderItemResponses = new List<OrderItemSummary>();// Danh sách trả về cho Frontend hiển thị

                if (preparedOrderItems.Any())
                {
                    var order = new Order
                    {
                        UserId = finalUserId,
                        BookingId = newBookingIds.FirstOrDefault(),
                        StaffId = staffId,
                        OrderDate = DateTime.Now,
                        TotalAmount = 0,        // Cập nhật sau khi tính xong
                        OrderType = request.BookingType.Trim(),
                        Status = ValidationMessages.StutusComfirmed
                    };
                    _context.Orders.Add(order);
                    await _context.SaveChangesAsync(); // Lấy OrderId

                    foreach (var (foodId, comboId, name, unitPrice, qty, snapshot) in preparedOrderItems)
                    {
                        decimal subtotal = unitPrice * qty;
                        foodTotal += subtotal;

                        _context.Orderitems.Add(new Orderitem
                        {
                            OrderId = order.OrderId,
                            FoodId = foodId,
                            ComboId = comboId,
                            Quantity = qty,
                            UnitPrice = unitPrice,
                            Subtotal = subtotal,
                            ComboSelectionSnapshot = foodId != null
                                ? RapchieuPhim.API.DTO.DTOResponse.OrderItemSnapshotHelper.Serialize(name)
                                : RapchieuPhim.API.DTO.DTOResponse.OrderItemSnapshotHelper.Serialize(
                                    name,
                                    RapchieuPhim.API.DTO.DTOResponse.OrderItemSnapshotHelper.Parse(snapshot).ComboSelections),
                            ComboSelections = comboId.HasValue
                                ? RapchieuPhim.API.DTO.DTOResponse.OrderItemSnapshotHelper.Parse(snapshot).ComboSelections.Select(selection => new OrderComboSelection
                                {
                                    ComboId = comboId.Value,
                                    FoodId = selection.FoodId,
                                    FoodNameSnapshot = selection.FoodName,
                                    CategorySnapshot = selection.Category,
                                    Quantity = selection.Quantity,
                                    CreatedAt = DateTime.Now
                                }).ToList()
                                : new List<OrderComboSelection>()
                        });

                        // [NOTE]: Trừ tồn kho (Quantity) trong DB để tránh bán quá lượng tồn
                        if (foodId != null)
                        {
                            var inventory = await _context.CinemaFoodInventories.SingleOrDefaultAsync(x => x.CinemaId == showtime.Room.CinemaId && x.FoodId == foodId.Value);
                            if (inventory == null || inventory.SaleStatus != "ACTIVE" || inventory.Quantity < qty)
                                throw new InvalidOperationException("Món hiện đã ngừng bán hoặc không đủ tồn kho tại rạp, vui lòng tải lại danh sách.");
                            inventory.Quantity -= qty;
                        }
                        else if (comboId != null && snapshot != null)
                        {
                            var selections = RapchieuPhim.API.DTO.DTOResponse.OrderItemSnapshotHelper.Parse(snapshot).ComboSelections;
                            foreach (var selection in selections)
                            {
                                var inventory = await _context.CinemaFoodInventories.SingleOrDefaultAsync(x => x.CinemaId == showtime.Room.CinemaId && x.FoodId == selection.FoodId);
                                if (inventory == null || inventory.SaleStatus != "ACTIVE" || inventory.Quantity < selection.Quantity)
                                    throw new InvalidOperationException($"Món {selection.FoodName} không đủ tồn kho tại rạp.");
                                inventory.Quantity -= selection.Quantity;
                            }
                        }

                        orderItemResponses.Add(new OrderItemSummary
                        {
                            Name = name,
                            FoodId = foodId,
                            ComboId = comboId,
                            UnitPrice = unitPrice,
                            Quantity = qty,
                            Subtotal = subtotal
                        });
                    }

                    // [NOTE]: Cập nhật lại tổng tiền thực của hóa đơn ăn uống
                    order.TotalAmount = foodTotal;
                    await _context.SaveChangesAsync();
                    newOrderId = order.OrderId;
                }

                // [NOTE]: Hoàn tất và ghi nhận toàn bộ dữ liệu xuống database
                await transaction.CommitAsync();

                // ── BƯỚC 10: Tổng hợp dữ liệu phản hồi (Response Summary) ────────────────────────────────────
                // [NOTE]: Tính tổng tiền vé sau giảm giá và tổng tiền cuối cùng bao gồm đồ ăn (GrandTotal)
                decimal ticketAfterDiscount = createdBookings.Sum(x => x.Booking.TotalAmount);
                decimal grandTotal = ticketAfterDiscount + foodTotal;

                var summary = new BookingSummaryResponse
                {
                    BookingIds = newBookingIds,
                    OrderId = newOrderId,

                    TicketTotal = ticketTotal,
                    DiscountAmt = totalDiscountAmt,
                    TicketAfterDiscount = ticketAfterDiscount,

                    FoodTotal = foodTotal,
                    OrderItems = orderItemResponses,

                    GrandTotal = grandTotal,
                    FinalAmount = grandTotal
                };

                return (true, ValidationMessages.CreateBookingSuccess, summary);
            }
            catch (Exception ex)
            {
                // [NOTE]: Nếu có bất kỳ lỗi nào xảy ra trong Transaction, khôi phục lại dữ liệu ban đầu
                await transaction.RollbackAsync();
                Console.WriteLine(ValidationMessages.ErrorAutoTicket + ex.Message);
                return (false, ValidationMessages.BookingMessages.CreateBookingFailed, null);
            }
        }

        // Hủy đơn đặt vé — cập nhật trạng thái thành "Cancelled" (Soft Delete) để lưu lịch sử đối soát
        public async Task<(bool IsSuccess, string Message)> CancelBookingAsync(int bookingId, int currentUserId, string currentRole)
        {
            var booking = await _context.Bookings.FindAsync(bookingId);
            if (booking == null)
                return (false, ValidationMessages.BookingNotFoundWithId(bookingId));

            if (currentRole == RoleConstants.Customer && booking.UserId != currentUserId)
                return (false, ValidationMessages.UnauthorizedBookingCancel);

            // [NOTE]: Cập nhật trạng thái Booking thành "Cancelled" để giải phóng ghế nhưng vẫn giữ lại lịch sử
            booking.Status = ShowtimeMessages.StatusCancelled;

            // [NOTE]: Cập nhật trạng thái toàn bộ vé (Ticket) liên quan của Booking này thành "Cancelled"
            var tickets = await _context.Tickets
                .Where(t => t.BookingId == bookingId)
                .ToListAsync();

            foreach (var ticket in tickets)
            {
                ticket.Status = ShowtimeMessages.StatusCancelled;
            }

            await _context.SaveChangesAsync();

            return (true, ValidationMessages.CancelBookingSuccess);
        }
    }
}
