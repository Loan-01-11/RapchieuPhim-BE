using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RapchieuPhim.API.Models;

namespace RapchieuPhim.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FoodsController : ControllerBase
    {
        private readonly CinemaManagementContext _context;

        public FoodsController(CinemaManagementContext context)
        {
            _context = context;
        }

        // GET: api/Foods
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var foods = await _context.Foods.ToListAsync();
            return Ok(foods);
        }

        // GET: api/Foods/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var food = await _context.Foods.FindAsync(id);
            if (food == null)
                return NotFound(new { Message = $"Food with id {id} not found." });
            return Ok(food);
        }

        // GET: api/Foods/Available – items currently available for purchase
        [HttpGet("Available")]
        public async Task<IActionResult> GetAvailable()
        {
            var foods = await _context.Foods
                .Where(f => f.IsAvailable)
                .ToListAsync();
            return Ok(foods);
        }

        // POST: api/Foods
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Food food)
        {
            _context.Foods.Add(food);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetById), new { id = food.FoodId }, food);
        }

        // PUT: api/Foods/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] Food food)
        {
            if (id != food.FoodId)
                return BadRequest(new { Message = "Id mismatch." });

            _context.Entry(food).State = EntityState.Modified;
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Foods.Any(e => e.FoodId == id))
                    return NotFound(new { Message = $"Food with id {id} not found." });
                throw;
            }
            return NoContent();
        }

        // DELETE: api/Foods/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var food = await _context.Foods.FindAsync(id);
            if (food == null)
                return NotFound(new { Message = $"Food with id {id} not found." });

            _context.Foods.Remove(food);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
