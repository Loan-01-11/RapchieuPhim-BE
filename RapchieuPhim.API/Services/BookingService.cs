using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using RapchieuPhim.API.Constants;
using RapchieuPhim.API.DTO.DTORequest;
using RapchieuPhim.API.DTOs.DTOResponse;
using RapchieuPhim.API.Models;
using System.Data;

namespace RapchieuPhim.API.Services
{
    public interface IBookingService
    {
        Task<List<BookingDetailResponse>> GetAllDetailsAsync();
        Task<BookingDetailResponse?> GetDetailByIdAsync(int id);
        Task<(bool IsSuccess, string Message, List<BookingDetailResponse>? Data)> GetHistoryByUserAsync(int userId, int currentUserId, string currentRole);
        Task<List<AvailableSeatResponse>> GetAvailableSeatsAsync(int showTimeId);
        Task<(bool IsSuccess, string Message, int BookingId)> CreateBookingAsync(BookingCreateRequest request, int currentUserId, string currentRole);
        Task<(bool IsSuccess, string Message)> CancelBookingAsync(int bookingId, int currentUserId, string currentRole);
    }


    public class BookingService : IBookingService
    {
        private readonly CinemaManagementContext _context;

        public BookingService(CinemaManagementContext context)
        {
            _context = context;
        }

        // 🌟 Tận dụng cái View VW_BOOKING_DETAIL của bạn để hốt toàn bộ lịch sử sạch sẽ
        public async Task<List<BookingDetailResponse>> GetAllDetailsAsync()
        {
            return await _context.VwBookingDetails
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

        // 🛡️ LUỒNG BẢO MẬT: Khách thường chỉ được xem vé của chính mình, Admin/Staff được xem hết
        public async Task<(bool IsSuccess, string Message, List<BookingDetailResponse>? Data)> GetHistoryByUserAsync(int userId, int currentUserId, string currentRole)
        {
            if (currentRole == "Customer" && userId != currentUserId)
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
                    BookingDate = b.BookingDate
                }).ToListAsync();

            return (true, "Lấy lịch sử thành công.", data);
        }

        // 🌟 Tận dụng cái View VW_AVAILABLE_SEATS để lọc danh sách ghế trống siêu tốc
        public async Task<List<AvailableSeatResponse>> GetAvailableSeatsAsync(int showTimeId)
        {
            return await _context.VwAvailableSeats
                .Where(s => s.ShowTimeId == showTimeId)
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

        // 🌟 THẦN CHÚ CAO CẤP: Gọi Stored Procedure SP_BOOK_TICKET để tránh lỗi tranh chấp ghế ngồi
        public async Task<(bool IsSuccess, string Message, int BookingId)> CreateBookingAsync(BookingCreateRequest request, int currentUserId, string currentRole)
        {
            int finalUserId = currentUserId;
            int? staffId = null;

            // Nếu Nhân viên bán vé tại quầy (Counter)
            if (request.BookingType.Trim() == "Counter")
            {
                if (currentRole != "Admin" && currentRole != "Staff")
                    return (false, "Chỉ nhân viên mới được quyền tạo đơn đặt vé tại quầy.", 0);

                // ➔ Nếu không truyền Id khách, đơn hàng sẽ ăn theo Id của chính nhân viên đang đăng nhập
                finalUserId = (request.TargetUserId == null || request.TargetUserId == 0)
                              ? currentUserId
                              : request.TargetUserId.Value;

                staffId = currentUserId;
            }

            // Khai báo các tham số đầu vào đầu ra khớp 100% với SQL Stored Procedure của bạn
            var pUserId = new SqlParameter("@UserId", finalUserId);
            var pShowTimeId = new SqlParameter("@ShowTimeId", request.ShowTimeId);
            var pSeatId = new SqlParameter("@SeatId", request.SeatId);
            var pDiscountCode = new SqlParameter("@DiscountCode", (object?)request.DiscountCode ?? DBNull.Value);
            var pBookingType = new SqlParameter("@BookingType", request.BookingType.Trim());
            var pStaffId = new SqlParameter("@StaffId", (object?)staffId ?? DBNull.Value);

            // Hai tham số lấy giá trị OUTPUT ngược từ SQL Server lên C#
            var pBookingId = new SqlParameter("@BookingId", SqlDbType.Int) { Direction = ParameterDirection.Output };
            var pMessage = new SqlParameter("@Message", SqlDbType.NVarChar, 200) { Direction = ParameterDirection.Output };

            // Thực thi lệnh chạy thủ tục lưu trữ dưới SQL Server
            await _context.Database.ExecuteSqlRawAsync(
                "EXEC SP_BOOK_TICKET @UserId, @ShowTimeId, @SeatId, @DiscountCode, @BookingType, @StaffId, @BookingId OUTPUT, @Message OUTPUT",
                pUserId, pShowTimeId, pSeatId, pDiscountCode, pBookingType, pStaffId, pBookingId, pMessage);

            int resultBookingId = (int)pBookingId.Value;
            string resultMessage = pMessage.Value.ToString() ?? "Lỗi hệ thống.";

            if (resultBookingId == 0)
                return (false, resultMessage, 0);

            return (true, resultMessage, resultBookingId);
        }

        //   Gọi Stored Procedure SP_CANCEL_BOOKING để hoàn trả ghế trống và hủy vé tự động
        public async Task<(bool IsSuccess, string Message)> CancelBookingAsync(int bookingId, int currentUserId, string currentRole)
        {
            var booking = await _context.Bookings.FindAsync(bookingId);
            if (booking == null)
                return (false, ValidationMessages.BookingNotFoundWithId(bookingId));

            // Bảo mật: Khách hàng chỉ được quyền tự hủy đơn của chính mình
            if (currentRole == "Customer" && booking.UserId != currentUserId)
                return (false, ValidationMessages.UnauthorizedBookingCancel);

            var pBookingId = new SqlParameter("@BookingId", bookingId);
            var pMessage = new SqlParameter("@Message", SqlDbType.NVarChar, 200) { Direction = ParameterDirection.Output };

            await _context.Database.ExecuteSqlRawAsync("EXEC SP_CANCEL_BOOKING @BookingId, @Message OUTPUT", pBookingId, pMessage);

            string resultMessage = pMessage.Value.ToString() ?? "Hủy thất bại.";
            if (resultMessage.Contains("successfully"))
                return (true, "Hủy đơn đặt vé thành công!");

            return (false, resultMessage);
        }
    }
}