using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RapchieuPhim.API.DTO.DTORequest;
using RapchieuPhim.API.Services;

namespace RapchieuPhim.API.Controllers;

[ApiController]
[Route("api/chatbot")]
public sealed class ChatbotController : ControllerBase
{
    private readonly IChatbotService _chatbotService;

    public ChatbotController(IChatbotService chatbotService)
    {
        _chatbotService = chatbotService;
    }

    [HttpPost("ask")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Ask([FromBody] ChatbotRequestDto request)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var response = await _chatbotService.AskAsync(request.Question);
        return Ok(response);
    }

    [HttpGet("suggestions")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSuggestions()
    {
        return Ok(await _chatbotService.GetSuggestionsAsync());
    }

    [HttpGet("upcoming-movies")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUpcomingMovies([FromQuery] int limit = 10)
    {
        return Ok(await _chatbotService.GetUpcomingMoviesAsync(limit));
    }

    [HttpGet("now-showing-movies")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetNowShowingMovies([FromQuery] int limit = 10)
    {
        return Ok(await _chatbotService.GetNowShowingMoviesAsync(limit));
    }

    [HttpGet("promotions")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPromotions([FromQuery] int limit = 20)
    {
        return Ok(await _chatbotService.GetActivePromotionsAsync(limit));
    }

    [HttpGet("showtimes")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetShowtimes([FromQuery] ChatbotShowtimeQueryDto query)
    {
        return Ok(await _chatbotService.GetShowtimesAsync(query));
    }

    [HttpPost("feedback")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SubmitFeedback([FromBody] ChatbotFeedbackRequestDto request)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var response = await _chatbotService.SubmitFeedbackAsync(request);
        return Accepted(response);
    }
}
