namespace RapchieuPhim.API.DTO.DTOResponse
{
    // ── Response sau khi đăng nhập / đăng ký thành công ─────────────────
    public class AuthResponse
    {
        public string Token { get; set; } = null!;
        public string TokenType { get; set; } = "Bearer";
        public DateTime ExpiresAt { get; set; }
        public UserInfo User { get; set; } = null!;
    }
}
