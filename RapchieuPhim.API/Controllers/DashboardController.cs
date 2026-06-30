using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RapchieuPhim.API.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace RapchieuPhim.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class DashboardController : ControllerBase
    {
        private readonly CinemaManagementContext _context;

        public DashboardController(CinemaManagementContext context)
        {
            _context = context;
        }

        // GET: api/Dashboard/Stats
        [HttpGet("Stats")]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> GetStats()
        {
            try
            {
                var totalMovies = await _context.Movies.CountAsync();
                var totalUsers = await _context.Users.CountAsync();
                var totalTickets = await _context.Tickets.CountAsync();
                var totalRevenue = await _context.Tickets.SumAsync(t => t.Price);

                return Ok(new
                {
                    TotalMovies = totalMovies,
                    TotalUsers = totalUsers,
                    TotalTickets = totalTickets,
                    TotalRevenue = totalRevenue
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Lấy thống kê thất bại: " + ex.Message });
            }
        }

        // GET: api/Dashboard/RecentTickets
        [HttpGet("RecentTickets")]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> GetRecentTickets()
        {
            try
            {
                var recentTickets = await _context.Tickets
                    .Include(t => t.Booking).ThenInclude(b => b.User)
                    .Include(t => t.Booking).ThenInclude(b => b.ShowTime).ThenInclude(s => s.Movie)
                    .Include(t => t.Booking).ThenInclude(b => b.ShowTime).ThenInclude(s => s.Room).ThenInclude(r => r.Cinema).ThenInclude(c => c.Area)
                    .Include(t => t.Booking).ThenInclude(b => b.Seat)
                    .OrderByDescending(t => t.TicketId)
                    .Take(10)
                    .Select(t => new
                    {
                        TicketId = t.TicketId,
                        TicketCode = t.TicketCode,
                        Price = t.Price,
                        CreatedAt = t.IssuedAt,
                        MovieTitle = t.Booking.ShowTime.Movie != null ? t.Booking.ShowTime.Movie.Title : "N/A",
                        CustomerName = t.Booking.User != null ? t.Booking.User.FullName : "Khách vãng lai",
                        SeatCode = t.Booking.Seat != null ? (t.Booking.Seat.SeatRow + t.Booking.Seat.SeatNumber) : "N/A",
                        CinemaName = t.Booking.ShowTime.Room != null && t.Booking.ShowTime.Room.Cinema != null ? t.Booking.ShowTime.Room.Cinema.CinemaName : "N/A",
                        AreaName = t.Booking.ShowTime.Room != null && t.Booking.ShowTime.Room.Cinema != null && t.Booking.ShowTime.Room.Cinema.Area != null ? t.Booking.ShowTime.Room.Cinema.Area.AreaName : "N/A"
                    })
                    .ToListAsync();

                return Ok(recentTickets);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Lấy danh sách vé gần đây thất bại: " + ex.Message });
            }
        }
    }
}
