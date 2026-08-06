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
        Task<List<OrderResponse>> GetAllAsync(string? date = null);
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
        private readonly IFoodInventoryService _inventory;

        public OrderService(CinemaManagementContext context, IFoodInventoryService inventory)
        {
            _context = context;
            _inventory = inventory;
        }

        private static List<OrderComboComponentResponse> ParseComboComponents(string? json)
        {
            return OrderItemSnapshotHelper.Parse(json).ComboSelections;
        }

        private static string ComponentGroup(string? category)
        {
            var value = (category ?? "").ToLowerInvariant();
            if (value.Contains("nước")) return "DRINK";
            if (value.Contains("bắp")) return "POPCORN";
            return "OTHER";
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
            OrderType     = o.OrderType ?? "Staff",
            Status        = o.Status ?? "Pending",
            CinemaId      = o.CinemaId ?? o.Booking?.ShowTime?.Room?.CinemaId ?? o.Staff?.CinemaId,
            Items         = (o.Orderitems ?? new List<Orderitem>()).Select(i =>
            {
                var currentName = i.Food?.FoodName ?? i.Combo?.ComboName ?? "Đồ ăn kèm";
                var snapshot = OrderItemSnapshotHelper.Parse(i.ComboSelectionSnapshot, currentName);
                var storedSelections = i.ComboSelections.Select(selection => new OrderComboComponentResponse
                {
                    FoodId = selection.FoodId,
                    FoodName = selection.FoodNameSnapshot,
                    Category = selection.CategorySnapshot,
                    Quantity = selection.Quantity
                }).ToList();
                var selections = storedSelections.Count > 0 ? storedSelections : snapshot.ComboSelections;
                var comboItems = selections.Select(selection =>
                {
                    // Old selection rows do not contain a price; preserve the price
                    // captured in the order snapshot instead of using today's catalog.
                    var snapshotSelection = snapshot.ComboSelections.FirstOrDefault(x => x.FoodId == selection.FoodId);
                    return new OrderComboItemResponse
                    {
                        ItemName = selection.FoodName,
                        Quantity = selection.Quantity,
                        UnitPrice = selection.UnitPriceSnapshot != 0
                            ? selection.UnitPriceSnapshot
                            : snapshotSelection?.UnitPriceSnapshot ?? 0
                    };
                }).ToList();
                return new OrderItemResponse
                {
                OrderItemId = i.OrderItemId,
                FoodOrderDetailId = i.OrderItemId,
                FoodId      = i.FoodId,
                FoodName    = i.Food?.FoodName,
                ComboId     = i.ComboId,
                ComboName   = i.Combo?.ComboName,
                Quantity    = i.Quantity,
                UnitPrice   = i.UnitPrice,
                Subtotal    = i.Subtotal,
                ItemType = i.ComboId.HasValue ? "COMBO" : "FOOD",
                ItemNameSnapshot = snapshot.ItemNameSnapshot,
                UnitPriceSnapshot = i.UnitPrice,
                LineTotal = i.Subtotal,
                ComboItems = comboItems,
                ComboComponents = selections,
                ComboSelections = selections,
                ComboSelectionDataUnavailable = i.ComboId.HasValue && selections.Count == 0
                };
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
                    .ThenInclude(i => i.Combo)
                .Include(o => o.Orderitems)
                    .ThenInclude(i => i.ComboSelections);

        // ─────────────────────────────────────────────────────────────────────────
        // LẤY TOÀN BỘ DANH SÁCH ĐƠN HÀNG (Chỉ Admin + Staff)
        // ─────────────────────────────────────────────────────────────────────────
        public async Task<List<OrderResponse>> GetAllAsync(string? date = null)
        {
            var query = QueryWithDetails();

            if (!string.IsNullOrEmpty(date) && DateTime.TryParse(date, out var parsedDate))
            {
                var start = parsedDate.Date;
                var end = start.AddDays(1);
                query = query.Where(o => o.OrderDate >= start && o.OrderDate < end);
            }

            var orders = await query
                .OrderByDescending(o => o.OrderDate)
                .Take(500)
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

            var cinemaId = request.CinemaId;
            if (request.BookingId.HasValue)
                cinemaId = await _context.Bookings.Where(x => x.BookingId == request.BookingId.Value)
                    .Select(x => (int?)x.ShowTime.Room.CinemaId).SingleOrDefaultAsync();
            if (!cinemaId.HasValue && staffId.HasValue)
                cinemaId = await _context.Users.Where(x => x.UserId == staffId.Value).Select(x => x.CinemaId).SingleOrDefaultAsync();
            if (!cinemaId.HasValue)
                return (false, "Phải xác định rạp bán hàng.", 400, null);
            if (currentRole == RoleConstants.Staff)
            {
                var staffCinemaId = await _context.Users.Where(x => x.UserId == currentUserId).Select(x => x.CinemaId).SingleOrDefaultAsync();
                if (staffCinemaId != cinemaId) return (false, "Nhân viên chỉ được bán hàng tại rạp của mình.", 403, null);
            }
            var inventoryChecks = new List<(int? FoodId, int? ComboId, int Quantity)>();
            foreach (var requestItem in request.Items)
            {
                if (!requestItem.ComboId.HasValue)
                {
                    inventoryChecks.Add((requestItem.FoodId, requestItem.ComboId, requestItem.Quantity));
                    continue;
                }

                var comboConfig = await _context.Combos.AsNoTracking().Include(x => x.Combofoodmappings).ThenInclude(x => x.Food)
                    .SingleOrDefaultAsync(x => x.ComboId == requestItem.ComboId.Value);
                var comboSaleStatus = await _context.CinemaComboSettings.Where(x => x.CinemaId == cinemaId.Value && x.ComboId == requestItem.ComboId.Value).Select(x => x.SaleStatus).SingleOrDefaultAsync();
                comboSaleStatus ??= comboConfig?.IsAvailable == true ? "ACTIVE" : "INACTIVE";
                if (comboConfig == null)
                    return (false, "Combo không tồn tại.", 400, null);
                if (comboSaleStatus != "ACTIVE")
                    return (false, "Combo hiện đã ngừng bán, vui lòng tải lại danh sách.", 400, null);
                if (requestItem.SelectedComponents == null || requestItem.SelectedComponents.Count == 0)
                    return (false, "Vui lòng chọn đủ thành phần cho Combo.", 400, null);
                var configured = comboConfig.Combofoodmappings.ToList();
                var selectedIds = requestItem.SelectedComponents.Select(x => x.FoodId).ToList();
                var selectedFoods = await _context.Foods.AsNoTracking().Where(x => selectedIds.Contains(x.FoodId)).ToDictionaryAsync(x => x.FoodId);
                if (configured.Count == 0 || selectedFoods.Count != selectedIds.Distinct().Count())
                    return (false, "Thành phần Combo lựa chọn không hợp lệ.", 400, null);
                var selectedGroups = requestItem.SelectedComponents.GroupBy(x => ComponentGroup(selectedFoods[x.FoodId].Category)).ToDictionary(g => g.Key, g => g.Sum(x => x.Quantity));
                var allowedIds = configured.Select(x => x.FoodId).ToHashSet();
                if (selectedIds.Any(x => !allowedIds.Contains(x)) ||
                    selectedGroups.GetValueOrDefault("DRINK") != comboConfig.DrinkSlotCount * requestItem.Quantity ||
                    selectedGroups.GetValueOrDefault("POPCORN") != comboConfig.PopcornSlotCount * requestItem.Quantity ||
                    selectedGroups.Keys.Any(x => x != "DRINK" && x != "POPCORN"))
                    return (false, "Số lượng nước hoặc bắp lựa chọn không đúng cấu hình Combo.", 400, null);
                inventoryChecks.AddRange(requestItem.SelectedComponents.Select(x => ((int?)x.FoodId, (int?)null, x.Quantity)));
            }
            await _inventory.ValidateOrderAsync(cinemaId.Value, inventoryChecks);

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
                        Subtotal  = unitPrice * itemReq.Quantity,
                        ComboSelectionSnapshot = OrderItemSnapshotHelper.Serialize(food.FoodName)
                    });
                }
                else // ComboId.HasValue
                {
                    // Lấy đơn giá Combo từ DB
                    var combo = await _context.Combos.Include(x => x.Combofoodmappings).ThenInclude(x => x.Food)
                        .SingleOrDefaultAsync(x => x.ComboId == itemReq.ComboId!.Value);
                    if (combo == null)
                        return (false, OrderMessages.ComboNotFoundWithId(itemReq.ComboId.Value), 404, null);

                    unitPrice = combo.Price;
                    List<OrderComboComponentResponse> snapshotComponents;
                    if (itemReq.SelectedComponents?.Count > 0)
                    {
                        var selectedIds = itemReq.SelectedComponents.Select(x => x.FoodId).ToList();
                        var selectedFoods = await _context.Foods.AsNoTracking().Where(x => selectedIds.Contains(x.FoodId)).ToDictionaryAsync(x => x.FoodId);
                        snapshotComponents = itemReq.SelectedComponents.Select(x => new OrderComboComponentResponse
                        {
                            FoodId = x.FoodId, FoodName = selectedFoods[x.FoodId].FoodName, Category = selectedFoods[x.FoodId].Category,
                            Quantity = x.Quantity,
                            UnitPriceSnapshot = selectedFoods[x.FoodId].Price
                        }).ToList();
                    }
                    else
                    {
                        snapshotComponents = combo.Combofoodmappings.Select(x => new OrderComboComponentResponse
                        {
                            FoodId = x.FoodId, FoodName = x.Food.FoodName, Category = x.Food.Category, Quantity = x.Quantity * itemReq.Quantity
                        }).ToList();
                    }
                    orderItems.Add(new Orderitem
                    {
                        FoodId    = null,
                        ComboId   = combo.ComboId,
                        Quantity  = itemReq.Quantity,
                        UnitPrice = unitPrice,
                        Subtotal  = unitPrice * itemReq.Quantity,
                        ComboSelectionSnapshot = OrderItemSnapshotHelper.Serialize(combo.ComboName, snapshotComponents),
                        ComboSelections = snapshotComponents.Select(selection => new OrderComboSelection
                        {
                            ComboId = combo.ComboId,
                            FoodId = selection.FoodId,
                            FoodNameSnapshot = selection.FoodName,
                            CategorySnapshot = selection.Category,
                            Quantity = selection.Quantity,
                            CreatedAt = DateTime.Now
                        }).ToList()
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
                CinemaId    = cinemaId,
                OrderDate   = DateTime.Now,
                TotalAmount = totalAmount,
                OrderType   = (request.OrderType ?? "Staff").Trim(),
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

            await using var transaction = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
            if (request.Status == OrderMessages.StatusConfirmed && order.Status != OrderMessages.StatusConfirmed)
                await _inventory.DeductOrderAsync(order.OrderId);
            if (request.Status == OrderMessages.StatusCancelled && order.Status == OrderMessages.StatusConfirmed)
                await _inventory.ReturnOrderAsync(order.OrderId);
            order.Status = request.Status;
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
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
            await using var transaction = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
            if (order.Status == OrderMessages.StatusConfirmed)
                await _inventory.ReturnOrderAsync(order.OrderId, currentUserId);

            order.Status = OrderMessages.StatusCancelled;
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            return (true, OrderMessages.CancelSuccess, 200);
        }
    }
}
