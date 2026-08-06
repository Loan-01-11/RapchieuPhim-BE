using System.Data;
using Microsoft.EntityFrameworkCore;
using RapchieuPhim.API.DTO.DTORequest;
using RapchieuPhim.API.DTO.DTOResponse;
using RapchieuPhim.API.Models;

namespace RapchieuPhim.API.Services;

public interface IFoodInventoryService
{
    Task<List<FoodInventoryResponse>> GetAsync(int cinemaId, int userId, string role);
    Task ReceiveAsync(ReceiveFoodStockRequest request, int userId, string role);
    Task AdjustAsync(AdjustFoodStockRequest request, int userId, string role);
    Task TransferAsync(TransferFoodStockRequest request, int userId, string role);
    Task<(bool Inactivated, string Message)> DeleteFoodAsync(int cinemaId, int foodId, int userId, string role);
    Task<string> UpdateSaleStatusAsync(int cinemaId, int foodId, string saleStatus, int userId, string role);
    Task ValidateOrderAsync(int cinemaId, IEnumerable<(int? FoodId, int? ComboId, int Quantity)> items);
    Task DeductOrderAsync(int orderId, int? performedBy = null);
    Task ReturnOrderAsync(int orderId, int? performedBy = null);
}

public class FoodInventoryService : IFoodInventoryService
{
    private readonly CinemaManagementContext _context;
    public FoodInventoryService(CinemaManagementContext context) => _context = context;

    private async Task EnsureCinemaAccessAsync(int cinemaId, int userId, string role)
    {
        if (role == "Admin") return;
        var ownCinema = await _context.Users.Where(x => x.UserId == userId).Select(x => x.CinemaId).SingleOrDefaultAsync();
        if ((role != "Staff" && role != "Manager") || ownCinema != cinemaId)
            throw new UnauthorizedAccessException("Bạn không có quyền thao tác kho của rạp này.");
    }

    public async Task<List<FoodInventoryResponse>> GetAsync(int cinemaId, int userId, string role)
    {
        await EnsureCinemaAccessAsync(cinemaId, userId, role);
        return await _context.CinemaFoodInventories.AsNoTracking().Where(i => i.CinemaId == cinemaId)
            .Join(_context.Foods, i => i.FoodId, f => f.FoodId, (i, f) => new { f, inv = i })
            .Select(x => new FoodInventoryResponse {
                CinemaId = cinemaId, FoodId = x.f.FoodId, FoodName = x.f.FoodName, Category = x.f.Category,
                Price = x.f.Price, ImageUrl = x.f.ImageUrl, Quantity = x.inv.Quantity,
                MinStock = x.inv.MinStock, SaleStatus = x.inv.SaleStatus,
                StockStatus = x.inv.Quantity == 0 ? "OUT_OF_STOCK" : x.inv.Quantity <= x.inv.MinStock ? "LOW_STOCK" : "IN_STOCK",
                Status = x.inv.Status,
                IsAvailable = x.inv.SaleStatus == "ACTIVE" && x.inv.Quantity > 0,
                UpdatedAt = x.inv.UpdatedAt
            }).OrderBy(x => x.Category).ThenBy(x => x.FoodName).ToListAsync();
    }

    public async Task ReceiveAsync(ReceiveFoodStockRequest request, int userId, string role)
    {
        await EnsureCinemaAccessAsync(request.CinemaId, userId, role);
        if (!await _context.Foods.AnyAsync(x => x.FoodId == request.FoodId)) throw new KeyNotFoundException("Không tìm thấy món.");
        if (request.ExpirationDate.HasValue && request.ExpirationDate < DateOnly.FromDateTime(request.ReceivedAt.Date))
            throw new ArgumentException("Hạn sử dụng không được trước ngày nhập.");

        await using var tx = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var inv = await GetOrCreateAsync(request.CinemaId, request.FoodId);
        var before = inv.Quantity;
        inv.Quantity += request.Quantity;
        UpdateStatus(inv);
        var receipt = new FoodStockReceipt { CinemaId=request.CinemaId, FoodId=request.FoodId, Quantity=request.Quantity,
            UnitCost=request.UnitCost, Supplier=null, ReceivedAt=request.ReceivedAt == default ? DateTime.Now : request.ReceivedAt,
            ExpirationDate=request.ExpirationDate, Notes=request.Notes?.Trim(), CreatedBy=userId, CreatedAt=DateTime.Now };
        _context.FoodStockReceipts.Add(receipt);
        await _context.SaveChangesAsync();
        AddHistory(inv, "RECEIVE", request.Quantity, before, userId, "RECEIPT", checked((int)receipt.ReceiptId), request.UnitCost, null, request.ExpirationDate, request.Notes);
        await _context.SaveChangesAsync();
        await tx.CommitAsync();
    }

