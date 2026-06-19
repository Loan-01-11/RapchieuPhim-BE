using System.ComponentModel.DataAnnotations;
using RapchieuPhim.API.Constants;

namespace RapchieuPhim.API.DTOs.DTORequest
{
    public class RoomRequest
    {
        [Required(ErrorMessage = ValidationMessages.RoomNameRequired)]
        [StringLength(100, ErrorMessage = ValidationMessages.RoomNameMaxLength)]
        public string RoomName { get; set; } = null!;

        [StringLength(50, ErrorMessage = ValidationMessages.RoomTypeMaxLength)]
        public string? RoomType { get; set; } // 2D | 3D 

        [Range(1, int.MaxValue, ErrorMessage = ValidationMessages.RoomCinemaRequired)]
        public int CinemaId { get; set; }

        public int TotalSeats { get; set; }
        public bool IsActive { get; set; }
    }
}