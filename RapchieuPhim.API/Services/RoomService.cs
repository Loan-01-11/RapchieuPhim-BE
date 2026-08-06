using Microsoft.EntityFrameworkCore;
using RapchieuPhim.API.Constants;      // Gọi file hằng số để dùng ValidationMessages
using RapchieuPhim.API.DTOs.DTORequest; // Gọi khay hứng dữ liệu đầu vào từ Frontend
using RapchieuPhim.API.DTOs.DTOResponse;// Gọi khay chứa dữ liệu sạch trả về cho Frontend
using RapchieuPhim.API.Models;          // Gọi thực thể Entity Model gốc map với Database

namespace RapchieuPhim.API.Services
{
    public interface IRoomService
    {
        Task<List<RoomResponse>> GetAllAsync();
        Task<RoomResponse?> GetByIdAsync(int id);
        Task<List<RoomResponse>> GetByCinemaAsync(int cinemaId);
        Task<RoomResponse> CreateAsync(RoomRequest request);
        Task<(bool IsSuccess, string Message, int StatusCode)> UpdateAsync(int id, RoomRequest request, string currentOperatorEmail);
        Task<(bool IsSuccess, string Message, int StatusCode)> DeleteAsync(int id, string currentOperatorEmail);
    }



    /// <summary>
    /// Lớp xử lý toàn bộ logic nghiệp vụ (Business Logic) liên quan đến Phòng chiếu.
    /// Giúp tách biệt hoàn toàn mã nguồn xử lý Database ra khỏi Controller.
    /// </summary>
    public class RoomService : IRoomService
    {
        // Khai báo kết nối Database thông qua DbContext
        private readonly CinemaManagementContext _context;