    public async Task AdjustAsync(AdjustFoodStockRequest request, int userId, string role)
    {
        await EnsureCinemaAccessAsync(request.CinemaId, userId, role);
        var allowed = new[] { "ADJUST", "DAMAGE", "RETURN", "TRANSFER" };
        var type = request.TransactionType.Trim().ToUpperInvariant();
        if (!allowed.Contains(type)) throw new ArgumentException("Loại thao tác kho không hợp lệ.");
        await using var tx = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var inv = await GetOrCreateAsync(request.CinemaId, request.FoodId);
        var before = inv.Quantity;
        if (before + request.QuantityChange < 0) throw new InvalidOperationException("Tồn kho không đủ; không thể để số lượng âm.");
        inv.Quantity += request.QuantityChange;
        inv.MinStock = request.MinStock;
        UpdateStatus(inv);
        AddHistory(inv, type, request.QuantityChange, before, userId, null, null, null, null, null, request.Notes);
        await _context.SaveChangesAsync();
        await tx.CommitAsync();
    }

    public async Task TransferAsync(TransferFoodStockRequest request, int userId, string role)
    {
        if (request.FromCinemaId == request.ToCinemaId) throw new ArgumentException("Rạp nhận phải khác rạp xuất.");
        await EnsureCinemaAccessAsync(request.FromCinemaId, userId, role);
        if (role != "Admin") throw new UnauthorizedAccessException("Chỉ Admin tổng được chuyển kho giữa các rạp.");
        await using var tx = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var source = await GetOrCreateAsync(request.FromCinemaId, request.FoodId);
        var target = await GetOrCreateAsync(request.ToCinemaId, request.FoodId);
        if (source.Quantity < request.Quantity) throw new InvalidOperationException("Kho xuất không đủ số lượng.");
        var sourceBefore = source.Quantity; var targetBefore = target.Quantity;
        source.Quantity -= request.Quantity; target.Quantity += request.Quantity;
        UpdateStatus(source); UpdateStatus(target);
        AddHistory(source, "TRANSFER", -request.Quantity, sourceBefore, userId, "CINEMA", request.ToCinemaId, notes: request.Notes);
        AddHistory(target, "TRANSFER", request.Quantity, targetBefore, userId, "CINEMA", request.FromCinemaId, notes: request.Notes);
        await _context.SaveChangesAsync(); await tx.CommitAsync();
    }

    public async Task<(bool Inactivated, string Message)> DeleteFoodAsync(int cinemaId, int foodId, int userId, string role)
    {
        await EnsureCinemaAccessAsync(cinemaId, userId, role);
        var inventory = await _context.CinemaFoodInventories.SingleOrDefaultAsync(x => x.CinemaId == cinemaId && x.FoodId == foodId)
            ?? throw new KeyNotFoundException("Món không tồn tại trong rạp đã chọn.");
        var hasSales = await _context.Orderitems.AnyAsync(x => x.FoodId == foodId && x.Order.CinemaId == cinemaId);
        var hasInventoryHistory = await _context.FoodInventoryTransactions.AnyAsync(x => x.CinemaId == cinemaId && x.FoodId == foodId)
            || await _context.FoodStockReceipts.AnyAsync(x => x.CinemaId == cinemaId && x.FoodId == foodId);
        var belongsToCombo = await _context.Combofoodmappings.AnyAsync(x => x.FoodId == foodId);

        await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        if (!hasSales && !hasInventoryHistory && !belongsToCombo)
        {
            _context.CinemaFoodInventories.Remove(inventory);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            return (false, "Đã xóa món khỏi rạp.");
        }

        inventory.SaleStatus = "INACTIVE";
        inventory.UpdatedAt = DateTime.Now;
        await _context.SaveChangesAsync();
        await transaction.CommitAsync();
        return (true, "Món đã có lịch sử hoặc thuộc combo nên đã chuyển sang Ngừng bán tại rạp này.");
    }

