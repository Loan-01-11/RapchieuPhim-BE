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

        [Required(ErrorMessage = ShowtimeMessages.StartTimeRequired)]
        public DateTime StartTime { get; set; }

        /// <summary>
        /// Giá vé cơ bản (VNĐ). Phải lớn hơn 0.
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

        [Required(ErrorMessage = ShowtimeMessages.StartTimeRequired)]
        public DateTime StartTime { get; set; }

        [Required(ErrorMessage = ShowtimeMessages.BasePriceRequired)]
        [Range(1000, double.MaxValue, ErrorMessage = ShowtimeMessages.BasePriceTooLow)]
        public decimal BasePrice { get; set; }

        [Required(ErrorMessage = ShowtimeMessages.StatusRequired)]
        public string Status { get; set; } = null!;
    }
}
