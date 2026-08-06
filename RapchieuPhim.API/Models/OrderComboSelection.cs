namespace RapchieuPhim.API.Models;

public class OrderComboSelection
{
    public int Id { get; set; }
    public int OrderDetailId { get; set; }
    public int ComboId { get; set; }
    public int FoodId { get; set; }
    public string FoodNameSnapshot { get; set; } = string.Empty;
    public string? CategorySnapshot { get; set; }
    public int Quantity { get; set; }
    public DateTime CreatedAt { get; set; }

    public virtual Orderitem OrderDetail { get; set; } = null!;
}
