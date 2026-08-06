namespace RapchieuPhim.API.Models;

public class CinemaFoodInventory
{
    public int CinemaId { get; set; }
    public int FoodId { get; set; }
    public int Quantity { get; set; }
    public int MinStock { get; set; }
    public string SaleStatus { get; set; } = "ACTIVE";
    public string Status { get; set; } = "InStock";
    public DateTime UpdatedAt { get; set; }
    public Cinema Cinema { get; set; } = null!;
    public Food Food { get; set; } = null!;
}
