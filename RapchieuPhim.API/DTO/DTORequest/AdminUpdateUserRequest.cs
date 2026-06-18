namespace RapchieuPhim.API.DTO.DTORequest
{
    public class AdminUpdateUserRequest
    {
        public string FullName { get; set; } = null!;
        public string Phone { get; set; } = null!;
        public string DateOfBirth { get; set; } = null!;
        public string? Gender { get; set; }
        public string Role { get; set; } = null!;
        public string Email { get; set; } = null!;
    }
}
