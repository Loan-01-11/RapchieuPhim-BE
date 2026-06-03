using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RapchieuPhim.API.Models;

namespace RapchieuPhim.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CinemasController : ControllerBase
    {
        private readonly CinemaManagementContext _context;

        public CinemasController(CinemaManagementContext context)
        {
            _context = context;
        }

        // GET: api/Cinemas
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var cinemas = await _context.Cinemas.ToListAsync();
            return Ok(cinemas);
        }

        // GET: api/Cinemas/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var cinema = await _context.Cinemas.FindAsync(id);
            if (cinema == null)
                return NotFound(new { Message = $"Cinema with id {id} not found." });
            return Ok(cinema);
        }

        // GET: api/Cinemas/ByArea/{areaId}
        [HttpGet("ByArea/{areaId}")]
        public async Task<IActionResult> GetByArea(int areaId)
        {
            var cinemas = await _context.Cinemas
                .Where(c => c.AreaId == areaId && c.IsActive)
                .ToListAsync();
            return Ok(cinemas);
        }

        // POST: api/Cinemas
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Cinema cinema)
        {
            _context.Cinemas.Add(cinema);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetById), new { id = cinema.CinemaId }, cinema);
        }

        // PUT: api/Cinemas/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] Cinema cinema)
        {
            if (id != cinema.CinemaId)
                return BadRequest(new { Message = "Id mismatch." });

            _context.Entry(cinema).State = EntityState.Modified;
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Cinemas.Any(e => e.CinemaId == id))
                    return NotFound(new { Message = $"Cinema with id {id} not found." });
                throw;
            }
            return NoContent();
        }

        // DELETE: api/Cinemas/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var cinema = await _context.Cinemas.FindAsync(id);
            if (cinema == null)
                return NotFound(new { Message = $"Cinema with id {id} not found." });

            _context.Cinemas.Remove(cinema);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
