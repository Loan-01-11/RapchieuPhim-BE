using System;
using System.Collections.Generic;

namespace RapchieuPhim.API.Models;

public partial class User
{
    public int UserId { get; set; }

    public string FullName { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string? Phone { get; set; }

    public string? AvatarUrl { get; set; }

    public DateOnly? DateOfBirth { get; set; }

    public string? Gender { get; set; }

    public string? Address { get; set; }

    public int RewardPoint { get; set; }

    public string? MembershipLevel { get; set; }

    public string Role { get; set; } = null!;

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<Booking> BookingStaffs { get; set; } = new List<Booking>();

    public virtual ICollection<Booking> BookingUsers { get; set; } = new List<Booking>();

    public virtual ICollection<Discount> Discounts { get; set; } = new List<Discount>();

    public virtual ICollection<Movie> Movies { get; set; } = new List<Movie>();

    public virtual ICollection<Order> OrderStaffs { get; set; } = new List<Order>();

    public virtual ICollection<Order> OrderUsers { get; set; } = new List<Order>();

    public virtual ICollection<Payment> PaymentStaffs { get; set; } = new List<Payment>();

    public virtual ICollection<Payment> PaymentUsers { get; set; } = new List<Payment>();

    public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();



    public virtual ICollection<Staffreport> Staffreports { get; set; } = new List<Staffreport>();

    public virtual ICollection<Staffshift> Staffshifts { get; set; } = new List<Staffshift>();

    public virtual ICollection<Ticketpricing> Ticketpricings { get; set; } = new List<Ticketpricing>();

    public virtual ICollection<Userdiscountusage> Userdiscountusages { get; set; } = new List<Userdiscountusage>();
}
