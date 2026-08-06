namespace RapchieuPhim.API.Constants
{
    public static class MovieStatus
    {
        public const string NowShowing = "Đang chiếu";
        public const string ComingSoon = "Sắp chiếu";
        public const string Special = "Đặc biệt";

        // Dữ liệu cũ từng lưu trạng thái với tiền tố "suất".
        public static readonly string[] NowShowingStatuses = { NowShowing, "suất đang chiếu" };
        public static readonly string[] ComingSoonStatuses = { ComingSoon, "suất sắp chiếu" };
        public static readonly string[] SpecialStatuses = { Special, "suất đặc biệt" };
    }
}
