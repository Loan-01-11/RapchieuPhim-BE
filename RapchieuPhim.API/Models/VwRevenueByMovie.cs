using System;
using System.Collections.Generic;

namespace RapchieuPhim.API.Models;

public partial class VwRevenueByMovie
{
    public int MovieId { get; set; }

    public string Title { get; set; } = null!;

    public int? TotalBookings { get; set; }

    public decimal? TotalRevenue { get; set; }
}
