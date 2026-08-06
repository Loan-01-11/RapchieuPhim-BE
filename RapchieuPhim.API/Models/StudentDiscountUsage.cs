namespace RapchieuPhim.API.Models;

public class StudentDiscountUsage
{
    public int Id { get; set; }
    public int VerificationId { get; set; }
    public int BookingId { get; set; }
    public string StudentCode { get; set; } = null!;
    public decimal DiscountPercent { get; set; }
    public decimal DiscountAmount { get; set; }
    public DateTime UsedAt { get; set; }
    public string Status { get; set; } = null!;
    public StudentCardVerification Verification { get; set; } = null!;
    public Booking Booking { get; set; } = null!;
}
