namespace RapchieuPhim.API.DTOs.DTOResponse
{
    public class FoodResponse
    {
        public int FoodId { get; set; }
        public string FoodName { get; set; } = null!;
        public string? Category { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public string? ImageUrl { get; set; }
        public bool IsAvailable { get; set; }
    }
}
