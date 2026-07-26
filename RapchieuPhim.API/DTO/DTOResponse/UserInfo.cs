namespace RapchieuPhim.API.DTO.DTOResponse
{
    public class UserInfo
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public string? AvatarUrl { get; set; }
        public string? MembershipLevel { get; set; }
        public int RewardPoint { get; set; }
        public string Role { get; set; } = null!;
        public int? CinemaId { get; set; }
    }
}
