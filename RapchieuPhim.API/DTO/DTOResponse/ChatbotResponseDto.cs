namespace RapchieuPhim.API.DTO.DTOResponse;

public sealed class ChatbotResponseDto
{
    public bool Success { get; set; }
    public string Intent { get; set; } = "UNKNOWN";
    public string Message { get; set; } = string.Empty;
    public object[] Data { get; set; } = Array.Empty<object>();
}
