using System;
using System.Collections.Generic;

namespace RapchieuPhim.API.Models;

public partial class Area
{
    public int AreaId { get; set; }

    public string AreaName { get; set; } = null!;

    public virtual ICollection<Cinema> Cinemas { get; set; } = new List<Cinema>();
}
