using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RapchieuPhim.API.Models;

namespace RapchieuPhim.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReviewsController : ControllerBase
    {
        private readonly CinemaManagementContext _context;

        public ReviewsController(CinemaManagementContext context)
        {
            _context = context;
        }

        // GET: api/Reviews
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var reviews = await _context.Reviews.ToListAsync();
            return Ok(reviews);
        }

        // GET: api/Reviews/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var review = await _context.Reviews.FindAsync(id);
            if (review == null)
                return NotFound(new { Message = $"Review with id {id} not found." });
            return Ok(review);
        }

        // GET: api/Reviews/ByMovie/{movieId}
        [HttpGet("ByMovie/{movieId}")]
        public async Task<IActionResult> GetByMovie(int movieId)
        {
            var reviews = await _context.Reviews
                .Where(r => r.MovieId == movieId && r.IsApproved)
                .OrderByDescending(r => r.ReviewDate)
                .ToListAsync();
            return Ok(reviews);
        }

        // GET: api/Reviews/Pending – reviews awaiting admin moderation
        [HttpGet("Pending")]
        public async Task<IActionResult> GetPending()
        {
            var reviews = await _context.Reviews
                .Where(r => !r.IsApproved)
                .OrderByDescending(r => r.ReviewDate)
                .ToListAsync();
            return Ok(reviews);
        }

        // POST: api/Reviews
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Review review)
        {
            review.ReviewDate = DateTime.Now;
            review.IsApproved = false; // requires admin approval
            _context.Reviews.Add(review);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetById), new { id = review.ReviewId }, review);
        }

        // PUT: api/Reviews/{id}/Approve – admin approves a review
        [HttpPut("{id}/Approve")]
        public async Task<IActionResult> Approve(int id)
        {
            var review = await _context.Reviews.FindAsync(id);
            if (review == null)
                return NotFound(new { Message = $"Review with id {id} not found." });

            review.IsApproved = true;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // DELETE: api/Reviews/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var review = await _context.Reviews.FindAsync(id);
            if (review == null)
                return NotFound(new { Message = $"Review with id {id} not found." });

            _context.Reviews.Remove(review);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
