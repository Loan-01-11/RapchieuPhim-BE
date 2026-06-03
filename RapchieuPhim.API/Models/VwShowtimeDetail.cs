using System;
using System.Collections.Generic;

namespace RapchieuPhim.API.Models;

public partial class VwShowtimeDetail
{
    public int ShowTimeId { get; set; }

    public int MovieId { get; set; }

    public string MovieTitle { get; set; } = null!;

    public int Duration { get; set; }

    public string? AgeRating { get; set; }

    public string? PosterUrl { get; set; }

    public string AreaName { get; set; } = null!;

    public int CinemaId { get; set; }

    public string CinemaName { get; set; } = null!;

    public int RoomId { get; set; }

    public string RoomName { get; set; } = null!;

    public string? RoomType { get; set; }

    public DateTime StartTime { get; set; }

    public DateTime EndTime { get; set; }

    public decimal BasePrice { get; set; }

    public string Status { get; set; } = null!;
}
