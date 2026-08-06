using System;
using System.ComponentModel.DataAnnotations;
using RapchieuPhim.API.DTOs.DTOResponse;

namespace RapchieuPhim.API.DTOs.DTORequest
{
    public class SeatExchangeRequest
    {
        [Required(ErrorMessage = "TicketId là bắt buộc.")]
        public int TicketId { get; set; }

        [Required(ErrorMessage = "NewSeatId là bắt buộc.")]
        public int NewSeatId { get; set; }
    }

    public class ConfirmCashExchangeRequest
    {
        [Required(ErrorMessage = "ExchangeId là bắt buộc.")]
        public int ExchangeId { get; set; }

        [Required(ErrorMessage = "Số tiền khách đưa là bắt buộc.")]
        [Range(0, 100000000, ErrorMessage = "Số tiền không hợp lệ.")]
        public decimal AmountPaid { get; set; }
    }
}

namespace RapchieuPhim.API.DTOs.DTOResponse
{
    public class SeatExchangeResponse
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = null!;
        public bool RequiresPayment { get; set; }
        public decimal AdditionalAmount { get; set; }
        public int? ExchangeId { get; set; }
        public DateTime? HoldUntil { get; set; }
        public TicketResponse? Ticket { get; set; }
    }
}
