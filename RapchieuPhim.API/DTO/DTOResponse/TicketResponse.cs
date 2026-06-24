namespace RapchieuPhim.API.DTOs.DTOResponse
{
    public class TicketResponse
    {
        public int TicketId { get; set; }
        public int BookingId { get; set; }
        public string TicketCode { get; set; } = null!;
        public string? QrCodeUrl { get; set; }
        public decimal Price { get; set; }
        public DateTime IssuedAt { get; set; }
        public string Status { get; set; } = null!;
    }
}