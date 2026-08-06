using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RapchieuPhim.API.Constants;
using RapchieuPhim.API.DTO.DTORequest;
using RapchieuPhim.API.DTO.DTOResponse;
using RapchieuPhim.API.Models;
using RapchieuPhim.API.Services;
using System.Security.Claims;

namespace RapchieuPhim.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin,Staff")]
    public class DailyRevenueController : ControllerBase
    {
        private readonly CinemaManagementContext _context;
        private readonly IStaffReportService _staffReportService;

        public DailyRevenueController(CinemaManagementContext context, IStaffReportService staffReportService)
        {
            _context = context;
            _staffReportService = staffReportService;
        }

        [HttpGet]
        public async Task<IActionResult> GetDailyRevenue([FromQuery] string date)
        {
            if (!DateOnly.TryParseExact(date, "yyyy-MM-dd", out var parsedDate))
            {
                return BadRequest(new { Message = "Định dạng ngày không hợp lệ, vui lòng dùng yyyy-MM-dd." });
            }

            var startOfDay = parsedDate.ToDateTime(TimeOnly.MinValue);
            var endOfDay = parsedDate.ToDateTime(TimeOnly.MaxValue);

            // Lấy toàn bộ giao dịch thanh toán thành công trong ngày
            var payments = await _context.Payments
                .Where(p => p.PaymentStatus == "Success" && p.CreatedAt >= startOfDay && p.CreatedAt <= endOfDay)
                .Include(p => p.User)
                .Include(p => p.Staff)
                .Include(p => p.Booking)
                    .ThenInclude(b => b.ShowTime)
                        .ThenInclude(s => s.Movie)
                .Include(p => p.Booking)
                    .ThenInclude(b => b.Seat)
                .Include(p => p.Order)
                    .ThenInclude(o => o.Orderitems)
                        .ThenInclude(oi => oi.Food)
                .Include(p => p.Order)
                    .ThenInclude(o => o.Orderitems)
                        .ThenInclude(oi => oi.Combo)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            var bills = new List<BillDetailResponse>();
            decimal totalTicketRevenue = 0;
            decimal totalConcessionRevenue = 0;
            decimal totalDiscount = 0;
            decimal totalOverallRevenue = 0;
            int totalTicketsCount = 0;

            foreach (var payment in payments)
            {
                var bill = new BillDetailResponse
                {
                    PaymentId = payment.PaymentId,
                    BillCode = $"BILL{payment.PaymentId:D6}",
                    PaymentDate = payment.CreatedAt,
                    CustomerName = payment.User?.FullName ?? "Khách vãng lai",
                    CustomerEmail = payment.User?.Email ?? "N/A",
                    StaffName = payment.Staff?.FullName ?? "Hệ thống Online",
                    PaymentMethod = payment.PaymentMethod,
                    DiscountAmt = payment.DiscountAmt,
                    TotalAmount = payment.TotalAmount
                };

                // Tìm toàn bộ danh sách ghế đặt cùng đợt (Booking batch)
                var rootBooking = payment.Booking;
                var ticketsInBill = new List<TicketBillDetail>();
                decimal ticketSubtotal = 0;

                if (rootBooking != null)
                {
                    var batchBookings = await _context.Bookings
                        .Where(b => b.UserId == rootBooking.UserId
                                 && b.ShowTimeId == rootBooking.ShowTimeId
                                 && b.BookingDate == rootBooking.BookingDate)
                        .Include(b => b.Seat)
                        .Include(b => b.ShowTime)
                            .ThenInclude(s => s.Movie)
                        .Include(b => b.ShowTime)
                            .ThenInclude(s => s.Room)
                        .ToListAsync();

                    foreach (var booking in batchBookings)
                    {
                        ticketsInBill.Add(new TicketBillDetail
                        {
                            BookingId = booking.BookingId,
                            MovieTitle = booking.ShowTime?.Movie?.Title ?? "N/A",
                            RoomName = booking.ShowTime?.Room?.RoomName ?? "N/A",
                            SeatNumber = booking.Seat != null ? $"{booking.Seat.SeatRow}{booking.Seat.SeatNumber}" : "N/A",
                            Showtime = booking.ShowTime?.StartTime ?? DateTime.MinValue,
                            Price = booking.TicketPrice
                        });
                        ticketSubtotal += booking.TicketPrice;
                    }
                }

                bill.Tickets = ticketsInBill;
                bill.TicketSubtotal = ticketSubtotal;
                totalTicketsCount += ticketsInBill.Count;

                // Load danh sách đồ ăn/combo
                var concessionsInBill = new List<ConcessionBillDetail>();
                decimal concessionSubtotal = 0;

                var relatedOrder = payment.Order;
                if (relatedOrder == null && rootBooking != null)
                {
                    relatedOrder = await _context.Orders.AsNoTracking()
                        .Include(o => o.Orderitems).ThenInclude(oi => oi.Food)
                        .Include(o => o.Orderitems).ThenInclude(oi => oi.Combo)
                        .Include(o => o.Orderitems).ThenInclude(oi => oi.ComboSelections)
                        .Where(o => o.BookingId == rootBooking.BookingId)
                        .OrderByDescending(o => o.OrderId)
                        .FirstOrDefaultAsync();
                }

                if (relatedOrder != null)
                {
                    foreach (var item in relatedOrder.Orderitems)
                    {
                        var currentName = item.Food?.FoodName ?? item.Combo?.ComboName ?? "Đồ ăn kèm";
                        var snapshot = OrderItemSnapshotHelper.Parse(item.ComboSelectionSnapshot, currentName);
                        var storedSelections = item.ComboSelections.Select(selection => new OrderComboComponentResponse
                        {
                            FoodId = selection.FoodId,
                            FoodName = selection.FoodNameSnapshot,
                            Category = selection.CategorySnapshot,
                            Quantity = selection.Quantity
                        }).ToList();
                        var selections = storedSelections.Count > 0 ? storedSelections : snapshot.ComboSelections;
                        concessionsInBill.Add(new ConcessionBillDetail
                        {
                            FoodOrderDetailId = item.OrderItemId,
                            FoodId = item.FoodId,
                            ComboId = item.ComboId,
                            ItemType = item.ComboId.HasValue ? "COMBO" : "FOOD",
                            ItemNameSnapshot = snapshot.ItemNameSnapshot,
                            Name = snapshot.ItemNameSnapshot,
                            Quantity = item.Quantity,
                            UnitPriceSnapshot = item.UnitPrice,
                            UnitPrice = item.UnitPrice,
                            LineTotal = item.Subtotal,
                            ComboSelections = selections,
                            ComboSelectionDataUnavailable = item.ComboId.HasValue && selections.Count == 0,
                            Subtotal = item.Subtotal
                        });
                        concessionSubtotal += item.Subtotal;
                    }
                }

                bill.Concessions = concessionsInBill;
                bill.ConcessionSubtotal = concessionSubtotal;
                bill.TotalAmount = Math.Max(0, ticketSubtotal + concessionSubtotal - payment.DiscountAmt);

                // Cộng dồn doanh thu tổng quát
                totalTicketRevenue += ticketSubtotal;
                totalConcessionRevenue += concessionSubtotal;
                totalDiscount += payment.DiscountAmt;
                totalOverallRevenue += bill.TotalAmount;

                bills.Add(bill);
            }

            var report = new DailyRevenueReportResponse
            {
                Date = date,
                TotalTicketRevenue = totalTicketRevenue,
                TotalConcessionRevenue = totalConcessionRevenue,
                TotalDiscount = totalDiscount,
                TotalOverallRevenue = totalOverallRevenue,
                TotalBillsCount = bills.Count,
                TotalTicketsCount = totalTicketsCount,
                Bills = bills
            };

            return Ok(report);
        }

        [HttpPost("SendReport")]
        public async Task<IActionResult> SendReport([FromBody] SendReportRequest request)
        {
            if (!DateOnly.TryParseExact(request.Date, "yyyy-MM-dd", out var parsedDate))
            {
                return BadRequest(new { Message = "Định dạng ngày không hợp lệ, vui lòng dùng yyyy-MM-dd." });
            }

            var startOfDay = parsedDate.ToDateTime(TimeOnly.MinValue);
            var endOfDay = parsedDate.ToDateTime(TimeOnly.MaxValue);

            // Lấy toàn bộ giao dịch thanh toán thành công trong ngày
            var payments = await _context.Payments
                .Where(p => p.PaymentStatus == "Success" && p.CreatedAt >= startOfDay && p.CreatedAt <= endOfDay)
                .Include(p => p.Booking)
                .Include(p => p.Order)
                .ToListAsync();

            decimal totalTicketRevenue = 0;
            decimal totalConcessionRevenue = 0;
            decimal totalRevenue = 0;
            int totalBookings = 0;
            int totalOrders = 0;

            foreach (var payment in payments)
            {
                decimal ticketSubtotal = 0;
                if (payment.BookingId.HasValue)
                {
                    var rootBooking = payment.Booking;
                    if (rootBooking != null)
                    {
                        var batchCount = await _context.Bookings
                            .CountAsync(b => b.UserId == rootBooking.UserId
                                     && b.ShowTimeId == rootBooking.ShowTimeId
                                     && b.BookingDate == rootBooking.BookingDate);
                        totalBookings += batchCount;

                        var ticketPrices = await _context.Bookings
                            .Where(b => b.UserId == rootBooking.UserId
                                     && b.ShowTimeId == rootBooking.ShowTimeId
                                     && b.BookingDate == rootBooking.BookingDate)
                            .SumAsync(b => b.TicketPrice);
                        ticketSubtotal = ticketPrices;
                    }
                }

                decimal concessionSubtotal = 0;
                if (payment.Order != null)
                {
                    concessionSubtotal = payment.Order.TotalAmount;
                    totalOrders++;
                }

                totalTicketRevenue += ticketSubtotal;
                totalConcessionRevenue += concessionSubtotal;
                totalRevenue += payment.TotalAmount;
            }

            // Lấy thông tin nhân viên gửi báo cáo
            int currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var staffUser = await _context.Users.FindAsync(currentUserId);
            if (staffUser == null)
            {
                return NotFound(new { Message = "Không tìm thấy thông tin nhân viên." });
            }

            // Tìm rạp phim dựa trên ca làm việc gần nhất của nhân viên
            var cinemaId = await _context.Staffshifts
                .Where(s => s.StaffId == currentUserId)
                .OrderByDescending(s => s.ShiftId)
                .Select(s => s.CinemaId)
                .FirstOrDefaultAsync();

            if (cinemaId == 0)
            {
                cinemaId = await _context.Cinemas.Select(c => c.CinemaId).FirstOrDefaultAsync();
            }

            if (cinemaId == 0)
            {
                return BadRequest(new { Message = "Không thể xác định rạp chiếu để tạo báo cáo." });
            }

            var shiftPrefix = !string.IsNullOrEmpty(request.ShiftName) ? $"[{request.ShiftName.ToUpper()}] " : "";
            var summaryText = $"{shiftPrefix}Báo cáo kết ca ngày {request.Date} tạo bởi {staffUser.FullName}:\n" +
                              $"- Tên ca: {request.ShiftName ?? "Ca làm việc"}\n" +
                              $"- Giờ gửi: {DateTime.Now:HH:mm:ss}\n" +
                              $"- Doanh thu vé: {totalTicketRevenue:N0}đ ({totalBookings} vé)\n" +
                              $"- Doanh thu bắp nước: {totalConcessionRevenue:N0}đ ({totalOrders} đơn hàng)\n" +
                              $"- Doanh thu Tiền mặt: {request.CashRevenue:N0}đ\n" +
                              $"- Doanh thu Chuyển khoản: {request.TransferRevenue:N0}đ\n" +
                              $"- Tổng doanh thu ca: {totalRevenue:N0}đ\n" +
                              $"- Kiểm kê két tiền: Ban đầu: {request.InitialCash:N0}đ | Thực tế: {request.ActualCash:N0}đ | Chênh lệch: {(request.CashDifference >= 0 ? "+" : "")}{request.CashDifference:N0}đ\n" +
                              $"- Ghi chú: {request.Notes ?? "Không có"}";

            var createReportRequest = new CreateStaffReportRequest
            {
                StaffId = currentUserId,
                CinemaId = cinemaId,
                ReportDate = request.Date,
                ShiftName = request.ShiftName,
                Summary = summaryText,
                TotalBookings = totalBookings,
                TotalOrders = totalOrders,
                TotalRevenue = totalRevenue,
                CashRevenue = request.CashRevenue,
                TransferRevenue = request.TransferRevenue,
                InitialCash = request.InitialCash,
                ActualCash = request.ActualCash,
                CashDifference = request.CashDifference
            };

            var result = await _staffReportService.CreateAsync(createReportRequest);
            return StatusCode(result.StatusCode, result.IsSuccess ? result.Data : new { result.Message });
        }
    }

    // DTOs dùng riêng cho báo cáo doanh thu hằng ngày
    public class DailyRevenueReportResponse
    {
        public string Date { get; set; } = string.Empty;
        public decimal TotalTicketRevenue { get; set; }
        public decimal TotalConcessionRevenue { get; set; }
        public decimal TotalDiscount { get; set; }
        public decimal TotalOverallRevenue { get; set; }
        public int TotalBillsCount { get; set; }
        public int TotalTicketsCount { get; set; }
        public List<BillDetailResponse> Bills { get; set; } = new();
    }

    public class BillDetailResponse
    {
        public int PaymentId { get; set; }
        public string BillCode { get; set; } = string.Empty;
        public DateTime PaymentDate { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public string StaffName { get; set; } = string.Empty;
        public string PaymentMethod { get; set; } = string.Empty;
        public decimal TicketSubtotal { get; set; }
        public decimal ConcessionSubtotal { get; set; }
        public decimal DiscountAmt { get; set; }
        public decimal TotalAmount { get; set; }
        public List<TicketBillDetail> Tickets { get; set; } = new();
        public List<ConcessionBillDetail> Concessions { get; set; } = new();
    }

    public class TicketBillDetail
    {
        public int BookingId { get; set; }
        public string MovieTitle { get; set; } = string.Empty;
        public string RoomName { get; set; } = string.Empty;
        public string SeatNumber { get; set; } = string.Empty;
        public DateTime Showtime { get; set; }
        public decimal Price { get; set; }
    }

    public class ConcessionBillDetail
    {
        public int FoodOrderDetailId { get; set; }
        public int? FoodId { get; set; }
        public int? ComboId { get; set; }
        public string ItemType { get; set; } = "FOOD";
        public string ItemNameSnapshot { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPriceSnapshot { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal LineTotal { get; set; }
        public decimal Subtotal { get; set; }
        public List<OrderComboComponentResponse> ComboSelections { get; set; } = new();
        public bool ComboSelectionDataUnavailable { get; set; }
    }

    public class SendReportRequest
    {
        public string Date { get; set; } = string.Empty;
        public string? ShiftName { get; set; }
        public string? Notes { get; set; }
        public decimal CashRevenue { get; set; } = 0;
        public decimal TransferRevenue { get; set; } = 0;
        public decimal InitialCash { get; set; } = 0;
        public decimal ActualCash { get; set; } = 0;
        public decimal CashDifference { get; set; } = 0;
    }
}
