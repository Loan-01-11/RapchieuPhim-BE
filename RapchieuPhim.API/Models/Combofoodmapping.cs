using System;
using System.Collections.Generic;

namespace RapchieuPhim.API.Models;

public partial class Combofoodmapping
{
    public int ComboId { get; set; }

    public int FoodId { get; set; }

    public int Quantity { get; set; }

    public virtual Combo Combo { get; set; } = null!;

    public virtual Food Food { get; set; } = null!;
}
