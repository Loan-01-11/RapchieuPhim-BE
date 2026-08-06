using System;
using System.Collections.Generic;

namespace RapchieuPhim.API.Models;

public partial class Orderitem
{
    public int OrderItemId { get; set; }

    public int OrderId { get; set; }

    public int? FoodId { get; set; }

    public int? ComboId { get; set; }

    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal Subtotal { get; set; }

    public string? ComboSelectionSnapshot { get; set; }

    public virtual Combo? Combo { get; set; }

    public virtual Food? Food { get; set; }

    public virtual Order Order { get; set; } = null!;

    public virtual ICollection<OrderComboSelection> ComboSelections { get; set; } = new List<OrderComboSelection>();
}
