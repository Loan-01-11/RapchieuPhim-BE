namespace RapchieuPhim.API.Models;

public class FoodStockReceipt
{
    public long ReceiptId { get; set; }
    public int CinemaId { get; set; }
    public int FoodId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitCost { get; set; }
    public string? Supplier { get; set; }
    public DateTime ReceivedAt { get; set; }
    public DateOnly? ExpirationDate { get; set; }
    public string? Notes { get; set; }
    public int? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
}
