namespace RapchieuPhim.API.DTO.DTOResponse;

public sealed class ChatbotSuggestionDto
{
    public string Intent { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Question { get; set; } = string.Empty;
}

public sealed class ChatbotMovieDto
{
    public int MovieId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateOnly? ReleaseDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public int Duration { get; set; }
    public string? AgeRating { get; set; }
    public string? PosterUrl { get; set; }
    public string? TrailerUrl { get; set; }
}

public sealed class ChatbotPromotionDto
{
    public int DiscountId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string DiscountType { get; set; } = string.Empty;
    public decimal DiscountValue { get; set; }
    public decimal MinimumOrderAmount { get; set; }
    public int MaximumUsagePerUser { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}

public sealed class ChatbotShowtimeDto
{
    public int ShowtimeId { get; set; }
    public int MovieId { get; set; }
    public string MovieTitle { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public decimal BasePrice { get; set; }
    public int RoomId { get; set; }
    public string RoomName { get; set; } = string.Empty;
    public int CinemaId { get; set; }
    public string CinemaName { get; set; } = string.Empty;
    public string CinemaAddress { get; set; } = string.Empty;
}

public sealed class ChatbotFeedbackResponseDto
{
    public Guid FeedbackId { get; set; }
    public bool Accepted { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime ReceivedAt { get; set; }
}
