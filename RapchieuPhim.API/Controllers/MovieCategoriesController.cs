using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RapchieuPhim.API.Models;

namespace RapchieuPhim.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MovieCategoriesController : ControllerBase
    {
        private readonly CinemaManagementContext _context;

        public MovieCategoriesController(CinemaManagementContext context)
        {
            _context = context;
        }

        // GET: api/MovieCategories
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var categories = await _context.Moviecategories.ToListAsync();
            return Ok(categories);
        }

        // GET: api/MovieCategories/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var category = await _context.Moviecategories.FindAsync(id);
            if (category == null)
                return NotFound(new { Message = $"Category with id {id} not found." });
            return Ok(category);
        }

        // POST: api/MovieCategories
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Moviecategory category)
        {
            _context.Moviecategories.Add(category);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetById), new { id = category.CategoryId }, category);
        }

        // PUT: api/MovieCategories/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] Moviecategory category)
        {
            if (id != category.CategoryId)
                return BadRequest(new { Message = "Id mismatch." });

            _context.Entry(category).State = EntityState.Modified;
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Moviecategories.Any(e => e.CategoryId == id))
                    return NotFound(new { Message = $"Category with id {id} not found." });
                throw;
            }
            return NoContent();
        }

        // DELETE: api/MovieCategories/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var category = await _context.Moviecategories.FindAsync(id);
            if (category == null)
                return NotFound(new { Message = $"Category with id {id} not found." });

            _context.Moviecategories.Remove(category);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
