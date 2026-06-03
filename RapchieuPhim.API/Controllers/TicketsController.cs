using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RapchieuPhim.API.Models;

namespace RapchieuPhim.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TicketsController : ControllerBase
    {
        private readonly CinemaManagementContext _context;

        public TicketsController(CinemaManagementContext context)
        {
            _context = context;
        }

        // GET: api/Tickets
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var tickets = await _context.Tickets.ToListAsync();
            return Ok(tickets);
        }

        // GET: api/Tickets/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var ticket = await _context.Tickets.FindAsync(id);
            if (ticket == null)
                return NotFound(new { Message = $"Ticket with id {id} not found." });
            return Ok(ticket);
        }

        // GET: api/Tickets/ByCode/{ticketCode}
        [HttpGet("ByCode/{ticketCode}")]
        public async Task<IActionResult> GetByCode(string ticketCode)
        {
            var ticket = await _context.Tickets
                .FirstOrDefaultAsync(t => t.TicketCode == ticketCode);
            if (ticket == null)
                return NotFound(new { Message = "Ticket code not found." });
            return Ok(ticket);
        }

        // GET: api/Tickets/ByBooking/{bookingId}
        [HttpGet("ByBooking/{bookingId}")]
        public async Task<IActionResult> GetByBooking(int bookingId)
        {
            var tickets = await _context.Tickets
                .Where(t => t.BookingId == bookingId)
                .ToListAsync();
            return Ok(tickets);
        }

        // POST: api/Tickets
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Ticket ticket)
        {
            ticket.IssuedAt = DateTime.Now;
            _context.Tickets.Add(ticket);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetById), new { id = ticket.TicketId }, ticket);
        }

        // PUT: api/Tickets/{id}/Status – update ticket status (Active | Used | Cancelled)
        [HttpPut("{id}/Status")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] string status)
        {
            var ticket = await _context.Tickets.FindAsync(id);
            if (ticket == null)
                return NotFound(new { Message = $"Ticket with id {id} not found." });

            ticket.Status = status;
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
