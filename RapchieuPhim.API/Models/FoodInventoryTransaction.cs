namespace RapchieuPhim.API.Models;

public class FoodInventoryTransaction
{
    public long InventoryTransactionId { get; set; }
    public int CinemaId { get; set; }
    public int FoodId { get; set; }
    public string TransactionType { get; set; } = null!;
    public int QuantityChange { get; set; }
    public int QuantityBefore { get; set; }
    public int QuantityAfter { get; set; }
    public decimal? UnitCost { get; set; }
    public string? ReferenceType { get; set; }
    public int? ReferenceId { get; set; }
    public string? Supplier { get; set; }
    public DateOnly? ExpirationDate { get; set; }
    public string? Notes { get; set; }
    public int? PerformedBy { get; set; }
    public DateTime CreatedAt { get; set; }
}
