using System;
using System.Collections.Generic;

namespace RapchieuPhim.API.Models;

public partial class Order
{
    public int OrderId { get; set; }

    public int UserId { get; set; }

    public int? BookingId { get; set; }

    public int? StaffId { get; set; }

    public int? DiscountId { get; set; }

    public int? CinemaId { get; set; }

    public DateTime OrderDate { get; set; }

    public decimal TotalAmount { get; set; }

    public string OrderType { get; set; } = null!;

    public string Status { get; set; } = null!;

    public virtual Booking? Booking { get; set; }

    public virtual Discount? Discount { get; set; }

    public virtual ICollection<Orderitem> Orderitems { get; set; } = new List<Orderitem>();

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public virtual User? Staff { get; set; }

    public virtual User User { get; set; } = null!;
}
