using Microsoft.EntityFrameworkCore;
using RapchieuPhim.API.Constants;
using RapchieuPhim.API.DTO.DTORequest;
using RapchieuPhim.API.DTO.DTOResponse;
using RapchieuPhim.API.Models;

namespace RapchieuPhim.API.Services
{
    // ─────────────────────────────────────────────────────────────────────────────
    // INTERFACE: ĐỊNH NGHĨA CÁC PHƯƠNG THỨC CỦA ORDER SERVICE
    // ─────────────────────────────────────────────────────────────────────────────
    public interface IOrderService
    {
        Task<List<OrderResponse>> GetAllAsync();
        Task<OrderResponse?> GetByIdAsync(int id);
        Task<List<OrderResponse>> GetByUserAsync(int userId, int currentUserId, string currentRole);
        Task<List<OrderResponse>> GetByBookingAsync(int bookingId);
        Task<(bool IsSuccess, string Message, int StatusCode, OrderResponse? Data)> CreateAsync(OrderCreateRequest request, int currentUserId, string currentRole);
        Task<(bool IsSuccess, string Message, int StatusCode)> UpdateStatusAsync(int id, OrderStatusRequest request, string currentRole);
        Task<(bool IsSuccess, string Message, int StatusCode)> CancelAsync(int id, int currentUserId, string currentRole);
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // LỚP XỬ LÝ NGHIỆP VỤ ĐƠN HÀNG ĐỒ ĂN (ORDER SERVICE)
    // ─────────────────────────────────────────────────────────────────────────────
    public class OrderService : IOrderService
    {
        private readonly CinemaManagementContext _context;

        public OrderService(CinemaManagementContext context)
        {
            _context = context;
        }

        // ── Helper: Ánh xạ Order Entity → OrderResponse DTO ──────────────────────
        private static OrderResponse MapToResponse(Order o) => new()
        {
            OrderId       = o.OrderId,
            UserId        = o.UserId,
            UserName      = o.User?.FullName,
            BookingId     = o.BookingId,
            StaffId       = o.StaffId,
            StaffName     = o.Staff?.FullName,
            DiscountId    = o.DiscountId,
            DiscountCode  = o.Discount?.DiscountCode,
            OrderDate     = o.OrderDate,
            TotalAmount   = o.TotalAmount,
            OrderType     = o.OrderType,
            Status        = o.Status,
            CinemaId      = o.Booking?.ShowTime?.Room?.CinemaId ?? o.Staff?.CinemaId,
            Items         = o.Orderitems.Select(i => new OrderItemResponse
            {
                OrderItemId = i.OrderItemId,
                FoodId      = i.FoodId,
                FoodName    = i.Food?.FoodName,
                ComboId     = i.ComboId,
                ComboName   = i.Combo?.ComboName,
                Quantity    = i.Quantity,
                UnitPrice   = i.UnitPrice,
                Subtotal    = i.Subtotal
            }).ToList()
        };

        // ── Query với đầy đủ Include (Eager Loading) ─────────────────────────────
        private IQueryable<Order> QueryWithDetails() =>
            _context.Orders
                .Include(o => o.User)
                .Include(o => o.Staff)
                .Include(o => o.Discount)
                .Include(o => o.Booking)
                    .ThenInclude(b => b.ShowTime)
                        .ThenInclude(s => s.Room)
                .Include(o => o.Orderitems)
                    .ThenInclude(i => i.Food)
                .Include(o => o.Orderitems)
                    .ThenInclude(i => i.Combo);

        // ─────────────────────────────────────────────────────────────────────────
        // LẤY TOÀN BỘ DANH SÁCH ĐƠN HÀNG (Chỉ Admin + Staff)
        // ─────────────────────────────────────────────────────────────────────────
        public async Task<List<OrderResponse>> GetAllAsync()
        {
            var orders = await QueryWithDetails()
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
            return orders.Select(MapToResponse).ToList();
        }

