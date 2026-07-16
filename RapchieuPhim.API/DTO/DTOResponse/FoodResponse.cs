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
        
        public int SoldThisMonth { get; set; }
        public decimal RevenueThisMonth { get; set; }
        public int SoldToday { get; set; }
        public decimal RevenueToday { get; set; }
        public int SoldThisWeek { get; set; }
        public decimal RevenueThisWeek { get; set; }
    }
}
