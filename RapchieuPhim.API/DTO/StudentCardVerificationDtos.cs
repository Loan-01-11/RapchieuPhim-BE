using System.ComponentModel.DataAnnotations;

namespace RapchieuPhim.API.DTOs;

public class CreateStudentCardVerificationRequest
{
    [Required] public int BookingId { get; set; }
    [Required, StringLength(50)] public string StudentCode { get; set; } = null!;
    [StringLength(150)] public string? StudentName { get; set; }
    [StringLength(200)] public string? SchoolName { get; set; }
    [Required] public DateOnly ExpiryDate { get; set; }
    [Required] public IFormFile CardImage { get; set; } = null!;
}

public class RejectStudentCardVerificationRequest
{
    [Required, StringLength(500, MinimumLength = 3)] public string Reason { get; set; } = null!;
}

public record StudentVerificationResult(int VerificationId, string Status, decimal DiscountPercent,
    decimal DiscountAmount, decimal? NewTotalAmount, DateTime? ReviewedAt, string? RejectionReason);

public class StudentVerificationQuery
{
    public string? Status { get; set; } = "PENDING";
    public string? StudentCode { get; set; }
    public int? CinemaId { get; set; }
    public DateOnly? SubmittedFrom { get; set; }
    public DateOnly? SubmittedTo { get; set; }
    [Range(1, int.MaxValue)] public int Page { get; set; } = 1;
    [Range(1, 100)] public int PageSize { get; set; } = 20;
}
