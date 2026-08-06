


using Microsoft.EntityFrameworkCore;
using RapchieuPhim.API.Constants;
using RapchieuPhim.API.DTOs.DTORequest;
using RapchieuPhim.API.DTOs.DTOResponse;
using RapchieuPhim.API.Models;

namespace RapchieuPhim.API.Services
{
    public interface ITicketService
    {
        Task<List<TicketResponse>> GetAllAsync(string? date = null);
        Task<TicketResponse?> GetByIdAsync(int id);
        Task<TicketResponse?> GetByCodeAsync(string ticketCode);
        Task<List<TicketResponse>> GetByBookingAsync(int bookingId);
        Task<TicketResponse> CreateAsync(TicketCreateRequest request);
        Task<(bool IsSuccess, string Message, int StatusCode, TicketResponse? Ticket)> UpdateStatusAsync(int id, TicketStatusRequest request);
        Task<(bool IsSuccess, string Message, int StatusCode, TicketResponse? Ticket)> ScanTicketAsync(string ticketCode, int? showtimeId = null);
        Task<SeatExchangeResponse> RequestSeatExchangeAsync(int userId, int? staffId, SeatExchangeRequest request);
        Task<SeatExchangeResponse> ConfirmCashSeatExchangeAsync(int staffId, ConfirmCashExchangeRequest request);
    }


    public class TicketService : ITicketService
    {
        private readonly CinemaManagementContext _context;
        private readonly ISeatHoldService _seatHoldService;

        public TicketService(CinemaManagementContext context, ISeatHoldService seatHoldService)
        {
            _context = context;
            _seatHoldService = seatHoldService;
        }

        public async Task<List<TicketResponse>> GetAllAsync(string? date = null)
        {
            var query = _context.Tickets
                .AsNoTracking()
                .AsSplitQuery()
                .Include(t => t.Booking).ThenInclude(b => b.User)
                .Include(t => t.Booking).ThenInclude(b => b.ShowTime).ThenInclude(s => s.Movie)
                .Include(t => t.Booking).ThenInclude(b => b.ShowTime).ThenInclude(s => s.Room).ThenInclude(r => r.Cinema).ThenInclude(c => c.Area)
                .Include(t => t.Booking).ThenInclude(b => b.Seat)
                .Include(t => t.Booking).ThenInclude(b => b.Orders).ThenInclude(o => o.Orderitems).ThenInclude(oi => oi.Food)
                .Include(t => t.Booking).ThenInclude(b => b.Orders).ThenInclude(o => o.Orderitems).ThenInclude(oi => oi.Combo)
                .AsQueryable();

            if (!string.IsNullOrEmpty(date) && DateTime.TryParse(date, out var parsedDate))
            {
                var start = parsedDate.Date;
                var end = start.AddDays(1);
                query = query.Where(t => t.IssuedAt >= start && t.IssuedAt < end);
            }

            var list = await query
                .OrderByDescending(t => t.IssuedAt)
                .Take(500)
                .ToListAsync();
            return list.Select(MapToResponse).ToList();
        }

        public async Task<TicketResponse?> GetByIdAsync(int id)
        {
            var ticket = await _context.Tickets
                .AsNoTracking()
                .AsSplitQuery()
                .Include(t => t.Booking).ThenInclude(b => b.User)
                .Include(t => t.Booking).ThenInclude(b => b.ShowTime).ThenInclude(s => s.Movie)
                .Include(t => t.Booking).ThenInclude(b => b.ShowTime).ThenInclude(s => s.Room).ThenInclude(r => r.Cinema).ThenInclude(c => c.Area)
                .Include(t => t.Booking).ThenInclude(b => b.Seat)
                .Include(t => t.Booking).ThenInclude(b => b.Orders).ThenInclude(o => o.Orderitems).ThenInclude(oi => oi.Food)
                .Include(t => t.Booking).ThenInclude(b => b.Orders).ThenInclude(o => o.Orderitems).ThenInclude(oi => oi.Combo)
                .Where(t => t.TicketId == id)
                .FirstOrDefaultAsync();
            return ticket == null ? null : MapToResponse(ticket);
        }

        public async Task<TicketResponse?> GetByCodeAsync(string ticketCode)
        {
            var cleanCode = ticketCode.Trim();
            var ticket = await _context.Tickets
                .AsNoTracking()
                .AsSplitQuery()
                .Include(t => t.Booking).ThenInclude(b => b.User)
                .Include(t => t.Booking).ThenInclude(b => b.ShowTime).ThenInclude(s => s.Movie)
                .Include(t => t.Booking).ThenInclude(b => b.ShowTime).ThenInclude(s => s.Room).ThenInclude(r => r.Cinema).ThenInclude(c => c.Area)
                .Include(t => t.Booking).ThenInclude(b => b.Seat)
                .Include(t => t.Booking).ThenInclude(b => b.Orders).ThenInclude(o => o.Orderitems).ThenInclude(oi => oi.Food)
                .Include(t => t.Booking).ThenInclude(b => b.Orders).ThenInclude(o => o.Orderitems).ThenInclude(oi => oi.Combo)
                .Where(t => t.TicketCode == cleanCode)
                .FirstOrDefaultAsync();
            return ticket == null ? null : MapToResponse(ticket);
        }

