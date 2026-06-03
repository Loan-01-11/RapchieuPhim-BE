using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RapchieuPhim.API.Models;

namespace RapchieuPhim.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ShowtimesController : ControllerBase
    {
        private readonly CinemaManagementContext _context;

        public ShowtimesController(CinemaManagementContext context)
        {
            _context = context;
        }

        // GET: api/Showtimes
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var showtimes = await _context.Showtimes.ToListAsync();
            return Ok(showtimes);
        }

        // GET: api/Showtimes/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var showtime = await _context.Showtimes.FindAsync(id);
            if (showtime == null)
                return NotFound(new { Message = $"Showtime with id {id} not found." });
            return Ok(showtime);
        }

        // GET: api/Showtimes/ByMovie/{movieId}
        [HttpGet("ByMovie/{movieId}")]
        public async Task<IActionResult> GetByMovie(int movieId)
        {
            var showtimes = await _context.Showtimes
                .Where(s => s.MovieId == movieId && s.Status == "Active")
                .OrderBy(s => s.StartTime)
                .ToListAsync();
            return Ok(showtimes);
        }

        // GET: api/Showtimes/ByRoom/{roomId}
        [HttpGet("ByRoom/{roomId}")]
        public async Task<IActionResult> GetByRoom(int roomId)
        {
            var showtimes = await _context.Showtimes
                .Where(s => s.RoomId == roomId)
                .OrderBy(s => s.StartTime)
                .ToListAsync();
            return Ok(showtimes);
        }

        // GET: api/Showtimes/Detail – uses VW_SHOWTIME_DETAIL view
        [HttpGet("Detail")]
        public async Task<IActionResult> GetDetail()
        {
            var detail = await _context.VwShowtimeDetails.ToListAsync();
            return Ok(detail);
        }

        // POST: api/Showtimes
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Showtime showtime)
        {
            _context.Showtimes.Add(showtime);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetById), new { id = showtime.ShowTimeId }, showtime);
        }

        // PUT: api/Showtimes/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] Showtime showtime)
        {
            if (id != showtime.ShowTimeId)
                return BadRequest(new { Message = "Id mismatch." });

            _context.Entry(showtime).State = EntityState.Modified;
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Showtimes.Any(e => e.ShowTimeId == id))
                    return NotFound(new { Message = $"Showtime with id {id} not found." });
                throw;
            }
            return NoContent();
        }

        // DELETE: api/Showtimes/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var showtime = await _context.Showtimes.FindAsync(id);
            if (showtime == null)
                return NotFound(new { Message = $"Showtime with id {id} not found." });

            _context.Showtimes.Remove(showtime);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
