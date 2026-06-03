using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RapchieuPhim.API.Models;

namespace RapchieuPhim.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RoomsController : ControllerBase
    {
        private readonly CinemaManagementContext _context;

        public RoomsController(CinemaManagementContext context)
        {
            _context = context;
        }

        // GET: api/Rooms
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var rooms = await _context.Rooms.ToListAsync();
            return Ok(rooms);
        }

        // GET: api/Rooms/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var room = await _context.Rooms.FindAsync(id);
            if (room == null)
                return NotFound(new { Message = $"Room with id {id} not found." });
            return Ok(room);
        }

        // GET: api/Rooms/ByCinema/{cinemaId}
        [HttpGet("ByCinema/{cinemaId}")]
        public async Task<IActionResult> GetByCinema(int cinemaId)
        {
            var rooms = await _context.Rooms
                .Where(r => r.CinemaId == cinemaId && r.IsActive)
                .ToListAsync();
            return Ok(rooms);
        }

        // POST: api/Rooms
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Room room)
        {
            _context.Rooms.Add(room);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetById), new { id = room.RoomId }, room);
        }

        // PUT: api/Rooms/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] Room room)
        {
            if (id != room.RoomId)
                return BadRequest(new { Message = "Id mismatch." });

            _context.Entry(room).State = EntityState.Modified;
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Rooms.Any(e => e.RoomId == id))
                    return NotFound(new { Message = $"Room with id {id} not found." });
                throw;
            }
            return NoContent();
        }

        // DELETE: api/Rooms/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var room = await _context.Rooms.FindAsync(id);
            if (room == null)
                return NotFound(new { Message = $"Room with id {id} not found." });

            _context.Rooms.Remove(room);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
