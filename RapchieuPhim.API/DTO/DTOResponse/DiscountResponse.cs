namespace RapchieuPhim.API.DTOs.DTOResponse
{
    public class DiscountResponse
    {
        public int DiscountId { get; set; }
        public string DiscountCode { get; set; } = null!;
        public string? Description { get; set; }
        public string DiscountType { get; set; } = null!;
        public decimal DiscountValue { get; set; }
        public decimal MinOrderAmount { get; set; }
        public int? MaxUsageTotal { get; set; }
        public int MaxUsagePerUser { get; set; }
        public int UsedCount { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool IsActive { get; set; }
        public int CreatedBy { get; set; }
    }
}
