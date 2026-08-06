using System.ComponentModel.DataAnnotations;

namespace RapchieuPhim.API.DTO.DTORequest;

public sealed class ChatbotRequestDto
{
    [Required(ErrorMessage = "Vui lòng nhập câu hỏi.")]
    [StringLength(500, MinimumLength = 2, ErrorMessage = "Câu hỏi phải có từ 2 đến 500 ký tự.")]
    public string Question { get; set; } = string.Empty;
}
