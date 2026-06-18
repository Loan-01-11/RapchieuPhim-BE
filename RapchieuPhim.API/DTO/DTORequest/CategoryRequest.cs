using System.ComponentModel.DataAnnotations;
using RapchieuPhim.API.Constants;

namespace RapchieuPhim.API.DTO.DTORequest
{
    public class CategoryRequest
    {
        [Required(ErrorMessage = ValidationMessages.CategoryNameRequired)]
        public string CategoryName { get; set; } = null!;
    }
}
