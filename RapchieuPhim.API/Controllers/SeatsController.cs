using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RapchieuPhim.API.Models;

namespace RapchieuPhim.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SeatsController : ControllerBase
    {
        private readonly CinemaManagementContext _context;

        public SeatsController(CinemaManagementContext context)
        {
            _context = context;
        }

        // GET: api/Seats
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var seats = await _context.Seats.ToListAsync();
            return Ok(seats);
        }

        // GET: api/Seats/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var seat = await _context.Seats.FindAsync(id);
            if (seat == null)
                return NotFound(new { Message = $"Seat with id {id} not found." });
            return Ok(seat);
        }

        // GET: api/Seats/ByRoom/{roomId}
        [HttpGet("ByRoom/{roomId}")]
        public async Task<IActionResult> GetByRoom(int roomId)
        {
            var seats = await _context.Seats
                .Where(s => s.RoomId == roomId && s.IsActive)
                .OrderBy(s => s.SeatRow)
                .ThenBy(s => s.SeatNumber)
                .ToListAsync();
            return Ok(seats);
        }

        // POST: api/Seats
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Seat seat)
        {
            _context.Seats.Add(seat);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetById), new { id = seat.SeatId }, seat);
        }

        // PUT: api/Seats/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] Seat seat)
        {
            if (id != seat.SeatId)
                return BadRequest(new { Message = "Id mismatch." });

            _context.Entry(seat).State = EntityState.Modified;
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Seats.Any(e => e.SeatId == id))
                    return NotFound(new { Message = $"Seat with id {id} not found." });
                throw;
            }
            return NoContent();
        }

        // DELETE: api/Seats/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var seat = await _context.Seats.FindAsync(id);
            if (seat == null)
                return NotFound(new { Message = $"Seat with id {id} not found." });

            _context.Seats.Remove(seat);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
