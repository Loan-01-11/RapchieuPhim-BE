using Microsoft.EntityFrameworkCore;
using RapchieuPhim.API.Constants;
using RapchieuPhim.API.DTOs.DTORequest;
using RapchieuPhim.API.DTOs.DTOResponse;
using RapchieuPhim.API.Models;

namespace RapchieuPhim.API.Services
{
    // ─────────────────────────────────────────────────────────────────────────────
    // INTERFACE: ĐỊNH NGHĨA CÁC PHƯƠNG THỨC MÀ PAYMENT SERVICE PHẢI CÓ
    // ─────────────────────────────────────────────────────────────────────────────
    public interface IPaymentService
    {
        Task<List<PaymentResponse>> GetAllAsync();
        Task<PaymentResponse?> GetByIdAsync(int id);
        Task<List<PaymentResponse>> GetByUserAsync(int userId, int currentUserId, string currentRole);
        Task<List<PaymentResponse>> GetByBookingAsync(int bookingId);
        Task<(bool IsSuccess, string Message, int StatusCode, PaymentResponse? Data)> CreateAsync(PaymentRequest request, int currentUserId, string currentRole);
        Task<(bool IsSuccess, string Message, int StatusCode)> UpdateStatusAsync(int id, PaymentStatusRequest request, string currentRole);

        // Webhook đối soát tự động từ Sepay
        Task<(bool IsSuccess, string Message)> ProcessSepayWebhookAsync(SepayWebhookRequest request, string authorizationHeader);
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // LỚP XỬ LÝ NGHIỆP VỤ THANH TOÁN (PAYMENT SERVICE)
    // ─────────────────────────────────────────────────────────────────────────────
    public class PaymentService : IPaymentService
    {
        private readonly CinemaManagementContext _context;

