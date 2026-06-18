namespace RapchieuPhim.API.DTOs.DTORequest
{
    public class CinemaRequest
    {
        public string CinemaName { get; set; } = null!;
        public string? Address { get; set; }
        public int AreaId { get; set; }
        public bool IsActive { get; set; }
    }
}