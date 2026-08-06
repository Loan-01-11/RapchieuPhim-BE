namespace RapchieuPhim.API.DTOs.DTOResponse
{
    public class BookingDetailResponse
    {
        public int BookingId { get; set; }
        public string CustomerName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string MovieTitle { get; set; } = null!;
        public string AreaName { get; set; } = null!;
        public string CinemaName { get; set; } = null!;
        public string RoomName { get; set; } = null!;
        public string? RoomType { get; set; }
        public string SeatCode { get; set; } = null!;
        public string SeatRow { get; set; } = null!;
        public string SeatNumber { get; set; } = null!;
        public string? SeatType { get; set; }
        public decimal Price { get; set; }
        public DateTime StartTime { get; set; }
        public decimal TicketPrice { get; set; }
        public decimal DiscountAmt { get; set; }
        public decimal TotalAmount { get; set; }
        public string BookingType { get; set; } = null!;
        public string Status { get; set; } = null!;
        public DateTime BookingDate { get; set; }
        public string? TicketCode { get; set; }

        public List<BookingFoodDetailResponse> Foods { get; set; } = new();
    }

    public class BookingFoodDetailResponse
    {
        public int FoodOrderDetailId { get; set; }
        public int? FoodId { get; set; }
        public int? ComboId { get; set; }
        public string ItemType { get; set; } = "FOOD";
        public string Name { get; set; } = null!;
        public string ItemNameSnapshot { get; set; } = null!;
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public decimal UnitPriceSnapshot { get; set; }
        public decimal LineTotal { get; set; }
        public List<RapchieuPhim.API.DTO.DTOResponse.OrderComboComponentResponse> ComboSelections { get; set; } = new();
        public bool ComboSelectionDataUnavailable { get; set; }
    }
}
