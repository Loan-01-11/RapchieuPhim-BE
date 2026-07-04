using RapchieuPhim.API.Constants;
using System.ComponentModel.DataAnnotations;

namespace RapchieuPhim.API.DTOs.DTORequest
{
    // ─── Request tạo / cập nhật Combo ────────────────────────────────────────────
    public class ComboRequest
    {
        [Required(ErrorMessage = ComboMessages.ComboNameRequired)]
        [StringLength(150, ErrorMessage = ComboMessages.ComboNameMaxLength)]
        public string ComboName { get; set; } = null!;

        [Range(0, double.MaxValue, ErrorMessage = ComboMessages.PriceInvalid)]
        public decimal Price { get; set; }

        [StringLength(500, ErrorMessage = ComboMessages.DescriptionMaxLength)]
        public string? Description { get; set; }

        public string? ImageUrl { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = ComboMessages.QuantityInvalid)]
        public int Quantity { get; set; }

        public bool IsAvailable { get; set; } = true;

        // Danh sách các món ăn trong combo (có thể null khi chỉ cập nhật thông tin combo)
        public List<ComboFoodItemRequest>? FoodItems { get; set; }
    }

    // ─── Request thêm / cập nhật một món trong Combo ─────────────────────────────
    public class ComboFoodItemRequest
    {
        [Required(ErrorMessage = ComboMessages.FoodIdRequired)]
        public int FoodId { get; set; }

        [Range(1, 100, ErrorMessage = ComboMessages.FoodQuantityInvalid)]
        public int Quantity { get; set; } = 1;
    }
}
