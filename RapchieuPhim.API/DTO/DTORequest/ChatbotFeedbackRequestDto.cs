using System.ComponentModel.DataAnnotations;

namespace RapchieuPhim.API.DTO.DTORequest;

public sealed class ChatbotFeedbackRequestDto
{
    [Required]
    [StringLength(500, MinimumLength = 2)]
    public string Question { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string Intent { get; set; } = string.Empty;

    public bool IsHelpful { get; set; }

    [StringLength(1000)]
    public string? Comment { get; set; }
}

public sealed class ChatbotShowtimeQueryDto
{
    public int? MovieId { get; set; }
    public int? CinemaId { get; set; }
    public DateOnly? Date { get; set; }

    [Range(1, 100)]
    public int Limit { get; set; } = 20;
}
