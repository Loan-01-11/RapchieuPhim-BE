using System.ComponentModel.DataAnnotations;
using RapchieuPhim.API.Constants;

namespace RapchieuPhim.API.DTO.DTORequest
{
    // ── Tạo ghế mới ─────────────────────────────────────────────────────────
    public class CreateSeatRequest
    {
        [Required(ErrorMessage = SeatMessages.RoomIdRequired)]
        public int RoomId { get; set; }

        [Required(ErrorMessage = SeatMessages.SeatRowRequired)]
        [MaxLength(5, ErrorMessage = SeatMessages.SeatRowMaxLength)]
        public string SeatRow { get; set; } = null!;

        [Required(ErrorMessage = SeatMessages.SeatNumberRequired)]
        [MaxLength(10, ErrorMessage = SeatMessages.SeatNumberMaxLength)]
        public string SeatNumber { get; set; } = null!;

        [Required(ErrorMessage = SeatMessages.SeatTypeRequired)]
        public string SeatType { get; set; } = null!;

        public bool IsActive { get; set; } = true;
    }

    // ── Tạo nhiều ghế cùng lúc (Batch) ─────────────────────────────────────
    public class CreateSeatBatchRequest
    {
        [Required(ErrorMessage = SeatMessages.RoomIdRequired)]
        public int RoomId { get; set; }

        /// <summary>
        /// Danh sách các hàng ghế cần tạo. Ví dụ: ["A", "B", "C"]
        /// </summary>
        [Required]
        public List<string> Rows { get; set; } = new();

        /// <summary>
        /// Số ghế trên mỗi hàng. Ví dụ: 10 → tạo ghế 1..10 cho mỗi hàng.
        /// </summary>
        [Required]
        [Range(1, 50, ErrorMessage = SeatMessages.SeatsPerRowRange)]
        public int SeatsPerRow { get; set; }

        [Required(ErrorMessage = SeatMessages.SeatTypeRequired)]
        public string SeatType { get; set; } = null!;
    }

    // ── Cập nhật thông tin ghế ───────────────────────────────────────────────
    public class UpdateSeatRequest
    {
        [Required(ErrorMessage = SeatMessages.SeatRowRequired)]
        [MaxLength(5, ErrorMessage = SeatMessages.SeatRowMaxLength)]
        public string SeatRow { get; set; } = null!;

        [Required(ErrorMessage = SeatMessages.SeatNumberRequired)]
        [MaxLength(10, ErrorMessage = SeatMessages.SeatNumberMaxLength)]
        public string SeatNumber { get; set; } = null!;

        [Required(ErrorMessage = SeatMessages.SeatTypeRequired)]
        public string SeatType { get; set; } = null!;

        public bool IsActive { get; set; }
    }

    // ── Đổi loại ghế hàng loạt ─────────────────────────────────────────────
    public class UpdateSeatTypeBatchRequest
    {
        [Required]
        public List<int> SeatIds { get; set; } = new();

        [Required(ErrorMessage = SeatMessages.SeatTypeRequired)]
        public string SeatType { get; set; } = null!;
    }

    // ── Bật/Tắt trạng thái ghế hàng loạt ───────────────────────────────────
    public class ToggleSeatStatusBatchRequest
    {
        [Required]
        public List<int> SeatIds { get; set; } = new();

        public bool IsActive { get; set; }
    }

    public class CreateSeatRangeRequest
    {
        [Required]
        public int RoomId { get; set; }
        [Required, MaxLength(5)]
        public string SeatRow { get; set; } = null!;
        [Range(1, 999)]
        public int FromSeat { get; set; }
        [Range(1, 999)]
        public int ToSeat { get; set; }
        [Required]
        public string SeatType { get; set; } = null!;
        public bool IsActive { get; set; } = true;
    }

    public class SeatLayoutChangeRequest
    {
        [Required]
        public int SeatId { get; set; }
        [Required]
        public string SeatType { get; set; } = null!;
        public bool IsActive { get; set; }
    }

    public class UpdateSeatLayoutRequest
    {
        [Required, MinLength(1)]
        public List<SeatLayoutChangeRequest> Changes { get; set; } = new();
    }
}
