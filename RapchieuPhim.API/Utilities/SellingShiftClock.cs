namespace RapchieuPhim.API.Utilities;

public sealed record SellingShift(int ShiftId, string Name, TimeOnly Start, TimeOnly End);

public static class SellingShiftClock
{
    public const string ClosedMessage = "Ngoài thời gian bán vé. Hệ thống chỉ hoạt động từ 08:00 đến 24:00.";
    private static readonly TimeZoneInfo VietnamTimeZone = ResolveVietnamTimeZone();

    public static DateTime GetVietnamNow() => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, VietnamTimeZone);
    public static SellingShift? GetCurrentShift() => GetCurrentShift(GetVietnamNow());
    public static bool IsSellingTime() => GetCurrentShift() is not null;

    public static SellingShift? GetCurrentShift(DateTime vietnamTime)
    {
        var time = TimeOnly.FromDateTime(vietnamTime);
        if (time >= new TimeOnly(8, 0) && time < new TimeOnly(16, 0))
            return new SellingShift(1, "Ca 1", new TimeOnly(8, 0), new TimeOnly(16, 0));
        if (time >= new TimeOnly(16, 0))
            return new SellingShift(2, "Ca 2", new TimeOnly(16, 0), TimeOnly.MaxValue);
        return null;
    }

    private static TimeZoneInfo ResolveVietnamTimeZone()
    {
        foreach (var id in new[] { "Asia/Ho_Chi_Minh", "SE Asia Standard Time" })
        {
            try { return TimeZoneInfo.FindSystemTimeZoneById(id); }
            catch (TimeZoneNotFoundException) { }
            catch (InvalidTimeZoneException) { }
        }
        return TimeZoneInfo.CreateCustomTimeZone("UTC+7", TimeSpan.FromHours(7), "UTC+7", "UTC+7");
    }
}
