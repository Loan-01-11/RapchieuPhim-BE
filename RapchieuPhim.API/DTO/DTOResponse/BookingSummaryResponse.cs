namespace RapchieuPhim.API.DTOs.DTOResponse
{
    /// <summary>
    /// Response tổng hợp sau khi đặt vé thành công —
    /// bao gồm thông tin vé, đồ ăn/combo đặt kèm và tổng tiền phải thanh toán.
    /// </summary>
    public class BookingSummaryResponse
    {
        // ── Thông tin vé ──────────────────────────────────────────────────────────
        public List<int> BookingIds { get; set; } = new();   // Các BookingId vừa tạo (mỗi ghế = 1 booking)
        public int? OrderId { get; set; }                    // OrderId nếu có đặt đồ ăn

        // ── Chi tiết tiền vé ──────────────────────────────────────────────────────
        public decimal TicketTotal          { get; set; }   // Tổng tiền vé (trước giảm giá)
        public decimal DiscountAmt          { get; set; }   // Số tiền được giảm
        public decimal TicketAfterDiscount  { get; set; }   // Tiền vé sau khi áp mã giảm giá

        // ── Chi tiết đồ ăn / combo ────────────────────────────────────────────────
        public decimal FoodTotal            { get; set; }   // Tổng tiền đồ ăn / combo
        public List<OrderItemSummary> OrderItems { get; set; } = new();

        // ── TỔNG TIỀN PHẢI THANH TOÁN ─────────────────────────────────────────────
        public decimal GrandTotal           { get; set; }   // = TicketAfterDiscount + FoodTotal
    }

    /// <summary>
    /// Thông tin từng dòng đồ ăn / combo trong đơn hàng.
    /// </summary>
    public class OrderItemSummary
    {
        public string Name      { get; set; } = null!;   // Tên món ăn hoặc tên combo
        public int? FoodId      { get; set; }
        public int? ComboId     { get; set; }
        public decimal UnitPrice { get; set; }
        public int Quantity      { get; set; }
        public decimal Subtotal  { get; set; }           // = UnitPrice × Quantity
    }
}
