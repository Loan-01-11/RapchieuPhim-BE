namespace RapchieuPhim.API.DTOs.DTOResponse
{
    // ─── Response trả về thông tin Combo kèm danh sách món ───────────────────────
    public class ComboResponse
    {
        public int ComboId { get; set; }
        public string ComboName { get; set; } = null!;
        public decimal Price { get; set; }
        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
        public int Quantity { get; set; }
        public bool IsAvailable { get; set; }
        
        public int SoldThisMonth { get; set; }
        public decimal RevenueThisMonth { get; set; }
        public int SoldToday { get; set; }
        public decimal RevenueToday { get; set; }
        public int SoldThisWeek { get; set; }
        public decimal RevenueThisWeek { get; set; }

        // Danh sách các món trong combo — trả về tên và số lượng thay vì chỉ FK
        public List<ComboFoodItemResponse> FoodItems { get; set; } = new();
    }

    // ─── Chi tiết từng món trong combo ───────────────────────────────────────────
    public class ComboFoodItemResponse
    {
        public int FoodId { get; set; }
        public string FoodName { get; set; } = null!;
        public string? Category { get; set; }
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }        // Số lượng món này trong combo
    }
}
