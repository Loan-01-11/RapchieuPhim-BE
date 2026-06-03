using System;
using System.Collections.Generic;

namespace RapchieuPhim.API.Models;

public partial class Cinema
{
    public int CinemaId { get; set; }

    public int AreaId { get; set; }

    public string CinemaName { get; set; } = null!;

    public string Address { get; set; } = null!;

    public string? Phone { get; set; }

    public bool IsActive { get; set; }

    public virtual Area Area { get; set; } = null!;

    public virtual ICollection<Room> Rooms { get; set; } = new List<Room>();

    public virtual ICollection<Staffreport> Staffreports { get; set; } = new List<Staffreport>();

    public virtual ICollection<Staffshift> Staffshifts { get; set; } = new List<Staffshift>();
}
