using System;
using System.Collections.Generic;

namespace RapchieuPhim.API.Models;

public partial class Food
{
    public int FoodId { get; set; }

    public string FoodName { get; set; } = null!;

    public string? Category { get; set; }

    public decimal Price { get; set; }

    public int Quantity { get; set; }

    public string? ImageUrl { get; set; }

    public bool IsAvailable { get; set; }

    public virtual ICollection<Combofoodmapping> Combofoodmappings { get; set; } = new List<Combofoodmapping>();

    public virtual ICollection<Orderitem> Orderitems { get; set; } = new List<Orderitem>();
}
