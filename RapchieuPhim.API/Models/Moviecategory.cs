using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace RapchieuPhim.API.Models;

public partial class Moviecategory
{
    public int CategoryId { get; set; }

    public string CategoryName { get; set; } = null!;

    [JsonIgnore]
    public virtual ICollection<Movie> Movies { get; set; } = new List<Movie>();
}
