using System;
using System.Collections.Generic;

namespace RapchieuPhim.API.Models;

public partial class Userdiscountusage
{
    public int UsageId { get; set; }

    public int UserId { get; set; }

    public int DiscountId { get; set; }

    public int UsedCount { get; set; }

    public DateTime LastUsedAt { get; set; }

    public virtual Discount Discount { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
