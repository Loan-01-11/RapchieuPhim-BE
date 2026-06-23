using System.ComponentModel.DataAnnotations;
using RapchieuPhim.API.Constants;

namespace RapchieuPhim.API.DTO.DTORequest
{
    // ── Tạo suất chiếu mới ──────────────────────────────────────────────────────
    public class CreateShowtimeRequest
    {
        [Required(ErrorMessage = ShowtimeMessages.MovieIdRequired)]
        public int MovieId { get; set; }

        [Required(ErrorMessage = ShowtimeMessages.RoomIdRequired)]
        public int RoomId { get; set; }

        /// <summary>
        /// Ngày chiếu. Định dạng: yyyy-MM-dd. Ví dụ: 2026-07-01
        /// </summary>
        [Required(ErrorMessage = ShowtimeMessages.ShowDateRequired)]
        public string ShowDate { get; set; } = null!;

        /// <summary>
        /// Giờ bắt đầu. Định dạng: HH:mm. Ví dụ: 09:00
        /// </summary>
        [Required(ErrorMessage = ShowtimeMessages.StartTimeRequired)]
        public string StartTime { get; set; } = null!;

        /// <summary>
        /// Giờ kết thúc. Định dạng: HH:mm. Ví dụ: 11:00
        /// </summary>
        [Required(ErrorMessage = ShowtimeMessages.EndTimeRequired)]
        public string EndTime { get; set; } = null!;

        /// <summary>
        /// Giá vé cơ bản (VNĐ). Phải lớn hơn 1.000.
        /// </summary>
        [Required(ErrorMessage = ShowtimeMessages.BasePriceRequired)]
        [Range(1000, double.MaxValue, ErrorMessage = ShowtimeMessages.BasePriceTooLow)]
        public decimal BasePrice { get; set; }

        /// <summary>
        /// Trạng thái suất chiếu: Active | Cancelled | Completed.
        /// Mặc định là "Active".
        /// </summary>
        public string Status { get; set; } = "Active";
    }

    // ── Cập nhật suất chiếu ─────────────────────────────────────────────────────
    public class UpdateShowtimeRequest
    {
        [Required(ErrorMessage = ShowtimeMessages.MovieIdRequired)]
        public int MovieId { get; set; }

        [Required(ErrorMessage = ShowtimeMessages.RoomIdRequired)]
        public int RoomId { get; set; }

        /// <summary>
        /// Ngày chiếu. Định dạng: yyyy-MM-dd. Ví dụ: 2026-07-01
        /// </summary>
        [Required(ErrorMessage = ShowtimeMessages.ShowDateRequired)]
        public string ShowDate { get; set; } = null!;

        /// <summary>
        /// Giờ bắt đầu. Định dạng: HH:mm. Ví dụ: 09:00
        /// </summary>
        [Required(ErrorMessage = ShowtimeMessages.StartTimeRequired)]
        public string StartTime { get; set; } = null!;

        /// <summary>
        /// Giờ kết thúc. Định dạng: HH:mm. Ví dụ: 11:00
        /// </summary>
        [Required(ErrorMessage = ShowtimeMessages.EndTimeRequired)]
        public string EndTime { get; set; } = null!;

        [Required(ErrorMessage = ShowtimeMessages.BasePriceRequired)]
        [Range(1000, double.MaxValue, ErrorMessage = ShowtimeMessages.BasePriceTooLow)]
        public decimal BasePrice { get; set; }

        [Required(ErrorMessage = ShowtimeMessages.StatusRequired)]
        public string Status { get; set; } = null!;
    }
}
