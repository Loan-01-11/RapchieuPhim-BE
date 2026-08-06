namespace RapchieuPhim.API.DTO.DTOResponse;

public class FoodInventoryResponse
{
    public int CinemaId { get; set; }
    public int FoodId { get; set; }
    public string FoodName { get; set; } = null!;
    public string? Category { get; set; }
    public decimal Price { get; set; }
    public string? ImageUrl { get; set; }
    public int Quantity { get; set; }
    public int MinStock { get; set; }
    public string SaleStatus { get; set; } = null!;
    public string StockStatus { get; set; } = null!;
    public string Status { get; set; } = null!;
    public bool IsAvailable { get; set; }
    public DateTime UpdatedAt { get; set; }
}
