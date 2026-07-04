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
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // LỚP XỬ LÝ NGHIỆP VỤ THANH TOÁN (PAYMENT SERVICE)
    // ─────────────────────────────────────────────────────────────────────────────
    public class PaymentService : IPaymentService
    {
        private readonly CinemaManagementContext _context;

        // Inject DbContext để kết nối và thao tác với Database
        public PaymentService(CinemaManagementContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Hàm phụ trợ (Helper): Chuyển đổi từ Model Entity (Database) sang Response DTO (Client nhận được)
        /// Nhằm che giấu các thông tin nhạy cảm của Database và định dạng lại thông tin QR ngân hàng.
        /// </summary>
        private static PaymentResponse MapToResponse(Payment p) => new()
        {
            PaymentId     = p.PaymentId,
            BookingId     = p.BookingId,
            OrderId       = p.OrderId,
            UserId        = p.UserId,
            StaffId       = p.StaffId,
            PaymentMethod = p.PaymentMethod,
            SubTotal      = p.SubTotal,     // Tổng số tiền trước khi giảm giá
            DiscountAmt   = p.DiscountAmt,  // Số tiền được giảm giá
            TotalAmount   = p.TotalAmount,  // Số tiền thực tế khách hàng phải trả
            TransactionId = p.TransactionId,
            CreatedAt     = p.CreatedAt,
            PaidAt        = p.PaidAt,
            PaymentStatus = p.PaymentStatus,
            Notes         = p.Notes,

            // 1. Tự động sinh link ảnh QR VietQR nếu phương thức là "QrCode" và có liên kết vé (BookingId)
            QrCodeUrl     = (p.PaymentMethod == PaymentMessages.MethodQrCode) && p.BookingId.HasValue
                            ? PaymentMessages.GenerateVietQrUrl(p.TotalAmount, p.BookingId.Value)
                            : null,

            // 2. Trả ra thông tin Tên Ngân Hàng để Frontend hiển thị dạng chữ
            BankId        = (p.PaymentMethod == PaymentMessages.MethodQrCode) ? PaymentMessages.BankId : null,

            // 3. Trả ra số tài khoản ngân hàng để Frontend hiển thị cho khách sao chép
            AccountNo     = (p.PaymentMethod == PaymentMessages.MethodQrCode) ? PaymentMessages.AccountNo : null,

            // 4. Trả ra tên chủ tài khoản ngân hàng nhận tiền
            AccountName   = (p.PaymentMethod == PaymentMessages.MethodQrCode) ? PaymentMessages.AccountName : null,

            // 5. Trả ra nội dung chuyển khoản tự động (Ví dụ: "THANH TOAN VE CP DAT VE 105")
            PaymentDescription = (p.PaymentMethod == PaymentMessages.MethodQrCode) && p.BookingId.HasValue
                                 ? $"THANH TOAN VE CP DAT VE {p.BookingId.Value}"
                                 : null
        };

        /// <summary>
        /// Lấy toàn bộ danh sách giao dịch thanh toán trong hệ thống.
        /// Thường dành cho trang quản trị của Admin / Staff.
        /// </summary>
        public async Task<List<PaymentResponse>> GetAllAsync()
        {
            var list = await _context.Payments
                .OrderByDescending(p => p.CreatedAt) // Sắp xếp giao dịch mới nhất lên đầu
                .ToListAsync();

            return list.Select(MapToResponse).ToList(); // Chuyển đổi cả danh sách sang DTO
        }

        /// <summary>
        /// Lấy chi tiết một giao dịch thanh toán cụ thể qua ID.
        /// </summary>
        public async Task<PaymentResponse?> GetByIdAsync(int id)
        {
            var p = await _context.Payments.FindAsync(id);
            return p == null ? null : MapToResponse(p);
        }

        /// <summary>
        /// Lấy lịch sử giao dịch thanh toán của riêng một người dùng.
        /// Bảo mật: Khách hàng thường chỉ xem được của chính họ. Admin/Staff được xem của tất cả.
        /// </summary>
        public async Task<List<PaymentResponse>> GetByUserAsync(int userId, int currentUserId, string currentRole)
        {
            // Kiểm tra phân quyền: Nếu là khách thường mà muốn xem giao dịch của người khác -> chặn lại
            if (currentRole == RoleConstants.Customer && userId != currentUserId)
                return new List<PaymentResponse>();

            var list = await _context.Payments
                .Where(p => p.UserId == userId)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            return list.Select(MapToResponse).ToList();
        }

        /// <summary>
        /// Tra cứu thông tin thanh toán dựa theo mã đặt vé (BookingId).
        /// </summary>
        public async Task<List<PaymentResponse>> GetByBookingAsync(int bookingId)
        {
            var list = await _context.Payments
                .Where(p => p.BookingId == bookingId)
                .ToListAsync();

            return list.Select(MapToResponse).ToList();
        }

        /// <summary>
        /// NGHIỆP VỤ TẠO MỚI GIAO DỊCH THANH TOÁN (Khi khách hàng bấm thanh toán đơn vé)
        /// Tự động tính toán tổng số tiền dựa trên thông tin Vé (Booking) và Đồ ăn (Order)
        /// </summary>
        public async Task<(bool IsSuccess, string Message, int StatusCode, PaymentResponse? Data)> CreateAsync(
            PaymentRequest request, int currentUserId, string currentRole)
        {
            // Bước 1: Kiểm tra xem đơn vé (Booking) có tồn tại trong hệ thống hay không
            var booking = await _context.Bookings.FindAsync(request.BookingId);
            if (booking == null)
                return (false, PaymentMessages.BookingNotFoundForPayment(request.BookingId), 404, null);

            // Bước 2: Bảo mật - Khách hàng thường không được phép thanh toán đơn vé của người khác
            if (currentRole == RoleConstants.Customer && booking.UserId != currentUserId)
                return (false, PaymentMessages.UnauthorizedPayment, 403, null);

            // Bước 3: Chặn thanh toán trùng - Kiểm tra xem đơn vé này đã có giao dịch nào thành công trước đó chưa
            bool alreadyPaid = await _context.Payments.AnyAsync(p =>
                p.BookingId == request.BookingId &&
                p.PaymentStatus == PaymentMessages.StatusSuccess);
            if (alreadyPaid)
                return (false, PaymentMessages.AlreadyPaid, 409, null);

            // Bước 4: Lấy thông tin tiền vé cơ bản (Tiền trước giảm, Tiền giảm giá, Tiền thực tế của vé)
            decimal subTotal    = booking.TicketPrice;
            decimal discountAmt = booking.DiscountAmt;
            decimal total       = booking.TotalAmount;

            // Bước 5: Nếu có mua kèm đồ ăn/đồ uống (Order) thì cộng thêm tiền đồ ăn vào tổng thanh toán
            if (request.OrderId.HasValue)
            {
                var order = await _context.Orders.FindAsync(request.OrderId.Value);
                if (order == null)
                    return (false, PaymentMessages.OrderNotFoundForPayment(request.OrderId.Value), 404, null);

                // Kiểm tra chéo xem Order đồ ăn này có đúng là của đơn vé Booking này hay không
                if (order.BookingId != request.BookingId)
                    return (false, PaymentMessages.OrderBookingMismatch, 400, null);

                subTotal += order.TotalAmount; // Cộng dồn tiền chưa giảm
                total    += order.TotalAmount; // Cộng dồn tiền thực tế phải trả
            }

            // Bước 6: Lưu thông tin người thực hiện giao dịch (Nếu là Admin/Nhân viên tạo thanh toán hộ tại quầy)
            int? staffId = null;
            if (currentRole == RoleConstants.Admin || currentRole == RoleConstants.Staff)
                staffId = currentUserId;

            // Bước 7: Phân loại trạng thái ban đầu dựa vào Phương thức thanh toán:
            // - Nếu trả Tiền mặt (Cash) -> Coi như giao dịch thành công ngay ("Success")
            // - Nếu trả bằng các ví điện tử/QR -> Chờ xác nhận hoặc chờ đối soát ("Pending")
            string initialStatus = request.PaymentMethod == PaymentMessages.MethodCash
                ? PaymentMessages.StatusSuccess
                : PaymentMessages.StatusPending;

            // Bước 8: Tạo bản ghi lưu vào Database
            var payment = new Payment
            {
                BookingId     = request.BookingId,
                OrderId       = request.OrderId,
                UserId        = booking.UserId,
                StaffId       = staffId,
                PaymentMethod = request.PaymentMethod.Trim(),
                SubTotal      = subTotal,
                DiscountAmt   = discountAmt,
                TotalAmount   = total,
                TransactionId = request.TransactionId?.Trim(),
                CreatedAt     = DateTime.Now,
                PaidAt        = initialStatus == PaymentMessages.StatusSuccess ? DateTime.Now : null,
                PaymentStatus = initialStatus,
                Notes         = request.Notes?.Trim()
            };

            _context.Payments.Add(payment);
            await _context.SaveChangesAsync(); // Lưu dữ liệu vào DB

            // Trả về DTO kèm mã QR và thông tin tài khoản ngân hàng để Frontend hiển thị
            return (true, PaymentMessages.CreateSuccess, 201, MapToResponse(payment));
        }

        /// <summary>
        /// CẬP NHẬT TRẠNG THÁI THANH TOÁN (Admin hoặc Nhân viên duyệt thanh toán)
        /// Cho phép chuyển trạng thái: Pending -> Success (Thành công) hoặc Failed (Thất bại), Refunded (Hoàn tiền)
        /// </summary>
        public async Task<(bool IsSuccess, string Message, int StatusCode)> UpdateStatusAsync(
            int id, PaymentStatusRequest request, string currentRole)
        {
            // Bước 1: Bảo mật - Chỉ có quyền quản trị (Admin/Staff) mới được thay đổi trạng thái giao dịch
            if (currentRole != RoleConstants.Admin && currentRole != RoleConstants.Staff)
                return (false, PaymentMessages.UnauthorizedStatusChange, 403);

            // Bước 2: Tìm giao dịch thanh toán trong DB
            var payment = await _context.Payments.FindAsync(id);
            if (payment == null)
                return (false, PaymentMessages.NotFoundWithId(id), 404);

            // Bước 3: Ràng buộc nghiệp vụ - Giao dịch đã hoàn tiền (Refunded) thì không được sửa trạng thái khác nữa
            if (payment.PaymentStatus == PaymentMessages.StatusRefunded)
                return (false, PaymentMessages.CannotUpdateRefunded, 409);

            // Bước 4: Kiểm tra xem trạng thái mới truyền lên có hợp lệ hay không
            var validStatuses = new[] { PaymentMessages.StatusPending, PaymentMessages.StatusSuccess,
                                        PaymentMessages.StatusFailed, PaymentMessages.StatusRefunded };
            if (!validStatuses.Contains(request.Status))
                return (false, PaymentMessages.InvalidStatus, 400);

            // Bước 5: Cập nhật thông tin mới
            payment.PaymentStatus = request.Status;
            payment.Notes         = request.Notes?.Trim() ?? payment.Notes;

            // Nếu chuyển thành công thì ghi nhận thời gian nhận được tiền (PaidAt)
            if (request.Status == PaymentMessages.StatusSuccess)
                payment.PaidAt = DateTime.Now;

            await _context.SaveChangesAsync(); // Lưu thay đổi
            return (true, PaymentMessages.UpdateStatusSuccess, 200);
        }
    }
}
