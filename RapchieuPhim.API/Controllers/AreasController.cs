using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RapchieuPhim.API.Models;

namespace RapchieuPhim.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AreasController : ControllerBase
    {
        private readonly CinemaManagementContext _context;

        public AreasController(CinemaManagementContext context)
        {
            _context = context;
        }

        // GET: api/Areas
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var areas = await _context.Areas.ToListAsync();
            return Ok(areas);
        }

        // GET: api/Areas/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var area = await _context.Areas.FindAsync(id);
            if (area == null)
                return NotFound(new { Message = $"Area with id {id} not found." });
            return Ok(area);
        }

        // POST: api/Areas
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Area area)
        {
            _context.Areas.Add(area);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetById), new { id = area.AreaId }, area);
        }

        // PUT: api/Areas/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] Area area)
        {
            if (id != area.AreaId)
                return BadRequest(new { Message = "Id mismatch." });

            _context.Entry(area).State = EntityState.Modified;
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Areas.Any(e => e.AreaId == id))
                    return NotFound(new { Message = $"Area with id {id} not found." });
                throw;
            }
            return NoContent();
        }

        // DELETE: api/Areas/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var area = await _context.Areas.FindAsync(id);
            if (area == null)
                return NotFound(new { Message = $"Area with id {id} not found." });

            _context.Areas.Remove(area);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
