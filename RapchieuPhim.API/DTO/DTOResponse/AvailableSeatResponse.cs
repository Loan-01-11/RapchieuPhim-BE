namespace RapchieuPhim.API.DTOs.DTOResponse
{
    public class AvailableSeatResponse
    {
        public int ShowTimeId { get; set; }
        public string MovieTitle { get; set; } = null!;
        public DateTime StartTime { get; set; }
        public int SeatId { get; set; }
        public string SeatNumber { get; set; } = null!;
        public string? SeatType { get; set; }
        public int RoomId { get; set; }
        public string RoomName { get; set; } = null!;
    }
}