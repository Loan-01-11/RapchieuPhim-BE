using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RapchieuPhim.API.Models;

namespace RapchieuPhim.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BookingsController : ControllerBase
    {
        private readonly CinemaManagementContext _context;

        public BookingsController(CinemaManagementContext context)
        {
            _context = context;
        }

        // GET: api/Bookings
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var bookings = await _context.Bookings.ToListAsync();
            return Ok(bookings);
        }

        // GET: api/Bookings/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var booking = await _context.Bookings.FindAsync(id);
            if (booking == null)
                return NotFound(new { Message = $"Booking with id {id} not found." });
            return Ok(booking);
        }

        // GET: api/Bookings/ByUser/{userId} – purchase history (UC-10)
        [HttpGet("ByUser/{userId}")]
        public async Task<IActionResult> GetByUser(int userId)
        {
            var bookings = await _context.Bookings
                .Where(b => b.UserId == userId)
                .OrderByDescending(b => b.BookingDate)
                .ToListAsync();
            return Ok(bookings);
        }

        // GET: api/Bookings/Detail – uses VW_BOOKING_DETAIL view
        [HttpGet("Detail")]
        public async Task<IActionResult> GetDetail()
        {
            var detail = await _context.VwBookingDetails.ToListAsync();
            return Ok(detail);
        }

        // GET: api/Bookings/AvailableSeats/{showTimeId} – uses VW_AVAILABLE_SEATS view
        [HttpGet("AvailableSeats/{showTimeId}")]
        public async Task<IActionResult> GetAvailableSeats(int showTimeId)
        {
            var seats = await _context.VwAvailableSeats
                .Where(s => s.ShowTimeId == showTimeId)
                .ToListAsync();
            return Ok(seats);
        }

        // POST: api/Bookings
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Booking booking)
        {
            booking.BookingDate = DateTime.Now;
            _context.Bookings.Add(booking);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetById), new { id = booking.BookingId }, booking);
        }

        // PUT: api/Bookings/{id}/Status – update booking status (Confirmed | Cancelled)
        [HttpPut("{id}/Status")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] string status)
        {
            var booking = await _context.Bookings.FindAsync(id);
            if (booking == null)
                return NotFound(new { Message = $"Booking with id {id} not found." });

            booking.Status = status;
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
