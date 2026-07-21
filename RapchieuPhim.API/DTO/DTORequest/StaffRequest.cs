using System.ComponentModel.DataAnnotations;

namespace RapchieuPhim.API.DTO.DTORequest
{
    // ── Mở ca làm việc mới ──────────────────────────────────────────────────
    public class CreateStaffShiftRequest
    {
        [Required(ErrorMessage = "StaffId là bắt buộc.")]
        public int StaffId { get; set; }

        [Required(ErrorMessage = "CinemaId là bắt buộc.")]
        public int CinemaId { get; set; }

        public string? ShiftName { get; set; } // "Ca 1 (08:00 - 16:00)" | "Ca 2 (16:00 - 24:00)"
    }

    // ── Đóng ca – ghi nhận kết quả cuối ca ──────────────────────────────────
    public class CloseStaffShiftRequest
    {
        public string? ShiftName { get; set; }
        public int TotalBookings { get; set; } = 0;
        public int TotalOrders   { get; set; } = 0;
        public decimal TotalRevenue { get; set; } = 0;
        public decimal CashRevenue { get; set; } = 0;
        public decimal TransferRevenue { get; set; } = 0;
        public decimal InitialCash { get; set; } = 0;
        public decimal ActualCash { get; set; } = 0;
        public decimal CashDifference { get; set; } = 0;
        public string? Summary { get; set; }
    }

    // ── Tạo báo cáo ca làm việc ─────────────────────────────────────────────
    public class CreateStaffReportRequest
    {
        [Required(ErrorMessage = "StaffId là bắt buộc.")]
        public int StaffId { get; set; }

        [Required(ErrorMessage = "CinemaId là bắt buộc.")]
        public int CinemaId { get; set; }

        [Required(ErrorMessage = "ReportDate là bắt buộc (yyyy-MM-dd).")]
        public string ReportDate { get; set; } = string.Empty;

        public string? ShiftName { get; set; }
        public string? Summary { get; set; }
        public int TotalBookings { get; set; } = 0;
        public int TotalOrders   { get; set; } = 0;
        public decimal TotalRevenue { get; set; } = 0;
        public decimal CashRevenue { get; set; } = 0;
        public decimal TransferRevenue { get; set; } = 0;
        public decimal InitialCash { get; set; } = 0;
        public decimal ActualCash { get; set; } = 0;
        public decimal CashDifference { get; set; } = 0;
    }
}
