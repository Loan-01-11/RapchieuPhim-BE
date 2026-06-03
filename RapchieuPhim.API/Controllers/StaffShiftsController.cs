using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RapchieuPhim.API.Models;

namespace RapchieuPhim.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StaffShiftsController : ControllerBase
    {
        private readonly CinemaManagementContext _context;

        public StaffShiftsController(CinemaManagementContext context)
        {
            _context = context;
        }

        // GET: api/StaffShifts
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var shifts = await _context.Staffshifts.ToListAsync();
            return Ok(shifts);
        }

        // GET: api/StaffShifts/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var shift = await _context.Staffshifts.FindAsync(id);
            if (shift == null)
                return NotFound(new { Message = $"StaffShift with id {id} not found." });
            return Ok(shift);
        }

        // GET: api/StaffShifts/ByStaff/{staffId}
        [HttpGet("ByStaff/{staffId}")]
        public async Task<IActionResult> GetByStaff(int staffId)
        {
            var shifts = await _context.Staffshifts
                .Where(s => s.StaffId == staffId)
                .OrderByDescending(s => s.ShiftDate)
                .ToListAsync();
            return Ok(shifts);
        }

        // GET: api/StaffShifts/ByCinema/{cinemaId}
        [HttpGet("ByCinema/{cinemaId}")]
        public async Task<IActionResult> GetByCinema(int cinemaId)
        {
            var shifts = await _context.Staffshifts
                .Where(s => s.CinemaId == cinemaId)
                .OrderByDescending(s => s.ShiftDate)
                .ToListAsync();
            return Ok(shifts);
        }

        // POST: api/StaffShifts
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Staffshift shift)
        {
            _context.Staffshifts.Add(shift);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetById), new { id = shift.ShiftId }, shift);
        }

        // PUT: api/StaffShifts/{id} – update shift (close shift with summary)
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] Staffshift shift)
        {
            if (id != shift.ShiftId)
                return BadRequest(new { Message = "Id mismatch." });

            _context.Entry(shift).State = EntityState.Modified;
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Staffshifts.Any(e => e.ShiftId == id))
                    return NotFound(new { Message = $"StaffShift with id {id} not found." });
                throw;
            }
            return NoContent();
        }
    }
}
