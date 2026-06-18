using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RapchieuPhim.API.Constants;
using RapchieuPhim.API.DTO.DTORequest;
using RapchieuPhim.API.Services;

namespace RapchieuPhim.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // 🔐 Mặc định toàn bộ Controller yêu cầu đăng nhập
    public class MovieCategoriesController : ControllerBase
    {
        private readonly IMovieCategoryService _movieCategoryService;

        public MovieCategoriesController(IMovieCategoryService movieCategoryService)
        {
            _movieCategoryService = movieCategoryService;
        }

        // 🔓 1. LẤY TOÀN BỘ THỂ LOẠI (CÔNG KHAI)
        // GET: api/MovieCategories
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAll()
        {
            var categories = await _movieCategoryService.GetAllAsync();
            return Ok(categories);
        }

        // 🔓 2. XEM CHI TIẾT THỂ LOẠI THEO ID (CÔNG KHAI)
        // GET: api/MovieCategories/{id}
        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetById(int id)
        {
            var category = await _movieCategoryService.GetByIdAsync(id);
            if (category == null)
                return NotFound(new { Message = ValidationMessages.CategoryNotFound });

            return Ok(category);
        }

        // 👑 3. THÊM THỂ LOẠI MỚI (CHỈ ADMIN)
        // POST: api/MovieCategories
        [HttpPost]
        [Authorize(Roles = RoleConstants.Admin)]
        public async Task<IActionResult> Create([FromBody] CategoryRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { Message = GetFirstError() });

            var result = await _movieCategoryService.CreateAsync(request);
            return StatusCode(result.StatusCode,
                result.IsSuccess ? result.Data : new { result.Message });
        }

        // 👑 4. CẬP NHẬT THỂ LOẠI (CHỈ ADMIN)
        // PUT: api/MovieCategories/{id}
        [HttpPut("{id}")]
        [Authorize(Roles = RoleConstants.Admin)]
        public async Task<IActionResult> Update(int id, [FromBody] CategoryRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { Message = GetFirstError() });

            var result = await _movieCategoryService.UpdateAsync(id, request);
            return StatusCode(result.StatusCode, new { result.Message });
        }

        // 👑 5. XÓA THỂ LOẠI (CHỈ ADMIN)
        // DELETE: api/MovieCategories/{id}
        [HttpDelete("{id}")]
        [Authorize(Roles = RoleConstants.Admin)]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _movieCategoryService.DeleteAsync(id);
            if (!result.IsSuccess)
                return StatusCode(result.StatusCode, new { result.Message });

            return NoContent();
        }

        // ── Private Helpers ──────────────────────────────────────────────────

        /// <summary>Lấy lỗi đầu tiên từ ModelState để trả về message thân thiện.</summary>
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