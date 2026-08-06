using System.ComponentModel.DataAnnotations;

namespace RapchieuPhim.API.DTO.DTORequest;

public class ReceiveFoodStockRequest
{
    [Range(1, int.MaxValue)] public int CinemaId { get; set; }
    [Range(1, int.MaxValue)] public int FoodId { get; set; }
    [Range(1, int.MaxValue)] public int Quantity { get; set; }
    [Range(typeof(decimal), "0.01", "999999999999")] public decimal UnitCost { get; set; }
    public DateTime ReceivedAt { get; set; }
    public DateOnly? ExpirationDate { get; set; }
    [MaxLength(500)] public string? Notes { get; set; }
}

public class UpdateFoodSaleStatusRequest
{
    [Required] public string SaleStatus { get; set; } = null!;
}

public class AdjustFoodStockRequest
{
    [Range(1, int.MaxValue)] public int CinemaId { get; set; }
    [Range(1, int.MaxValue)] public int FoodId { get; set; }
    public int QuantityChange { get; set; }
    [Range(0, int.MaxValue)] public int MinStock { get; set; }
    [Required] public string TransactionType { get; set; } = "ADJUST";
    [MaxLength(500)] public string? Notes { get; set; }
}

public class TransferFoodStockRequest
{
    [Range(1, int.MaxValue)] public int FromCinemaId { get; set; }
    [Range(1, int.MaxValue)] public int ToCinemaId { get; set; }
    [Range(1, int.MaxValue)] public int FoodId { get; set; }
    [Range(1, int.MaxValue)] public int Quantity { get; set; }
    [MaxLength(500)] public string? Notes { get; set; }
}
