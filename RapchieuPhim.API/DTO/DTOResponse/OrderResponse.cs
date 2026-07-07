namespace RapchieuPhim.API.DTO.DTOResponse
{
    // ─────────────────────────────────────────────────────────────────────────────
    // DTO PHẢN HỒI CHO ĐƠN HÀNG ĐỒ ĂN (Trả về cho Frontend)
    // ─────────────────────────────────────────────────────────────────────────────
    public class OrderResponse
    {
        public int OrderId        { get; set; }
        public int UserId         { get; set; }
        public string? UserName   { get; set; }  // Tên khách hàng đặt đồ ăn
        public int? BookingId     { get; set; }  // Đơn vé liên kết (nếu có)
        public int? StaffId       { get; set; }  // Nhân viên xử lý (nếu có)
        public string? StaffName  { get; set; }  // Tên nhân viên
        public int? DiscountId    { get; set; }
        public string? DiscountCode { get; set; } // Mã giảm giá đã áp dụng
        public DateTime OrderDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string OrderType   { get; set; } = null!; // "DineIn" | "Takeaway" | "Online"
        public string Status      { get; set; } = null!; // "Pending" | "Confirmed" | "Cancelled"

        // Danh sách chi tiết các món trong đơn
        public List<OrderItemResponse> Items { get; set; } = new();
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // DTO PHẢN HỒI CHO 1 DÒNG MÓN TRONG ĐƠN
    // ─────────────────────────────────────────────────────────────────────────────
    public class OrderItemResponse
    {
        public int OrderItemId  { get; set; }
        public int? FoodId      { get; set; }
        public string? FoodName { get; set; }   // Tên món ăn (nếu là Food)
        public int? ComboId     { get; set; }
        public string? ComboName { get; set; }  // Tên combo (nếu là Combo)
        public int Quantity      { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Subtotal  { get; set; }
    }
}
