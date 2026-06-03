using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RapchieuPhim.API.Models;

namespace RapchieuPhim.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrdersController : ControllerBase
    {
        private readonly CinemaManagementContext _context;

        public OrdersController(CinemaManagementContext context)
        {
            _context = context;
        }

        // GET: api/Orders
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var orders = await _context.Orders.ToListAsync();
            return Ok(orders);
        }

        // GET: api/Orders/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null)
                return NotFound(new { Message = $"Order with id {id} not found." });
            return Ok(order);
        }

        // GET: api/Orders/ByUser/{userId}
        [HttpGet("ByUser/{userId}")]
        public async Task<IActionResult> GetByUser(int userId)
        {
            var orders = await _context.Orders
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
            return Ok(orders);
        }

        // GET: api/Orders/{id}/Items – line items for an order
        [HttpGet("{id}/Items")]
        public async Task<IActionResult> GetItems(int id)
        {
            var items = await _context.Orderitems
                .Where(i => i.OrderId == id)
                .ToListAsync();
            return Ok(items);
        }

        // POST: api/Orders
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Order order)
        {
            order.OrderDate = DateTime.Now;
            _context.Orders.Add(order);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetById), new { id = order.OrderId }, order);
        }

        // PUT: api/Orders/{id}/Status – update order status (Confirmed | Cancelled)
        [HttpPut("{id}/Status")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] string status)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null)
                return NotFound(new { Message = $"Order with id {id} not found." });

            order.Status = status;
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
