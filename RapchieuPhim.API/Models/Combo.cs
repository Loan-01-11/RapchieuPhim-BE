using System;
using System.Collections.Generic;

namespace RapchieuPhim.API.Models;

public partial class Combo
{
    public int ComboId { get; set; }

    public string ComboName { get; set; } = null!;

    public decimal Price { get; set; }

    public string? Description { get; set; }

    public string? ImageUrl { get; set; }

    public int Quantity { get; set; }

    public bool IsAvailable { get; set; }
    public bool AllowsCustomization { get; set; }
    public int DrinkSlotCount { get; set; }
    public int PopcornSlotCount { get; set; }

    public virtual ICollection<Combofoodmapping> Combofoodmappings { get; set; } = new List<Combofoodmapping>();

    public virtual ICollection<Orderitem> Orderitems { get; set; } = new List<Orderitem>();
}