        public async Task<List<TicketResponse>> GetByBookingAsync(int bookingId)
        {
            var list = await _context.Tickets
                .AsNoTracking()
                .AsSplitQuery()
                .Include(t => t.Booking).ThenInclude(b => b.User)
                .Include(t => t.Booking).ThenInclude(b => b.ShowTime).ThenInclude(s => s.Movie)
                .Include(t => t.Booking).ThenInclude(b => b.ShowTime).ThenInclude(s => s.Room).ThenInclude(r => r.Cinema).ThenInclude(c => c.Area)
                .Include(t => t.Booking).ThenInclude(b => b.Seat)
                .Include(t => t.Booking).ThenInclude(b => b.Orders).ThenInclude(o => o.Orderitems).ThenInclude(oi => oi.Food)
                .Include(t => t.Booking).ThenInclude(b => b.Orders).ThenInclude(o => o.Orderitems).ThenInclude(oi => oi.Combo)
                .Where(t => t.BookingId == bookingId)
                .ToListAsync();
            return list.Select(MapToResponse).ToList();
        }

        public async Task<TicketResponse> CreateAsync(TicketCreateRequest request)
        {
            string autoTicketCode = "TIC" + Guid.NewGuid().ToString().Replace("-", "").Substring(0, 7).ToUpper();
            string autoQrCodeUrl = $"https://api.qrserver.com/v1/create-qr-code/?size=250x250&data={autoTicketCode}";

            var ticket = new Ticket
            {
                BookingId = request.BookingId,
                TicketCode = autoTicketCode,
                QrCodeUrl = autoQrCodeUrl,
                Price = request.Price,
                IssuedAt = DateTime.Now,
                Status = ValidationMessages.TicketStatusActive
            };

            _context.Tickets.Add(ticket);
            await _context.SaveChangesAsync();

            var created = await _context.Tickets
                .Include(t => t.Booking).ThenInclude(b => b.User)
                .Include(t => t.Booking).ThenInclude(b => b.ShowTime).ThenInclude(s => s.Movie)
                .Include(t => t.Booking).ThenInclude(b => b.ShowTime).ThenInclude(s => s.Room).ThenInclude(r => r.Cinema).ThenInclude(c => c.Area)
                .Include(t => t.Booking).ThenInclude(b => b.Seat)
                .FirstOrDefaultAsync(t => t.TicketId == ticket.TicketId);

            return new TicketResponse
            {
                TicketId = ticket.TicketId,
                BookingId = ticket.BookingId,
                TicketCode = ticket.TicketCode,
                QrCodeUrl = ticket.QrCodeUrl,
                Price = ticket.Price,
                IssuedAt = ticket.IssuedAt,
                Status = ticket.Status,
                CustomerName = created?.Booking?.User?.FullName ?? "Khách vãng lai",
                MovieTitle = created?.Booking?.ShowTime?.Movie?.Title ?? "N/A",
                SeatCode = created?.Booking?.Seat != null ? (created.Booking.Seat.SeatRow + created.Booking.Seat.SeatNumber) : "N/A",
                AreaName = created?.Booking?.ShowTime?.Room?.Cinema?.Area?.AreaName ?? "N/A",
                CinemaId = created?.Booking?.ShowTime?.Room?.Cinema?.CinemaId,
                CinemaName = created?.Booking?.ShowTime?.Room?.Cinema?.CinemaName ?? "N/A",
                RoomName = created?.Booking?.ShowTime?.Room?.RoomName ?? "N/A",
                ShowtimeStart = created?.Booking?.ShowTime?.StartTime,
                ShowtimeEnd = created?.Booking?.ShowTime?.EndTime
            };
        }

