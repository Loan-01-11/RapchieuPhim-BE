using RapchieuPhim.API.Constants;
using System.ComponentModel.DataAnnotations;

namespace RapchieuPhim.API.DTO.DTORequest
{
    public class AreaRequest
    {
        // Sử dụng hằng số trung tâm thay vì viết tay chữ tiếng Việt
        [Required(ErrorMessage = ValidationMessages.AreaNameRequired)]
        [StringLength(100, ErrorMessage = ValidationMessages.AreaNameMaxLength)]
        public string AreaName { get; set; } = null!;
    }
}
