using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RapchieuPhim.API.Models;

namespace RapchieuPhim.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TicketPricingController : ControllerBase
    {
        private readonly CinemaManagementContext _context;

        public TicketPricingController(CinemaManagementContext context)
        {
            _context = context;
        }

        // GET: api/TicketPricing
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var pricing = await _context.Ticketpricings.ToListAsync();
            return Ok(pricing);
        }

        // GET: api/TicketPricing/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var pricing = await _context.Ticketpricings.FindAsync(id);
            if (pricing == null)
                return NotFound(new { Message = $"TicketPricing with id {id} not found." });
            return Ok(pricing);
        }

        // GET: api/TicketPricing/Active – currently effective pricing rules
        [HttpGet("Active")]
        public async Task<IActionResult> GetActive()
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            var pricing = await _context.Ticketpricings
                .Where(p => p.IsActive && p.EffectFrom <= today && (p.EffectTo == null || p.EffectTo >= today))
                .ToListAsync();
            return Ok(pricing);
        }

        // POST: api/TicketPricing
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Ticketpricing pricing)
        {
            _context.Ticketpricings.Add(pricing);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetById), new { id = pricing.PricingId }, pricing);
        }

        // PUT: api/TicketPricing/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] Ticketpricing pricing)
        {
            if (id != pricing.PricingId)
                return BadRequest(new { Message = "Id mismatch." });

            _context.Entry(pricing).State = EntityState.Modified;
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Ticketpricings.Any(e => e.PricingId == id))
                    return NotFound(new { Message = $"TicketPricing with id {id} not found." });
                throw;
            }
            return NoContent();
        }

        // DELETE: api/TicketPricing/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var pricing = await _context.Ticketpricings.FindAsync(id);
            if (pricing == null)
                return NotFound(new { Message = $"TicketPricing with id {id} not found." });

            _context.Ticketpricings.Remove(pricing);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
