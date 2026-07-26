using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RapchieuPhim.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NotificationsController : ControllerBase
    {
        // Static in-memory list to store notifications
        private static readonly List<NotificationDto> _notifications = new List<NotificationDto>
        {
            new NotificationDto
            {
                NotificationId = 1,
                Title = "🎉 Ưu đãi cuối tuần — Giảm 30%",
                Content = "Đặt vé từ T6 đến CN tuần này, nhận ngay ưu đãi giảm 30% cho tất cả suất chiếu buổi tối.",
                Type = "promo",
                Target = "all",
                CreatedAt = DateTime.Now.AddHours(-2)
            },
            new NotificationDto
            {
                NotificationId = 2,
                Title = "✅ Đặt vé thành công",
                Content = "Vé xem phim \"Avengers: Doomsday\" lúc 19:30 ngày 20/06 tại Rạp T&M Quận 1 đã được xác nhận.",
                Type = "ticket",
                Target = "customers",
                CreatedAt = DateTime.Now.AddHours(-5)
            }
        };

        [HttpGet]
        public async Task<IActionResult> Get([FromServices] RapchieuPhim.API.Models.CinemaManagementContext context)
        {
            var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
            var userIdStr = User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value;
            
            var filtered = _notifications.AsQueryable();

            if (string.IsNullOrEmpty(role))
            {
                filtered = filtered.Where(n => n.Target == "all");
            }
            else if (role == "Customer")
            {
                filtered = filtered.Where(n => n.Target == "all" || n.Target == "customers");
            }
            else if (role == "Staff")
            {
                int? cinemaId = null;
                if (int.TryParse(userIdStr, out int userId))
                {
                    var user = await context.Users.FindAsync(userId);
                    cinemaId = user?.CinemaId;
                }
                
                filtered = filtered.Where(n => n.Target == "all" || n.Target == "all_cinemas" || 
                                               (cinemaId.HasValue && n.Target == $"cinema_{cinemaId}"));
            }
            // Admin sees all notifications without filtering.

            // Return notifications sorted by CreatedAt descending
            return Ok(filtered.OrderByDescending(n => n.CreatedAt).ToList());
        }

        [HttpPost]
        public IActionResult Post([FromBody] CreateNotificationDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.Title) || string.IsNullOrWhiteSpace(dto.Content))
            {
                return BadRequest(new { Message = "Tiêu đề và nội dung không được để trống." });
            }

            var notification = new NotificationDto
            {
                NotificationId = _notifications.Count > 0 ? _notifications.Max(n => n.NotificationId) + 1 : 1,
                Title = dto.Title,
                Content = dto.Content,
                Type = dto.Type ?? "info",
                Target = dto.Target ?? "all",
                CreatedAt = DateTime.Now
            };

            _notifications.Add(notification);
            return Ok(notification);
        }

        [HttpDelete("{id}")]
        public IActionResult Delete(int id)
        {
            var notification = _notifications.FirstOrDefault(n => n.NotificationId == id);
            if (notification == null)
            {
                return NotFound(new { Message = "Không tìm thấy thông báo." });
            }

            _notifications.Remove(notification);
            return Ok(new { Message = "Xóa thông báo thành công." });
        }
    }

    public class NotificationDto
    {
        public int NotificationId { get; set; }
        public string Title { get; set; } = null!;
        public string Content { get; set; } = null!;
        public string Type { get; set; } = "info"; // promo, ticket, system, info, warning
        public string Target { get; set; } = "all"; // all, customers, staff, or cinema_[id]
        public DateTime CreatedAt { get; set; }
    }

    public class CreateNotificationDto
    {
        public string Title { get; set; } = null!;
        public string Content { get; set; } = null!;
        public string? Type { get; set; }
        public string? Target { get; set; }
    }
}
