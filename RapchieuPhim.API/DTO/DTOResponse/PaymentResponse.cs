namespace RapchieuPhim.API.DTOs.DTOResponse
{
    /// <summary>
    /// Response chi tiết một giao dịch thanh toán.
    /// </summary>
    public class PaymentResponse
    {
        public int     PaymentId      { get; set; }
        public int?    BookingId      { get; set; }
        public int?    OrderId        { get; set; }
        public int     UserId         { get; set; }
        public int?    StaffId        { get; set; }

        public string  PaymentMethod  { get; set; } = null!;
        public decimal SubTotal       { get; set; }   // Tiền trước giảm
        public decimal DiscountAmt    { get; set; }   // Số tiền giảm
        public decimal TotalAmount    { get; set; }   // Tiền thực tế phải trả

        public string? TransactionId  { get; set; }
        public DateTime CreatedAt     { get; set; }
        public DateTime? PaidAt       { get; set; }
        public string  PaymentStatus  { get; set; } = null!;
        public string? Notes          { get; set; }
        
        // URL ảnh QR Code thanh toán nếu phương thức là QrCode hoặc BankTransfer
        public string? QrCodeUrl          { get; set; }

        // Thông tin ngân hàng dạng chữ để hiển thị bên cạnh QR
        public string? BankId             { get; set; }
        public string? AccountNo          { get; set; }
        public string? AccountName        { get; set; }
        public string? PaymentDescription { get; set; }
        public string InvoiceCode { get; set; } = string.Empty;
        public decimal TicketTotal { get; set; }
        public decimal FoodTotal { get; set; }
        public List<RapchieuPhim.API.DTO.DTOResponse.OrderItemResponse> FoodItems { get; set; } = new();
    }
}
