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

        /// <summary>
        /// L?y ton b? danh sch v trong h? th?ng (Dnh cho Admin qu?n ly)
        /// </summary>
        public async Task<List<TicketResponse>> GetAllAsync()
        {
            return await _context.Tickets
                .Include(t => t.Booking).ThenInclude(b => b.User)
                .Include(t => t.Booking).ThenInclude(b => b.ShowTime).ThenInclude(s => s.Movie)
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
                    SeatCode = t.Booking.Seat != null ? (t.Booking.Seat.SeatRow + t.Booking.Seat.SeatNumber) : "N/A"
                }).ToListAsync();
        }

        /// <summary>
        /// Tm chi ti?t v d?a theo ID v
        /// </summary>
        public async Task<TicketResponse?> GetByIdAsync(int id)
        {
            return await _context.Tickets
                .Include(t => t.Booking).ThenInclude(b => b.User)
                .Include(t => t.Booking).ThenInclude(b => b.ShowTime).ThenInclude(s => s.Movie)
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
                    SeatCode = t.Booking.Seat != null ? (t.Booking.Seat.SeatRow + t.Booking.Seat.SeatNumber) : "N/A"
                }).FirstOrDefaultAsync();
        }

        /// <summary>
        /// D tm v theo ma Code (C?c k? quan tr?ng d? nhn vin qut QR Code t?i c?a r?p)
        /// </summary>
        public async Task<TicketResponse?> GetByCodeAsync(string ticketCode)
        {
            var cleanCode = ticketCode.Trim();
            return await _context.Tickets
                .Include(t => t.Booking).ThenInclude(b => b.User)
                .Include(t => t.Booking).ThenInclude(b => b.ShowTime).ThenInclude(s => s.Movie)
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
                    SeatCode = t.Booking.Seat != null ? (t.Booking.Seat.SeatRow + t.Booking.Seat.SeatNumber) : "N/A"
                }).FirstOrDefaultAsync();
        }

        /// <summary>
        /// L?y danh sch v thu?c v? m?t don d?t v c? th?
        /// </summary>
        public async Task<List<TicketResponse>> GetByBookingAsync(int bookingId)
        {
            return await _context.Tickets
                .Include(t => t.Booking).ThenInclude(b => b.User)
                .Include(t => t.Booking).ThenInclude(b => b.ShowTime).ThenInclude(s => s.Movie)
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
                    SeatCode = t.Booking.Seat != null ? (t.Booking.Seat.SeatRow + t.Booking.Seat.SeatNumber) : "N/A"
                }).ToListAsync();
        }

        /// <summary>
        /// T?o m?i m?t b?n ghi v xem phim th?c t?
        /// </summary>
        public async Task<TicketResponse> CreateAsync(TicketCreateRequest request)
        {
            // ?? 1. T? D?NG SINH TICKET CODE (Di 10 ky t?, khng bao gi? trng)
            // Guid sinh ra chu?i d?ng: 74b88612-4293-47e2... Ta c?t l?y 7 ky t? d?u ghp v?i ch? TIC
            string autoTicketCode = "TIC" + Guid.NewGuid().ToString().Replace("-", "").Substring(0, 7).ToUpper();

            // ?? 2. T? D?NG SINH QR CODE URL
            // S? d?ng API t?o ma QR cng c?ng mi?n ph (Ch? c?n truy?n data vo l n t? v? thnh ?nh QR)
            // Khi qut ci ?nh QR ny, my qut s? d?c ra dng chu?i autoTicketCode ? trn
            string autoQrCodeUrl = $"https://api.qrserver.com/v1/create-qr-code/?size=250x250&data={autoTicketCode}";

            var ticket = new Ticket
            {
                BookingId = request.BookingId,
                TicketCode = autoTicketCode, // N?p ma t? sinh xu?ng DB
                QrCodeUrl = autoQrCodeUrl,   // N?p link ?nh QR t? sinh xu?ng DB
                Price = request.Price,
                IssuedAt = DateTime.Now, // Ghi nh?n th?i gian xu?t v hi?n t?i
                Status = "Active"       // Tr?ng thi m?c d?nh ban d?u l ho?t d?ng
            };

            _context.Tickets.Add(ticket);
            await _context.SaveChangesAsync();

            // Fetch relations for DTO
            var created = await _context.Tickets
                .Include(t => t.Booking).ThenInclude(b => b.User)
                .Include(t => t.Booking).ThenInclude(b => b.ShowTime).ThenInclude(s => s.Movie)
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
                SeatCode = created?.Booking?.Seat != null ? (created.Booking.Seat.SeatRow + created.Booking.Seat.SeatNumber) : "N/A"
            };
        }

        /// <summary>
        /// C?p nh?t tr?ng thi v (S? d?ng khi sot v t?i c?a r?p: Active -> Used)
        /// </summary>
        public async Task<(bool IsSuccess, string Message, int StatusCode)> UpdateStatusAsync(int id, TicketStatusRequest request)
        {
            var ticket = await _context.Tickets.FindAsync(id);
            if (ticket == null)
                return (false, ValidationMessages.TicketNotFoundWithId(id), 404);

            var newStatus = request.Status.Trim();

            // Ch?t ch?n danh sch tr?ng: Ngan ch?n vi?c truy?n tr?ng thi b?y b? ph ho?i d? li?u
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