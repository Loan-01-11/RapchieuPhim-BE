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
        /// Lấy toàn bộ danh sách vé trong hệ thống (Dành cho Admin quản lý)
        /// </summary>
        public async Task<List<TicketResponse>> GetAllAsync()
        {
            return await _context.Tickets
                .Select(t => new TicketResponse
                {
                    TicketId = t.TicketId,
                    BookingId = t.BookingId,
                    TicketCode = t.TicketCode,
                    QrCodeUrl = t.QrCodeUrl,
                    Price = t.Price,
                    IssuedAt = t.IssuedAt,
                    Status = t.Status
                }).ToListAsync();
        }

        /// <summary>
        /// Tìm chi tiết vé dựa theo ID vé
        /// </summary>
        public async Task<TicketResponse?> GetByIdAsync(int id)
        {
            return await _context.Tickets
                .Where(t => t.TicketId == id)
                .Select(t => new TicketResponse
                {
                    TicketId = t.TicketId,
                    BookingId = t.BookingId,
                    TicketCode = t.TicketCode,
                    QrCodeUrl = t.QrCodeUrl,
                    Price = t.Price,
                    IssuedAt = t.IssuedAt,
                    Status = t.Status
                }).FirstOrDefaultAsync();
        }

        /// <summary>
        /// Dò tìm vé theo mã Code (Cực kỳ quan trọng để nhân viên quét QR Code tại cửa rạp)
        /// </summary>
        public async Task<TicketResponse?> GetByCodeAsync(string ticketCode)
        {
            var cleanCode = ticketCode.Trim();
            return await _context.Tickets
                .Where(t => t.TicketCode == cleanCode)
                .Select(t => new TicketResponse
                {
                    TicketId = t.TicketId,
                    BookingId = t.BookingId,
                    TicketCode = t.TicketCode,
                    QrCodeUrl = t.QrCodeUrl,
                    Price = t.Price,
                    IssuedAt = t.IssuedAt,
                    Status = t.Status
                }).FirstOrDefaultAsync();
        }

        /// <summary>
        /// Lấy danh sách vé thuộc về một đơn đặt vé cụ thể
        /// </summary>
        public async Task<List<TicketResponse>> GetByBookingAsync(int bookingId)
        {
            return await _context.Tickets
                .Where(t => t.BookingId == bookingId)
                .Select(t => new TicketResponse
                {
                    TicketId = t.TicketId,
                    BookingId = t.BookingId,
                    TicketCode = t.TicketCode,
                    QrCodeUrl = t.QrCodeUrl,
                    Price = t.Price,
                    IssuedAt = t.IssuedAt,
                    Status = t.Status
                }).ToListAsync();
        }

        /// <summary>
        /// Tạo mới một bản ghi vé xem phim thực tế
        /// </summary>
        public async Task<TicketResponse> CreateAsync(TicketCreateRequest request)
        {
            // 🌟 1. TỰ ĐỘNG SINH TICKET CODE (Dài 10 ký tự, không bao giờ trùng)
            // Guid sinh ra chuỗi dạng: 74b88612-4293-47e2... Ta cắt lấy 7 ký tự đầu ghép với chữ TIC
            string autoTicketCode = "TIC" + Guid.NewGuid().ToString().Replace("-", "").Substring(0, 7).ToUpper();

            // 🌟 2. TỰ ĐỘNG SINH QR CODE URL
            // Sử dụng API tạo mã QR công cộng miễn phí (Chỉ cần truyền data vào là nó tự vẽ thành ảnh QR)
            // Khi quét cái ảnh QR này, máy quét sẽ đọc ra đúng chuỗi autoTicketCode ở trên
            string autoQrCodeUrl = $"https://api.qrserver.com/v1/create-qr-code/?size=250x250&data={autoTicketCode}";

            var ticket = new Ticket
            {
                BookingId = request.BookingId,
                TicketCode = autoTicketCode, // Nạp mã tự sinh xuống DB
                QrCodeUrl = autoQrCodeUrl,   // Nạp link ảnh QR tự sinh xuống DB
                Price = request.Price,
                IssuedAt = DateTime.Now, // Ghi nhận thời gian xuất vé hiện tại
                Status = "Active"       // Trạng thái mặc định ban đầu là hoạt động
            };

            _context.Tickets.Add(ticket);
            await _context.SaveChangesAsync();

            return new TicketResponse
            {
                TicketId = ticket.TicketId,
                BookingId = ticket.BookingId,
                TicketCode = ticket.TicketCode,
                QrCodeUrl = ticket.QrCodeUrl,
                Price = ticket.Price,
                IssuedAt = ticket.IssuedAt,
                Status = ticket.Status
            };
        }

        /// <summary>
        /// Cập nhật trạng thái vé (Sử dụng khi soát vé tại cửa rạp: Active -> Used)
        /// </summary>
        public async Task<(bool IsSuccess, string Message, int StatusCode)> UpdateStatusAsync(int id, TicketStatusRequest request)
        {
            var ticket = await _context.Tickets.FindAsync(id);
            if (ticket == null)
                return (false, ValidationMessages.TicketNotFoundWithId(id), 404);

            var newStatus = request.Status.Trim();

            // Chốt chặn danh sách trắng: Ngăn chặn việc truyền trạng thái bậy bạ phá hoại dữ liệu
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