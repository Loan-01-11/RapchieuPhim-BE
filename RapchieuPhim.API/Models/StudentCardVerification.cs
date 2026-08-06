namespace RapchieuPhim.API.Models;

public class StudentCardVerification
{
    public int Id { get; set; }
    public int BookingId { get; set; }
    public int? CustomerId { get; set; }
    public string StudentCode { get; set; } = null!;
    public string? StudentName { get; set; }
    public string? SchoolName { get; set; }
    public DateOnly ExpiryDate { get; set; }
    public string? ImagePath { get; set; }
    public byte[]? ImageData { get; set; }
    public string? ImageContentType { get; set; }
    public string Status { get; set; } = null!;
    public int CinemaId { get; set; }
    public int SubmittedByStaffId { get; set; }
    public DateTime SubmittedAt { get; set; }
    public int? ReviewedByAdminId { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? RejectionReason { get; set; }
    public decimal DiscountPercent { get; set; }
    public decimal DiscountAmount { get; set; }
    public byte[] RowVersion { get; set; } = null!;
    public Booking Booking { get; set; } = null!;
    public User? Customer { get; set; }
    public User SubmittedByStaff { get; set; } = null!;
    public User? ReviewedByAdmin { get; set; }
    public Cinema Cinema { get; set; } = null!;
    public StudentDiscountUsage? Usage { get; set; }
}
