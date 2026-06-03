using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RapchieuPhim.API.Models;

namespace RapchieuPhim.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CombosController : ControllerBase
    {
        private readonly CinemaManagementContext _context;

        public CombosController(CinemaManagementContext context)
        {
            _context = context;
        }

        // GET: api/Combos
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var combos = await _context.Combos.ToListAsync();
            return Ok(combos);
        }

        // GET: api/Combos/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var combo = await _context.Combos.FindAsync(id);
            if (combo == null)
                return NotFound(new { Message = $"Combo with id {id} not found." });
            return Ok(combo);
        }

        // GET: api/Combos/Available
        [HttpGet("Available")]
        public async Task<IActionResult> GetAvailable()
        {
            var combos = await _context.Combos
                .Where(c => c.IsAvailable)
                .ToListAsync();
            return Ok(combos);
        }

        // POST: api/Combos
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Combo combo)
        {
            _context.Combos.Add(combo);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetById), new { id = combo.ComboId }, combo);
        }

        // PUT: api/Combos/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] Combo combo)
        {
            if (id != combo.ComboId)
                return BadRequest(new { Message = "Id mismatch." });

            _context.Entry(combo).State = EntityState.Modified;
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Combos.Any(e => e.ComboId == id))
                    return NotFound(new { Message = $"Combo with id {id} not found." });
                throw;
            }
            return NoContent();
        }

        // DELETE: api/Combos/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var combo = await _context.Combos.FindAsync(id);
            if (combo == null)
                return NotFound(new { Message = $"Combo with id {id} not found." });

            _context.Combos.Remove(combo);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
