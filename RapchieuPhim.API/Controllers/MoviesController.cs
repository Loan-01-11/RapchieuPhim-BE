using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RapchieuPhim.API.Models;

namespace RapchieuPhim.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MoviesController : ControllerBase
    {
        private readonly CinemaManagementContext _context;

        public MoviesController(CinemaManagementContext context)
        {
            _context = context;
        }

        // GET: api/Movies
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var movies = await _context.Movies.ToListAsync();
            return Ok(movies);
        }

        // GET: api/Movies/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var movie = await _context.Movies.FindAsync(id);
            if (movie == null)
                return NotFound(new { Message = $"Movie with id {id} not found." });
            return Ok(movie);
        }

        // GET: api/Movies/ByStatus/{status}
        // status values: Active | Inactive | Coming Soon
        [HttpGet("ByStatus/{status}")]
        public async Task<IActionResult> GetByStatus(string status)
        {
            var movies = await _context.Movies
                .Where(m => m.Status == status)
                .ToListAsync();
            return Ok(movies);
        }

        // POST: api/Movies
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Movie movie)
        {
            movie.CreatedAt = DateTime.Now;
            _context.Movies.Add(movie);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetById), new { id = movie.MovieId }, movie);
        }

        // PUT: api/Movies/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] Movie movie)
        {
            if (id != movie.MovieId)
                return BadRequest(new { Message = "Id mismatch." });

            _context.Entry(movie).State = EntityState.Modified;
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Movies.Any(e => e.MovieId == id))
                    return NotFound(new { Message = $"Movie with id {id} not found." });
                throw;
            }
            return NoContent();
        }

        // DELETE: api/Movies/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var movie = await _context.Movies.FindAsync(id);
            if (movie == null)
                return NotFound(new { Message = $"Movie with id {id} not found." });

            _context.Movies.Remove(movie);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}