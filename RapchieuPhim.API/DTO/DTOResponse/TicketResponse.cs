namespace RapchieuPhim.API.DTOs.DTOResponse
{
    public class TicketResponse
    {
        public int TicketId { get; set; }
        public int BookingId { get; set; }
        public int? ShowtimeId { get; set; }
        public int? RoomId { get; set; }
        public string TicketCode { get; set; } = null!;
        public string? QrCodeUrl { get; set; }
        public decimal Price { get; set; }
        public DateTime IssuedAt { get; set; }
        public string Status { get; set; } = null!;
        public string? CustomerName { get; set; }
        public string? MovieTitle { get; set; }
        public string? SeatCode { get; set; }
        public string? AreaName { get; set; }
        public string? CinemaName { get; set; }
        public string? RoomName { get; set; }

        public List<BookingFoodDetailResponse> Foods { get; set; } = new();
    }
}
