using System;
using System.Collections.Generic;

namespace RapchieuPhim.API.Models;

public partial class Seat
{
    public int SeatId { get; set; }

    public int RoomId { get; set; }

    public string SeatRow { get; set; } = null!;

    public string SeatNumber { get; set; } = null!;

    public string? SeatType { get; set; }

    public bool IsActive { get; set; }

    public Guid? CoupleGroupId { get; set; }

    public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();

    public virtual Room Room { get; set; } = null!;
}