        public PaymentService(CinemaManagementContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Hàm phụ trợ: Chuyển đổi từ Model Entity sang Response DTO.
        /// Tự động sinh link ảnh QR VietQR và thông tin tài khoản ngân hàng.
        /// </summary>
        private static PaymentResponse MapToResponse(Payment p) => new()
        {
            PaymentId     = p.PaymentId,
            BookingId     = p.BookingId,
            OrderId       = p.OrderId,
            UserId        = p.UserId,
            StaffId       = p.StaffId,
            PaymentMethod = p.PaymentMethod,
            SubTotal      = p.SubTotal,
            DiscountAmt   = p.DiscountAmt,
            TotalAmount   = p.TotalAmount,
            TransactionId = p.TransactionId,
            CreatedAt     = p.CreatedAt,
            PaidAt        = p.PaidAt,
            PaymentStatus = p.PaymentStatus,
            Notes         = p.Notes,

            // QR VietQR ngân hàng (chứa tổng tiền toàn giao dịch, dùng để chuyển khoản)
            QrCodeUrl     = (p.PaymentMethod == PaymentMessages.MethodQrCode) && p.BookingId.HasValue
                            ? PaymentMessages.GenerateVietQrUrl(p.TotalAmount, p.BookingId.Value)
                            : null,

            BankId        = (p.PaymentMethod == PaymentMessages.MethodQrCode) ? PaymentMessages.BankId : null,
            AccountNo     = (p.PaymentMethod == PaymentMessages.MethodQrCode) ? PaymentMessages.AccountNo : null,
            AccountName   = (p.PaymentMethod == PaymentMessages.MethodQrCode) ? PaymentMessages.AccountName : null,

            // Nội dung chuyển khoản (Ví dụ: "THANH TOAN VE CP DAT VE 10")
            // BookingId ở đây là booking đại diện đầu tiên của cả nhóm ghế
            PaymentDescription = (p.PaymentMethod == PaymentMessages.MethodQrCode) && p.BookingId.HasValue
                                 ? $"THANH TOAN VE CP DAT VE {p.BookingId.Value}"
                                 : null
        };

        public async Task<List<PaymentResponse>> GetAllAsync()
        {
            var list = await _context.Payments
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
            return list.Select(MapToResponse).ToList();
        }

        public async Task<PaymentResponse?> GetByIdAsync(int id)
        {
            var p = await _context.Payments.FindAsync(id);
            return p == null ? null : MapToResponse(p);
        }

        public async Task<List<PaymentResponse>> GetByUserAsync(int userId, int currentUserId, string currentRole)
        {
            if (currentRole == RoleConstants.Customer && userId != currentUserId)
                return new List<PaymentResponse>();

            var list = await _context.Payments
                .Where(p => p.UserId == userId)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
            return list.Select(MapToResponse).ToList();
        }

        public async Task<List<PaymentResponse>> GetByBookingAsync(int bookingId)
        {
            var list = await _context.Payments
                .Where(p => p.BookingId == bookingId)
                .ToListAsync();
            return list.Select(MapToResponse).ToList();
        }

        /// <summary>
        /// TẠO MỚI GIAO DỊCH THANH TOÁN
        ///
        /// Logic gom nhóm (Batch):
        ///   - Khi khách đặt nhiều ghế cùng lúc, các Booking có cùng UserId + ShowTimeId + BookingDate.
        ///   - Hệ thống tự động cộng dồn tiền của TẤT CẢ các ghế đó vào 1 giao dịch thanh toán duy nhất.
        ///   - Chỉ cần 1 mã QR VietQR để khách chuyển khoản toàn bộ số tiền.
        /// </summary>
        public async Task<(bool IsSuccess, string Message, int StatusCode, PaymentResponse? Data)> CreateAsync(
            PaymentRequest request, int currentUserId, string currentRole)
        {
            // Bước 1: Kiểm tra Booking tồn tại
            var booking = await _context.Bookings.FindAsync(request.BookingId);
            if (booking == null)
                return (false, PaymentMessages.BookingNotFoundForPayment(request.BookingId), 404, null);

            // Bước 2: Bảo mật - Khách hàng không được thanh toán đơn vé của người khác
            if (currentRole == RoleConstants.Customer && booking.UserId != currentUserId)
                return (false, PaymentMessages.UnauthorizedPayment, 403, null);

            // Bước 3: Chặn thanh toán trùng
            bool alreadyPaid = await _context.Payments.AnyAsync(p =>
                p.BookingId == request.BookingId &&
                p.PaymentStatus == PaymentMessages.StatusSuccess);
            if (alreadyPaid)
                return (false, PaymentMessages.AlreadyPaid, 409, null);

            // ─────────────────────────────────────────────────────────────────────
            // 🌟 LOGIC GOM NHÓM: Tìm toàn bộ các ghế được đặt cùng lúc
            //    (Cùng UserId + ShowTimeId + BookingDate chính xác đến millisecond)
            //    → Cộng dồn tiền của TẤT CẢ ghế trong cùng đợt đặt vào 1 thanh toán
            // ─────────────────────────────────────────────────────────────────────
            var batchBookings = await _context.Bookings
                .Where(b => b.UserId     == booking.UserId
                         && b.ShowTimeId == booking.ShowTimeId
                         && b.BookingDate == booking.BookingDate)
                .ToListAsync();

            // Bước 4: Cộng dồn tiền của toàn bộ ghế trong nhóm
            decimal subTotal    = batchBookings.Sum(b => b.TicketPrice);
            decimal discountAmt = batchBookings.Sum(b => b.DiscountAmt);
            decimal total       = batchBookings.Sum(b => b.TotalAmount);

            // Bước 5: Cộng thêm tiền đồ ăn nếu có
            //Kiểm tra xem khách hàng có gửi kèm mã hóa đơn đồ ăn (OrderId) lên cùng đợt thanh toán này hay không. Nếu có, hệ thống mới nhảy vào xử lý khối lệnh bên trong.
            if (request.OrderId.HasValue)
            {
                var order = await _context.Orders.FindAsync(request.OrderId.Value);
                if (order == null)
                    return (false, PaymentMessages.OrderNotFoundForPayment(request.OrderId.Value), 404, null);

                var batchBookingIds = batchBookings.Select(b => b.BookingId).ToList();
                if (!order.BookingId.HasValue || !batchBookingIds.Contains(order.BookingId.Value))
                    return (false, PaymentMessages.OrderBookingMismatch, 400, null);

                subTotal += order.TotalAmount;
                total    += order.TotalAmount;
            }

            // Bước 6: Xác định người thực hiện giao dịch
            int? staffId = null;
            if (currentRole == RoleConstants.Admin || currentRole == RoleConstants.Staff)
                staffId = currentUserId;

            // Bước 7: Trạng thái ban đầu theo phương thức thanh toán
            string initialStatus = request.PaymentMethod == PaymentMessages.MethodCash
                ? PaymentMessages.StatusSuccess
                : PaymentMessages.StatusPending;

            // Bước 8: Lưu giao dịch vào Database
            // BookingId = booking đại diện đầu tiên của cả nhóm (dùng làm mã QR đại diện)
            var payment = new Payment
            {
                BookingId     = request.BookingId,   // Booking đại diện (ghế đầu tiên)
                OrderId       = request.OrderId,
                UserId        = booking.UserId,
                StaffId       = staffId,
                PaymentMethod = request.PaymentMethod.Trim(),
                SubTotal      = subTotal,            // Tổng tiền của TẤT CẢ ghế (+ đồ ăn)
                DiscountAmt   = discountAmt,
                TotalAmount   = total,               // Số tiền thực tế khách phải chuyển khoản
                TransactionId = request.TransactionId?.Trim(),
                CreatedAt     = DateTime.Now,
                PaidAt        = initialStatus == PaymentMessages.StatusSuccess ? DateTime.Now : null,
                PaymentStatus = initialStatus,
                Notes         = batchBookings.Count > 1
                                ? $"[Gom nhóm] {batchBookings.Count} ghế | " + (request.Notes?.Trim() ?? "")
                                : request.Notes?.Trim()
            };

            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();

            return (true, PaymentMessages.CreateSuccess, 201, MapToResponse(payment));  
        }

        /// <summary>
        /// CẬP NHẬT TRẠNG THÁI THANH TOÁN (Admin / Staff duyệt thủ công)
        /// </summary>
        public async Task<(bool IsSuccess, string Message, int StatusCode)> UpdateStatusAsync(
            int id, PaymentStatusRequest request, string currentRole)
        {
            if (currentRole != RoleConstants.Admin && currentRole != RoleConstants.Staff)
                return (false, PaymentMessages.UnauthorizedStatusChange, 403);

            var payment = await _context.Payments.FindAsync(id);
            if (payment == null)
                return (false, PaymentMessages.NotFoundWithId(id), 404);

            if (payment.PaymentStatus == PaymentMessages.StatusRefunded)
                return (false, PaymentMessages.CannotUpdateRefunded, 409);

            var validStatuses = new[] { PaymentMessages.StatusPending, PaymentMessages.StatusSuccess,
                                        PaymentMessages.StatusFailed, PaymentMessages.StatusRefunded };
            if (!validStatuses.Contains(request.Status))
                return (false, PaymentMessages.InvalidStatus, 400);

            payment.PaymentStatus = request.Status;
            payment.Notes         = request.Notes?.Trim() ?? payment.Notes;

            if (request.Status == PaymentMessages.StatusSuccess)
                payment.PaidAt = DateTime.Now;

            await _context.SaveChangesAsync();
            return (true, PaymentMessages.UpdateStatusSuccess, 200);
        }

        // ─────────────────────────────────────────────────────────────────────────
        // WEBHOOK ĐỐI SOÁT TỰ ĐỘNG TỪ SEPAY
        //
        // Khi Sepay phát hiện tài khoản nhận tiền → gọi vào đây.
        // Hệ thống sẽ:
        //   1. Xác thực API Key
        //   2. Trích xuất BookingId đại diện từ nội dung chuyển khoản
        //   3. Kiểm tra số tiền
        //   4. Cập nhật Payment → Success
        //   5. Gom nhóm: tìm toàn bộ các Booking cùng đợt
        //   6. Kích hoạt TẤT CẢ Ticket của nhóm → Active + sinh QR vé
        // ─────────────────────────────────────────────────────────────────────────
        public async Task<(bool IsSuccess, string Message)> ProcessSepayWebhookAsync(
            SepayWebhookRequest request, string authorizationHeader)
        {
            // Bước 1: Xác thực API Key
            string expectedHeader = $"Apikey {PaymentMessages.SepayApiKey}";
            if (string.IsNullOrEmpty(authorizationHeader) || authorizationHeader != expectedHeader)
                return (false, "Xác thực Webhook Sepay thất bại. API Key không hợp lệ.");

            // Bước 2: Trích xuất BookingId đại diện từ nội dung chuyển khoản
            // Ví dụ: "THANH TOAN VE CP DAT VE 10" → lấy ra số 10
            var match = System.Text.RegularExpressions.Regex.Match(
                request.TransactionContent ?? "",
                @"(?i)(DAT\s*VE|BOOKING)\s*(\d+)"
            );

            int bookingId = 0;
            if (match.Success)
                int.TryParse(match.Groups[2].Value, out bookingId);
            else
            {
                var digits = System.Text.RegularExpressions.Regex.Matches(
                    request.TransactionContent ?? "", @"\d+");
                if (digits.Count > 0)
                    int.TryParse(digits[^1].Value, out bookingId);
            }

            if (bookingId == 0)
                return (false, $"Không thể trích xuất BookingId từ nội dung: '{request.TransactionContent}'");

            // Bước 3: Tìm giao dịch thanh toán đang Pending
            var payment = await _context.Payments
                .FirstOrDefaultAsync(p => p.BookingId == bookingId
                                       && p.PaymentStatus == PaymentMessages.StatusPending);
            if (payment == null)
                return (false, $"Không tìm thấy giao dịch Pending nào cho BookingId = {bookingId}");

            // Bước 4: Kiểm tra số tiền chuyển khoản ≥ số tiền đơn hàng
            if (request.AmountIn < payment.TotalAmount)
                return (false, $"Số tiền chuyển khoản ({request.AmountIn:N0}) < số tiền đơn hàng ({payment.TotalAmount:N0})");

            // Bước 5: Cập nhật Payment → Success
            payment.PaymentStatus = PaymentMessages.StatusSuccess;
            payment.PaidAt        = DateTime.Now;
            payment.TransactionId = request.ReferenceNumber;
            payment.Notes         = $"[Sepay Auto] Đối soát thành công lúc {DateTime.Now:dd/MM/yyyy HH:mm:ss}. Gateway: {request.Gateway}";

            // ─────────────────────────────────────────────────────────────────────
            // 🌟 BƯỚC 6: GOM NHÓM — Tìm toàn bộ Booking cùng đợt đặt vé
            //    (Cùng UserId + ShowTimeId + BookingDate chính xác)
            //    → Kích hoạt TẤT CẢ vé của nhóm (không chỉ booking đại diện)
            // ─────────────────────────────────────────────────────────────────────
            var rootBooking = await _context.Bookings.FindAsync(bookingId);

            var allBatchBookingIds = new List<int> { bookingId };

            if (rootBooking != null)
            {
                var siblingIds = await _context.Bookings
                    .Where(b => b.UserId     == rootBooking.UserId
                             && b.ShowTimeId == rootBooking.ShowTimeId
                             && b.BookingDate == rootBooking.BookingDate
                             && b.BookingId  != bookingId)
                    .Select(b => b.BookingId)
                    .ToListAsync();

                allBatchBookingIds.AddRange(siblingIds);
            }

            // Bước 7: Kích hoạt toàn bộ vé (Pending → Active) và sinh QR vé thật
            var tickets = await _context.Tickets
                .Where(t => allBatchBookingIds.Contains(t.BookingId) && t.Status == "Pending")
                .ToListAsync();

            foreach (var ticket in tickets)
            {
                ticket.Status    = ShowtimeMessages.StatusActive;
                ticket.QrCodeUrl = $"https://api.qrserver.com/v1/create-qr-code/?size=250x250&data={ticket.TicketCode}";
            }

            await _context.SaveChangesAsync();

            return (true, $"Đối soát Sepay thành công. " +
                          $"Đã kích hoạt {tickets.Count} vé cho {allBatchBookingIds.Count} ghế.");
        }
    }
}
