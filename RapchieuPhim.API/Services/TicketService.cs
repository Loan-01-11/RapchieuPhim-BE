


using Microsoft.EntityFrameworkCore;
using RapchieuPhim.API.Constants;
using RapchieuPhim.API.DTOs.DTORequest;
using RapchieuPhim.API.DTOs.DTOResponse;
using RapchieuPhim.API.Models;

namespace RapchieuPhim.API.Services
{
    public interface ITicketService
    {
        Task<List<TicketResponse>> GetAllAsync();
        Task<TicketResponse?> GetByIdAsync(int id);
        Task<TicketResponse?> GetByCodeAsync(string ticketCode);
        Task<List<TicketResponse>> GetByBookingAsync(int bookingId);
        Task<TicketResponse> CreateAsync(TicketCreateRequest request);
        Task<(bool IsSuccess, string Message, int StatusCode, TicketResponse? Ticket)> UpdateStatusAsync(int id, TicketStatusRequest request);
        Task<(bool IsSuccess, string Message, int StatusCode, TicketResponse? Ticket)> ScanTicketAsync(string ticketCode);
    }


    public class TicketService : ITicketService
    {
        private readonly CinemaManagementContext _context;

        public TicketService(CinemaManagementContext context)
        {
            _context = context;
        }

        public async Task<List<TicketResponse>> GetAllAsync()
        {
            var list = await _context.Tickets
                .Include(t => t.Booking).ThenInclude(b => b.User)
                .Include(t => t.Booking).ThenInclude(b => b.ShowTime).ThenInclude(s => s.Movie)
                .Include(t => t.Booking).ThenInclude(b => b.ShowTime).ThenInclude(s => s.Room).ThenInclude(r => r.Cinema).ThenInclude(c => c.Area)
                .Include(t => t.Booking).ThenInclude(b => b.Seat)
                .Include(t => t.Booking).ThenInclude(b => b.Orders).ThenInclude(o => o.Orderitems).ThenInclude(oi => oi.Food)
                .Include(t => t.Booking).ThenInclude(b => b.Orders).ThenInclude(o => o.Orderitems).ThenInclude(oi => oi.Combo)
                .ToListAsync();
            return list.Select(MapToResponse).ToList();
        }

        public async Task<TicketResponse?> GetByIdAsync(int id)
        {
            var ticket = await _context.Tickets
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
                CinemaName = created?.Booking?.ShowTime?.Room?.Cinema?.CinemaName ?? "N/A",
                RoomName = created?.Booking?.ShowTime?.Room?.RoomName ?? "N/A"
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
        public async Task<(bool IsSuccess, string Message, int StatusCode, TicketResponse? Ticket)> ScanTicketAsync(string ticketCode)
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
            var response = new TicketResponse
            {
                TicketId = ticket.TicketId,
                BookingId = ticket.BookingId,
                TicketCode = ticket.TicketCode,
                QrCodeUrl = ticket.QrCodeUrl,
                Price = ticket.Price,
                IssuedAt = ticket.IssuedAt,
                Status = ticket.Status,
                CustomerName = booking?.User?.FullName ?? "Khách vãng lai",
                MovieTitle = showtime?.Movie?.Title ?? "N/A",
                SeatCode = booking?.Seat != null ? (booking.Seat.SeatRow + booking.Seat.SeatNumber) : "N/A",
                AreaName = showtime?.Room?.Cinema?.Area?.AreaName ?? "N/A",
                CinemaName = showtime?.Room?.Cinema?.CinemaName ?? "N/A",
                RoomName = showtime?.Room?.RoomName ?? "N/A"
            };

            if (booking?.Orders != null)
            {
                foreach (var order in booking.Orders)
                {
                    if (order.Orderitems != null)
                    {
                        foreach (var oi in order.Orderitems)
                        {
                            var name = oi.Food?.FoodName ?? oi.Combo?.ComboName ?? "Đồ ăn kèm";
                            response.Foods.Add(new BookingFoodDetailResponse
                            {
                                Name = name,
                                Quantity = oi.Quantity,
                                Price = oi.UnitPrice
                            });
                        }
                    }
                }
            }

            return response;
        }
    }
}