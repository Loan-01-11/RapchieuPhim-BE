using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RapchieuPhim.API.Constants;
using RapchieuPhim.API.DTOs.DTORequest;
using RapchieuPhim.API.Services;
using System.Security.Claims;

namespace RapchieuPhim.API.Controllers
{
    [Route("api/[controller]")] // Định nghĩa đường dẫn URL (Ví dụ: api/Discounts)
    [ApiController]             // Báo hiệu đây là API Controller (Tự động bắt lỗi dữ liệu rác từ Frontend)
    [Authorize]                 // 🔐 KHÓA TỔNG: Bắt buộc phải đăng nhập mới được truy cập
    public class DiscountsController : ControllerBase
    {
        // 1. Khai báo "ông trợ lý" tầng Service để lo việc tính toán nghiệp vụ
        private readonly IDiscountService _discountService;

        // 2. Hàm khởi tạo: Hệ thống .NET tự động Inject Service vào khi có người gọi API
        public DiscountsController(IDiscountService discountService)
        {
            _discountService = discountService;
        }

        /// <summary>
        /// API LẤY TẤT CẢ MÃ GIẢM GIÁ
        /// Quyền hạn: Admin và Staff được xem toàn bộ danh sách
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> GetAll()
        {
            // Sai bảo ông Service hốt hết danh sách mã giảm giá lên
            var discounts = await _discountService.GetAllAsync();
            return Ok(discounts);
        }

        /// <summary>
        /// API XEM CHI TIẾT 1 MÃ GIẢM GIÁ THEO ID
        /// Quyền hạn: Admin và Staff được xem
        /// </summary>
        [HttpGet("{id}")] // Ví dụ: api/Discounts/5
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> GetById(int id)
        {
            // Nhờ Service tìm kiếm theo ID
            var discount = await _discountService.GetByIdAsync(id);

            // BẪY LỖI: Không tìm thấy mã giảm giá
            if (discount == null)
                return NotFound(new { Message = DiscountMessages.NotFoundWithId(id) });

            return Ok(discount);
        }

        /// <summary>
        /// API KIỂM TRA VÀ LẤY MÃ GIẢM GIÁ THEO CODE
        /// Quyền hạn: Bất kỳ ai đã đăng nhập đều dùng được (để áp mã khi đặt vé)
        /// Chỉ trả về mã còn hiệu lực (IsActive = true, trong khoảng thời gian hợp lệ)
        /// </summary>
        [HttpGet("ByCode/{code}")] // Ví dụ: api/Discounts/ByCode/SUMMER2026
        public async Task<IActionResult> GetByCode(string code)
        {
            // Nhờ Service xác thực và tìm mã theo Code
            var discount = await _discountService.GetByCodeAsync(code);

            // Nếu không tìm thấy hoặc mã đã hết hạn/vô hiệu
            if (discount == null)
                return NotFound(new { Message = DiscountMessages.InvalidOrExpiredCode });

            return Ok(discount);
        }

        /// <summary>
        /// API TẠO MÃ GIẢM GIÁ MỚI
        /// Quyền hạn: 👑 CHỈ ADMIN MỚI ĐƯỢC PHÉP tạo
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([FromBody] DiscountRequest request)
        {
            // 1. Bóc Token để lấy UserId của Admin đang thao tác
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdStr, out int createdByUserId))
                return Unauthorized(new { Message = ValidationMessages.TokenInvalid });

            // 2. Đẩy xuống Service để xử lý nghiệp vụ và lưu DB
            var result = await _discountService.CreateAsync(request, createdByUserId);

            // 3. Nếu Service báo thất bại (trùng code, sai loại, ...)
            if (!result.IsSuccess)
                return StatusCode(result.StatusCode, new { Message = result.Message });

            // 4. Trả về 201 Created kèm theo dữ liệu mã giảm giá vừa tạo
            return CreatedAtAction(nameof(GetById), new { id = result.Data!.DiscountId }, result.Data);
        }

        /// <summary>
        /// API CẬP NHẬT MÃ GIẢM GIÁ
        /// Quyền hạn: 👑 CHỈ SUPER ADMIN MỚI ĐƯỢC SỬA (bảo vệ ở tầng Service)
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(int id, [FromBody] DiscountRequest request)
        {
            // 1. Bóc Token để lấy Email của Admin đang thao tác
            var currentOperatorEmail = User.FindFirst(ClaimTypes.Email)?.Value;

            // 2. Truyền xuống Service để thẩm định quyền và cập nhật
            var result = await _discountService.UpdateAsync(id, request, currentOperatorEmail ?? string.Empty);

            // 3. Nếu Service báo thất bại (403, 404, 409, 400)
            if (!result.IsSuccess)
                return StatusCode(result.StatusCode, new { Message = result.Message });

            // 4. Thành công
            return Ok(new { Message = result.Message });
        }

        /// <summary>
        /// API XÓA MÃ GIẢM GIÁ
        /// Quyền hạn: 👑 CHỈ SUPER ADMIN MỚI ĐƯỢC XÓA (bảo vệ ở tầng Service)
        /// Bảo vệ thêm: Không xóa được mã đã có lịch sử sử dụng
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            // 1. Bóc Token để lấy Email của Admin đang thao tác
            var currentOperatorEmail = User.FindFirst(ClaimTypes.Email)?.Value;

            // 2. Truyền xuống Service để thẩm định quyền và xóa
            var result = await _discountService.DeleteAsync(id, currentOperatorEmail ?? string.Empty);

            // 3. Nếu Service báo thất bại (403, 404, 409)
            if (!result.IsSuccess)
                return StatusCode(result.StatusCode, new { Message = result.Message });

            // 4. Thành công
            return Ok(new { Message = result.Message });
        }
    }
}
