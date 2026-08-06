using System;
using System.Collections.Generic;

namespace RapchieuPhim.API.Models;

public partial class TicketExchange
{
    public int ExchangeId { get; set; }

    public int TicketId { get; set; }

    public int OldSeatId { get; set; }

    public int NewSeatId { get; set; }

    public int ShowTimeId { get; set; }

    public int UserId { get; set; }

    public int? StaffId { get; set; }

    public decimal AdditionalAmount { get; set; }

    public DateTime HoldUntil { get; set; }

    public string Status { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public virtual Ticket Ticket { get; set; } = null!;

    public virtual Seat OldSeat { get; set; } = null!;

    public virtual Seat NewSeat { get; set; } = null!;

    public virtual Showtime ShowTime { get; set; } = null!;

    public virtual User User { get; set; } = null!;

    public virtual User? Staff { get; set; }
}
