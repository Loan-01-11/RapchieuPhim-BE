using System.ComponentModel.DataAnnotations;

namespace RapchieuPhim.API.DTOs.Auth
{
    // ?? Response sau khi ðãng nh?p / ðãng k? thành công ?????????????????
    public class AuthResponse
    {
        public string Token { get; set; } = null!;
        public string TokenType { get; set; } = "Bearer";
        public DateTime ExpiresAt { get; set; }
        public UserInfo User { get; set; } = null!;
    }
}