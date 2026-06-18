using Microsoft.EntityFrameworkCore;
using RapchieuPhim.API.Constants;
using RapchieuPhim.API.DTO.DTORequest;
using RapchieuPhim.API.Models;

namespace RapchieuPhim.API.Services
{
    public interface IMovieCategoryService
    {
        Task<List<Moviecategory>> GetAllAsync();
        Task<Moviecategory?> GetByIdAsync(int id);
        Task<(bool IsSuccess, string Message, int StatusCode, object? Data)> CreateAsync(CategoryRequest request);
        Task<(bool IsSuccess, string Message, int StatusCode, object? Data)> UpdateAsync(int id, CategoryRequest request);
        Task<(bool IsSuccess, string Message, int StatusCode, object? Data)> DeleteAsync(int id);
    }

    public class MovieCategoryService : IMovieCategoryService
    {
        private readonly CinemaManagementContext _context;

        public MovieCategoryService(CinemaManagementContext context)
        {
            _context = context;
        }

        // 🔓 1. LẤY TOÀN BỘ THỂ LOẠI
        public async Task<List<Moviecategory>> GetAllAsync()
        {
            return await _context.Moviecategories.ToListAsync();
        }

        // 🔓 2. XEM CHI TIẾT THỂ LOẠI THEO ID
        public async Task<Moviecategory?> GetByIdAsync(int id)
        {
            return await _context.Moviecategories.FindAsync(id);
        }

        // 👑 3. THÊM THỂ LOẠI MỚI (ADMIN)
        public async Task<(bool IsSuccess, string Message, int StatusCode, object? Data)> CreateAsync(CategoryRequest request)
        {
            var category = new Moviecategory
            {
                CategoryName = request.CategoryName.Trim()
            };

            _context.Moviecategories.Add(category);
            await _context.SaveChangesAsync();

            return (true, string.Empty, 201, category);
        }

        // 👑 4. CẬP NHẬT THỂ LOẠI (ADMIN)
        public async Task<(bool IsSuccess, string Message, int StatusCode, object? Data)> UpdateAsync(int id, CategoryRequest request)
        {
            var category = await _context.Moviecategories.FindAsync(id);
            if (category == null)
                return (false, ValidationMessages.CategoryNotFound, 404, null);

            category.CategoryName = request.CategoryName.Trim();

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Moviecategories.Any(e => e.CategoryId == id))
                    return (false, ValidationMessages.CategoryNotFound, 404, null);
                throw;
            }

            return (true, ValidationMessages.CategoryUpdateSuccess, 200, null);
        }

        // 👑 5. XÓA THỂ LOẠI (ADMIN)
        public async Task<(bool IsSuccess, string Message, int StatusCode, object? Data)> DeleteAsync(int id)
        {
            var category = await _context.Moviecategories.FindAsync(id);
            if (category == null)
                return (false, ValidationMessages.CategoryNotFound, 404, null);

            _context.Moviecategories.Remove(category);
            await _context.SaveChangesAsync();

            return (true, string.Empty, 204, null);
        }
    }
}