    public async Task<string> UpdateSaleStatusAsync(int cinemaId, int foodId, string saleStatus, int userId, string role)
    {
        await EnsureCinemaAccessAsync(cinemaId, userId, role);
        var normalized = saleStatus?.Trim().ToUpperInvariant();
        if (normalized != "ACTIVE" && normalized != "INACTIVE")
            throw new ArgumentException("Trạng thái bán chỉ chấp nhận ACTIVE hoặc INACTIVE.");
        if (!await _context.Cinemas.AnyAsync(x => x.CinemaId == cinemaId))
            throw new KeyNotFoundException("Không tìm thấy rạp.");
        var inventory = await _context.CinemaFoodInventories.SingleOrDefaultAsync(x => x.CinemaId == cinemaId && x.FoodId == foodId)
            ?? throw new KeyNotFoundException("Món không tồn tại tại rạp đã chọn.");
        inventory.SaleStatus = normalized;
        inventory.UpdatedAt = DateTime.Now;
        await _context.SaveChangesAsync();
        return normalized;
    }

    public Task DeductOrderAsync(int orderId, int? performedBy = null) => ApplyOrderAsync(orderId, false, performedBy);
    public Task ReturnOrderAsync(int orderId, int? performedBy = null) => ApplyOrderAsync(orderId, true, performedBy);

    public async Task ValidateOrderAsync(int cinemaId, IEnumerable<(int? FoodId, int? ComboId, int Quantity)> items)
    {
        var needs = new Dictionary<int, int>();
        foreach (var item in items)
        {
            if (item.FoodId.HasValue) needs[item.FoodId.Value] = needs.GetValueOrDefault(item.FoodId.Value) + item.Quantity;
            else if (item.ComboId.HasValue)
            {
                var parts = await _context.Combofoodmappings.AsNoTracking().Where(x => x.ComboId == item.ComboId.Value).ToListAsync();
                if (parts.Count == 0) throw new InvalidOperationException($"Combo {item.ComboId} chưa thiết lập thành phần.");
                foreach (var part in parts) needs[part.FoodId] = needs.GetValueOrDefault(part.FoodId) + part.Quantity * item.Quantity;
            }
        }
        foreach (var need in needs)
        {
            var inventory = await _context.CinemaFoodInventories.AsNoTracking()
                .Where(x => x.CinemaId == cinemaId && x.FoodId == need.Key)
                .Select(x => new { x.Quantity, x.SaleStatus }).SingleOrDefaultAsync();
            if (inventory == null || inventory.SaleStatus != "ACTIVE")
                throw new InvalidOperationException($"Món {need.Key} đang ngừng bán tại rạp.");
            if (inventory.Quantity < need.Value) throw new InvalidOperationException($"Món {need.Key} đã hết hoặc không đủ tồn kho tại rạp.");
        }
    }

