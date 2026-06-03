using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RapchieuPhim.API.Models;

namespace RapchieuPhim.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StaffReportsController : ControllerBase
    {
        private readonly CinemaManagementContext _context;

        public StaffReportsController(CinemaManagementContext context)
        {
            _context = context;
        }

        // GET: api/StaffReports
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var reports = await _context.Staffreports.ToListAsync();
            return Ok(reports);
        }

        // GET: api/StaffReports/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var report = await _context.Staffreports.FindAsync(id);
            if (report == null)
                return NotFound(new { Message = $"StaffReport with id {id} not found." });
            return Ok(report);
        }

        // GET: api/StaffReports/ByStaff/{staffId}
        [HttpGet("ByStaff/{staffId}")]
        public async Task<IActionResult> GetByStaff(int staffId)
        {
            var reports = await _context.Staffreports
                .Where(r => r.StaffId == staffId)
                .OrderByDescending(r => r.ReportDate)
                .ToListAsync();
            return Ok(reports);
        }

        // GET: api/StaffReports/ByCinema/{cinemaId}
        [HttpGet("ByCinema/{cinemaId}")]
        public async Task<IActionResult> GetByCinema(int cinemaId)
        {
            var reports = await _context.Staffreports
                .Where(r => r.CinemaId == cinemaId)
                .OrderByDescending(r => r.ReportDate)
                .ToListAsync();
            return Ok(reports);
        }

        // POST: api/StaffReports
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Staffreport report)
        {
            report.GeneratedAt = DateTime.Now;
            _context.Staffreports.Add(report);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetById), new { id = report.ReportId }, report);
        }
    }
}
