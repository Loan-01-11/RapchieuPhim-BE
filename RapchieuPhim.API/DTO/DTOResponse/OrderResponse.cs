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
        public int? CinemaId      { get; set; }  // Chi nhánh của đơn hàng
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
        public int FoodOrderDetailId { get; set; }
        public int? FoodId      { get; set; }
        public string? FoodName { get; set; }   // Tên món ăn (nếu là Food)
        public int? ComboId     { get; set; }
        public string? ComboName { get; set; }  // Tên combo (nếu là Combo)
        public int Quantity      { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Subtotal  { get; set; }
        // Snapshot of the components selected by the customer for this combo.
        public List<OrderComboItemResponse> ComboItems { get; set; } = new();
        public List<OrderComboComponentResponse> ComboComponents { get; set; } = new();
        public string ItemType { get; set; } = "FOOD";
        public string ItemNameSnapshot { get; set; } = string.Empty;
        public decimal UnitPriceSnapshot { get; set; }
        public decimal LineTotal { get; set; }
        public List<OrderComboComponentResponse> ComboSelections { get; set; } = new();
        public bool ComboSelectionDataUnavailable { get; set; }
    }

    public class OrderComboComponentResponse
    {
        public int FoodId { get; set; }
        public string FoodName { get; set; } = null!;
        public string FoodNameSnapshot { get => FoodName; set => FoodName = value; }
        public string? Category { get; set; }
        public string? CategorySnapshot { get => Category; set => Category = value; }
        public int Quantity { get; set; }
        public decimal UnitPriceSnapshot { get; set; }
    }

    public class OrderComboItemResponse
    {
        public string ItemName { get; set; } = string.Empty;
        public string? FlavorName { get; set; }
        public string? SizeName { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }

    public class OrderItemSnapshotEnvelope
    {
        public string ItemNameSnapshot { get; set; } = string.Empty;
        public List<OrderComboComponentResponse> ComboSelections { get; set; } = new();
    }

    public static class OrderItemSnapshotHelper
    {
        private static readonly System.Text.Json.JsonSerializerOptions Options = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public static string Serialize(string itemName, IEnumerable<OrderComboComponentResponse>? selections = null) =>
            System.Text.Json.JsonSerializer.Serialize(new OrderItemSnapshotEnvelope
            {
                ItemNameSnapshot = itemName,
                ComboSelections = selections?.ToList() ?? new()
            }, Options);

        public static OrderItemSnapshotEnvelope Parse(string? json, string fallbackName = "")
        {
            if (string.IsNullOrWhiteSpace(json))
                return new OrderItemSnapshotEnvelope { ItemNameSnapshot = fallbackName };

            try
            {
                if (json.TrimStart().StartsWith("["))
                {
                    return new OrderItemSnapshotEnvelope
                    {
                        ItemNameSnapshot = fallbackName,
                        ComboSelections = System.Text.Json.JsonSerializer.Deserialize<List<OrderComboComponentResponse>>(json, Options) ?? new()
                    };
                }

                var snapshot = System.Text.Json.JsonSerializer.Deserialize<OrderItemSnapshotEnvelope>(json, Options) ?? new();
                if (string.IsNullOrWhiteSpace(snapshot.ItemNameSnapshot)) snapshot.ItemNameSnapshot = fallbackName;
                return snapshot;
            }
            catch
            {
                return new OrderItemSnapshotEnvelope { ItemNameSnapshot = fallbackName };
            }
        }
    }
}
