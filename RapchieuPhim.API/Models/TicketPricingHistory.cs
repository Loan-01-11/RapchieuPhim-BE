namespace RapchieuPhim.API.Models;

public class TicketPricingHistory
{
    public long HistoryId { get; set; }
    public int PricingId { get; set; }
    public int RoomId { get; set; }
    public string SeatType { get; set; } = null!;
    public string DayType { get; set; } = null!;
    public decimal OldPrice { get; set; }
    public decimal NewPrice { get; set; }
    public int ChangedBy { get; set; }
    public DateTime ChangedAt { get; set; }
}
