using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RapchieuPhim.API.Models;

namespace RapchieuPhim.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DiscountsController : ControllerBase
    {
        private readonly CinemaManagementContext _context;

        public DiscountsController(CinemaManagementContext context)
        {
            _context = context;
        }

        // GET: api/Discounts
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var discounts = await _context.Discounts.ToListAsync();
            return Ok(discounts);
        }

        // GET: api/Discounts/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var discount = await _context.Discounts.FindAsync(id);
            if (discount == null)
                return NotFound(new { Message = $"Discount with id {id} not found." });
            return Ok(discount);
        }

        // GET: api/Discounts/ByCode/{code} – validate a discount code
        [HttpGet("ByCode/{code}")]
        public async Task<IActionResult> GetByCode(string code)
        {
            var discount = await _context.Discounts
                .Where(d => d.DiscountCode == code && d.IsActive
                    && d.StartDate <= DateTime.Now
                    && (d.EndDate == null || d.EndDate >= DateTime.Now))
                .FirstOrDefaultAsync();

            if (discount == null)
                return NotFound(new { Message = "Discount code is invalid or expired." });
            return Ok(discount);
        }

        // POST: api/Discounts
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Discount discount)
        {
            _context.Discounts.Add(discount);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetById), new { id = discount.DiscountId }, discount);
        }

        // PUT: api/Discounts/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] Discount discount)
        {
            if (id != discount.DiscountId)
                return BadRequest(new { Message = "Id mismatch." });

            _context.Entry(discount).State = EntityState.Modified;
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Discounts.Any(e => e.DiscountId == id))
                    return NotFound(new { Message = $"Discount with id {id} not found." });
                throw;
            }
            return NoContent();
        }

        // DELETE: api/Discounts/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var discount = await _context.Discounts.FindAsync(id);
            if (discount == null)
                return NotFound(new { Message = $"Discount with id {id} not found." });

            _context.Discounts.Remove(discount);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