    private async Task ApplyOrderAsync(int orderId, bool isReturn, int? performedBy)
    {
        var type = isReturn ? "RETURN" : "SALE";
        if (await _context.FoodInventoryTransactions.AnyAsync(x => x.ReferenceType == "ORDER" && x.ReferenceId == orderId && x.TransactionType == type)) return;
        var order = await _context.Orders.Include(x => x.Staff).Include(x => x.Booking).ThenInclude(x => x!.ShowTime).ThenInclude(x => x.Room)
            .Include(x => x.Orderitems).SingleOrDefaultAsync(x => x.OrderId == orderId) ?? throw new KeyNotFoundException("Không tìm thấy đơn đồ ăn.");
        var cinemaId = order.CinemaId ?? order.Booking?.ShowTime.Room.CinemaId ?? order.Staff?.CinemaId
            ?? throw new InvalidOperationException("Đơn hàng chưa xác định rạp, không thể cập nhật kho.");
        var needs = new Dictionary<int,int>();
        foreach (var item in order.Orderitems)
        {
            if (item.FoodId.HasValue) needs[item.FoodId.Value] = needs.GetValueOrDefault(item.FoodId.Value) + item.Quantity;
            else if (item.ComboId.HasValue)
            {
                if (!isReturn)
                {
                    var saleStatus = await _context.CinemaComboSettings.AsNoTracking()
                        .Where(x => x.CinemaId == cinemaId && x.ComboId == item.ComboId.Value)
                        .Select(x => x.SaleStatus).SingleOrDefaultAsync();
                    if (saleStatus == null)
                        saleStatus = await _context.Combos.AsNoTracking().Where(x => x.ComboId == item.ComboId.Value)
                            .Select(x => x.IsAvailable ? "ACTIVE" : "INACTIVE").SingleOrDefaultAsync();
                    if (saleStatus != "ACTIVE")
                        throw new InvalidOperationException("Combo hiện đã ngừng bán, vui lòng tải lại danh sách.");
                }
                var snapshots = OrderItemSnapshotHelper.Parse(item.ComboSelectionSnapshot).ComboSelections;
                if (snapshots?.Count > 0)
                {
                    foreach (var part in snapshots) needs[part.FoodId] = needs.GetValueOrDefault(part.FoodId) + part.Quantity;
                }
                else
                {
                    var parts = await _context.Combofoodmappings.Where(x => x.ComboId == item.ComboId).ToListAsync();
                    if (parts.Count == 0) throw new InvalidOperationException($"Combo {item.ComboId} chưa thiết lập thành phần kho.");
                    foreach (var part in parts) needs[part.FoodId] = needs.GetValueOrDefault(part.FoodId) + part.Quantity * item.Quantity;
                }
            }
        }
        foreach (var need in needs)
        {
            var inv = await _context.CinemaFoodInventories.SingleOrDefaultAsync(x => x.CinemaId == cinemaId && x.FoodId == need.Key)
                ?? throw new InvalidOperationException($"Món {need.Key} chưa được thiết lập kho tại rạp này.");
            var before = inv.Quantity;
            var delta = isReturn ? need.Value : -need.Value;
            if (!isReturn && inv.SaleStatus != "ACTIVE") throw new InvalidOperationException($"Món {inv.FoodId} đang ngừng bán tại rạp.");
            if (before + delta < 0) throw new InvalidOperationException($"Món {inv.FoodId} không đủ tồn kho.");
            inv.Quantity += delta;
            UpdateStatus(inv);
            AddHistory(inv, type, delta, before, performedBy, "ORDER", orderId);
        }
        await _context.SaveChangesAsync();
    }

    private async Task<CinemaFoodInventory> GetOrCreateAsync(int cinemaId, int foodId)
    {
        var inv = await _context.CinemaFoodInventories.SingleOrDefaultAsync(x => x.CinemaId == cinemaId && x.FoodId == foodId);
        if (inv != null) return inv;
        inv = new CinemaFoodInventory { CinemaId=cinemaId, FoodId=foodId, Quantity=0, MinStock=10, SaleStatus="ACTIVE", Status="OutOfStock", UpdatedAt=DateTime.Now };
        _context.CinemaFoodInventories.Add(inv);
        return inv;
    }
    private static void UpdateStatus(CinemaFoodInventory inv) { inv.Status = inv.Quantity == 0 ? "OutOfStock" : inv.Quantity <= inv.MinStock ? "LowStock" : "InStock"; inv.UpdatedAt=DateTime.Now; }
    private void AddHistory(CinemaFoodInventory inv,string type,int delta,int before,int? userId,string? refType=null,int? refId=null,decimal? cost=null,string? supplier=null,DateOnly? expiry=null,string? notes=null) =>
        _context.FoodInventoryTransactions.Add(new FoodInventoryTransaction { CinemaId=inv.CinemaId,FoodId=inv.FoodId,TransactionType=type,QuantityChange=delta,QuantityBefore=before,QuantityAfter=before+delta,UnitCost=cost,ReferenceType=refType,ReferenceId=refId,Supplier=supplier,ExpirationDate=expiry,Notes=notes?.Trim(),PerformedBy=userId,CreatedAt=DateTime.Now });
}
