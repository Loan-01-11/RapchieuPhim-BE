namespace RapchieuPhim.API.Models;

public class CinemaComboSetting
{
    public int CinemaId { get; set; }
    public int ComboId { get; set; }
    public string SaleStatus { get; set; } = "ACTIVE";
    public DateTime UpdatedAt { get; set; }
    public Cinema Cinema { get; set; } = null!;
    public Combo Combo { get; set; } = null!;
}
