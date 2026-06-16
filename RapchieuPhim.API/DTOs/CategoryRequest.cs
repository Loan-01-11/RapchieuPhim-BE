using System.ComponentModel.DataAnnotations;
using RapchieuPhim.API.Constants;

namespace RapchieuPhim.API.DTOs
{
    public class CategoryRequest
    {
        [Required(ErrorMessage = ValidationMessages.CategoryNameRequired)]
        public string CategoryName { get; set; } = null!;
    }
}