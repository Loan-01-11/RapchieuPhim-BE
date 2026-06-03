using System;
using System.Collections.Generic;

namespace RapchieuPhim.API.Models;

public partial class Staffshift
{
    public int ShiftId { get; set; }

    public int StaffId { get; set; }

    public int CinemaId { get; set; }

    public DateOnly ShiftDate { get; set; }

    public DateTime ShiftStart { get; set; }

    public DateTime? ShiftEnd { get; set; }

    public int TotalBookings { get; set; }

    public int TotalOrders { get; set; }

    public decimal TotalRevenue { get; set; }

    public string? Summary { get; set; }

    public string Status { get; set; } = null!;

    public virtual Cinema Cinema { get; set; } = null!;

    public virtual User Staff { get; set; } = null!;
}