        public async Task<(bool IsSuccess, string Message, int StatusCode, TicketResponse? Ticket)> UpdateStatusAsync(int id, TicketStatusRequest request)
        {
            var ticket = await _context.Tickets
                .Include(t => t.Booking)
                    .ThenInclude(b => b.ShowTime)
                        .ThenInclude(s => s.Movie)
                .Include(t => t.Booking)
                    .ThenInclude(b => b.Seat)
                .Include(t => t.Booking)
                    .ThenInclude(b => b.Orders)
                        .ThenInclude(o => o.Orderitems)
                            .ThenInclude(oi => oi.Food)
                .Include(t => t.Booking)
                    .ThenInclude(b => b.Orders)
                        .ThenInclude(o => o.Orderitems)
                            .ThenInclude(oi => oi.Combo)
                .FirstOrDefaultAsync(t => t.TicketId == id);

            if (ticket == null)
                return (false, ValidationMessages.TicketNotFoundWithId(id), 404, null);

            var newStatus = request.Status.Trim();

            var validStatuses = new[] { ValidationMessages.TicketStatusActive, ValidationMessages.TicketStatusUsed, ValidationMessages.TicketStatusCancelled };
            if (!validStatuses.Contains(newStatus))
                return (false, ValidationMessages.TicketStatusInvalid, 400, null);

            ticket.Status = newStatus;

            // Generate QR Code if transitioned to Active and currently missing QrCodeUrl
            if (newStatus == ValidationMessages.TicketStatusActive && string.IsNullOrEmpty(ticket.QrCodeUrl))
            {
                var booking = ticket.Booking;
                var showtime = booking?.ShowTime;
                var movie = showtime?.Movie;
                var seat = booking?.Seat;

                string movieTitle = movie?.Title ?? "Phim";
                string seatInfo = seat != null ? $"{seat.SeatRow}{seat.SeatNumber}" : "N/A";
                string showtimeInfo = showtime != null
                    ? showtime.StartTime.ToString("dd/MM/yyyy HH:mm")
                    : "N/A";
                string priceInfo = booking != null
                    ? $"{booking.TotalAmount:N0} VND"
                    : "N/A";

                var allOrderItems = booking?.Orders
                    .SelectMany(o => o.Orderitems)
                    .ToList() ?? new List<Orderitem>();

                var foodParts = allOrderItems
                    .Where(oi => oi.Food != null)
                    .Select(oi => $"{oi.Food!.FoodName}x{oi.Quantity}")
                    .ToList();

                var comboParts = allOrderItems
                    .Where(oi => oi.Combo != null)
                    .Select(oi => $"{oi.Combo!.ComboName}x{oi.Quantity}")
                    .ToList();

                var allFoodComboParts = foodParts.Concat(comboParts).ToList();
                string foodInfo = allFoodComboParts.Count > 0
                    ? string.Join(",", allFoodComboParts)
                    : string.Empty;

                string qrData = $"VE:{ticket.TicketCode}|PHIM:{movieTitle}|SUAT:{showtimeInfo}|GHE:{seatInfo}|GIA:{priceInfo}|TRANG_THAI:{ticket.Status}";
                if (!string.IsNullOrEmpty(foodInfo))
                    qrData += $"|DO_AN:{foodInfo}";

                string encodedQrData = Uri.EscapeDataString(qrData);
                ticket.QrCodeUrl = $"https://api.qrserver.com/v1/create-qr-code/?size=300x300&data={encodedQrData}";
            }

            try
            {
                await _context.SaveChangesAsync();
                return (true, ValidationMessages.TicketUpdateStatusSuccess, 200, MapToResponse(ticket));
            }
            catch (DbUpdateConcurrencyException)
            {
                return (false, ValidationMessages.TicketConcurrencyError, 409, null);
            }
        }

        // ─────────────────────────────────────────────────────────────────────────
        // SOÁT VÉ QR — Nhân viên quét mã vé tại cửa phòng chiếu
        //
        // Logic:
        //   - Active   → Hợp lệ → Cho vào → Đổi sang "Used"
        //   - Used     → Từ chối (Vé đã được sử dụng rồi)
        //   - Pending  → Từ chối (Chưa thanh toán)
        //   - Cancelled→ Từ chối (Vé đã bị hủy)
        // ─────────────────────────────────────────────────────────────────────────
        public async Task<(bool IsSuccess, string Message, int StatusCode, TicketResponse? Ticket)> ScanTicketAsync(string ticketCode, int? showtimeId = null)
        {
            var cleanCode = ticketCode.Trim().ToUpper();

            var ticket = await _context.Tickets
                .Include(t => t.Booking).ThenInclude(b => b.User)
                .Include(t => t.Booking).ThenInclude(b => b.ShowTime).ThenInclude(s => s.Movie)
                .Include(t => t.Booking).ThenInclude(b => b.ShowTime).ThenInclude(s => s.Room).ThenInclude(r => r.Cinema).ThenInclude(c => c.Area)
                .Include(t => t.Booking).ThenInclude(b => b.Seat)
                .Include(t => t.Booking).ThenInclude(b => b.Orders).ThenInclude(o => o.Orderitems).ThenInclude(oi => oi.Food)
                .Include(t => t.Booking).ThenInclude(b => b.Orders).ThenInclude(o => o.Orderitems).ThenInclude(oi => oi.Combo)
                .FirstOrDefaultAsync(t => t.TicketCode == cleanCode);

            if (ticket == null)
                return (false, ValidationMessages.TicketNotFoundWithCode(cleanCode), 404, null);

            // Kiểm tra trạng thái vé
            if (ticket.Status == ValidationMessages.TicketStatusUsed)
                return (false, ValidationMessages.TicketScanAlreadyUsed, 409, MapToResponse(ticket));

            if (ticket.Status == ValidationMessages.TicketStatusCancelled)
                return (false, ValidationMessages.TicketScanCancelled, 400, MapToResponse(ticket));

            if (ticket.Status != ValidationMessages.TicketStatusActive)
                return (false, ValidationMessages.TicketScanInvalidStatus(ticket.Status), 400, MapToResponse(ticket));

            var isPaid = await _context.Payments.AnyAsync(payment =>
                payment.BookingId == ticket.BookingId && payment.PaymentStatus == PaymentMessages.StatusSuccess);
            if (!isPaid)
                return (false, "Vé chưa được thanh toán.", 400, MapToResponse(ticket));

            if (showtimeId.HasValue && ticket.Booking.ShowTimeId != showtimeId.Value)
                return (false, "Vé không thuộc suất chiếu tại cửa này.", 400, MapToResponse(ticket));

            // Vé hợp lệ (Active) → Đổi sang Used
            ticket.Status = ValidationMessages.TicketStatusUsed;
            await _context.SaveChangesAsync();

            return (true, ValidationMessages.TicketScanSuccess, 200, MapToResponse(ticket));
        }

