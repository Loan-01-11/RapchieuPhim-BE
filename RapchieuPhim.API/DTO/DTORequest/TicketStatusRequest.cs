using System.ComponentModel.DataAnnotations;
using RapchieuPhim.API.Constants;

namespace RapchieuPhim.API.DTOs.DTORequest
{
    public class TicketStatusRequest
    {
        [Required(ErrorMessage = ValidationMessages.TicketStatusRequired)]
        public string Status { get; set; } = null!; // Active | Used | Cancelled
    }
}