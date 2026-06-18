using RapchieuPhim.API.Constants;

namespace RapchieuPhim.API.DTO.DTOResponse
{
    // ── Response khi Google token hợp lệ nhưng chưa có tài khoản ───────
    public class GoogleProfileResponse
    {
        public bool NeedsAdditionalInfo { get; set; } = true;
        public string Email { get; set; } = null!;
        public string FullName { get; set; } = null!;
        public string? AvatarUrl { get; set; }
        public string Message { get; set; } = ValidationMessages.RegistrationAdditionalInfo;
    }
}
