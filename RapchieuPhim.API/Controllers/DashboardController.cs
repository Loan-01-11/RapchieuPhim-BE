using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RapchieuPhim.API.Models;
using System;
using System.Globalization;
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
        public async Task<IActionResult> GetStats([FromQuery] string? filter, [FromQuery] string? cinemaId)
        {
            try
            {
                var totalMovies = await _context.Movies.CountAsync();
                var totalUsers = await _context.Users.CountAsync();
                
                var ticketQuery = _context.Tickets.Include(t => t.Booking).ThenInclude(b => b.ShowTime).ThenInclude(s => s.Room).AsQueryable();
                ticketQuery = FilterTickets(ticketQuery, filter, cinemaId);
                
                var totalTickets = await ticketQuery.CountAsync();
                var totalTicketRevenue = await ticketQuery.Select(t => (decimal?)t.Price).SumAsync() ?? 0m;

                var orderQuery = _context.Orders.Where(o => o.Status == "Completed" || o.Status == "Paid" || o.Status == "Confirmed").AsQueryable();
                orderQuery = FilterOrders(orderQuery, filter, cinemaId);
                var totalFoodRevenue = await orderQuery.Select(o => (decimal?)o.TotalAmount).SumAsync() ?? 0m;

                var totalRevenue = totalTicketRevenue + totalFoodRevenue;

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
                    .AsSplitQuery()
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

        // GET: api/Dashboard/MovieStats
        [HttpGet("MovieStats")]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> GetMovieStats([FromQuery] string? filter, [FromQuery] string? cinemaId)
        {
            var query = _context.Tickets
                .Include(t => t.Booking).ThenInclude(b => b.ShowTime).ThenInclude(s => s.Room).ThenInclude(r => r.Cinema)
                .Include(t => t.Booking).ThenInclude(b => b.ShowTime).ThenInclude(s => s.Movie)
                .AsQueryable();
                
            query = FilterTickets(query, filter, cinemaId);

            var hasCinemaFilter = !string.IsNullOrEmpty(cinemaId) && int.TryParse(cinemaId, out _);

            var tickets = await query
                .Where(t => t.Booking != null && t.Booking.ShowTime != null && t.Booking.ShowTime.Movie != null)
                .Select(t => new {
                    MovieId = t.Booking.ShowTime.Movie.MovieId,
                    MovieTitle = t.Booking.ShowTime.Movie.Title,
                    PosterUrl = t.Booking.ShowTime.Movie.PosterUrl,
                    Price = t.Price,
                    CinemaName = hasCinemaFilter 
                        ? t.Booking.ShowTime.Room.RoomName 
                        : t.Booking.ShowTime.Room.RoomName + " (" + t.Booking.ShowTime.Room.Cinema.CinemaName + ")",
                    ShowTimeId = t.Booking.ShowTimeId,
                    TotalSeats = t.Booking.ShowTime.Room.TotalSeats
                })
                .ToListAsync();

            var totalOverallRevenue = tickets.Sum(t => t.Price);

            var movieStats = tickets.GroupBy(t => new { t.MovieId, t.MovieTitle, t.PosterUrl })
                .Select(g => {
                    var totalRevenue = g.Sum(t => (decimal?)t.Price) ?? 0m;
                    var totalTicketsSold = g.Count();
                    var revenuePercentage = totalOverallRevenue > 0 ? (totalRevenue / totalOverallRevenue) * 100 : 0;
                    
                    var distinctShowtimes = g.GroupBy(t => t.ShowTimeId).Select(sg => sg.First()).ToList();
                    var totalAvailableSeats = distinctShowtimes.Sum(s => s.TotalSeats);
                    var seatOccupancyPercentage = totalAvailableSeats > 0 ? (totalTicketsSold * 100.0 / totalAvailableSeats) : 0;

                    var cinemaDistributions = g.GroupBy(t => t.CinemaName)
                        .Select(cg => {
                            var tSold = cg.Count();
                            return new {
                                CinemaName = cg.Key,
                                TicketsSold = tSold,
                                Percentage = totalTicketsSold > 0 ? Math.Round((tSold * 100.0 / totalTicketsSold), 1) : 0
                            };
                        }).ToList();

                    return new {
                        MovieId = g.Key.MovieId,
                        MovieTitle = g.Key.MovieTitle,
                        PosterUrl = g.Key.PosterUrl,
                        TotalRevenue = totalRevenue,
                        TotalTicketsSold = totalTicketsSold,
                        RevenueContributionPercentage = Math.Round(revenuePercentage, 1),
                        SeatOccupancyPercentage = Math.Round(seatOccupancyPercentage, 1),
                        CinemaDistributions = cinemaDistributions
                    };
                })
                .OrderByDescending(m => m.TotalRevenue)
                .ToList();

            return Ok(movieStats);
        }

        // GET: api/Dashboard/RevenueChart
        [HttpGet("RevenueChart")]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> GetRevenueChart([FromQuery] string? filter, [FromQuery] string? cinemaId)
        {
            var query = _context.Tickets.Include(t => t.Booking).ThenInclude(b => b.ShowTime).ThenInclude(s => s.Room).AsQueryable();
            query = FilterTickets(query, filter, cinemaId);
            
            var totalTicketRevenue = await query.Select(t => (decimal?)t.Price).SumAsync() ?? 0m;

            var orderQuery = _context.Orders.Where(o => o.Status == "Completed" || o.Status == "Paid" || o.Status == "Confirmed").AsQueryable();
            orderQuery = FilterOrders(orderQuery, filter, cinemaId);
            var totalFoodRevenue = await orderQuery.Select(o => (decimal?)o.TotalAmount).SumAsync() ?? 0m;
            
            var totalRev = totalTicketRevenue + totalFoodRevenue;
            var ticketPerc = totalRev > 0 ? (int)Math.Round((totalTicketRevenue / totalRev) * 100) : 0;
            var foodPerc = totalRev > 0 ? (int)Math.Round((totalFoodRevenue / totalRev) * 100) : 0;

            return Ok(new {
                TotalTicketRevenue = totalTicketRevenue,
                TotalFoodRevenue = totalFoodRevenue,
                TicketRevenuePercentage = ticketPerc,
                FoodRevenuePercentage = foodPerc,
                FoodDistributions = new object[] { },
                TopShowtimes = new object[] { },
                RevenueByTime = new object[] { }
            });
        }
        
        private IQueryable<Ticket> FilterTickets(IQueryable<Ticket> query, string? filter, string? cinemaId)
        {
            if (!string.IsNullOrEmpty(cinemaId) && int.TryParse(cinemaId, out int cid))
            {
                query = query.Where(t => t.Booking != null && t.Booking.ShowTime != null && t.Booking.ShowTime.Room != null && t.Booking.ShowTime.Room.CinemaId == cid);
            }

            if (!string.IsNullOrEmpty(filter))
            {
                var now = DateTime.Now;
                if (filter == "week")
                {
                    var startOfWeek = now.Date.AddDays(-(int)now.DayOfWeek);
                    query = query.Where(t => t.IssuedAt >= startOfWeek);
                }
                else if (filter == "month")
                {
                    var startOfMonth = new DateTime(now.Year, now.Month, 1);
                    query = query.Where(t => t.IssuedAt >= startOfMonth);
                }
                else if (DateTime.TryParse(filter, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime specificDate))
                {
                    query = query.Where(t => t.IssuedAt.Date == specificDate.Date);
                }
            }
            return query;
        }

        private IQueryable<Order> FilterOrders(IQueryable<Order> query, string? filter, string? cinemaId)
        {
            if (!string.IsNullOrEmpty(cinemaId) && int.TryParse(cinemaId, out int cid))
            {
                query = query.Where(o => o.Booking != null && o.Booking.ShowTime != null && o.Booking.ShowTime.Room != null && o.Booking.ShowTime.Room.CinemaId == cid);
            }

            if (!string.IsNullOrEmpty(filter))
            {
                var now = DateTime.Now;
                if (filter == "week")
                {
                    var startOfWeek = now.Date.AddDays(-(int)now.DayOfWeek);
                    query = query.Where(o => o.OrderDate >= startOfWeek);
                }
                else if (filter == "month")
                {
                    var startOfMonth = new DateTime(now.Year, now.Month, 1);
                    query = query.Where(o => o.OrderDate >= startOfMonth);
                }
                else if (DateTime.TryParse(filter, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime specificDate))
                {
                    query = query.Where(o => o.OrderDate.Date == specificDate.Date);
                }
            }
            return query;
        }
    }
}
