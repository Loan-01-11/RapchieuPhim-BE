using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RapchieuPhim.API.Models;

namespace RapchieuPhim.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PaymentsController : ControllerBase
    {
        private readonly CinemaManagementContext _context;

        public PaymentsController(CinemaManagementContext context)
        {
            _context = context;
        }

        // GET: api/Payments
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var payments = await _context.Payments.ToListAsync();
            return Ok(payments);
        }

        // GET: api/Payments/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var payment = await _context.Payments.FindAsync(id);
            if (payment == null)
                return NotFound(new { Message = $"Payment with id {id} not found." });
            return Ok(payment);
        }

        // GET: api/Payments/ByBooking/{bookingId}
        [HttpGet("ByBooking/{bookingId}")]
        public async Task<IActionResult> GetByBooking(int bookingId)
        {
            var payments = await _context.Payments
                .Where(p => p.BookingId == bookingId)
                .ToListAsync();
            return Ok(payments);
        }

        // GET: api/Payments/ByOrder/{orderId}
        [HttpGet("ByOrder/{orderId}")]
        public async Task<IActionResult> GetByOrder(int orderId)
        {
            var payments = await _context.Payments
                .Where(p => p.OrderId == orderId)
                .ToListAsync();
            return Ok(payments);
        }

        // GET: api/Payments/RevenueByMovie – uses VW_REVENUE_BY_MOVIE view
        [HttpGet("RevenueByMovie")]
        public async Task<IActionResult> GetRevenueByMovie()
        {
            var revenue = await _context.VwRevenueByMovies.ToListAsync();
            return Ok(revenue);
        }

        // POST: api/Payments
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Payment payment)
        {
            payment.CreatedAt = DateTime.Now;
            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetById), new { id = payment.PaymentId }, payment);
        }

        // PUT: api/Payments/{id}/Status – update PaymentStatus (Pending | Success | Failed | Refunded)
        [HttpPut("{id}/Status")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] string status)
        {
            var payment = await _context.Payments.FindAsync(id);
            if (payment == null)
                return NotFound(new { Message = $"Payment with id {id} not found." });

            payment.PaymentStatus = status;
            if (status == "Success")
                payment.PaidAt = DateTime.Now;

            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
