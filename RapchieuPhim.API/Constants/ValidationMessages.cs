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
        public const string StartTimeRequired    = "Thời gian bắt đầu không được để trống.";
        public const string BasePriceRequired    = "Giá vé cơ bản không được để trống.";
        public const string BasePriceTooLow      = "Giá vé cơ bản phải ít nhất 1.000 VNĐ.";
        public const string StatusRequired       = "Trạng thái suất chiếu không được để trống.";

        public const string CreateSuccess        = "Tạo suất chiếu mới thành công!";
        public const string UpdateSuccess        = "Cập nhật suất chiếu thành công!";
        public const string DeleteSuccess        = "Đã xoá suất chiếu thành công!";
        public const string CancelSuccess        = "Đã huỷ suất chiếu thành công!";

        public const string MovieNotFound        = "Phim không tồn tại hoặc đã bị xoá.";
        public const string RoomNotFound         = "Phòng chiếu không tồn tại hoặc đã bị vô hiệu hoá.";
        public const string MovieNotActive       = "Phim đã ngừng chiếu, không thể tạo suất chiếu mới.";
        public const string StartTimePast        = "Thời gian bắt đầu không được là thời điểm trong quá khứ.";
        public const string RoomConflict         = "Phòng chiếu đã có suất chiếu khác trong khung giờ này (bao gồm 15 phút dọn phòng).";
        public const string HasBookings          = "Không thể xoá suất chiếu đã có vé đặt. Vui lòng huỷ suất chiếu thay thế.";

        // Các trạng thái hợp lệ
        public static readonly string[] ValidStatuses = { "Active", "Cancelled", "Completed" };
        public static string InvalidStatus(string s) => $"Trạng thái '{s}' không hợp lệ. Chỉ chấp nhận: Active, Cancelled, Completed.";

        public static string NotFoundWithId(int id) => $"Không tìm thấy suất chiếu có ID: {id}.";
    }
}