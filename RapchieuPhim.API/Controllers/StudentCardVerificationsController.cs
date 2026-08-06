using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RapchieuPhim.API.DTOs;
using RapchieuPhim.API.Services;

namespace RapchieuPhim.API.Controllers;

[ApiController, Route("api/student-card-verifications"), Authorize(Roles="Staff,Admin")]
public class StudentCardVerificationsController : ControllerBase
{
    private readonly IStudentCardVerificationService _service;
    public StudentCardVerificationsController(IStudentCardVerificationService service)=>_service=service;
    private int UserId=>int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)??"0");
    private string Role=>User.FindFirstValue(ClaimTypes.Role)??"";

    [HttpPost, Authorize(Roles="Staff"), RequestSizeLimit(5*1024*1024+64*1024)]
    public async Task<IActionResult> Create([FromForm] CreateStudentCardVerificationRequest request)=>Ok(await _service.CreateAsync(request,UserId));
    [HttpGet("{id:int}/status")] public async Task<IActionResult> Status(int id){var value=await _service.GetStatusAsync(id,UserId,Role);return value==null?NotFound():Ok(value);}
    [HttpGet("by-booking/{bookingId:int}")] public async Task<IActionResult> ByBooking(int bookingId){var value=await _service.GetByBookingAsync(bookingId,UserId,Role);return value==null?NotFound():Ok(value);}
    [HttpGet("{id:int}/image")] public async Task<IActionResult> Image(int id){var file=await _service.OpenImageAsync(id,UserId,Role);return File(file.Stream,file.ContentType);}
    [HttpPatch("{id:int}/cancel"),Authorize(Roles="Staff")] public async Task<IActionResult> Cancel(int id){await _service.CancelAsync(id,UserId);return NoContent();}
}
