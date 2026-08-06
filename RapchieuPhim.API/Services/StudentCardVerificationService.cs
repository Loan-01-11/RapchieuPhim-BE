using System.Data;
using Microsoft.EntityFrameworkCore;
using RapchieuPhim.API.DTOs;
using RapchieuPhim.API.Models;

namespace RapchieuPhim.API.Services;

public interface IStudentCardVerificationService
{
    Task<StudentVerificationResult> CreateAsync(CreateStudentCardVerificationRequest request, int staffId);
    Task<object?> GetStatusAsync(int id, int userId, string role);
    Task<object?> GetByBookingAsync(int bookingId, int userId, string role);
    Task CancelAsync(int id, int staffId);
    Task<object> GetAdminListAsync(StudentVerificationQuery query);
    Task<object?> GetDetailAsync(int id);
    Task<(Stream Stream, string ContentType)> OpenImageAsync(int id, int userId, string role);
    Task<StudentVerificationResult> ApproveAsync(int id, int adminId);
    Task<StudentVerificationResult> RejectAsync(int id, int adminId, string reason);
}

public class StudentCardVerificationService : IStudentCardVerificationService
{
    private static readonly HashSet<string> Extensions = new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".webp" };
    private static readonly HashSet<string> ContentTypes = new(StringComparer.OrdinalIgnoreCase) { "image/jpeg", "image/png", "image/webp" };
    private const decimal Percent = 15m;
    private readonly CinemaManagementContext _db;
    private readonly IWebHostEnvironment _environment;

    public StudentCardVerificationService(CinemaManagementContext db, IWebHostEnvironment environment)
    { _db = db; _environment = environment; }

    public async Task<StudentVerificationResult> CreateAsync(CreateStudentCardVerificationRequest request, int staffId)
    {
        var extension = Path.GetExtension(request.CardImage.FileName);
        if (request.CardImage.Length == 0 || request.CardImage.Length > 5 * 1024 * 1024 || !Extensions.Contains(extension) || !ContentTypes.Contains(request.CardImage.ContentType))
            throw new ArgumentException("Ảnh thẻ chỉ chấp nhận JPG, JPEG, PNG hoặc WEBP và tối đa 5 MB.");
        var booking = await _db.Bookings.Include(x => x.ShowTime).ThenInclude(x => x.Room).SingleOrDefaultAsync(x => x.BookingId == request.BookingId)
            ?? throw new KeyNotFoundException("Không tìm thấy booking.");
        if (booking.StaffId != staffId) throw new UnauthorizedAccessException("Booking không thuộc nhân viên hiện tại.");
        if (booking.Status.Equals("Cancelled", StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Booking đã bị hủy.");
        if (request.ExpiryDate < DateOnly.FromDateTime(DateTime.Today)) throw new ArgumentException("Thẻ sinh viên đã hết hạn.");
        if (await _db.StudentCardVerifications.AnyAsync(x => x.BookingId == request.BookingId && x.Status != "CANCELLED" && x.Status != "REJECTED"))
            throw new InvalidOperationException("Booking đã có thông tin xác minh thẻ sinh viên.");

        var studentCode = Normalize(request.StudentCode);
        var monthStart = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
        var monthEnd = monthStart.AddMonths(1);
        if (await _db.StudentDiscountUsages.CountAsync(x => x.StudentCode == studentCode && x.Status == "APPLIED" && x.UsedAt >= monthStart && x.UsedAt < monthEnd) >= 3)
            throw new InvalidOperationException("Mã sinh viên đã đủ 3 lượt trong tháng.");

        var bookingGroup = await _db.Bookings
            .Where(b => b.BookingDate == booking.BookingDate && b.ShowTimeId == booking.ShowTimeId &&
                        b.UserId == booking.UserId && b.StaffId == booking.StaffId && b.Status != "Cancelled")
            .ToListAsync();
        var appliedDiscount = bookingGroup.Sum(b => b.DiscountAmt);
        var currentTotal = bookingGroup.Sum(b => b.TotalAmount);

        await using var imageBuffer = new MemoryStream((int)request.CardImage.Length);
        await request.CardImage.CopyToAsync(imageBuffer);
        var imageData = imageBuffer.ToArray();

        await using var tx = await _db.Database.BeginTransactionAsync();
        var entity = new StudentCardVerification {
            BookingId = booking.BookingId, CustomerId = booking.UserId,
            StudentCode = studentCode, StudentName = request.StudentName?.Trim(), SchoolName = request.SchoolName?.Trim(),
            ExpiryDate = request.ExpiryDate, ImageData = imageData, ImageContentType = request.CardImage.ContentType,
            Status = "APPROVED", CinemaId = booking.ShowTime.Room.CinemaId,
            SubmittedByStaffId = staffId, SubmittedAt = DateTime.UtcNow, ReviewedAt = DateTime.UtcNow,
            DiscountPercent = Percent, DiscountAmount = appliedDiscount
        };
        _db.StudentCardVerifications.Add(entity);
        await _db.SaveChangesAsync();
        await tx.CommitAsync();
        return Result(entity, currentTotal + await FoodTotal(entity));
    }

    public async Task<object?> GetStatusAsync(int id, int userId, string role)
    { var v = await Allowed(id, userId, role); return v == null ? null : Result(v, await GroupTotal(v)); }

    public async Task<object?> GetByBookingAsync(int bookingId, int userId, string role)
    {
        var id = await _db.StudentCardVerifications.Where(x => x.BookingId == bookingId).OrderByDescending(x => x.SubmittedAt).Select(x => (int?)x.Id).FirstOrDefaultAsync();
        return id == null ? null : await GetStatusAsync(id.Value, userId, role);
    }

    public async Task CancelAsync(int id, int staffId)
    {
        var v = await _db.StudentCardVerifications.SingleOrDefaultAsync(x => x.Id == id) ?? throw new KeyNotFoundException("Không tìm thấy yêu cầu.");
        if (v.SubmittedByStaffId != staffId) throw new UnauthorizedAccessException();
        if (v.Status != "PENDING") throw new InvalidOperationException("Chỉ có thể hủy yêu cầu đang chờ duyệt.");
        v.Status = "CANCELLED"; v.ReviewedAt = DateTime.UtcNow; await _db.SaveChangesAsync();
    }

    public async Task<object> GetAdminListAsync(StudentVerificationQuery q)
    {
        var query = _db.StudentCardVerifications.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(q.Status)) query = query.Where(x => x.Status == q.Status.Trim().ToUpper());
        if (!string.IsNullOrWhiteSpace(q.StudentCode)) { var code = Normalize(q.StudentCode); query = query.Where(x => x.StudentCode.Contains(code)); }
        if (q.CinemaId.HasValue) query = query.Where(x => x.CinemaId == q.CinemaId);
        if (q.SubmittedFrom.HasValue) query = query.Where(x => x.SubmittedAt >= q.SubmittedFrom.Value.ToDateTime(TimeOnly.MinValue));
        if (q.SubmittedTo.HasValue) query = query.Where(x => x.SubmittedAt < q.SubmittedTo.Value.AddDays(1).ToDateTime(TimeOnly.MinValue));
        var total = await query.CountAsync();
        var rows = await query.OrderByDescending(x => x.SubmittedAt).Skip((q.Page - 1) * q.PageSize).Take(q.PageSize)
            .Select(x => new { x.Id, x.BookingId, x.StudentCode, x.SchoolName, x.ExpiryDate, x.CinemaId, CinemaName=x.Cinema.CinemaName,
                SubmittedBy=x.SubmittedByStaff.FullName, x.SubmittedAt, x.Status,
                MonthlyUsageCount=_db.StudentDiscountUsages.Count(u => u.StudentCode==x.StudentCode && u.Status=="APPLIED" && u.UsedAt.Year==DateTime.UtcNow.Year && u.UsedAt.Month==DateTime.UtcNow.Month) }).ToListAsync();
        return new { items=rows, total, q.Page, q.PageSize };
    }

    public async Task<object?> GetDetailAsync(int id)
    {
        var now=DateTime.UtcNow;
        return await _db.StudentCardVerifications.AsNoTracking().Where(x=>x.Id==id).Select(x=>new {
            x.Id,x.BookingId,x.StudentCode,x.StudentName,x.SchoolName,x.ExpiryDate,x.Status,x.CinemaId,CinemaName=x.Cinema.CinemaName,
            PurchaseTime=x.Booking.BookingDate,
            SubmittedBy=x.SubmittedByStaff.FullName,x.SubmittedAt,ReviewedBy=x.ReviewedByAdmin!=null?x.ReviewedByAdmin.FullName:null,x.ReviewedAt,x.RejectionReason,
            TotalTicketAmount=_db.Bookings.Where(b=>b.BookingDate==x.Booking.BookingDate&&b.ShowTimeId==x.Booking.ShowTimeId&&b.UserId==x.Booking.UserId&&b.StaffId==x.Booking.StaffId).Sum(b=>b.TicketPrice),
            ExpectedDiscountAmount=_db.Bookings.Where(b=>b.BookingDate==x.Booking.BookingDate&&b.ShowTimeId==x.Booking.ShowTimeId&&b.UserId==x.Booking.UserId&&b.StaffId==x.Booking.StaffId).Sum(b=>b.TicketPrice)*Percent/100m,
            MonthlyUsageCount=_db.StudentDiscountUsages.Count(u=>u.StudentCode==x.StudentCode&&u.Status=="APPLIED"&&u.UsedAt.Year==now.Year&&u.UsedAt.Month==now.Month)
        }).SingleOrDefaultAsync();
    }

    public async Task<(Stream Stream, string ContentType)> OpenImageAsync(int id, int userId, string role)
    {
        var v=await Allowed(id,userId,role) ?? throw new KeyNotFoundException();
        if (v.ImageData is { Length: > 0 })
            return (new MemoryStream(v.ImageData, writable: false), v.ImageContentType ?? "application/octet-stream");

        if (string.IsNullOrWhiteSpace(v.ImagePath)) throw new FileNotFoundException();
        var path=Path.Combine(_environment.ContentRootPath,"App_Data","student-cards",v.ImagePath);
        if(!File.Exists(path)) throw new FileNotFoundException();
        return (new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.Read), Path.GetExtension(path).ToLowerInvariant() switch { ".png"=>"image/png", ".webp"=>"image/webp", _=>"image/jpeg" });
    }

    public async Task<StudentVerificationResult> ApproveAsync(int id, int adminId)
    {
        await using var tx=await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var v=await _db.StudentCardVerifications.Include(x=>x.Booking).SingleOrDefaultAsync(x=>x.Id==id) ?? throw new KeyNotFoundException("Không tìm thấy yêu cầu.");
        if(v.Status!="PENDING") throw new InvalidOperationException("Yêu cầu không còn ở trạng thái PENDING.");
        if(v.ExpiryDate<DateOnly.FromDateTime(DateTime.Today)){v.Status="EXPIRED";await _db.SaveChangesAsync();await tx.CommitAsync();throw new InvalidOperationException("Thẻ sinh viên đã hết hạn.");}
        if(v.Booking.Status.Equals("Cancelled",StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Booking đã bị hủy.");
        if(await _db.Payments.AnyAsync(p=>p.BookingId==v.BookingId&&(p.PaymentStatus=="Success"||p.PaymentStatus=="Paid"))) throw new InvalidOperationException("Booking đã thanh toán.");
        var start=new DateTime(DateTime.UtcNow.Year,DateTime.UtcNow.Month,1); var end=start.AddMonths(1);
        if(await _db.StudentDiscountUsages.CountAsync(x=>x.StudentCode==v.StudentCode&&x.Status=="APPLIED"&&x.UsedAt>=start&&x.UsedAt<end)>=3) throw new InvalidOperationException("Mã sinh viên đã đủ 3 lượt trong tháng.");
        if(await _db.StudentDiscountUsages.AnyAsync(x=>x.BookingId==v.BookingId&&x.Status=="APPLIED")) throw new InvalidOperationException("Booking đã áp dụng ưu đãi sinh viên.");
        // Giá cuối đã được BookingService tính. Duyệt thẻ chỉ xác nhận hồ sơ,
        // tuyệt đối không trừ tiền Booking/Payment/Ticket thêm lần nữa.
        var group=await Group(v).ToListAsync();
        var amount=group.Sum(x=>x.DiscountAmt);
        v.Status="APPROVED";v.ReviewedByAdminId=adminId;v.ReviewedAt=DateTime.UtcNow;v.DiscountAmount=amount;
        await _db.SaveChangesAsync();await tx.CommitAsync(); return Result(v,group.Sum(x=>x.TotalAmount)+await FoodTotal(v));
    }

    public async Task<StudentVerificationResult> RejectAsync(int id,int adminId,string reason)
    { var v=await _db.StudentCardVerifications.SingleOrDefaultAsync(x=>x.Id==id)??throw new KeyNotFoundException();if(v.Status!="PENDING")throw new InvalidOperationException("Yêu cầu không còn ở trạng thái PENDING.");v.Status="REJECTED";v.RejectionReason=reason.Trim();v.ReviewedByAdminId=adminId;v.ReviewedAt=DateTime.UtcNow;await _db.SaveChangesAsync();return Result(v,null); }

    private static string Normalize(string value)=>value.Trim().ToUpperInvariant();
    private IQueryable<Booking> Group(StudentCardVerification v)=>_db.Bookings.Where(b=>b.BookingDate==v.Booking.BookingDate&&b.ShowTimeId==v.Booking.ShowTimeId&&b.UserId==v.Booking.UserId&&b.StaffId==v.Booking.StaffId&&b.Status!="Cancelled").OrderBy(b=>b.BookingId);
    private async Task<decimal> GroupTotal(StudentCardVerification v)=> (await Group(v).SumAsync(x=>x.TotalAmount))+await FoodTotal(v);
    private async Task<decimal> FoodTotal(StudentCardVerification v)=>await _db.Orders.Where(o=>o.BookingId==v.BookingId&&o.Status!="Cancelled").SumAsync(o=>(decimal?)o.TotalAmount)??0;
    private static StudentVerificationResult Result(StudentCardVerification v,decimal? total)=>new(v.Id,v.Status,v.DiscountPercent,v.DiscountAmount,total,v.ReviewedAt,v.RejectionReason);
    private async Task<StudentCardVerification?> Allowed(int id,int userId,string role)=>await _db.StudentCardVerifications.Include(x=>x.Booking).SingleOrDefaultAsync(x=>x.Id==id&&(role=="Admin"||x.SubmittedByStaffId==userId));
}
