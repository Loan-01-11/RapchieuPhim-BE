using System;
using System.Collections.Generic;

namespace RapchieuPhim.API.Models;

public partial class Staffreport
{
    public int ReportId { get; set; }

    public int StaffId { get; set; }

    public int CinemaId { get; set; }

    public DateOnly ReportDate { get; set; }

    public string? Summary { get; set; }

    public int TotalBookings { get; set; }

    public int TotalOrders { get; set; }

    public decimal TotalRevenue { get; set; }

    public DateTime GeneratedAt { get; set; }

    public virtual Cinema Cinema { get; set; } = null!;

    public virtual User Staff { get; set; } = null!;
}
