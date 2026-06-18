using Microsoft.EntityFrameworkCore;
using RapchieuPhim.API.Constants;
using RapchieuPhim.API.DTO.DTORequest;
using RapchieuPhim.API.DTOs;
using RapchieuPhim.API.DTOs.DTOResponse;
using RapchieuPhim.API.Models;

namespace RapchieuPhim.API.Services
{
    public interface IAreaService
    {
        Task<List<AreaResponse>> GetAllAsync();
        Task<AreaResponse?> GetByIdAsync(int id);
        Task<(bool IsSuccess, string Message, int StatusCode, AreaResponse? Data)> CreateAsync(AreaRequest request);
        Task<(bool IsSuccess, string Message, int StatusCode)> UpdateAsync(int id, AreaRequest request);
        Task<(bool IsSuccess, string Message, int StatusCode)> DeleteAsync(int id, string currentOperatorEmail);
    }


    public class AreaService : IAreaService
    {
        private readonly CinemaManagementContext _context;

        public AreaService(CinemaManagementContext context)
        {
            _context = context;
        }

        // Lấy toàn bộ khu vực
        public async Task<List<AreaResponse>> GetAllAsync()
        {
            return await _context.Areas
                .Select(a => new AreaResponse { AreaId = a.AreaId, AreaName = a.AreaName })
                .ToListAsync();
        }

        // Lấy khu vực theo ID
        public async Task<AreaResponse?> GetByIdAsync(int id)
        {
            return await _context.Areas
                .Where(a => a.AreaId == id)
                .Select(a => new AreaResponse { AreaId = a.AreaId, AreaName = a.AreaName })
                .FirstOrDefaultAsync();
        }

        // Tạo khu vực mới (Có check trùng tên khu vực)
        public async Task<(bool IsSuccess, string Message, int StatusCode, AreaResponse? Data)> CreateAsync(AreaRequest request)
        {
            var cleanName = request.AreaName.Trim();

            // Check trùng tên khu vực dưới Database để tránh lỗi ràng buộc UNIQUE
            if (await _context.Areas.AnyAsync(a => a.AreaName.ToLower() == cleanName.ToLower()))
                return (false, ValidationMessages.AreaNameAlreadyExists, 400, null);

            var area = new Area { AreaName = cleanName };
            _context.Areas.Add(area);
            await _context.SaveChangesAsync();

            var response = new AreaResponse { AreaId = area.AreaId, AreaName = area.AreaName };
            return (true, "Tạo khu vực thành công!", 201, response);
        }

        // Cập nhật thông tin khu vực
        public async Task<(bool IsSuccess, string Message, int StatusCode)> UpdateAsync(int id, AreaRequest request)
        {
            var area = await _context.Areas.FindAsync(id);
            if (area == null)
                return (false, ValidationMessages.AreaNotFoundWithId(id), 404);

            var cleanName = request.AreaName.Trim();

            // Check nếu đổi sang tên mới mà tên đó đã có khu vực khác chiếm mất rồi
            if (await _context.Areas.AnyAsync(a => a.AreaId != id && a.AreaName.ToLower() == cleanName.ToLower()))
                return (false, ValidationMessages.AreaNameAlreadyExists, 400);

            area.AreaName = cleanName;

            try
            {
                await _context.SaveChangesAsync();
                return (true, ValidationMessages.AreaUpdateSuccess, 200);
            }
            catch (DbUpdateConcurrencyException)
            {
                return (false, ValidationMessages.AreaConcurrencyError, 409);
            }
        }

        // Xóa khu vực (Chỉ cho phép Super Admin)
        public async Task<(bool IsSuccess, string Message, int StatusCode)> DeleteAsync(int id, string currentOperatorEmail)
        {
            var area = await _context.Areas.FindAsync(id);
            if (area == null)
                return (false, ValidationMessages.AreaNotFoundWithId(id), 404);

            // 👑 CHỐT CHẶN TỐI CAO: Kiểm tra email sếp lớn
            if (currentOperatorEmail != ValidationMessages.SuperAdminEmail)
                return (false, ValidationMessages.UnauthorizedDelete, 403);

            _context.Areas.Remove(area);
            await _context.SaveChangesAsync();

            return (true, ValidationMessages.AreaDeleteSuccess, 200);
        }
    }
}