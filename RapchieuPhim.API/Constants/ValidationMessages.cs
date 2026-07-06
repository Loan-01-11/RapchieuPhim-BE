namespace RapchieuPhim.API.Constants
{
    public static class ValidationMessages
    {
        #region 1. RÀNG BUỘC DỮ LIỆU ĐẦU VÀO (Input Validation Constraints)

        public const string EmailRequired = "Email không được để trống.";
        public const string PasswordRequired = "Mật khẩu không được để trống.";
        public const string FullNameRequired = "Họ tên không được để trống.";
        public const string ConfirmPasswordRequired = "Xác nhận mật khẩu không được để trống.";
        public const string DateOfBirthRequired = "Ngày sinh không được để trống.";
        public const string PhoneRequired = "Số điện thoại không được để trống.";
        public const string OtpRequired = "Mã xác nhận không được để trống.";
        public const string NewPasswordRequired = "Mật khẩu mới không được để trống.";
        public const string GoogleIdTokenRequired = "Google ID token không được để trống.";
        public const string PasswordMinLength = "Mật khẩu phải có ít nhất 6 ký tự.";
        public const string OtpLength = "Mã xác nhận gồm 6 chữ số.";
        public const string IdMismatch = "Mã số (ID) không khớp với hệ thống.";

        #endregion

        #region 2. ĐỊNH DẠNG DỮ LIỆU (Format Validation)

        public const string EmailInvalid = "Email không đúng định dạng.";
        public const string DateOfBirthInvalidFormat = "Ngày sinh không đúng định dạng. Vui lòng dùng định dạng yyyy-MM-dd (ví dụ: 2000-01-15).";
        public const string DateOfBirthInvalidFormatSimple = "Ngày sinh không đúng định dạng yyyy-MM-dd.";

        #endregion

        #region 3. TRẠNG THÁI TÀI KHOẢN & ĐĂNG NHẬP (Account & Auth Status)

        public const string InvalidCredentials = "Email hoặc mật khẩu không chính xác.";
        public const string ConfirmPasswordMismatch = "Mật khẩu xác nhận không khớp.";
        public const string EmailAlreadyRegistered = "Email này đã được đăng ký. Vui lòng sử dụng email khác hoặc đăng nhập.";
        public const string AccountLocked = "Tài khoản của bạn đã bị khoá. Vui lòng liên hệ hỗ trợ.";
        public const string UserLocked = "Tài khoản đã bị khoá. Vui lòng liên hệ hỗ trợ.";
        public const string UserNotFound = "Không tìm thấy tài khoản.";
        public const string UserNotFoundInSystem = "Tài khoản không tồn tại trên hệ thống.";
        public const string AccountNotFoundOrLocked = "Tài khoản không tồn tại hoặc đã bị khóa.";

        // --- CÁC HÀM TRẢ VỀ THÔNG BÁO CHỨA ID BIẾN ĐỘNG ---
        public static string UserNotFoundWithId(int id) => $"Không tìm thấy người dùng có ID: {id}.";
        public static string UserUpdateSuccessWithId(int id) => $"Đã cập nhật thành công tài khoản ID {id}.";
        public static string SeatNotFoundWithId(int id) => $"Không tìm thấy ghế có ID: {id}.";
        public static string RoomNotFoundWithId(int id) => $"Không tìm thấy phòng chiếu có ID: {id}.";

        #endregion

        #region 4. CHU KỲ MÃ OTP & KHÔI PHỤC MẬT KHẨU (OTP & Reset Flow)

        public const string OtpInvalidOrExpired = "Mã xác nhận không đúng hoặc đã hết hạn. Vui lòng yêu cầu mã mới.";
        public const string OtpValid = "Mã xác nhận hợp lệ.";
        public const string OtpSentSuccess = "Mã xác nhận đã được gửi đến email của bạn. Vui lòng kiểm tra hộp thư (cả mục Spam).";
        public const string IfEmailExistsOtpSent = "Nếu email tồn tại, mã xác nhận đã được gửi. Vui lòng kiểm tra hộp thư.";
        public const string ResetPasswordSuccess = "Đổi mật khẩu thành công! Vui lòng đăng nhập lại.";

        #endregion

        #region 5. PHÂN QUYỀN & BẢO MẬT TOKEN (Authorization & Security)

        public const string TokenInvalidOrExpired = "Token không hợp lệ hoặc đã hết hạn.";
        public const string TokenInvalid = "Token không hợp lệ.";
        public const string InvalidRole = "Role chỉ được là Admin, Staff hoặc Customer.";
        public const string InvalidInternalRole = "Quyền tài khoản không hợp lệ. Chỉ được phép chọn quyền 'Admin' hoặc 'Staff'.";
        public const string RoleSelectionInvalid = "Quyền (Role) chọn không hợp lệ.";
        public const string OnlyCustomerRegistrationAllowed = "Chỉ cho phép đăng ký tài khoản Customer qua endpoint này.";
        public const string UnauthorizedRoleChange = "Quyền hạn bị từ chối! Chỉ Admin tối cao duy nhất mới có quyền thay đổi chức vụ (Phân quyền) của người khác.";
        public const string UnauthorizedDelete = "Quyền hạn bị từ chối! Chỉ duy nhất Admin cấp cao mới có quyền xóa dữ liệu khỏi hệ thống.";
        public const string UpdatedProfileSuccessfully = "Cập nhật hồ sơ cá nhân thành công!";
        #endregion

        #region 6. ĐĂNG NHẬP BÊN THỨ BA (Third-party Authentication)

        public const string GoogleAccountNotRegistered = "Tài khoản chưa đăng ký. Vui lòng bổ sung thông tin để hoàn tất.";
        public const string GoogleTokenInvalid = "Google token không hợp lệ hoặc đã hết hạn.";
        public const string RegistrationAdditionalInfo = "Vui lòng bổ sung thông tin để hoàn tất đăng ký.";

        #endregion

        #region 7. PHÂN HỆ QUẢN LÝ PHIM (Movie Management)

        public const string MovieTitleRequired = "Tiêu đề phim không được để trống.";
        public const string MovieDurationRequired = "Thời lượng phim không được để trống.";
        public const string MovieReleaseDateRequired = "Ngày khởi chiếu không được để trống.";
        public const string MovieEndDateRequired = "Ngày kết thúc không được để trống.";
        public const string MovieStatusRequired = "Trạng thái phim không được để trống.";
        public const string CategoryNotFound = "Thể loại phim không tồn tại hoặc đã bị xóa.";
        public const string CategoryNameRequired = "Tên thể loại không được để trống.";
        public const string CategoryUpdateSuccess = "Cập nhật thể loại phim thành công!";
        public const string UpdateMovieSuccess = "Cập nhật thông tin bộ phim thành công!";
        public const string DeleteMovieSuccess = "Đã xóa phim thành công khỏi hệ thống!";

        // --- CÁC HÀM TRẢ VỀ THÔNG BÁO CHỨA ID BIẾN ĐỘNG ---
        public static string MovieNotFoundWithId(int id) => $"Không tìm thấy bộ phim có ID: {id}.";

        #endregion

        #region 8. PHÂN HỆ QUẢN LÝ RẠP CHIẾU PHIM (Cinema Management)

        public const string CinemaUpdateSuccess = "Cập nhật thông tin rạp phim thành công!";
        public const string CinemaDeleteSuccess = "Xóa rạp chiếu phim thành công!";
        public const string CinemaConcurrencyError = "Dữ liệu đã bị thay đổi bởi một luồng khác, vui lòng thử lại.";
        public const string UnauthorizedCinemaUpdate = "Bạn không có quyền chỉnh sửa thông tin rạp chiếu phim này.";
        public const string CinemaPhoneMaxLength = "Số điện thoại rạp phim không được vượt quá 20 ký tự.";
        public const string CinemaNameRequired = "Tên rạp chiếu phim không được để trống.";
        public const string CinemaNameMaxLength = "Tên rạp chiếu phim không được vượt quá 150 ký tự.";
        public const string CinemaAddressRequired = "Địa chỉ rạp chiếu phim không được để trống.";
        public const string CinemaAddressMaxLength = "Địa chỉ rạp chiếu phim không được vượt quá 255 ký tự.";
        public const string CinemaAreaRequired = "Vui lòng chọn khu vực hợp lệ cho rạp chiếu phim.";

        // --- CÁC HÀM TRẢ VỀ THÔNG BÁO CHỨA ID BIẾN ĐỘNG ---
        public static string CinemaNotFoundWithId(int id) => $"Không tìm thấy rạp chiếu phim có ID = {id}.";

        #endregion

        #region 9. PHÂN HỆ QUẢN LÝ KHU VỰC (Area Management)

        public const string AreaUpdateSuccess = "Cập nhật thông tin khu vực thành công!";
        public const string AreaDeleteSuccess = "Xóa khu vực thành công!";
        public const string AreaConcurrencyError = "Dữ liệu khu vực đã bị thay đổi bởi một luồng khác, vui lòng thử lại.";
        public const string AreaNameAlreadyExists = "Tên khu vực này đã tồn tại trong hệ thống.";
        public const string AreaNameRequired = "Tên khu vực không được để trống.";
        public const string AreaNameMaxLength = "Tên khu vực không được vượt quá 100 ký tự.";

        // --- CÁC HÀM TRẢ VỀ THÔNG BÁO CHỨA ID BIẾN ĐỘNG ---
        public static string AreaNotFoundWithId(int id) => $"Không tìm thấy khu vực có ID = {id}.";

        #endregion

        #region 10. HỆ THỐNG & CẤU HÌNH GỐC (System & Configurations)

        public const string DataInvalid = "Dữ liệu không hợp lệ.";

        /// <summary>
        /// Email đặc quyền tối cao của hệ thống.
        /// </summary>
        public const string SuperAdminEmail = "admin@123.com";

        #endregion

        #region 11. PHÂN HỆ QUẢN LÝ PHÒNG CHIẾU (Room Management)

        public const string RoomNameRequired = "Tên phòng chiếu không được để trống.";
        public const string RoomNameMaxLength = "Tên phòng chiếu không được vượt quá 100 ký tự.";
        public const string RoomTypeMaxLength = "Loại phòng chiếu không được vượt quá 50 ký tự.";
        public const string RoomCinemaRequired = "Vui lòng chọn rạp chiếu phim hợp lệ cho phòng chiếu này.";
        public const string RoomUpdateSuccess = "Cập nhật thông tin phòng chiếu thành công!";
        public const string RoomDeleteSuccess = "Xóa phòng chiếu thành công!";
        public const string RoomConcurrencyError = "Dữ liệu phòng chiếu đã bị thay đổi bởi một luồng khác, vui lòng thử lại.";
        public const string UnauthorizedRoomUpdate = "Bạn không có quyền chỉnh sửa thông tin phòng chiếu này.";
        #endregion

        #region 12. PHÂN HỆ QUẢN LÝ VÉ (Ticket Management)

        public const string TicketCodeRequired = "Mã vé không được để trống.";
        public const string TicketStatusRequired = "Trạng thái vé không được để trống.";
        public const string TicketStatusInvalid = "Trạng thái vé không hợp lệ (Chỉ chấp nhận: Active | Used | Cancelled).";
        public const string TicketUpdateStatusSuccess = "Cập nhật trạng thái vé thành công!";
        public const string TicketConcurrencyError = "Dữ liệu vé đã bị thay đổi bởi một luồng khác, vui lòng thử lại.";
        public static string ErrorAutoTicket = "Lỗi khi tự động cấp vé: ";
        // --- CÁC HÀM TRẢ VỀ THÔNG BÁO CHỨA BIẾN ĐỘNG ĐỘNG ---
        public static string TicketNotFoundWithId(int id) => $"Không tìm thấy vé có ID = {id}.";
        public static string TicketNotFoundWithCode(string code) => $"Không tìm thấy vé mang mã Code: {code}.";
        public static string TicketNotFoundWithBooking(int bookingId) => $"Không tìm thấy vé nào thuộc đơn đặt vé ID: {bookingId}.";

        #endregion

        #region 13. PHÂN HỆ CẤU HÌNH GIÁ VÉ (Ticket Pricing Management)

        public const string PricingPriceInvalid = "Giá vé cấu hình phải lớn hơn hoặc bằng 0.";
        public const string PricingRoomTypeMaxLength = "Loại phòng chiếu không được vượt quá 50 ký tự.";
        public const string PricingSeatTypeMaxLength = "Loại ghế không được vượt quá 30 ký tự.";
        public const string PricingDayTypeMaxLength = "Loại ngày không được vượt quá 20 ký tự.";
        public const string PricingUpdateSuccess = "Cập nhật cấu hình giá vé thành công!";
        public const string PricingDeleteSuccess = "Xóa cấu hình giá vé khỏi hệ thống thành công!";
        public const string PricingConcurrencyError = "Dữ liệu cấu hình giá đã bị thay đổi bởi một luồng khác, vui lòng thử lại.";
        public const string UnauthorizedPricingUpdate = "Quyền hạn bị từ chối! Chỉ Admin tối cao mới được quyền chỉnh sửa ma trận giá vé.";

        // --- CÁC HÀM TRẢ VỀ THÔNG BÁO CHỨA BIẾN ĐỘNG ĐỘNG ---
        public static string PricingNotFoundWithId(int id) => $"Không tìm thấy cấu hình giá vé có ID = {id}.";

        #endregion

        #region 14. PHÂN HỆ QUẢN LÝ ĐẶT VÉ (Booking Management)

        public const string BookingTypeInvalid = "Phương thức đặt vé không hợp lệ (Chỉ chấp nhận: Online | Counter).";
        public const string BookingStatusInvalid = "Trạng thái đơn đặt vé không hợp lệ.";
        public const string BookingUpdateStatusSuccess = "Cập nhật trạng thái đơn đặt vé thành công!";
        public const string BookingConcurrencyError = "Dữ liệu đơn đặt vé đã bị thay đổi bởi một luồng khác, vui lòng thử lại.";
        public const string UnauthorizedBookingView = "Bạn không có quyền xem lịch sử đặt vé của tài khoản này.";
        public const string UnauthorizedBookingCancel = "Bạn không có quyền hủy đơn đặt vé này.";
        public const string CancelBookingSuccess = "Hủy đơn đặt vé và giải phóng ghế thành công!";
        public const string CreateBookingSuccess = "Đặt vé xem phim thành công!";
        public const string GetHistorySuccess = "Lấy lịch sử thành công.";
        public const string OnlyStaffCanCreateCounterBooking = "Chỉ nhân viên mới được quyền tạo đơn đặt vé tại quầy.";
        public const string Counter = "Counter";
        public const string Online = "Online";
        public const string StutusComfirmed = "Confirmed";
        // --- CÁC HÀM TRẢ VỀ THÔNG BÁO CHỨA BIẾN ĐỘNG ĐỘNG ---
        public static string BookingNotFoundWithId(int id) => $"Không tìm thấy đơn đặt vé có ID = {id}.";

        #endregion

        // ── Hằng số cho nghiệp vụ đặt vé có mã giảm giá ────────────────────────────
        public static class BookingMessages
        {
            public const string SeatAlreadyBooked        = "Ghế này đã được đặt trong suất chiếu, vui lòng chọn ghế khác.";
            public const string DuplicateSeatIds         = "Danh sách ghế bị trùng, vui lòng chọn lại.";
            public const string DiscountMaxUsageReached  = "Mã giảm giá đã hết lượt sử dụng.";
            public const string DiscountUserLimitReached = "Bạn đã dùng mã giảm giá này đủ số lần cho phép.";
            public const string CreateBookingFailed      = "Đặt vé thất bại, vui lòng thử lại.";
            public const string PricingNotFound          = "Không tìm thấy cấu hình giá vé phù hợp với loại phòng và loại ghế này.";
            public const string SeatIdsMinLength         = "Vui lòng chọn ít nhất 1 ghế.";
            public const string SeatIdsMaxLength         = "Chỉ được đặt tối đa 10 ghế mỗi lần.";
            public const string OrderItemQuantityInvalid = "Số lượng mỗi món phải từ 1 đến 50.";
            public const string FoodOrComboRequired      = "Mỗi dòng order phải có FoodId hoặc ComboId.";
            public const string FoodOrComboNotBoth       = "Mỗi dòng order chỉ được chọn 1 trong 2: FoodId hoặc ComboId.";
            public const string FoodOrComboUnavailable   = "Món ăn hoặc combo này hiện không còn phục vụ.";

            public static string OrderBelowMinAmount(decimal minAmount)
                => $"Giá trị đơn hàng chưa đạt mức tối thiểu {minAmount:N0} VNĐ để áp dụng mã giảm giá.";

            public static string SeatsAlreadyBooked(List<int> seatIds)
                => $"Các ghế sau đã được đặt: {string.Join(", ", seatIds)}. Vui lòng chọn ghế khác.";
        }
    }


    // ── Hằng số dành riêng cho phân hệ Quản lý Thực Đơn (Food) ─────────────────
    public static class FoodMessages
    {
        // Validation dữ liệu đầu vào
        public const string FoodNameRequired     = "Tên món ăn không được để trống.";
        public const string FoodNameMaxLength    = "Tên món ăn không được vượt quá 150 ký tự.";
        public const string CategoryMaxLength    = "Danh mục không được vượt quá 50 ký tự.";
        public const string PriceInvalid         = "Giá món ăn không được âm.";
        public const string QuantityInvalid      = "Số lượng không được âm.";

        // Validation nghiệp vụ
        public const string FoodNameAlreadyExists  = "Tên món ăn này đã tồn tại trong cùng danh mục.";
        public const string CannotDeleteUsedFood   = "Không thể xóa món ăn đã có lịch sử trong đơn hàng.";

        // Phân quyền
        public const string UnauthorizedUpdate   = "Quyền hạn bị từ chối! Chỉ Admin tối cao mới có quyền chỉnh sửa thực đơn.";

        // Thành công
        public const string CreateSuccess        = "Thêm món ăn mới thành công!";
        public const string UpdateSuccess        = "Cập nhật thông tin món ăn thành công!";
        public const string DeleteSuccess        = "Xóa món ăn thành công!";
        public const string ConcurrencyError     = "Dữ liệu món ăn đã bị thay đổi bởi một luồng khác, vui lòng thử lại.";

        // Hàm trả về thông báo chứa ID động
        public static string NotFoundWithId(int id) => $"Không tìm thấy món ăn có ID = {id}.";
    }

    // ── Hằng số dành riêng cho phân hệ Quản lý Combo ────────────────────────────
    public static class ComboMessages
    {
        // Validation dữ liệu đầu vào
        public const string ComboNameRequired      = "Tên combo không được để trống.";
        public const string ComboNameMaxLength     = "Tên combo không được vượt quá 150 ký tự.";
        public const string DescriptionMaxLength   = "Mô tả combo không được vượt quá 500 ký tự.";
        public const string PriceInvalid           = "Giá combo không được âm.";
        public const string QuantityInvalid        = "Số lượng combo không được âm.";
        public const string FoodIdRequired         = "Vui lòng chọn món ăn để thêm vào combo.";
        public const string FoodQuantityInvalid    = "Số lượng món trong combo phải từ 1 đến 100.";

        // Validation nghiệp vụ
        public const string ComboNameAlreadyExists = "Tên combo này đã tồn tại.";
        public const string CannotDeleteUsedCombo  = "Không thể xóa combo đã có lịch sử trong đơn hàng.";
        public const string FoodAlreadyInCombo     = "Món ăn này đã có trong combo, vui lòng cập nhật số lượng.";
        public const string FoodNotInCombo         = "Không tìm thấy món ăn này trong combo.";

        // Phân quyền
        public const string UnauthorizedModify     = "Quyền hạn bị từ chối! Chỉ Admin tối cao mới có quyền chỉnh sửa combo.";

        // Thành công
        public const string CreateSuccess          = "Tạo combo mới thành công!";
        public const string UpdateSuccess          = "Cập nhật thông tin combo thành công!";
        public const string DeleteSuccess          = "Xóa combo thành công!";
        public const string AddFoodSuccess         = "Thêm món vào combo thành công!";
        public const string RemoveFoodSuccess      = "Xóa món khỏi combo thành công!";
        public const string UpdateFoodQuantitySuccess = "Cập nhật số lượng món trong combo thành công!";
        public const string ConcurrencyError       = "Dữ liệu combo đã bị thay đổi bởi một luồng khác, vui lòng thử lại.";

        // Hàm trả về thông báo chứa ID động
        public static string NotFoundWithId(int id) => $"Không tìm thấy combo có ID = {id}.";
    }

    // ── Hằng số dành riêng cho phân hệ Thanh Toán (Payment) ─────────────────────
    public static class PaymentMessages
    {
        // Validation dữ liệu đầu vào
        public const string BookingIdRequired       = "Vui lòng cung cấp mã đặt vé (BookingId).";
        public const string PaymentMethodRequired   = "Vui lòng chọn phương thức thanh toán.";
        public const string PaymentMethodMaxLength  = "Phương thức thanh toán không được vượt quá 50 ký tự.";
        public const string StatusRequired          = "Vui lòng cung cấp trạng thái thanh toán.";

        // Phương thức thanh toán hợp lệ
        public const string MethodCash             = "Cash";
        public const string MethodQrCode           = "QrCode";

        // Cấu hình ngân hàng nhận thanh toán (VietQR)
        public const string BankId                 = "TPB"; // Tên viết tắt ngân hàng (VCB, MB, ACB,...)
        public const string AccountNo              = "15145686888"; // Số tài khoản ngân hàng nhận tiền
        public const string AccountName            = "Nguyen Quang Vinh"; // Tên chủ tài khoản ngân hàng
        public const string QrTemplate             = "compact"; // Template hiển thị (compact, qr_only, print)
        public const string SepayApiKey            = "quangvinh"; // ⚠️ Thay bằng API Key thật từ Sepay Dashboard

        // Trạng thái thanh toán hợp lệ
        public const string StatusPending          = "Pending";
        public const string StatusSuccess          = "Success";
        public const string StatusFailed           = "Failed";
        public const string StatusRefunded         = "Refunded";

        // Validation nghiệp vụ
        public const string AlreadyPaid            = "Đơn đặt vé này đã được thanh toán thành công trước đó.";
        public const string OrderBookingMismatch   = "Đơn hàng đồ ăn này không thuộc về đơn đặt vé đã cung cấp.";
        public const string CannotUpdateRefunded   = "Không thể cập nhật trạng thái giao dịch đã hoàn tiền.";
        public const string InvalidStatus          = "Trạng thái thanh toán không hợp lệ. Chỉ chấp nhận: Pending | Success | Failed | Refunded.";

        // Phân quyền
        public const string UnauthorizedPayment    = "Bạn không có quyền thanh toán cho đơn đặt vé này.";
        public const string UnauthorizedStatusChange = "Quyền hạn bị từ chối! Chỉ Admin và Staff mới được cập nhật trạng thái thanh toán.";

        // Thành công
        public const string CreateSuccess          = "Tạo giao dịch thanh toán thành công!";
        public const string UpdateStatusSuccess    = "Cập nhật trạng thái thanh toán thành công!";

        // Hàm trả về thông báo động
        public static string NotFoundWithId(int id)
            => $"Không tìm thấy giao dịch thanh toán có ID = {id}.";
        public static string BookingNotFoundForPayment(int bookingId)
            => $"Không tìm thấy đơn đặt vé có ID = {bookingId} để thanh toán.";
        public static string OrderNotFoundForPayment(int orderId)
            => $"Không tìm thấy đơn hàng đồ ăn có ID = {orderId}.";

        // Tạo URL ảnh mã VietQR tự động
        public static string GenerateVietQrUrl(decimal amount, int bookingId)
        {
            string description = $"THANH TOAN VE CP DAT VE {bookingId}";
            return $"https://img.vietqr.io/image/{BankId}-{AccountNo}-{QrTemplate}.png?amount={amount:0}&addInfo={Uri.EscapeDataString(description)}&accountName={Uri.EscapeDataString(AccountName)}";
        }
    }

    // ── Hằng số dành riêng cho phân hệ Quản lý Ghế (Seat) ──────────────────
    public static class SeatMessages
    {
        public const string RoomIdRequired        = "Vui lòng chọn phòng chiếu.";
        public const string SeatRowRequired       = "Hàng ghế không được để trống.";
        public const string SeatRowMaxLength      = "Ký hiệu hàng ghế tối đa 5 ký tự.";
        public const string SeatNumberRequired    = "Số ghế không được để trống.";
        public const string SeatNumberMaxLength   = "Số ghế tối đa 10 ký tự.";
        public const string SeatTypeRequired      = "Loại ghế không được để trống.";
        public const string SeatsPerRowRange      = "Số ghế mỗi hàng phải từ 1 đến 50.";
        public const string SeatIdsRequired       = "Danh sách ID ghế không được để trống.";
        public const string SeatAlreadyExists     = "Ghế {0}{1} đã tồn tại trong phòng này.";
        public const string CreateSeatSuccess     = "Tạo ghế thành công.";
        public const string CreateBatchSuccess    = "Tạo hàng loạt ghế thành công.";
        public const string UpdateSeatSuccess     = "Cập nhật thông tin ghế thành công.";
        public const string UpdateTypeSuccess     = "Cập nhật loại ghế hàng loạt thành công.";
        public const string UpdateStatusSuccess   = "Cập nhật trạng thái ghế hàng loạt thành công.";
        public const string DeleteSeatSuccess     = "Đã xóa ghế thành công.";
        public const string RoomNotFound          = "Phòng chiếu không tồn tại hoặc đã bị vô hiệu hoá.";

        // Loại ghế hợp lệ
        public static readonly string[] ValidSeatTypes = { "Standard", "VIP", "Couple" };
        public static string InvalidSeatType(string t) => $"Loại ghế '{t}' không hợp lệ. Chỉ chấp nhận: Standard, VIP, Couple.";
    }

    // ── Hằng số dành riêng cho phân hệ Quản lý Suất Chiếu (Showtime) ──────────
    public static class ShowtimeMessages
    {
        public const string MovieIdRequired      = "Vui lòng chọn phim.";
        public const string RoomIdRequired       = "Vui lòng chọn phòng chiếu.";
        public const string ShowDateRequired     = "Ngày chiếu không được để trống.";
        public const string StartTimeRequired    = "Giờ bắt đầu không được để trống.";
        public const string EndTimeRequired      = "Giờ kết thúc không được để trống.";
        public const string BasePriceRequired    = "Giá vé cơ bản không được để trống.";
        public const string BasePriceTooLow      = "Giá vé cơ bản phải ít nhất 1.000 VNĐ.";
        public const string StatusRequired       = "Trạng thái suất chiếu không được để trống.";

        // Lỗi định dạng
        public const string ShowDateInvalidFormat = "Ngày chiếu không đúng định dạng. Vui lòng dùng yyyy-MM-dd (ví dụ: 2026-07-01).";
        public const string StartTimeInvalidFormat = "Giờ bắt đầu không đúng định dạng. Vui lòng dùng HH:mm (ví dụ: 09:00).";
        public const string EndTimeInvalidFormat  = "Giờ kết thúc không đúng định dạng. Vui lòng dùng HH:mm (ví dụ: 11:00).";

        // Lỗi logic thời gian
        public const string EndTimeBeforeStart   = "Giờ kết thúc phải sau giờ bắt đầu.";
        public const string StartTimePast        = "Ngày và giờ bắt đầu không được là thời điểm trong quá khứ.";

        public const string CreateSuccess        = "Tạo suất chiếu mới thành công!";
        public const string UpdateSuccess        = "Cập nhật suất chiếu thành công!";
        public const string DeleteSuccess        = "Đã xoá suất chiếu thành công!";
        public const string CancelSuccess        = "Đã huỷ suất chiếu thành công!";

        public const string MovieNotFound        = "Phim không tồn tại hoặc đã bị xoá.";
        public const string RoomNotFound         = "Phòng chiếu không tồn tại hoặc đã bị vô hiệu hoá.";
        public const string MovieNotActive       = "Phim đã ngừng chiếu, không thể tạo suất chiếu mới.";
        public const string RoomConflict         = "Phòng chiếu đã có suất chiếu khác trong khung giờ này (bao gồm 15 phút dọn phòng).";
        public const string HasBookings          = "Không thể xoá suất chiếu đã có vé đặt. Vui lòng huỷ suất chiếu thay thế.";

        // Các trạng thái hợp lệ
        public const string StatusActive    = "Active";
        public const string StatusCancelled = "Cancelled";
        public const string StatusCompleted = "Completed";

        public static readonly string[] ValidStatuses = { StatusActive, StatusCancelled, StatusCompleted };
        public static string InvalidStatus(string s) => $"Trạng thái '{s}' không hợp lệ. Chỉ chấp nhận: Active, Cancelled, Completed.";

        // Thông báo huỷ lặp
        public const string AlreadyCancelled = "Suất chiếu này đã được huỷ trước đó.";

        // Trạng thái phim không được phép tạo suất chiếu mới
        public static readonly string[] InactiveMovieStatuses = { "Deleted", "Archived" };

        // Trạng thái ghế trong sơ đồ chọn ghế
        public const string SeatStatusAvailable = "Available";
        public const string SeatStatusBooked    = "Booked";
        public const string SeatStatusHeld      = "Held";

        public static string NotFoundWithId(int id) => $"Không tìm thấy suất chiếu có ID: {id}.";
    }

    // ── Hằng số dành riêng cho phân hệ Quản lý Mã Giảm Giá (Discount) ────────────
    public static class DiscountMessages
    {
        // Validation dữ liệu đầu vào
        public const string DiscountCodeRequired      = "Mã giảm giá không được để trống.";
        public const string DiscountCodeMaxLength     = "Mã giảm giá không được vượt quá 50 ký tự.";
        public const string DiscountTypeRequired      = "Loại giảm giá không được để trống.";
        public const string DiscountValueInvalid      = "Giá trị giảm phải lớn hơn 0.";
        public const string MinOrderAmountInvalid     = "Giá trị đơn hàng tối thiểu không được âm.";
        public const string MaxUsagePerUserInvalid    = "Số lần dùng tối đa mỗi người phải ít nhất là 1.";
        public const string StartDateRequired         = "Ngày bắt đầu không được để trống.";

        // Validation nghiệp vụ
        public const string InvalidDiscountType       = "Loại giảm giá không hợp lệ. Chỉ chấp nhận: Percent hoặc Fixed.";
        public const string PercentValueExceeds100    = "Giá trị giảm theo phần trăm không được vượt quá 100%.";
        public const string EndDateBeforeStartDate    = "Ngày hết hạn phải sau ngày bắt đầu.";
        public const string DiscountCodeAlreadyExists = "Mã giảm giá này đã tồn tại trong hệ thống.";
        public const string CannotDeleteUsedDiscount  = "Không thể xóa mã giảm giá đã được sử dụng.";
        public const string InvalidOrExpiredCode      = "Mã giảm giá không hợp lệ hoặc đã hết hạn.";

        // Phân quyền
        public const string UnauthorizedUpdate        = "Quyền hạn bị từ chối! Chỉ Admin tối cao mới có quyền chỉnh sửa mã giảm giá.";

        // Thành công
        public const string CreateSuccess             = "Tạo mã giảm giá mới thành công!";
        public const string UpdateSuccess             = "Cập nhật mã giảm giá thành công!";
        public const string DeleteSuccess             = "Xóa mã giảm giá thành công!";
        public const string ConcurrencyError          = "Dữ liệu mã giảm giá đã bị thay đổi bởi một luồng khác, vui lòng thử lại.";

        // Loại giảm giá hợp lệ
        public const string TypePercent               = "Percent";
        public const string TypeFixed                 = "Fixed";
        public static readonly string[] ValidDiscountTypes = { TypePercent, TypeFixed };

        // Hàm trả về thông báo chứa ID động
        public static string NotFoundWithId(int id) => $"Không tìm thấy mã giảm giá có ID = {id}.";
    }

    // ── Hằng số dành riêng cho phân hệ Giữ Ghế Tạm Thời (Seat Hold) ────────────
    public static class SeatHoldMessages
    {
        // Thành công
        public static string HoldSuccess(int minutes, string until)
            => $"Đã giữ ghế thành công trong {minutes} phút (đến {until}).";
        public const string ReleaseSuccess      = "Đã huỷ giữ ghế thành công.";

        // Lỗi giữ ghế
        public const string AlreadyHeldBySelf   = "Bạn đã giữ ghế này rồi.";
        public static string HeldByOther(string until)
            => $"Ghế này đang được người khác giữ đến {until}.";

        // Lỗi huỷ
        public const string HoldNotFound        = "Không tìm thấy lần giữ ghế này hoặc đã hết hạn.";
        public const string UnauthorizedRelease = "Bạn không có quyền huỷ giữ ghế của người khác.";
    }
}