        // Hàm khởi tạo: Nhờ .NET Core tự động bơm (Inject) cục Context vào để dùng
        public RoomService(CinemaManagementContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Logic hốt toàn bộ danh sách phòng chiếu từ Database lên
        /// </summary>
        public async Task<List<RoomResponse>> GetAllAsync()
        {
            return await _context.Rooms
                // Sử dụng .Select() để gọt vỏ dữ liệu thô đổ vào khay DTO sạch
                // Việc này giúp tối ưu tốc độ RAM và tránh lỗi vòng lặp tuần hoàn JSON
                .Select(r => new RoomResponse
                {
                    RoomId = r.RoomId,
                    CinemaId = r.CinemaId,
                    RoomName = r.RoomName,
                    RoomType = r.RoomType,
                    TotalSeats = r.Seats.Any() ? r.Seats.Count() : r.TotalSeats,
                    IsActive = r.IsActive
                })
                .OrderBy(r => r.RoomName.Length)
                .ThenBy(r => r.RoomName)
                .ToListAsync(); // Thực thi câu lệnh gọi xuống SQL Server
        }

        /// <summary>
        /// Logic lấy chi tiết một phòng chiếu theo ID
        /// </summary>
        public async Task<RoomResponse?> GetByIdAsync(int id)
        {
            return await _context.Rooms
                .Where(r => r.RoomId == id) // Lọc đúng phòng mang ID yêu cầu
                .Select(r => new RoomResponse
                {
                    RoomId = r.RoomId,
                    CinemaId = r.CinemaId,
                    RoomName = r.RoomName,
                    RoomType = r.RoomType,
                    TotalSeats = r.Seats.Any() ? r.Seats.Count() : r.TotalSeats,
                    IsActive = r.IsActive
                }).FirstOrDefaultAsync(); // Trả về 1 kết quả đầu tiên tìm thấy hoặc null nếu không có
        }

        /// <summary>
        /// Logic lọc danh sách phòng chiếu thuộc về một rạp cụ thể (CinemaId)
        /// </summary>
        public async Task<List<RoomResponse>> GetByCinemaAsync(int cinemaId)
        {
            return await _context.Rooms
                // Lọc điều kiện: Phải thuộc rạp 'cinemaId' VÀ phòng đó phải đang mở cửa (IsActive == true)
                .Where(r => r.CinemaId == cinemaId && r.IsActive)
                .Select(r => new RoomResponse
                {
                    RoomId = r.RoomId,
                    CinemaId = r.CinemaId,
                    RoomName = r.RoomName,
                    RoomType = r.RoomType,
                    TotalSeats = r.Seats.Any() ? r.Seats.Count() : r.TotalSeats,
                    IsActive = r.IsActive
                })
                .OrderBy(r => r.RoomName.Length)
                .ThenBy(r => r.RoomName)
                .ToListAsync();
        }

        /// <summary>
        /// Logic thêm mới một phòng chiếu vào hệ thống (Quyền Admin thường trở lên)
        /// </summary>
        public async Task<RoomResponse> CreateAsync(RoomRequest request)
        {
            // Bốc dữ liệu từ khay Request đổ vào đối tượng Entity Model thô để chuẩn bị nạp xuống DB
            var room = new Room
            {
                CinemaId = request.CinemaId,

                // 🌟 MẸO KHỬ CẢNH BÁO: Dấu chấm than '!' (Null-forgiving) đứng sau request.RoomName
                // Báo hiệu với C# rằng: "Tôi chắc chắn trường này không null đâu vì tầng DTO đã dán nhãn [Required]". 
                // Điều này giúp dập tắt hoàn toàn cảnh báo màu vàng CS8601 trên màn hình.
                RoomName = request.RoomName!.Trim(),

                RoomType = request.RoomType?.Trim(), // Dấu '?' nghĩa là nếu Frontend gửi null thì gán null, không lỗi
                TotalSeats = request.TotalSeats,
                IsActive = request.IsActive
            };

            _context.Rooms.Add(room);
            await _context.SaveChangesAsync();

            if (request.TotalSeats > 0)
            {
                int totalSeats = request.TotalSeats;
                int seatsPerRow = 20; // Default to 20 seats per row as seen in UI
                
                int totalRows = (int)Math.Ceiling((double)totalSeats / seatsPerRow);
                int standardRows = totalRows / 3;
                int vipRows = totalRows / 3;
                int coupleRows = totalRows - standardRows - vipRows;

                int remainder = totalRows % 3;
                if (remainder == 1)
                {
                    standardRows++;
                }
                else if (remainder == 2)
                {
                    standardRows++;
                    vipRows++;
                }

                var newSeats = new List<Seat>();

                char currentRowChar = 'A';
                int currentSeatInRow = 1;
                int currentRowIndex = 1;
                Guid? currentCoupleGroupId = null;

                for (int i = 0; i < totalSeats; i++)
                {
                    string seatType = "Standard";
                    if (currentRowIndex > standardRows && currentRowIndex <= standardRows + vipRows)
                    {
                        seatType = "VIP";
                    }
                    else if (currentRowIndex > standardRows + vipRows)
                    {
                        seatType = "Couple";
                        if (currentSeatInRow % 2 == 1) currentCoupleGroupId = Guid.NewGuid();
                    }

                    string seatNumber = $"{currentRowChar}{currentSeatInRow}";
                    newSeats.Add(new Seat
                    {
                        RoomId = room.RoomId,
                        SeatRow = currentRowChar.ToString(),
                        SeatNumber = seatNumber,
                        SeatType = seatType,
                        CoupleGroupId = seatType == "Couple" ? currentCoupleGroupId : null,
                        IsActive = true
                    });

                    currentSeatInRow++;

                    if (currentSeatInRow > seatsPerRow)
                    {
                        currentSeatInRow = 1;
                        currentRowChar++;
                        currentRowIndex++;
                    }
                }

                _context.Seats.AddRange(newSeats);
                await _context.SaveChangesAsync();
            }

            // Trả về kết quả sau khi tạo thành công
            return new RoomResponse
            {
                RoomId = room.RoomId,
                CinemaId = room.CinemaId,
                RoomName = room.RoomName,
                RoomType = room.RoomType,
                TotalSeats = room.TotalSeats,
                IsActive = room.IsActive
            };
        }

        /// <summary>
        /// Logic chỉnh sửa thông tin phòng chiếu (👑 CHỈ SUPER ADMIN MỚI ĐƯỢC CHẠY)
        /// </summary>
        public async Task<(bool IsSuccess, string Message, int StatusCode)> UpdateAsync(int id, RoomRequest request, string currentOperatorEmail)
        {
            // 1. Dò tìm xem phòng chiếu cần sửa có thực sự tồn tại dưới DB không
            var room = await _context.Rooms.FindAsync(id);
            if (room == null)
            {
                // Trả về bộ kết quả thất bại kèm mã lỗi 404 chuẩn RESTful
                return (false, ValidationMessages.RoomNotFoundWithId(id), 404);
            }

            // 2. 🛡️ CHỐT CHẶN BẢO MẬT: Quét Email người gọi lệnh xem có phải Sếp lớn tối cao không
            if (currentOperatorEmail != ValidationMessages.SuperAdminEmail)
            {
                // Nếu là Admin thường cố tình "hack" API, trả về mã 403 Forbidden (Bị cấm đặc quyền)
                return (false, ValidationMessages.UnauthorizedRoomUpdate, 403);
            }

            // 3. Nếu đúng là Super Admin, thực hiện đè dữ liệu mới lên thực thể cũ
            room.CinemaId = request.CinemaId;
            room.RoomName = request.RoomName!.Trim(); // Tiếp tục dùng dấu '!' để xóa cảnh báo vàng an toàn
            room.RoomType = request.RoomType?.Trim();
            room.TotalSeats = request.TotalSeats;
            room.IsActive = request.IsActive;

            try
            {
                await _context.SaveChangesAsync(); // Lưu vĩnh viễn thay đổi vào SQL Server
                return (true, ValidationMessages.RoomUpdateSuccess, 200); // Trả về thông báo thành công mã 200 OK
            }
            catch (DbUpdateConcurrencyException)
            {
                // BẪY LỖI ĐỒNG THỜI: Phòng hờ trường hợp 2 Admin cùng nhấn nút lưu 1 phòng chiếu tại cùng 1 tích tắc
                return (false, ValidationMessages.RoomConcurrencyError, 409); // Mã 409 Conflict (Xung đột dữ liệu)
            }
        }

        /// <summary>
        /// Logic xóa vĩnh viễn phòng chiếu khỏi hệ thống (👑 CHỈ SUPER ADMIN MỚI ĐƯỢC XÓA)
        /// </summary>
        public async Task<(bool IsSuccess, string Message, int StatusCode)> DeleteAsync(int id, string currentOperatorEmail)
        {
            // 1. Tìm kiếm phòng chiếu cần xóa dưới DB
            var room = await _context.Rooms.FindAsync(id);
            if (room == null)
            {
                return (false, ValidationMessages.RoomNotFoundWithId(id), 404);
            }

            // 2. 🛡️ CHỐT CHẶN BẢO MẬT: Kiểm tra xem có đúng Email Sếp tổng không
            if (currentOperatorEmail != ValidationMessages.SuperAdminEmail)
            {
                return (false, ValidationMessages.UnauthorizedDelete, 403); // Từ chối quyền truy cập mã 403
            }
            try
            {
                _context.Rooms.Remove(room);
                await _context.SaveChangesAsync(); // Xác nhận lệnh xóa xuống Database
                return (true, ValidationMessages.RoomDeleteSuccess, 200); // Thành công mỹ mãn
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException)
            {
                return (false, "Không thể xóa phòng chiếu này vì đang có dữ liệu liên kết (lịch chiếu, ghế ngồi...). Vui lòng xóa dữ liệu liên quan trước!", 400);
            }
        }
    }
}
