using Microsoft.Extensions.Caching.Memory;
using RapchieuPhim.API.Constants;
using RapchieuPhim.API.Models;

namespace RapchieuPhim.API.Services
{
    // ── Record lưu thông tin 1 lần giữ ghế ─────────────────────────────────────
    public record SeatHoldInfo(int UserId, int ShowTimeId, int SeatId, DateTime HeldUntil);

    public interface ISeatHoldService
    {
        /// <summary>Giữ ghế tạm thời. Trả về holdKey nếu thành công, null nếu ghế đã được giữ.</summary>
        (bool IsSuccess, string Message, string? HoldKey) HoldSeat(int userId, int showTimeId, int seatId);

        /// <summary>Huỷ giữ ghế theo holdKey.</summary>
        (bool IsSuccess, string Message) ReleaseHold(string holdKey, int userId);

        /// <summary>Kiểm tra ghế còn trống không (không bị giữ và không bị booked).</summary>
        bool IsSeatHeld(int showTimeId, int seatId);

        /// <summary>Lấy danh sách SeatId đang bị giữ của 1 suất chiếu.</summary>
        List<int> GetHeldSeatIds(int showTimeId);
    }

    public class SeatHoldService : ISeatHoldService
    {
        private readonly IMemoryCache _cache;
        // Thời gian giữ ghế (phút)
        private const int HoldMinutes = 5;
        // Prefix key cache
        private const string HoldPrefix = "seat_hold:";
        private const string UserHoldsPrefix = "user_holds:";

        public SeatHoldService(IMemoryCache cache)
        {
            _cache = cache;
        }

        // ── Tạo cache key cho 1 ghế trong 1 suất chiếu ──────────────────────────
        // Dùng để kiểm tra xung đột nhanh: ai đó đang hold ghế này chưa?
        private static string SeatKey(int showTimeId, int seatId)
            => $"{HoldPrefix}{showTimeId}:{seatId}";

        // ── Tạo holdKey duy nhất trả về cho client sau khi giữ ghế thành công ───
        // Client dùng holdKey này khi gọi DELETE /Hold để huỷ giữ ghế
        private static string NewHoldKey(int showTimeId, int seatId)
            => $"{showTimeId}_{seatId}_{Guid.NewGuid():N}";

        // ─────────────────────────────────────────────────────────────────────
        // 1. GIỮ GHẾ (HoldSeat)
        // Chiến lược dual-key cache:
        //   - seatKey  → lưu theo ghế, dùng để check xung đột nhanh O(1)
        //   - holdKey  → lưu theo lần giữ, dùng để client release đúng ghế
        // Cả hai entry đều có cùng AbsoluteExpiration = 5 phút
        // ─────────────────────────────────────────────────────────────────────
        public (bool IsSuccess, string Message, string? HoldKey) HoldSeat(int userId, int showTimeId, int seatId)
        {
            var seatKey = SeatKey(showTimeId, seatId);

            // Kiểm tra ghế đã bị giữ chưa
            // → nếu chính user này đang giữ → báo đã giữ rồi
            // → nếu user khác đang giữ → báo ghế đang bận kèm thời hạn hết giữ
            if (_cache.TryGetValue(seatKey, out SeatHoldInfo? existing))
            {
                if (existing!.UserId == userId)
                    return (false, SeatHoldMessages.AlreadyHeldBySelf, null);

                return (false, SeatHoldMessages.HeldByOther(existing.HeldUntil.ToString("HH:mm:ss")), null);
            }

            var holdKey   = NewHoldKey(showTimeId, seatId);
            var heldUntil = DateTime.Now.AddMinutes(HoldMinutes);
            var holdInfo  = new SeatHoldInfo(userId, showTimeId, seatId, heldUntil);

            // Cache tự động xoá sau 5 phút (AbsoluteExpiration)
            var options = new MemoryCacheEntryOptions
            {
                AbsoluteExpiration = heldUntil
            };

            // Lưu theo seatKey để kiểm tra xung đột nhanh khi ai khác muốn giữ cùng ghế
            _cache.Set(seatKey, holdInfo, options);
            // Lưu theo holdKey để client có thể huỷ giữ ghế trước khi hết 5 phút
            _cache.Set($"{HoldPrefix}key:{holdKey}", holdInfo, options);

            return (true, SeatHoldMessages.HoldSuccess(HoldMinutes, heldUntil.ToString("HH:mm:ss")), holdKey);
        }

        // ─────────────────────────────────────────────────────────────────────
        // 2. HUỶ GIỮ GHẾ (ReleaseHold)
        // Client gửi holdKey nhận được từ POST /Hold để huỷ trước khi hết 5 phút.
        // Chỉ user sở hữu holdKey mới được huỷ — không thể huỷ của người khác.
        // Sau khi huỷ, xoá cả 2 entry (holdKey + seatKey) khỏi cache.
        // ─────────────────────────────────────────────────────────────────────
        public (bool IsSuccess, string Message) ReleaseHold(string holdKey, int userId)
        {
            var fullKey = $"{HoldPrefix}key:{holdKey}";

            // Không tìm thấy → đã hết hạn 5 phút hoặc holdKey sai
            if (!_cache.TryGetValue(fullKey, out SeatHoldInfo? info))
                return (false, SeatHoldMessages.HoldNotFound);

            // Kiểm tra quyền: chỉ đúng chủ nhân mới được huỷ
            if (info!.UserId != userId)
                return (false, SeatHoldMessages.UnauthorizedRelease);

            // Xoá cả 2 entry để ghế trở về trạng thái Available ngay lập tức
            _cache.Remove(fullKey);
            _cache.Remove(SeatKey(info.ShowTimeId, info.SeatId));

            return (true, SeatHoldMessages.ReleaseSuccess);
        }

        // 3. KIỂM TRA GHẾ CÓ ĐANG BỊ GIỮ KHÔNG
        public bool IsSeatHeld(int showTimeId, int seatId)
            => _cache.TryGetValue(SeatKey(showTimeId, seatId), out _);

        // 4. LẤY TẤT CẢ GHẾ ĐANG BỊ GIỮ CỦA 1 SUẤT CHIẾU
        // (Dùng trong GetSeatsByShowtime để gắn status = "Held")
        public List<int> GetHeldSeatIds(int showTimeId)
        {
            // MemoryCache không hỗ trợ scan theo prefix → dùng pattern nhận diện qua key prefix
            // Workaround: lưu thêm 1 Set tracking per showtime
            var trackKey = $"{HoldPrefix}track:{showTimeId}";
            if (_cache.TryGetValue(trackKey, out HashSet<int>? tracked))
            {
                // Lọc lại những ghế còn thực sự bị giữ
                var stillHeld = tracked!
                    .Where(seatId => _cache.TryGetValue(SeatKey(showTimeId, seatId), out _))
                    .ToList();
                return stillHeld;
            }
            return new List<int>();
        }
    }
}
