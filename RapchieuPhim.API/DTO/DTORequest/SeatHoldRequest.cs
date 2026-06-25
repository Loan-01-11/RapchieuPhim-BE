using System.ComponentModel.DataAnnotations;
using RapchieuPhim.API.Constants;

namespace RapchieuPhim.API.DTO.DTORequest
{
    // ── Giữ ghế tạm thời ────────────────────────────────────────────────────────
    public class SeatHoldRequest
    {
        [Required(ErrorMessage = "Vui lòng chọn suất chiếu.")]
        public int ShowTimeId { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn ghế.")]
        public int SeatId { get; set; }
    }

    // ── Huỷ giữ ghế ─────────────────────────────────────────────────────────────
    public class SeatReleaseRequest
    {
        [Required(ErrorMessage = "HoldKey không được để trống.")]
        public string HoldKey { get; set; } = null!;
    }
}
