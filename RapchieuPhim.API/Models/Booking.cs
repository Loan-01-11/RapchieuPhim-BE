using System;
using System.Collections.Generic;

namespace RapchieuPhim.API.Models;

public partial class Booking
{
    public int BookingId { get; set; }

    public int UserId { get; set; }

    public int ShowTimeId { get; set; }

    public int SeatId { get; set; }

    public int? DiscountId { get; set; }

    public DateTime BookingDate { get; set; }

    public decimal TicketPrice { get; set; }

    public decimal DiscountAmt { get; set; }

    public decimal TotalAmount { get; set; }

    public string BookingType { get; set; } = null!;

    public int? StaffId { get; set; }

    public int? ShiftId { get; set; }

    public string Status { get; set; } = null!;

    public virtual Discount? Discount { get; set; }

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public virtual Seat Seat { get; set; } = null!;

    public virtual Showtime ShowTime { get; set; } = null!;

    public virtual User? Staff { get; set; }

    public virtual ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();

    public virtual User User { get; set; } = null!;
}
