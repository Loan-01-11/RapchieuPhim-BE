using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RapchieuPhim.API.Constants; // Gọi thư mục hằng số để dùng ValidationMessages
using RapchieuPhim.API.DTOs;      // Gọi khuôn dữ liệu CinemaRequest và CinemaResponse
using RapchieuPhim.API.DTOs.DTORequest;
using RapchieuPhim.API.Services;  // Gọi giao tiếp ICinemaService

namespace RapchieuPhim.API.Controllers
{
    [Route("api/[controller]")] // Định nghĩa đường dẫn URL cho API (Ví dụ: api/Cinemas)
    [ApiController]             // Báo hiệu đây là API Controller (Tự động bắt lỗi dữ liệu rác từ Frontend)
    [Authorize]                 // 🔐 KHÓA TỔNG: Bắt buộc phải đăng nhập (có Token hợp lệ) mới được sờ vào bất kỳ hàm nào ở dưới
    public class CinemasController : ControllerBase
    {
        // 1. Khai báo "ông trợ lý" tầng Service để lo việc tính toán nghiệp vụ
        // Từ khóa 'readonly' giúp bảo vệ ghế của ông này, không ai đổi được người khác vào giữa chừng
        private readonly ICinemaService _cinemaService;

        // 2. Hàm khởi tạo (Constructor): Nơi hệ thống .NET tự động bàn giao (Inject) Service vào khi có người gọi API
        public CinemasController(ICinemaService cinemaService)
        {
            _cinemaService = cinemaService; // Cất ông trợ lý vào biến toàn cục để các hàm dưới cùng dùng chung
        }

        /// <summary>
        /// API LẤY TẤT CẢ RẠP CHIẾU PHIM
        /// Quyền hạn: Bất kỳ ai đã đăng nhập (Admin, Staff, Customer) đều xem được
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            // Sai bảo ông Service xuống DB hốt hết danh sách rạp lên
            var cinemas = await _cinemaService.GetAllAsync();

            // Trả về mã thành công 200 OK kèm theo danh sách rạp
            return Ok(cinemas);
        }

        /// <summary>
        /// API XEM CHI TIẾT 1 RẠP THEO ID
        /// Quyền hạn: Tất cả thành viên đã đăng nhập đều xem được
        /// </summary>
        [HttpGet("{id}")] // Frontend phải truyền ID lên URL (Ví dụ: api/Cinemas/5)
        public async Task<IActionResult> GetById(int id)
        {
            // Chuyển số ID xuống Service nhờ tìm kiếm hộ
            var cinema = await _cinemaService.GetByIdAsync(id);

            // BẪY LỖI: Nếu không tìm thấy rạp nào mang ID này
            if (cinema == null)
            {
                // Trả về mã lỗi 404 không tìm thấy + Câu thông báo sạch từ file hằng số trung tâm
                return NotFound(new { Message = ValidationMessages.CinemaNotFoundWithId(id) });
            }

            // Nếu tìm thấy mượt mà, trả về mã thành công 200 OK + cục dữ liệu của rạp đó
            return Ok(cinema);
        }

        /// <summary>
        /// API LỌC RẠP PHIM THEO KHU VỰC (AREA)
        /// Quyền hạn: Tất cả thành viên đã đăng nhập đều xem được
        /// </summary>
        [HttpGet("ByArea/{areaId}")] // Đường dẫn có dạng: api/Cinemas/ByArea/1
        public async Task<IActionResult> GetByArea(int areaId)
        {
            // Nhờ Service lọc ra những rạp nào thuộc mã vùng 'areaId' và đang mở cửa (IsActive == true)
            var cinemas = await _cinemaService.GetByAreaAsync(areaId);

            // Trả về danh sách kết quả (nếu không có rạp nào thì trả về mảng rỗng [] kèm mã 200)
            return Ok(cinemas);
        }

        /// <summary>
        /// API THÊM RẠP CHIẾU PHIM MỚI
        /// Quyền hạn: 👑 CHỈ ADMIN MỚI ĐƯỢC PHÉP bấm nút này
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin")] // Chốt chặn bảo mật: Token phải mang nhãn chức vụ là "Admin" mới được qua cửa
        public async Task<IActionResult> Create([FromBody] CinemaRequest request)
        {
            // Đưa khay dữ liệu của rạp mới xuống Service để thực hiện lệnh INSERT vào DB
            var result = await _cinemaService.CreateAsync(request);

            // Trả về mã chuẩn RESTful 201 Created. 
            // Lệnh này vừa báo thành công, vừa đính kèm đường dẫn để xem lại cái rạp vừa tạo (nameof(GetById))
            return CreatedAtAction(nameof(GetById), new { id = result.CinemaId }, result);
        }

        /// <summary>
        /// API CHỈNH SỬA THÔNG TIN RẠP PHIM
        /// Quyền hạn: 👑 CHỈ ADMIN SUPER MỚI ĐƯỢC PHÉP sửa
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")] // Admin thường vẫn qua được cửa này, nhưng sẽ bị khóa chặt ở Service
        public async Task<IActionResult> Update(int id, [FromBody] CinemaRequest request)
        {
            // 1. 🌟 Bóc cục Token của Admin đang ngồi thao tác để lấy ra Email của họ
            var currentOperatorEmail = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;

            // 2. Truyền ID rạp, Khay dữ liệu sửa và Email xuống cho Service "thẩm định"
            var result = await _cinemaService.UpdateAsync(id, request, currentOperatorEmail ?? string.Empty);

            // 3. Nếu Service báo thất bại (ví dụ dính lỗi 403 ở chốt chặn nâng cao)
            if (!result.IsSuccess)
            {
                // Trả về mã lỗi tương ứng (403, 404, 409) kèm thông báo lỗi sạch cho Frontend
                return StatusCode(result.StatusCode, new { Message = result.Message });
            }

            // 4. Nếu thành công rực rỡ
            return Ok(new { Message = result.Message });
        }

        /// <summary>
        /// DELETE: api/Cinemas/{id}
        /// API XÓA RẠP CHIẾU PHIM
        /// Quyền hạn: 👑 CHỈ ADMIN MỚI ĐƯỢC PHÉP xóa
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")] // Admin thường vẫn đi qua được cửa này, nhưng sẽ bị kẹt lại ở tầng Service
        public async Task<IActionResult> Delete(int id)
        {
            // 1. 🌟 Bóc cục Token của ông Admin đang ngồi thao tác để lấy ra Email của ông ấy
            var currentOperatorEmail = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;

            // 2. Truyền cả ID rạp và Email của ông Admin này xuống tầng Service
            var result = await _cinemaService.DeleteAsync(id, currentOperatorEmail ?? string.Empty);

            // 3. Nếu Service trả về thất bại (ví dụ dính lỗi 403 ở trên), Controller bắn lỗi ra Frontend luôn
            if (!result.IsSuccess)
                return StatusCode(result.StatusCode, new { Message = result.Message });

            // 4. Trả về thông báo xóa thành công rực rỡ
            return Ok(new { Message = result.Message });
        }
    }
}