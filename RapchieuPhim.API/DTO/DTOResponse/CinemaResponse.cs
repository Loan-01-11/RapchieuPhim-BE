namespace RapchieuPhim.API.DTOs.DTOResponse
{
    public class CinemaResponse
    {
        public int CinemaId { get; set; }
        public string CinemaName { get; set; } = null!;
        public string? Address { get; set; }
        public int AreaId { get; set; }
        public bool IsActive { get; set; }
        public string? Phone { get; set; }
    }
}