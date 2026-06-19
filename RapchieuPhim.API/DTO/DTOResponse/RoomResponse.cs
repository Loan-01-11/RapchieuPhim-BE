namespace RapchieuPhim.API.DTOs.DTOResponse
{
    public class RoomResponse
    {
        public int RoomId { get; set; }
        public int CinemaId { get; set; }
        public string RoomName { get; set; } = null!;
        public string? RoomType { get; set; }
        public int TotalSeats { get; set; }
        public bool IsActive { get; set; }
    }
}