        // ─────────────────────────────────────────────────────────────────────────
        // LẤY ĐƠN HÀNG THEO ID
        // ─────────────────────────────────────────────────────────────────────────
        public async Task<OrderResponse?> GetByIdAsync(int id)
        {
            var order = await QueryWithDetails().FirstOrDefaultAsync(o => o.OrderId == id);
            return order == null ? null : MapToResponse(order);
        }

        // ─────────────────────────────────────────────────────────────────────────
        // LẤY LỊCH SỬ ĐƠN HÀNG THEO KHÁCH HÀNG
        // Bảo mật: Khách chỉ xem được của chính mình, Admin/Staff xem được hết
        // ─────────────────────────────────────────────────────────────────────────
        public async Task<List<OrderResponse>> GetByUserAsync(int userId, int currentUserId, string currentRole)
        {
            if (currentRole == RoleConstants.Customer && userId != currentUserId)
                return new List<OrderResponse>();

            var orders = await QueryWithDetails()
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
            return orders.Select(MapToResponse).ToList();
        }

        // ─────────────────────────────────────────────────────────────────────────
        // LẤY DANH SÁCH ĐƠN HÀNG ĐỒ ĂN THEO ĐƠN VÉ (Để kết hợp thanh toán)
        // ─────────────────────────────────────────────────────────────────────────
        public async Task<List<OrderResponse>> GetByBookingAsync(int bookingId)
        {
            var orders = await QueryWithDetails()
                .Where(o => o.BookingId == bookingId)
                .ToListAsync();
            return orders.Select(MapToResponse).ToList();
        }

