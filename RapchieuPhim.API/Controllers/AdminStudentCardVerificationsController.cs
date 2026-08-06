using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RapchieuPhim.API.DTOs;
using RapchieuPhim.API.Services;

namespace RapchieuPhim.API.Controllers;

[ApiController, Route("api/admin/student-card-verifications"), Authorize(Roles="Admin")]
public class AdminStudentCardVerificationsController : ControllerBase
{
    private readonly IStudentCardVerificationService _service;
    public AdminStudentCardVerificationsController(IStudentCardVerificationService service)=>_service=service;
    private int UserId=>int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)??"0");
    [HttpGet] public async Task<IActionResult> List([FromQuery] StudentVerificationQuery query)=>Ok(await _service.GetAdminListAsync(query));
    [HttpGet("{id:int}")] public async Task<IActionResult> Detail(int id){var value=await _service.GetDetailAsync(id);return value==null?NotFound():Ok(value);}
    [HttpGet("{id:int}/image")] public async Task<IActionResult> Image(int id){var file=await _service.OpenImageAsync(id,UserId,"Admin");return File(file.Stream,file.ContentType);}
    [HttpPatch("{id:int}/approve")] public async Task<IActionResult> Approve(int id)=>Ok(await _service.ApproveAsync(id,UserId));
    [HttpPatch("{id:int}/reject")] public async Task<IActionResult> Reject(int id,[FromBody] RejectStudentCardVerificationRequest request)=>Ok(await _service.RejectAsync(id,UserId,request.Reason));
}
