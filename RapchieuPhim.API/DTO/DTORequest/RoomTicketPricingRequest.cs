using System.ComponentModel.DataAnnotations;

namespace RapchieuPhim.API.DTOs.DTORequest;

public class RoomTicketPricingItemRequest
{
    [Required]
    public string SeatType { get; set; } = null!;

    [Required]
    public string DayType { get; set; } = null!;

    [Range(typeof(decimal), "0.01", "999999999")]
    public decimal Price { get; set; }
}

public class RoomTicketPricingBulkRequest
{
    [Required, MinLength(6)]
    public List<RoomTicketPricingItemRequest> Prices { get; set; } = new();
}
