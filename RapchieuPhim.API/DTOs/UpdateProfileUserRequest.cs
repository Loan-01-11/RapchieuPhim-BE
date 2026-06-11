namespace RapchieuPhim.API.DTOs
{
    public class UpdateProfileUserRequest
    {
        public string FullName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Phone { get; set; } = null!;
        public string DateOfBirth { get; set; } = null!;
        public string? Gender { get; set; }

    }
}
