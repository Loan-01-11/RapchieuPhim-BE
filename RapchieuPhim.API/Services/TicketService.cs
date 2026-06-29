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
        Task<(bool IsSuccess, string Message, int StatusCode)> UpdateStatusAsync(int id, TicketStatusRequest request);
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
            return await _context.Tickets
                .Include(t => t.Booking).ThenInclude(b => b.User)
                .Include(t => t.Booking).ThenInclude(b => b.ShowTime).ThenInclude(s => s.Movie)
                .Include(t => t.Booking).ThenInclude(b => b.ShowTime).ThenInclude(s => s.Room).ThenInclude(r => r.Cinema).ThenInclude(c => c.Area)
                .Include(t => t.Booking).ThenInclude(b => b.Seat)
                .Select(t => new TicketResponse
                {
                    TicketId = t.TicketId,
                    BookingId = t.BookingId,
                    TicketCode = t.TicketCode,
                    QrCodeUrl = t.QrCodeUrl,
                    Price = t.Price,
                    IssuedAt = t.IssuedAt,
                    Status = t.Status,
                    CustomerName = t.Booking.User != null ? t.Booking.User.FullName : "Khách vãng lai",
                    MovieTitle = t.Booking.ShowTime.Movie != null ? t.Booking.ShowTime.Movie.Title : "N/A",
                    SeatCode = t.Booking.Seat != null ? (t.Booking.Seat.SeatRow + t.Booking.Seat.SeatNumber) : "N/A",
                    AreaName = t.Booking.ShowTime.Room != null && t.Booking.ShowTime.Room.Cinema != null && t.Booking.ShowTime.Room.Cinema.Area != null ? t.Booking.ShowTime.Room.Cinema.Area.AreaName : "N/A",
                    CinemaName = t.Booking.ShowTime.Room != null && t.Booking.ShowTime.Room.Cinema != null ? t.Booking.ShowTime.Room.Cinema.CinemaName : "N/A",
                    RoomName = t.Booking.ShowTime.Room != null ? t.Booking.ShowTime.Room.RoomName : "N/A"
                }).ToListAsync();
        }

        public async Task<TicketResponse?> GetByIdAsync(int id)
        {
            return await _context.Tickets
                .Include(t => t.Booking).ThenInclude(b => b.User)
                .Include(t => t.Booking).ThenInclude(b => b.ShowTime).ThenInclude(s => s.Movie)
                .Include(t => t.Booking).ThenInclude(b => b.ShowTime).ThenInclude(s => s.Room).ThenInclude(r => r.Cinema).ThenInclude(c => c.Area)
                .Include(t => t.Booking).ThenInclude(b => b.Seat)
                .Where(t => t.TicketId == id)
                .Select(t => new TicketResponse
                {
                    TicketId = t.TicketId,
                    BookingId = t.BookingId,
                    TicketCode = t.TicketCode,
                    QrCodeUrl = t.QrCodeUrl,
                    Price = t.Price,
                    IssuedAt = t.IssuedAt,
                    Status = t.Status,
                    CustomerName = t.Booking.User != null ? t.Booking.User.FullName : "Khách vãng lai",
                    MovieTitle = t.Booking.ShowTime.Movie != null ? t.Booking.ShowTime.Movie.Title : "N/A",
                    SeatCode = t.Booking.Seat != null ? (t.Booking.Seat.SeatRow + t.Booking.Seat.SeatNumber) : "N/A",
                    AreaName = t.Booking.ShowTime.Room != null && t.Booking.ShowTime.Room.Cinema != null && t.Booking.ShowTime.Room.Cinema.Area != null ? t.Booking.ShowTime.Room.Cinema.Area.AreaName : "N/A",
                    CinemaName = t.Booking.ShowTime.Room != null && t.Booking.ShowTime.Room.Cinema != null ? t.Booking.ShowTime.Room.Cinema.CinemaName : "N/A",
                    RoomName = t.Booking.ShowTime.Room != null ? t.Booking.ShowTime.Room.RoomName : "N/A"
                }).FirstOrDefaultAsync();
        }

        public async Task<TicketResponse?> GetByCodeAsync(string ticketCode)
        {
            var cleanCode = ticketCode.Trim();
            return await _context.Tickets
                .Include(t => t.Booking).ThenInclude(b => b.User)
                .Include(t => t.Booking).ThenInclude(b => b.ShowTime).ThenInclude(s => s.Movie)
                .Include(t => t.Booking).ThenInclude(b => b.ShowTime).ThenInclude(s => s.Room).ThenInclude(r => r.Cinema).ThenInclude(c => c.Area)
                .Include(t => t.Booking).ThenInclude(b => b.Seat)
                .Where(t => t.TicketCode == cleanCode)
                .Select(t => new TicketResponse
                {
                    TicketId = t.TicketId,
                    BookingId = t.BookingId,
                    TicketCode = t.TicketCode,
                    QrCodeUrl = t.QrCodeUrl,
                    Price = t.Price,
                    IssuedAt = t.IssuedAt,
                    Status = t.Status,
                    CustomerName = t.Booking.User != null ? t.Booking.User.FullName : "Khách vãng lai",
                    MovieTitle = t.Booking.ShowTime.Movie != null ? t.Booking.ShowTime.Movie.Title : "N/A",
                    SeatCode = t.Booking.Seat != null ? (t.Booking.Seat.SeatRow + t.Booking.Seat.SeatNumber) : "N/A",
                    AreaName = t.Booking.ShowTime.Room != null && t.Booking.ShowTime.Room.Cinema != null && t.Booking.ShowTime.Room.Cinema.Area != null ? t.Booking.ShowTime.Room.Cinema.Area.AreaName : "N/A",
                    CinemaName = t.Booking.ShowTime.Room != null && t.Booking.ShowTime.Room.Cinema != null ? t.Booking.ShowTime.Room.Cinema.CinemaName : "N/A",
                    RoomName = t.Booking.ShowTime.Room != null ? t.Booking.ShowTime.Room.RoomName : "N/A"
                }).FirstOrDefaultAsync();
        }

        public async Task<List<TicketResponse>> GetByBookingAsync(int bookingId)
        {
            return await _context.Tickets
                .Include(t => t.Booking).ThenInclude(b => b.User)
                .Include(t => t.Booking).ThenInclude(b => b.ShowTime).ThenInclude(s => s.Movie)
                .Include(t => t.Booking).ThenInclude(b => b.ShowTime).ThenInclude(s => s.Room).ThenInclude(r => r.Cinema).ThenInclude(c => c.Area)
                .Include(t => t.Booking).ThenInclude(b => b.Seat)
                .Where(t => t.BookingId == bookingId)
                .Select(t => new TicketResponse
                {
                    TicketId = t.TicketId,
                    BookingId = t.BookingId,
                    TicketCode = t.TicketCode,
                    QrCodeUrl = t.QrCodeUrl,
                    Price = t.Price,
                    IssuedAt = t.IssuedAt,
                    Status = t.Status,
                    CustomerName = t.Booking.User != null ? t.Booking.User.FullName : "Khách vãng lai",
                    MovieTitle = t.Booking.ShowTime.Movie != null ? t.Booking.ShowTime.Movie.Title : "N/A",
                    SeatCode = t.Booking.Seat != null ? (t.Booking.Seat.SeatRow + t.Booking.Seat.SeatNumber) : "N/A",
                    AreaName = t.Booking.ShowTime.Room != null && t.Booking.ShowTime.Room.Cinema != null && t.Booking.ShowTime.Room.Cinema.Area != null ? t.Booking.ShowTime.Room.Cinema.Area.AreaName : "N/A",
                    CinemaName = t.Booking.ShowTime.Room != null && t.Booking.ShowTime.Room.Cinema != null ? t.Booking.ShowTime.Room.Cinema.CinemaName : "N/A",
                    RoomName = t.Booking.ShowTime.Room != null ? t.Booking.ShowTime.Room.RoomName : "N/A"
                }).ToListAsync();
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
                Status = "Active"
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

        public async Task<(bool IsSuccess, string Message, int StatusCode)> UpdateStatusAsync(int id, TicketStatusRequest request)
        {
            var ticket = await _context.Tickets.FindAsync(id);
            if (ticket == null)
                return (false, ValidationMessages.TicketNotFoundWithId(id), 404);

            var newStatus = request.Status.Trim();

            var validStatuses = new[] { "Active", "Used", "Cancelled" };
            if (!validStatuses.Contains(newStatus))
                return (false, ValidationMessages.TicketStatusInvalid, 400);

            ticket.Status = newStatus;

            try
            {
                await _context.SaveChangesAsync();
                return (true, ValidationMessages.TicketUpdateStatusSuccess, 200);
            }
            catch (DbUpdateConcurrencyException)
            {
                return (false, ValidationMessages.TicketConcurrencyError, 409);
            }
        }
    }
}