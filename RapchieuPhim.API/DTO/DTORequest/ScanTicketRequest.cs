using System.ComponentModel.DataAnnotations;
using RapchieuPhim.API.Constants;

namespace RapchieuPhim.API.DTOs.DTORequest
{
    /// <summary>
    /// Request body dùng để soát vé QR tại cửa phòng chiếu.
    /// Nhân viên quét QR Code → gửi mã vé lên API Scan → hệ thống kiểm tra và cập nhật trạng thái.
    /// </summary>
    public class ScanTicketRequest
    {
        /// <summary>
        /// Mã vé trích xuất từ nội dung QR Code (ví dụ: TIC911720B).
        /// </summary>
        [Required(ErrorMessage = ValidationMessages.TicketCodeRequired)]
        public string TicketCode { get; set; } = null!;
    }
}