        // Helper: chuyển Ticket entity sang TicketResponse
        private static TicketResponse MapToResponse(Ticket ticket)
        {
            var booking = ticket.Booking;
            var showtime = booking?.ShowTime;
            var seatRow = booking?.Seat?.SeatRow?.Trim() ?? string.Empty;
            var seatNumber = booking?.Seat?.SeatNumber?.Trim() ?? string.Empty;
            var seatCode = !string.IsNullOrEmpty(seatRow) && !seatNumber.StartsWith(seatRow, StringComparison.OrdinalIgnoreCase)
                ? $"{seatRow}{seatNumber}"
                : seatNumber;
            var response = new TicketResponse
            {
                TicketId = ticket.TicketId,
                BookingId = ticket.BookingId,
                ShowtimeId = booking?.ShowTimeId,
                RoomId = showtime?.RoomId,
                TicketCode = ticket.TicketCode,
                QrCodeUrl = ticket.QrCodeUrl,
                Price = ticket.Price,
                IssuedAt = ticket.IssuedAt,
                Status = ticket.Status,
                CustomerName = booking?.User?.FullName ?? "Khách vãng lai",
                MovieTitle = showtime?.Movie?.Title ?? "N/A",
                SeatCode = string.IsNullOrEmpty(seatCode) ? "N/A" : seatCode,
                SeatRow = seatRow,
                SeatNumber = seatNumber,
                SeatType = booking?.Seat?.SeatType,
                AreaName = showtime?.Room?.Cinema?.Area?.AreaName ?? "N/A",
                CinemaId = showtime?.Room?.Cinema?.CinemaId,
                CinemaName = showtime?.Room?.Cinema?.CinemaName ?? "N/A",
                RoomName = showtime?.Room?.RoomName ?? "N/A",
                ShowtimeStart = showtime?.StartTime,
                ShowtimeEnd = showtime?.EndTime
            };

            if (string.IsNullOrEmpty(response.QrCodeUrl) && !string.IsNullOrEmpty(response.TicketCode))
            {
                string qrData = $"VE:{response.TicketCode}|PHIM:{response.MovieTitle}|GHE:{response.SeatCode}|GIA:{response.Price:N0} VND|TRANG_THAI:{response.Status}";
                response.QrCodeUrl = $"https://api.qrserver.com/v1/create-qr-code/?size=300x300&data={Uri.EscapeDataString(qrData)}";
            }

            if (booking?.Orders != null)
            {
                foreach (var order in booking.Orders)
                {
                    if (order.Orderitems != null)
                    {
                        foreach (var oi in order.Orderitems)
                        {
                            var name = oi.Food?.FoodName ?? oi.Combo?.ComboName ?? "Đồ ăn kèm";
                            var snapshot = RapchieuPhim.API.DTO.DTOResponse.OrderItemSnapshotHelper.Parse(oi.ComboSelectionSnapshot, name);
                            response.Foods.Add(new BookingFoodDetailResponse
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
            }

            return response;
        }

        // ─────────────────────────────────────────────────────────────────────────
        // ĐỔI GHẾ TẠI QUẦY — 1. YÊU CẦU ĐỔI GHẾ
        // ─────────────────────────────────────────────────────────────────────────
        public async Task<SeatExchangeResponse> RequestSeatExchangeAsync(int userId, int? staffId, SeatExchangeRequest request)
        {
            var ticket = await _context.Tickets
                .Include(t => t.Booking).ThenInclude(b => b.User)
                .Include(t => t.Booking).ThenInclude(b => b.ShowTime).ThenInclude(s => s.Room).ThenInclude(r => r.Cinema).ThenInclude(c => c.Area)
                .Include(t => t.Booking).ThenInclude(b => b.Seat)
                .FirstOrDefaultAsync(t => t.TicketId == request.TicketId);

            if (ticket == null)
                return new SeatExchangeResponse { IsSuccess = false, Message = ValidationMessages.TicketNotFoundWithId(request.TicketId) };

            if (ticket.Status == ValidationMessages.TicketStatusCancelled)
                return new SeatExchangeResponse { IsSuccess = false, Message = "Vé đã bị hủy, không thể thực hiện đổi ghế." };

            var booking = ticket.Booking;
            if (booking == null)
                return new SeatExchangeResponse { IsSuccess = false, Message = "Thông tin đơn đặt vé không tồn tại." };

            var oldSeat = booking.Seat;
            if (oldSeat == null)
                return new SeatExchangeResponse { IsSuccess = false, Message = "Thông tin ghế cũ không tồn tại." };

            var newSeat = await _context.Seats
                .Include(s => s.Room)
                .FirstOrDefaultAsync(s => s.SeatId == request.NewSeatId);

            if (newSeat == null)
                return new SeatExchangeResponse { IsSuccess = false, Message = "Ghế mới không tồn tại trên hệ thống." };

            var oldIsCouple = string.Equals(oldSeat.SeatType, "Couple", StringComparison.OrdinalIgnoreCase);
            var newIsCouple = string.Equals(newSeat.SeatType, "Couple", StringComparison.OrdinalIgnoreCase);
            if (oldIsCouple != newIsCouple)
                return new SeatExchangeResponse { IsSuccess = false, Message = "Ghế Couple chỉ được đổi sang một cặp Couple hợp lệ khác." };

            Seat? newPartnerSeat = null;
            Booking? oldPartnerBooking = null;
            if (oldIsCouple)
            {
                if (!oldSeat.CoupleGroupId.HasValue || !newSeat.CoupleGroupId.HasValue)
                    return new SeatExchangeResponse { IsSuccess = false, Message = "Cặp ghế Couple chưa có CoupleGroupId hợp lệ." };

                newPartnerSeat = await _context.Seats.FirstOrDefaultAsync(s =>
                    s.RoomId == newSeat.RoomId && s.CoupleGroupId == newSeat.CoupleGroupId && s.SeatId != newSeat.SeatId && s.IsActive);
                oldPartnerBooking = await _context.Bookings
                    .Include(b => b.Seat).Include(b => b.Tickets)
                    .FirstOrDefaultAsync(b => b.ShowTimeId == booking.ShowTimeId
                        && b.BookingDate == booking.BookingDate
                        && b.UserId == booking.UserId
                        && b.Seat.CoupleGroupId == oldSeat.CoupleGroupId
                        && b.BookingId != booking.BookingId
                        && b.Status != "Cancelled");
                if (newPartnerSeat == null || oldPartnerBooking == null)
                    return new SeatExchangeResponse { IsSuccess = false, Message = "Không tìm thấy đầy đủ hai ghế của cặp Couple." };
            }

            // 0. TỰ ĐỘNG HỦY YÊU CẦU ĐỔI GHẾ PENDING CŨ CỦA VÉ NÀY VÀ GIẢI PHÓNG GHẾ ĐANG GIỮ
            var pendingExchanges = await _context.TicketExchanges
                .Where(e => e.TicketId == ticket.TicketId && e.Status == "PENDING_CASH_PAYMENT")
                .ToListAsync();

            foreach (var oldEx in pendingExchanges)
            {
                oldEx.Status = "CANCELLED";
                _seatHoldService.ReleaseSeatBySeat(oldEx.ShowTimeId, oldEx.NewSeatId);
            }
            if (pendingExchanges.Any())
            {
                await _context.SaveChangesAsync();
            }

            var showtime = booking.ShowTime;
            if (showtime == null)
                return new SeatExchangeResponse { IsSuccess = false, Message = "Thông tin suất chiếu không tồn tại." };

            // 1. CHỈ ĐƯỢC ĐỔI GHẾ TRONG CÙNG SUẤT CHIẾU VÀ CÙNG PHÒNG
            if (newSeat.RoomId != showtime.RoomId)
            {
                return new SeatExchangeResponse
                {
                    IsSuccess = false,
                    Message = "Khách chỉ được đổi ghế trong cùng suất chiếu và cùng phòng."
                };
            }

            if (!newSeat.IsActive)
                return new SeatExchangeResponse { IsSuccess = false, Message = "Ghế mới đang Inactive." };

            if (newSeat.SeatId == oldSeat.SeatId)
            {
                return new SeatExchangeResponse
                {
                    IsSuccess = false,
                    Message = "Ghế mới trùng với ghế hiện tại của vé."
                };
            }

            // 2. KIỂM TRA GHẾ MỚI CÓ BỊ TRÙNG / GIỮ KHÔNG
            bool isSeatBooked = await _context.Bookings
                .AnyAsync(b => b.ShowTimeId == booking.ShowTimeId
                            && b.SeatId == request.NewSeatId
                            && b.Status != "Cancelled"
                            && b.BookingId != booking.BookingId);

            bool isSeatHeld = _seatHoldService.IsSeatHeld(booking.ShowTimeId, request.NewSeatId);

            if (newPartnerSeat != null)
            {
                isSeatBooked = isSeatBooked || await _context.Bookings.AnyAsync(b =>
                    b.ShowTimeId == booking.ShowTimeId && b.SeatId == newPartnerSeat.SeatId && b.Status != "Cancelled");
                isSeatHeld = isSeatHeld || _seatHoldService.IsSeatHeld(booking.ShowTimeId, newPartnerSeat.SeatId);
            }

            if (isSeatBooked || isSeatHeld)
            {
                return new SeatExchangeResponse
                {
                    IsSuccess = false,
                    Message = $"Ghế {newSeat.SeatRow}{newSeat.SeatNumber} đã có người đặt hoặc đang được giữ."
                };
            }

            // 3. TÍNH GIÁ TỪ DATABASE (KHÔNG TIN TƯỞNG CLIENT)
            decimal oldPrice = ticket.Price > 0 ? ticket.Price : await CalculateSeatPriceAsync(showtime, oldSeat);
            decimal newPrice = await CalculateSeatPriceAsync(showtime, newSeat);
            if (oldPartnerBooking != null && newPartnerSeat != null)
            {
                oldPrice += oldPartnerBooking.Tickets.FirstOrDefault()?.Price ?? oldPartnerBooking.TicketPrice;
                newPrice += await CalculateSeatPriceAsync(showtime, newPartnerSeat);
            }

            // CHỈ ĐƯỢC CHỌN GHẾ CÙNG GIÁ HOẶC GIÁ CAO HƠN
            if (newPrice < oldPrice)
            {
                return new SeatExchangeResponse
                {
                    IsSuccess = false,
                    Message = $"Chỉ được chọn ghế cùng giá hoặc giá cao hơn. (Ghế cũ: {oldPrice:N0}đ, Ghế mới: {newPrice:N0}đ)"
                };
            }

            decimal additionalAmount = newPrice - oldPrice;

            // TRƯỜNG HỢP 1: GHẾ MỚI CÙNG GIÁ
            if (additionalAmount == 0)
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    booking.SeatId = newSeat.SeatId;
                    booking.TicketPrice = newPrice;
                    booking.TotalAmount = booking.TotalAmount - oldPrice + newPrice;
                    ticket.Price = newPrice;
                    if (oldPartnerBooking != null && newPartnerSeat != null)
                    {
                        var pairUnitPrice = newPrice / 2m;
                        booking.TicketPrice = pairUnitPrice;
                        booking.TotalAmount = booking.TotalAmount - (oldPrice / 2m) + pairUnitPrice;
                        ticket.Price = pairUnitPrice;
                        oldPartnerBooking.SeatId = newPartnerSeat.SeatId;
                        oldPartnerBooking.TicketPrice = pairUnitPrice;
                        oldPartnerBooking.TotalAmount = oldPartnerBooking.TotalAmount - (oldPrice / 2m) + pairUnitPrice;
                        var partnerTicket = oldPartnerBooking.Tickets.FirstOrDefault();
                        if (partnerTicket != null) partnerTicket.Price = pairUnitPrice;
                    }
                    _seatHoldService.ReleaseSeatBySeat(booking.ShowTimeId, oldSeat.SeatId);

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    var updatedTicket = MapToResponse(ticket);
                    return new SeatExchangeResponse
                    {
                        IsSuccess = true,
                        Message = $"Đổi ghế thành công sang ghế {newSeat.SeatRow}{newSeat.SeatNumber} (cùng giá {oldPrice:N0}đ). Không phát sinh thanh toán.",
                        RequiresPayment = false,
                        AdditionalAmount = 0,
                        Ticket = updatedTicket
                    };
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    return new SeatExchangeResponse
                    {
                        IsSuccess = false,
                        Message = "Lỗi khi cập nhật ghế mới: " + ex.Message
                    };
                }
            }

            // TRƯỜNG HỢP 2: GHẾ MỚI GIÁ CAO HƠN
            // Giữ ghế mới trong 10 phút. Ghế cũ vẫn thuộc về khách đến khi thanh toán xong.
            var holdResult = _seatHoldService.HoldSeat(userId, booking.ShowTimeId, newSeat.SeatId, 10);
            if (!holdResult.IsSuccess)
            {
                return new SeatExchangeResponse
                {
                    IsSuccess = false,
                    Message = holdResult.Message
                };
            }
            if (newPartnerSeat != null)
            {
                var pairHold = _seatHoldService.HoldSeat(userId, booking.ShowTimeId, newPartnerSeat.SeatId, 10);
                if (!pairHold.IsSuccess)
                {
                    _seatHoldService.ReleaseSeatBySeat(booking.ShowTimeId, newSeat.SeatId);
                    return new SeatExchangeResponse { IsSuccess = false, Message = pairHold.Message };
                }
            }

            var holdUntil = DateTime.Now.AddMinutes(10);
            var exchange = new TicketExchange
            {
                TicketId = ticket.TicketId,
                OldSeatId = oldSeat.SeatId,
                NewSeatId = newSeat.SeatId,
                ShowTimeId = booking.ShowTimeId,
                UserId = booking.UserId,
                StaffId = staffId,
                AdditionalAmount = additionalAmount,
                HoldUntil = holdUntil,
                Status = "PENDING_CASH_PAYMENT",
                CreatedAt = DateTime.Now
            };

            _context.TicketExchanges.Add(exchange);
            await _context.SaveChangesAsync();

            return new SeatExchangeResponse
            {
                IsSuccess = true,
                Message = $"Ghế mới {newSeat.SeatRow}{newSeat.SeatNumber} có giá {newPrice:N0}đ (chênh lệch: {additionalAmount:N0}đ). Ghế được giữ trong 10 phút. Vui lòng thu tiền mặt tại quầy.",
                RequiresPayment = true,
                AdditionalAmount = additionalAmount,
                ExchangeId = exchange.ExchangeId,
                HoldUntil = holdUntil
            };
        }

        // ─────────────────────────────────────────────────────────────────────────
        // ĐỔI GHẾ TẠI QUẦY — 2. XÁC NHẬN THU TIỀN MẶT (CONFIRM-CASH)
        // ─────────────────────────────────────────────────────────────────────────
        public async Task<SeatExchangeResponse> ConfirmCashSeatExchangeAsync(int staffId, ConfirmCashExchangeRequest request)
        {
            var exchange = await _context.TicketExchanges
                .Include(e => e.Ticket).ThenInclude(t => t.Booking).ThenInclude(b => b.User)
                .Include(e => e.Ticket).ThenInclude(t => t.Booking).ThenInclude(b => b.ShowTime).ThenInclude(s => s.Room).ThenInclude(r => r.Cinema).ThenInclude(c => c.Area)
                .Include(e => e.NewSeat)
                .Include(e => e.OldSeat)
                .FirstOrDefaultAsync(e => e.ExchangeId == request.ExchangeId);

            if (exchange == null)
                return new SeatExchangeResponse { IsSuccess = false, Message = "Yêu cầu đổi ghế không tồn tại." };

            if (exchange.Status != "PENDING_CASH_PAYMENT")
            {
                return new SeatExchangeResponse
                {
                    IsSuccess = false,
                    Message = $"Yêu cầu đổi ghế không ở trạng thái chờ thanh toán (Trạng thái hiện tại: {exchange.Status})."
                };
            }

            if (DateTime.Now > exchange.HoldUntil)
            {
                exchange.Status = "EXPIRED";
                await _context.SaveChangesAsync();
                return new SeatExchangeResponse
                {
                    IsSuccess = false,
                    Message = "Hết thời gian 10 phút giữ ghế cho giao dịch đổi ghế này. Vui lòng thao tác lại."
                };
            }

            if (request.AmountPaid < exchange.AdditionalAmount)
            {
                return new SeatExchangeResponse
                {
                    IsSuccess = false,
                    Message = $"Số tiền khách đưa ({request.AmountPaid:N0}đ) chưa đủ số tiền cần thu ({exchange.AdditionalAmount:N0}đ)."
                };
            }

            // Thực hiện giao dịch nguyên tử (Atomic Transaction)
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var ticket = exchange.Ticket;
                var booking = ticket.Booking;
                var newSeat = exchange.NewSeat;
                if (!newSeat.IsActive || await _context.Bookings.AnyAsync(b =>
                    b.ShowTimeId == booking.ShowTimeId && b.SeatId == newSeat.SeatId
                    && b.Status != "Cancelled" && b.BookingId != booking.BookingId))
                    return new SeatExchangeResponse { IsSuccess = false, Message = "Ghế mới không còn khả dụng." };
                var isCoupleExchange = string.Equals(exchange.OldSeat.SeatType, "Couple", StringComparison.OrdinalIgnoreCase);
                Seat? newPartnerSeat = null;
                Booking? oldPartnerBooking = null;
                if (isCoupleExchange)
                {
                    newPartnerSeat = await _context.Seats.FirstOrDefaultAsync(s =>
                        s.RoomId == newSeat.RoomId && s.CoupleGroupId == newSeat.CoupleGroupId && s.SeatId != newSeat.SeatId && s.IsActive);
                    oldPartnerBooking = await _context.Bookings.Include(b => b.Seat).Include(b => b.Tickets)
                        .FirstOrDefaultAsync(b => b.ShowTimeId == booking.ShowTimeId
                            && b.BookingDate == booking.BookingDate && b.UserId == booking.UserId
                            && b.Seat.CoupleGroupId == exchange.OldSeat.CoupleGroupId
                            && b.BookingId != booking.BookingId && b.Status != "Cancelled");
                    if (newPartnerSeat == null || oldPartnerBooking == null
                        || await _context.Bookings.AnyAsync(b => b.ShowTimeId == booking.ShowTimeId
                            && b.SeatId == newPartnerSeat.SeatId && b.Status != "Cancelled"))
                        return new SeatExchangeResponse { IsSuccess = false, Message = "Cặp ghế Couple mới không còn khả dụng." };
                }

                // 1. Tạo Payment mới với PaymentType = SEAT_EXCHANGE và PaymentMethod = CASH
                var payment = new Payment
                {
                    BookingId = booking.BookingId,
                    UserId = booking.UserId,
                    StaffId = staffId,
                    PaymentMethod = "CASH",
                    PaymentType = "SEAT_EXCHANGE",
                    SubTotal = exchange.AdditionalAmount,
                    DiscountAmt = 0,
                    TotalAmount = exchange.AdditionalAmount,
                    PaymentStatus = "Success",
                    PaidAt = DateTime.Now,
                    CreatedAt = DateTime.Now,
                    Notes = $"Thu tiền mặt chênh lệch đổi ghế từ {exchange.OldSeat.SeatRow}{exchange.OldSeat.SeatNumber} sang {newSeat.SeatRow}{newSeat.SeatNumber} (Mã vé: {ticket.TicketCode})"
                };

                _context.Payments.Add(payment);

                // 2. Cập nhật ghế cho Booking & Ticket theo logic thay thế giá vé
                decimal oldSeatPrice = await CalculateSeatPriceAsync(booking.ShowTime, exchange.OldSeat);
                decimal newSeatPrice = await CalculateSeatPriceAsync(booking.ShowTime, newSeat);

                booking.SeatId = newSeat.SeatId;
                booking.TicketPrice = newSeatPrice;
                booking.TotalAmount = booking.TotalAmount - oldSeatPrice + newSeatPrice;
                ticket.Price = newSeatPrice;
                if (oldPartnerBooking != null && newPartnerSeat != null)
                {
                    oldPartnerBooking.SeatId = newPartnerSeat.SeatId;
                    oldPartnerBooking.TicketPrice = newSeatPrice;
                    oldPartnerBooking.TotalAmount = oldPartnerBooking.TotalAmount - oldSeatPrice + newSeatPrice;
                    var partnerTicket = oldPartnerBooking.Tickets.FirstOrDefault();
                    if (partnerTicket != null) partnerTicket.Price = newSeatPrice;
                }

                // 3. Đánh dấu TicketExchange hoàn thành & Giải phóng SeatHold
                exchange.Status = "COMPLETED";
                _seatHoldService.ReleaseSeatBySeat(exchange.ShowTimeId, newSeat.SeatId);
                if (newPartnerSeat != null)
                    _seatHoldService.ReleaseSeatBySeat(exchange.ShowTimeId, newPartnerSeat.SeatId);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                var updatedTicket = MapToResponse(ticket);
                return new SeatExchangeResponse
                {
                    IsSuccess = true,
                    Message = $"Xác nhận thu {exchange.AdditionalAmount:N0}đ tiền mặt và chuyển sang ghế {newSeat.SeatRow}{newSeat.SeatNumber} thành công!",
                    RequiresPayment = false,
                    AdditionalAmount = 0,
                    ExchangeId = exchange.ExchangeId,
                    Ticket = updatedTicket
                };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return new SeatExchangeResponse
                {
                    IsSuccess = false,
                    Message = "Lỗi hệ thống khi xác nhận thanh toán đổi ghế: " + ex.Message
                };
            }
        }

        // Helper tính giá ghế từ bảng TICKETPRICING / BasePrice
        private async Task<decimal> CalculateSeatPriceAsync(Showtime showtime, Seat seat)
        {
            var roomType = showtime.Room?.RoomType;
            var seatType = seat.SeatType;
            var today = DateOnly.FromDateTime(showtime.StartTime.Date);

            var dayOfWeek = showtime.StartTime.DayOfWeek;
            var dayType = (dayOfWeek == DayOfWeek.Saturday || dayOfWeek == DayOfWeek.Sunday) ? "Weekend" : "Weekday";

            var matchingPricing = await _context.Ticketpricings
                .Where(p => p.IsActive
                    && p.EffectFrom <= today
                    && (p.EffectTo == null || p.EffectTo >= today)
                    && (p.RoomId == showtime.RoomId || p.RoomId == null)
                    && (p.RoomType == null || p.RoomType == roomType)
                    && (p.SeatType == null || p.SeatType == seatType)
                    && (p.DayType == null || p.DayType == dayType))
                .OrderByDescending(p => p.RoomId == showtime.RoomId)
                .ThenByDescending(p => p.SeatType != null)
                .ThenByDescending(p => p.RoomType != null)
                .FirstOrDefaultAsync();

            if (matchingPricing != null && matchingPricing.Price > 0)
            {
                return string.Equals(seat.SeatType, "Couple", StringComparison.OrdinalIgnoreCase)
                    ? matchingPricing.Price / 2m
                    : matchingPricing.Price;
            }

            decimal basePrice = showtime.BasePrice > 0 ? showtime.BasePrice : 75000m;
            string sType = (seat.SeatType ?? "").ToLower();
            if (sType.Contains("vip")) return basePrice + 15000m;
            if (sType.Contains("couple") || sType.Contains("sweetbox") || sType.Contains("đôi")) return basePrice * 2;
            return basePrice;
        }
    }
}
