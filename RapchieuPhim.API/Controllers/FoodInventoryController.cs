using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RapchieuPhim.API.DTO.DTORequest;
using RapchieuPhim.API.Models;
using RapchieuPhim.API.Services;

namespace RapchieuPhim.API.Controllers;

[ApiController, Route("api/food-inventory"), Authorize(Roles = "Admin,Staff,Manager")]
public class FoodInventoryController : ControllerBase
{
    private readonly IFoodInventoryService _service;
    private readonly RapchieuPhim.API.Models.CinemaManagementContext _context;
    public FoodInventoryController(IFoodInventoryService service, RapchieuPhim.API.Models.CinemaManagementContext context) { _service = service; _context = context; }
    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
    private string Role => User.FindFirstValue(ClaimTypes.Role) ?? "";

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] int cinemaId) => Ok(await _service.GetAsync(cinemaId, UserId, Role));

    [HttpGet("menu"), AllowAnonymous]
    public async Task<IActionResult> Menu([FromQuery] int cinemaId)
    {
        if (!await _context.Cinemas.AsNoTracking().AnyAsync(x => x.CinemaId == cinemaId))
            return NotFound(new { message = "Không tìm thấy rạp." });
        var foods = await LoadCinemaFoodsAsync(cinemaId);
        var combos = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync(
            _context.Combos.AsNoTracking().Include(x => x.Combofoodmappings).ThenInclude(x => x.Food));
        var comboStatuses = await _context.CinemaComboSettings.AsNoTracking().Where(x => x.CinemaId == cinemaId).ToDictionaryAsync(x => x.ComboId, x => x.SaleStatus);
        var comboResult = combos.Select(c => {
            var quantity = CalculateComboQuantity(c, foods);
            var saleStatus = comboStatuses.GetValueOrDefault(c.ComboId, c.IsAvailable ? "ACTIVE" : "INACTIVE");
            var valid = saleStatus == "ACTIVE" && c.Combofoodmappings.Count > 0;
            return new { c.ComboId, c.ComboName, c.Price, c.Description, c.ImageUrl, c.AllowsCustomization, c.DrinkSlotCount, c.PopcornSlotCount, Quantity = quantity, SaleStatus = saleStatus, IsAvailable = valid && quantity > 0,
                Status = saleStatus == "INACTIVE" ? "Inactive" : quantity == 0 ? "OutOfComponents" : "InStock",
                FoodItems = c.Combofoodmappings.Select(p => new { p.FoodId, p.Food.FoodName, p.Food.Category, ItemType = FoodGroup(p.Food.Category) }).ToList() };
        });
        var isManagementView = User.Identity?.IsAuthenticated == true && Role is "Admin" or "Manager";
        return Ok(new {
            foods = isManagementView ? foods : foods.Where(x => x.IsAvailable).ToList(),
            combos = isManagementView ? comboResult : comboResult.Where(x => x.IsAvailable).ToList()
        });
    }

    [HttpGet("/api/cinemas/{cinemaId:int}/food-inventory")]
    public async Task<IActionResult> CinemaInventory(int cinemaId) => Ok(await BuildMenuAsync(cinemaId));

    [HttpGet("/api/cinemas/{cinemaId:int}/food-statistics")]
    public async Task<IActionResult> CinemaStatistics(int cinemaId, [FromQuery] string? period, [FromQuery] DateTime? date)
    {
        await _service.GetAsync(cinemaId, UserId, Role);
        return Ok(await BuildSalesAsync(cinemaId, period, date));
    }

    [HttpGet("/api/cinemas/{cinemaId:int}/food-revenue")]
    public async Task<IActionResult> CinemaRevenue(int cinemaId, [FromQuery] string? period, [FromQuery] DateTime? date)
    {
        await _service.GetAsync(cinemaId, UserId, Role);
        var sales = await BuildSalesAsync(cinemaId, period, date);
        return Ok(new { cinemaId, totalSold = sales.Sum(x => x.Quantity), totalRevenue = sales.Sum(x => x.Revenue) });
    }

    [HttpGet("/api/cinemas/{cinemaId:int}/top-selling-foods")]
    public async Task<IActionResult> TopSelling(int cinemaId, [FromQuery] string? period, [FromQuery] DateTime? date)
    {
        await _service.GetAsync(cinemaId, UserId, Role);
        return Ok((await BuildSalesAsync(cinemaId, period, date)).OrderByDescending(x => x.Quantity).ThenByDescending(x => x.Revenue).Take(10));
    }

    [HttpDelete("/api/cinemas/{cinemaId:int}/foods/{foodId:int}"), Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> DeleteFood(int cinemaId, int foodId)
    {
        var result = await _service.DeleteFoodAsync(cinemaId, foodId, UserId, Role);
        return Ok(new { message = result.Message, inactivated = result.Inactivated, cinemaId, foodId });
    }

    [HttpPatch("/api/cinemas/{cinemaId:int}/foods/{foodId:int}/status"), Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> UpdateFoodStatus(int cinemaId, int foodId, [FromBody] UpdateFoodSaleStatusRequest request)
    {
        var status = await _service.UpdateSaleStatusAsync(cinemaId, foodId, request.SaleStatus, UserId, Role);
        return Ok(new { message = status == "ACTIVE" ? "Đã bật bán món tại rạp." : "Đã ngừng bán món tại rạp.", cinemaId, foodId, saleStatus = status });
    }

    [HttpPatch("/api/cinemas/{cinemaId:int}/combos/{comboId:int}/status"), Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> UpdateComboStatus(int cinemaId, int comboId, [FromBody] UpdateFoodSaleStatusRequest request)
    {
        var saleStatus = (request.SaleStatus ?? "").Trim().ToUpperInvariant();
        if (saleStatus is not ("ACTIVE" or "INACTIVE")) return BadRequest(new { message = "Trạng thái chỉ nhận ACTIVE hoặc INACTIVE." });
        if (!await _context.Cinemas.AnyAsync(x => x.CinemaId == cinemaId)) return NotFound(new { message = "Không tìm thấy rạp." });
        if (Role != "Admin")
        {
            var ownCinema = await _context.Users.Where(x => x.UserId == UserId).Select(x => x.CinemaId).SingleOrDefaultAsync();
            if (ownCinema != cinemaId) return Forbid();
        }
        var combo = await _context.Combos.Include(x => x.Combofoodmappings).ThenInclude(x => x.Food).SingleOrDefaultAsync(x => x.ComboId == comboId);
        if (combo == null) return NotFound(new { message = "Không tìm thấy Combo." });
        if (saleStatus == "ACTIVE")
        {
            if (combo.DrinkSlotCount <= 0 || combo.PopcornSlotCount <= 0) return BadRequest(new { message = "Combo chưa cấu hình số lượng nước và bắp hợp lệ." });
            var inventoryRows = await _context.CinemaFoodInventories.Where(x => x.CinemaId == cinemaId).ToListAsync();
            var inventoryIds = inventoryRows.Select(x => x.FoodId).ToList();
            if (!combo.Combofoodmappings.Any(x => inventoryIds.Contains(x.FoodId) && FoodGroup(x.Food.Category) == "DRINK") || !combo.Combofoodmappings.Any(x => inventoryIds.Contains(x.FoodId) && FoodGroup(x.Food.Category) == "POPCORN"))
                return BadRequest(new { message = "Rạp chưa có đủ danh sách nước và bắp được phép cho Combo." });
            var sellableIds = inventoryRows.Where(x => x.SaleStatus == "ACTIVE" && x.Quantity > 0).Select(x => x.FoodId).ToHashSet();
            if (!combo.Combofoodmappings.Any(x => sellableIds.Contains(x.FoodId) && FoodGroup(x.Food.Category) == "DRINK") || !combo.Combofoodmappings.Any(x => sellableIds.Contains(x.FoodId) && FoodGroup(x.Food.Category) == "POPCORN"))
                return BadRequest(new { message = "Combo hiện không còn đủ lựa chọn nước và bắp để bật bán." });
        }
        var setting = await _context.CinemaComboSettings.SingleOrDefaultAsync(x => x.CinemaId == cinemaId && x.ComboId == comboId);
        if (setting == null) _context.CinemaComboSettings.Add(setting = new CinemaComboSetting { CinemaId=cinemaId, ComboId=comboId });
        setting.SaleStatus = saleStatus; setting.UpdatedAt = DateTime.Now;
        await _context.SaveChangesAsync();
        return Ok(new { message = saleStatus == "ACTIVE" ? "Đã bật bán Combo tại rạp." : "Đã ngừng bán Combo tại rạp.", cinemaId, comboId, saleStatus });
    }

    [HttpPost("receive")]
    public async Task<IActionResult> Receive([FromBody] ReceiveFoodStockRequest request)
    {
        await _service.ReceiveAsync(request, UserId, Role);
        return Ok(new { message = "Nhập hàng thành công.", data = await _service.GetAsync(request.CinemaId, UserId, Role) });
    }

    [HttpPost("adjust")]
    public async Task<IActionResult> Adjust([FromBody] AdjustFoodStockRequest request)
    {
        await _service.AdjustAsync(request, UserId, Role);
        return Ok(new { message = "Cập nhật tồn kho thành công.", data = await _service.GetAsync(request.CinemaId, UserId, Role) });
    }

    [HttpPost("transfer"), Authorize(Roles = "Admin")]
    public async Task<IActionResult> Transfer([FromBody] TransferFoodStockRequest request)
    {
        await _service.TransferAsync(request, UserId, Role);
        return Ok(new { message = "Chuyển kho thành công." });
    }

    [HttpGet("history")]
    public async Task<IActionResult> History([FromQuery] int cinemaId, [FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        await _service.GetAsync(cinemaId, UserId, Role);
        var query = _context.FoodInventoryTransactions.AsNoTracking().Where(x => x.CinemaId == cinemaId);
        if (from.HasValue) query = query.Where(x => x.CreatedAt >= from.Value);
        if (to.HasValue) query = query.Where(x => x.CreatedAt < to.Value.AddDays(1));
        return Ok(await query.OrderByDescending(x => x.CreatedAt).Take(1000).ToListAsync());
    }

    [HttpGet("alerts")]
    public async Task<IActionResult> Alerts([FromQuery] int cinemaId)
    {
        var inventory = await _service.GetAsync(cinemaId, UserId, Role);
        return Ok(inventory.Where(x => x.Quantity <= x.MinStock));
    }

    private async Task<object> BuildMenuAsync(int cinemaId)
    {
        var foods = await _service.GetAsync(cinemaId, UserId, Role);
        var combos = await _context.Combos.AsNoTracking().Include(x => x.Combofoodmappings).ThenInclude(x => x.Food).ToListAsync();
        var comboStatuses = await _context.CinemaComboSettings.AsNoTracking().Where(x => x.CinemaId == cinemaId).ToDictionaryAsync(x => x.ComboId, x => x.SaleStatus);
        var comboResult = combos.Select(c => {
            var quantity = CalculateComboQuantity(c, foods);
            var saleStatus = comboStatuses.GetValueOrDefault(c.ComboId, c.IsAvailable ? "ACTIVE" : "INACTIVE");
            var valid = saleStatus == "ACTIVE" && c.Combofoodmappings.Count > 0;
            return new { c.ComboId, c.ComboName, c.Price, c.Description, c.ImageUrl, c.AllowsCustomization, c.DrinkSlotCount, c.PopcornSlotCount, Quantity = quantity,
                SaleStatus = saleStatus, IsAvailable = valid && quantity > 0, Status = saleStatus == "INACTIVE" ? "Inactive" : quantity == 0 ? "OutOfComponents" : "InStock",
                FoodItems = c.Combofoodmappings.Select(p => new { p.FoodId, p.Food.FoodName, p.Food.Category, ItemType = FoodGroup(p.Food.Category) }).ToList() };
        }).ToList();
        return new { cinemaId, foods, combos = comboResult };
    }

    private Task<List<RapchieuPhim.API.DTO.DTOResponse.FoodInventoryResponse>> LoadCinemaFoodsAsync(int cinemaId)
        => _context.CinemaFoodInventories.AsNoTracking()
            .Where(i => i.CinemaId == cinemaId)
            .Join(_context.Foods.AsNoTracking(), i => i.FoodId, f => f.FoodId, (i, f) => new { f, inv = i })
            .Select(x => new RapchieuPhim.API.DTO.DTOResponse.FoodInventoryResponse {
                CinemaId = cinemaId, FoodId = x.f.FoodId, FoodName = x.f.FoodName, Category = x.f.Category,
                Price = x.f.Price, ImageUrl = x.f.ImageUrl, Quantity = x.inv.Quantity,
                MinStock = x.inv.MinStock, SaleStatus = x.inv.SaleStatus,
                StockStatus = x.inv.Quantity == 0 ? "OUT_OF_STOCK" : x.inv.Quantity <= x.inv.MinStock ? "LOW_STOCK" : "IN_STOCK",
                Status = x.inv.Status, IsAvailable = x.inv.SaleStatus == "ACTIVE" && x.inv.Quantity > 0,
                UpdatedAt = x.inv.UpdatedAt
            }).OrderBy(x => x.Category).ThenBy(x => x.FoodName).ToListAsync();

    private static string FoodGroup(string? category)
    {
        var value = (category ?? "").ToLowerInvariant();
        if (value.Contains("nước")) return "DRINK";
        if (value.Contains("bắp")) return "POPCORN";
        return "OTHER";
    }

    private static int CalculateComboQuantity(RapchieuPhim.API.Models.Combo combo, List<RapchieuPhim.API.DTO.DTOResponse.FoodInventoryResponse> foods)
    {
        // Tồn kho và trạng thái bán là hai khái niệm riêng. Combo ngừng bán vẫn
        // phải hiển thị đúng số lượng có thể tạo từ các món thành phần.
        if (combo.Combofoodmappings.Count == 0) return 0;
        var capacities = new List<int>();
        if (combo.DrinkSlotCount > 0)
            capacities.Add(foods.Where(x => x.SaleStatus == "ACTIVE" && FoodGroup(x.Category) == "DRINK" && combo.Combofoodmappings.Any(m => m.FoodId == x.FoodId)).Sum(x => x.Quantity) / combo.DrinkSlotCount);
        if (combo.PopcornSlotCount > 0)
            capacities.Add(foods.Where(x => x.SaleStatus == "ACTIVE" && FoodGroup(x.Category) == "POPCORN" && combo.Combofoodmappings.Any(m => m.FoodId == x.FoodId)).Sum(x => x.Quantity) / combo.PopcornSlotCount);
        return capacities.Count == 0 ? 0 : capacities.Min();
    }

    private async Task<List<FoodSaleStat>> BuildSalesAsync(int cinemaId, string? period, DateTime? selectedDate)
    {
        var (from, to) = ResolvePeriod(period, selectedDate);
        var query = _context.Orderitems.AsNoTracking().Where(x => x.Order.CinemaId == cinemaId &&
            (x.Order.Status == "Confirmed" || x.Order.Status == "Completed" || x.Order.Status == "Paid") &&
            x.Order.OrderDate >= from && x.Order.OrderDate < to);
        return await query.GroupBy(x => new { x.FoodId, x.ComboId }).Select(g => new FoodSaleStat {
            FoodId = g.Key.FoodId, ComboId = g.Key.ComboId, Quantity = g.Sum(x => x.Quantity), Revenue = g.Sum(x => x.Subtotal)
        }).ToListAsync();
    }

    private static (DateTime From, DateTime To) ResolvePeriod(string? period, DateTime? selectedDate)
    {
        if (selectedDate.HasValue) return (selectedDate.Value.Date, selectedDate.Value.Date.AddDays(1));
        var now = DateTime.Now;
        return period?.ToLowerInvariant() switch {
            "today" => (now.Date, now.Date.AddDays(1)),
            "week" => (now.Date.AddDays(-(((int)now.DayOfWeek + 6) % 7)), now.Date.AddDays(-(((int)now.DayOfWeek + 6) % 7)).AddDays(7)),
            _ => (new DateTime(now.Year, now.Month, 1), new DateTime(now.Year, now.Month, 1).AddMonths(1))
        };
    }

    private sealed class FoodSaleStat
    {
        public int? FoodId { get; set; }
        public int? ComboId { get; set; }
        public int Quantity { get; set; }
        public decimal Revenue { get; set; }
    }
}
