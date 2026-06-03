using System;
using System.Collections.Generic;

namespace RapchieuPhim.API.Models;

public partial class Ticketpricing
{
    public int PricingId { get; set; }

    public string? RoomType { get; set; }

    public string? SeatType { get; set; }

    public string? DayType { get; set; }

    public decimal Price { get; set; }

    public DateOnly EffectFrom { get; set; }

    public DateOnly? EffectTo { get; set; }

    public bool IsActive { get; set; }

    public int CreatedBy { get; set; }

    public virtual User CreatedByNavigation { get; set; } = null!;
}
