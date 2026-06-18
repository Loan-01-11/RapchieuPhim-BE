using Microsoft.EntityFrameworkCore;
using RapchieuPhim.API.Constants; // 🌟 Gọi thư mục hằng số ra dùng
using RapchieuPhim.API.DTOs;
using RapchieuPhim.API.DTOs.DTORequest;
using RapchieuPhim.API.DTOs.DTOResponse;
using RapchieuPhim.API.Models;


namespace RapchieuPhim.API.Services
{
    public interface ICinemaService
    {
        Task<List<CinemaResponse>> GetAllAsync();
        Task<CinemaResponse?> GetByIdAsync(int id);
        Task<List<CinemaResponse>> GetByAreaAsync(int areaId);
        Task<CinemaResponse> CreateAsync(CinemaRequest request);
        Task<(bool IsSuccess, string Message, int StatusCode)> UpdateAsync(int id, CinemaRequest request, string currentOperatorEmail);
        Task<(bool IsSuccess, string Message, int StatusCode)> DeleteAsync(int id, string currentOperatorEmail);
    }



    public class CinemaService : ICinemaService
    {
        private readonly CinemaManagementContext _context;

        public CinemaService(CinemaManagementContext context)
        {
            _context = context;
        }

        public async Task<List<CinemaResponse>> GetAllAsync()
        {
            return await _context.Cinemas
                .Select(c => new CinemaResponse
                {
                    CinemaId = c.CinemaId,
                    CinemaName = c.CinemaName,
                    Address = c.Address,
                    AreaId = c.AreaId,
                    IsActive = c.IsActive
                }).ToListAsync();
        }

        public async Task<CinemaResponse?> GetByIdAsync(int id)
        {
            return await _context.Cinemas
                .Where(c => c.CinemaId == id)
                .Select(c => new CinemaResponse
                {
                    CinemaId = c.CinemaId,
                    CinemaName = c.CinemaName,
                    Address = c.Address,
                    AreaId = c.AreaId,
                    IsActive = c.IsActive
                }).FirstOrDefaultAsync();
        }

        public async Task<List<CinemaResponse>> GetByAreaAsync(int areaId)
        {
            return await _context.Cinemas
                .Where(c => c.AreaId == areaId && c.IsActive)
                .Select(c => new CinemaResponse
                {
                    CinemaId = c.CinemaId,
                    CinemaName = c.CinemaName,
                    Address = c.Address,
                    AreaId = c.AreaId,
                    IsActive = c.IsActive
                }).ToListAsync();
        }

        public async Task<CinemaResponse> CreateAsync(CinemaRequest request)
        {
            var cinema = new Cinema
            {
                CinemaName = request.CinemaName.Trim(),
                Address = request.Address?.Trim(),
                AreaId = request.AreaId,
                IsActive = request.IsActive
            };

            _context.Cinemas.Add(cinema);
            await _context.SaveChangesAsync();

            return new CinemaResponse
            {
                CinemaId = cinema.CinemaId,
                CinemaName = cinema.CinemaName,
                Address = cinema.Address,
                AreaId = cinema.AreaId,
                IsActive = cinema.IsActive
            };
        }

        public async Task<(bool IsSuccess, string Message, int StatusCode)> UpdateAsync(int id, CinemaRequest request, string currentOperatorEmail)
        {
            // 1. Kiểm tra xem rạp phim có tồn tại thật dưới DB không
            var cinema = await _context.Cinemas.FindAsync(id);
            if (cinema == null)
                return (false, ValidationMessages.CinemaNotFoundWithId(id), 404);

            // 2. 🌟 CHỐT CHẶN TỐI CAO: Chỉ duy nhất Super Admin mới được đi tiếp xuống dưới
            if (currentOperatorEmail != ValidationMessages.SuperAdminEmail)
            {
                // Trả về lỗi 403 Forbidden và lời nhắn từ chối
                return (false, ValidationMessages.UnauthorizedCinemaUpdate, 403);
            }

            // 3. Nếu chuẩn là Super Admin gõ cửa, tiến hành gán dữ liệu mới
            cinema.CinemaName = request.CinemaName.Trim();
            cinema.Address = request.Address?.Trim();
            cinema.AreaId = request.AreaId;
            cinema.IsActive = request.IsActive;

            try
            {
                // Thực hiện lưu vĩnh viễn vào SQL Server
                await _context.SaveChangesAsync();
                return (true, ValidationMessages.CinemaUpdateSuccess, 200);
            }
            catch (DbUpdateConcurrencyException)
            {
                return (false, ValidationMessages.CinemaConcurrencyError, 409);
            }
        }

        public async Task<(bool IsSuccess, string Message, int StatusCode)> DeleteAsync(int id, string currentOperatorEmail)
        {
            // 1. Tìm xem rạp phim cần xóa có tồn tại thật dưới DB không
            var cinema = await _context.Cinemas.FindAsync(id);
            if (cinema == null)
                return (false, ValidationMessages.CinemaNotFoundWithId(id), 404);

            // 2. 🌟 CHỐT CHẶN TỐI CAO: Kiểm tra xem Email người bấm nút có phải của Sếp lớn không
            if (currentOperatorEmail != ValidationMessages.SuperAdminEmail)
            {
                // Nếu KHÔNG phải Super Admin, trả về lỗi 403 Forbidden (Bị cấm quyền)
                return (false, ValidationMessages.UnauthorizedDelete, 403);
            }

            // 3. Nếu chuẩn là Super Admin ra tay, tiến hành xóa vĩnh viễn
            _context.Cinemas.Remove(cinema);
            await _context.SaveChangesAsync();

            return (true, ValidationMessages.CinemaDeleteSuccess, 200);
        }
    }
}