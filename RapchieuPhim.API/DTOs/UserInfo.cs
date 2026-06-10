using System.ComponentModel.DataAnnotations;
namespace RapchieuPhim.API.DTOs.Auth
{
    public class UserInfo
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string? Phone { get; set; }
        public string? AvatarUrl { get; set; }
        public string? MembershipLevel { get; set; }
        public int RewardPoint { get; set; }
        public string Role { get; set; } = null!;
    }
}