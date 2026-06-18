using Microsoft.AspNetCore.Authorization; // 🌟 BẮT BUỘC: Thư viện phục vụ phân quyền
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RapchieuPhim.API.Constants;       // 🌟 Gọi file hằng số hệ thống
using RapchieuPhim.API.DTO.DTORequest;
using RapchieuPhim.API.Models;

namespace RapchieuPhim.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // 🔐 KHÓA TỔNG: Tất cả các API trong này mặc định đều phải đăng nhập mới dùng được
    public class MovieCategoriesController : ControllerBase
    {
        private readonly CinemaManagementContext _context;

        public MovieCategoriesController(CinemaManagementContext context)
        {
            _context = context;
        }

        // 🔓 1. LẤY TOÀN BỘ THỂ LOẠI (MỌI ĐỐI TƯỢNG)
        // GET: api/MovieCategories
        [HttpGet]
        [AllowAnonymous] // 🌍 Mở khóa riêng: Khách vãng lai chưa đăng nhập vẫn xem được danh sách thể loại
        public async Task<IActionResult> GetAll()
        {
            var categories = await _context.Moviecategories.ToListAsync();
            return Ok(categories);
        }

        // 🔓 2. LẤY CHI TIẾT THỂ LOẠI THEO ID (MỌI ĐỐI TƯỢNG)
        // GET: api/MovieCategories/{id}
        [HttpGet("{id}")]
        [AllowAnonymous] // 🌍 Mở khóa riêng
        public async Task<IActionResult> GetById(int id)
        {
            var category = await _context.Moviecategories.FindAsync(id);
            if (category == null)
                return NotFound(new { Message = ValidationMessages.CategoryNotFound });

            return Ok(category);
        }

        // 👑 3. THÊM THỂ LOẠI MỚI
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([FromBody] CategoryRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { Message = GetFirstError() });

            var category = new Moviecategory
            {
                CategoryName = request.CategoryName.Trim() // DB tự tăng ID, không cần nạp!
            };

            _context.Moviecategories.Add(category);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = category.CategoryId }, category);
        }

        // 👑 4. CẬP NHẬT THỂ LOẠI
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(int id, [FromBody] CategoryRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { Message = GetFirstError() });

            // 1. Tìm bản ghi gốc trong DB bằng chính cái id trên URL đường dẫn
            var category = await _context.Moviecategories.FindAsync(id);
            if (category == null)
                return NotFound(new { Message = ValidationMessages.CategoryNotFound });

            // 2. Tiến hành gán cập nhật thông tin
            category.CategoryName = request.CategoryName.Trim();

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Moviecategories.Any(e => e.CategoryId == id))
                    return NotFound(new { Message = ValidationMessages.CategoryNotFound });
                throw;
            }

            return Ok(new { Message = ValidationMessages.CategoryUpdateSuccess });
        }

        // 👑 5. XÓA THỂ LOẠI (CHỈ ADMIN)
        // DELETE: api/MovieCategories/{id}
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")] // 🔐 Phân quyền nghiêm ngặt: Chỉ duy nhất ADMIN tối cao mới được quyền xóa thể loại phim!
        public async Task<IActionResult> Delete(int id)
        {
            var category = await _context.Moviecategories.FindAsync(id);
            if (category == null)
                return NotFound(new { Message = ValidationMessages.CategoryNotFound });

            _context.Moviecategories.Remove(category);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private string GetFirstError()
        {
            foreach (var state in ModelState.Values)
                foreach (var error in state.Errors)
                    if (!string.IsNullOrEmpty(error.ErrorMessage))
                        return error.ErrorMessage;

            return ValidationMessages.DataInvalid;
        }
    }
}