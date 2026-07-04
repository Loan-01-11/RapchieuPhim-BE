using RapchieuPhim.API.Constants;
using System.ComponentModel.DataAnnotations;

namespace RapchieuPhim.API.DTOs.DTORequest
{
    public class FoodRequest
    {
        // 1. Tên món ăn (Bắt buộc, tối đa 150 ký tự)
        [Required(ErrorMessage = FoodMessages.FoodNameRequired)]
        [StringLength(150, ErrorMessage = FoodMessages.FoodNameMaxLength)]
        public string FoodName { get; set; } = null!;

        // 2. Danh mục (Không bắt buộc, tối đa 50 ký tự)
        [StringLength(50, ErrorMessage = FoodMessages.CategoryMaxLength)]
        public string? Category { get; set; }

        // 3. Giá (Bắt buộc, phải >= 0)
        [Range(0, double.MaxValue, ErrorMessage = FoodMessages.PriceInvalid)]
        public decimal Price { get; set; }

        // 4. Số lượng tồn kho (Bắt buộc, phải >= 0)
        [Range(0, int.MaxValue, ErrorMessage = FoodMessages.QuantityInvalid)]
        public int Quantity { get; set; }

        // 5. Đường dẫn ảnh (Không bắt buộc)
        public string? ImageUrl { get; set; }

        // 6. Trạng thái có thể bán
        public bool IsAvailable { get; set; } = true;
    }
}