        // ─────────────────────────────────────────────────────────────────────────
        // TẠO MỚI ĐƠN HÀNG ĐỒ ĂN
        //
        // Luồng xử lý:
        //   1. Validate từng dòng món (phải có FoodId hoặc ComboId, không cả hai)
        //   2. Truy DB lấy đơn giá thực tế của từng món/combo
        //   3. Áp mã giảm giá (nếu có)
        //   4. Tính tổng tiền và lưu xuống DB
        //   5. Giảm số lượng kho (Stock) của các Combo (nếu có)
        // ─────────────────────────────────────────────────────────────────────────
        public async Task<(bool IsSuccess, string Message, int StatusCode, OrderResponse? Data)> CreateAsync(
            OrderCreateRequest request, int currentUserId, string currentRole)
        {
            // Bước 1: Validate từng dòng món trong đơn
            foreach (var item in request.Items)
            {
                if (!item.FoodId.HasValue && !item.ComboId.HasValue)
                    return (false, OrderMessages.ItemMustHaveFoodOrCombo, 400, null);

                if (item.FoodId.HasValue && item.ComboId.HasValue)
                    return (false, OrderMessages.ItemCannotHaveBoth, 400, null);
            }

            // Bước 2: Xác định người dùng và nhân viên thực hiện
            int? staffId = null;
            if (currentRole == RoleConstants.Admin || currentRole == RoleConstants.Staff)
                staffId = currentUserId;

            // Bước 3: Tính tiền từng dòng món bằng cách tra đơn giá thực tế trong DB
            var orderItems = new List<Orderitem>();
            decimal totalAmount = 0;

            foreach (var itemReq in request.Items)
            {
                decimal unitPrice;

                if (itemReq.FoodId.HasValue)
                {
                    // Lấy đơn giá Food từ DB
                    var food = await _context.Foods.FindAsync(itemReq.FoodId.Value);
                    if (food == null)
                        return (false, OrderMessages.FoodNotFoundWithId(itemReq.FoodId.Value), 404, null);

                    unitPrice = food.Price;
                    orderItems.Add(new Orderitem
                    {
                        FoodId    = food.FoodId,
                        ComboId   = null,
                        Quantity  = itemReq.Quantity,
                        UnitPrice = unitPrice,
                        Subtotal  = unitPrice * itemReq.Quantity
                    });
                }
                else // ComboId.HasValue
                {
                    // Lấy đơn giá Combo từ DB
                    var combo = await _context.Combos.FindAsync(itemReq.ComboId!.Value);
                    if (combo == null)
                        return (false, OrderMessages.ComboNotFoundWithId(itemReq.ComboId.Value), 404, null);

                    unitPrice = combo.Price;
                    orderItems.Add(new Orderitem
                    {
                        FoodId    = null,
                        ComboId   = combo.ComboId,
                        Quantity  = itemReq.Quantity,
                        UnitPrice = unitPrice,
                        Subtotal  = unitPrice * itemReq.Quantity
                    });
                }

                totalAmount += unitPrice * itemReq.Quantity;
            }

            // Bước 4: Áp mã giảm giá nếu có
            if (request.DiscountId.HasValue)
            {
                var discount = await _context.Discounts.FindAsync(request.DiscountId.Value);
                if (discount != null && discount.IsActive == true)
                {
                    if (discount.DiscountType == DiscountMessages.TypePercent)
                        totalAmount -= totalAmount * (discount.DiscountValue / 100);
                    else if (discount.DiscountType == DiscountMessages.TypeFixed)
                        totalAmount -= discount.DiscountValue;

                    if (totalAmount < 0) totalAmount = 0;
                }
            }

            // Bước 5: Tạo đơn hàng và lưu xuống DB
            var order = new Order
            {
                UserId      = currentUserId,
                BookingId   = request.BookingId,
                StaffId     = staffId,
                DiscountId  = request.DiscountId,
                OrderDate   = DateTime.Now,
                TotalAmount = totalAmount,
                OrderType   = request.OrderType.Trim(),
                Status      = OrderMessages.StatusPending,
                Orderitems  = orderItems
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            // Tải lại dữ liệu đầy đủ để trả về (bao gồm tên user, tên món)
            var created = await QueryWithDetails().FirstOrDefaultAsync(o => o.OrderId == order.OrderId);
            return (true, OrderMessages.CreateSuccess, 201, created == null ? null : MapToResponse(created));
        }

        // ─────────────────────────────────────────────────────────────────────────
        // CẬP NHẬT TRẠNG THÁI ĐƠN HÀNG (Admin / Staff duyệt hoặc hủy)
        // ─────────────────────────────────────────────────────────────────────────
        public async Task<(bool IsSuccess, string Message, int StatusCode)> UpdateStatusAsync(
            int id, OrderStatusRequest request, string currentRole)
        {
            if (currentRole != RoleConstants.Admin && currentRole != RoleConstants.Staff)
                return (false, OrderMessages.UnauthorizedStatus, 403);

            var order = await _context.Orders.FindAsync(id);
            if (order == null)
                return (false, OrderMessages.NotFoundWithId(id), 404);

            var validStatuses = new[] { OrderMessages.StatusPending, OrderMessages.StatusConfirmed, OrderMessages.StatusCancelled };
            if (!validStatuses.Contains(request.Status))
                return (false, OrderMessages.InvalidStatus, 400);

            order.Status = request.Status;
            await _context.SaveChangesAsync();
            return (true, OrderMessages.UpdateSuccess, 200);
        }

        // ─────────────────────────────────────────────────────────────────────────
        // HỦY ĐƠN HÀNG (Khách tự hủy khi đơn còn Pending)
        // ─────────────────────────────────────────────────────────────────────────
        public async Task<(bool IsSuccess, string Message, int StatusCode)> CancelAsync(
            int id, int currentUserId, string currentRole)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null)
                return (false, OrderMessages.NotFoundWithId(id), 404);

            // Bảo mật: Khách chỉ hủy được đơn của chính mình
            if (currentRole == RoleConstants.Customer && order.UserId != currentUserId)
                return (false, OrderMessages.UnauthorizedCancel, 403);

            // Chặn hủy đơn đã được xác nhận
            if (order.Status == OrderMessages.StatusConfirmed)
                return (false, OrderMessages.CannotCancelConfirmed, 409);

            order.Status = OrderMessages.StatusCancelled;
            await _context.SaveChangesAsync();
            return (true, OrderMessages.CancelSuccess, 200);
        }
    }
}
