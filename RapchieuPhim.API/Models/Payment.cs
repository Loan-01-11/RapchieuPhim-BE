using System;
using System.Collections.Generic;

namespace RapchieuPhim.API.Models;

public partial class Payment
{
    public int PaymentId { get; set; }

    public int? BookingId { get; set; }

    public int? OrderId { get; set; }

    public int UserId { get; set; }

    public int? StaffId { get; set; }

    public int? ShiftId { get; set; }

    public string PaymentMethod { get; set; } = null!;

    public string? PaymentType { get; set; }

    public decimal SubTotal { get; set; }

    public decimal DiscountAmt { get; set; }

    public decimal TotalAmount { get; set; }

    public string? TransactionId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? PaidAt { get; set; }

    public string PaymentStatus { get; set; } = null!;

    public string? Notes { get; set; }

    public virtual Booking? Booking { get; set; }

    public virtual Order? Order { get; set; }

    public virtual User? Staff { get; set; }

    public virtual User User { get; set; } = null!;
}
