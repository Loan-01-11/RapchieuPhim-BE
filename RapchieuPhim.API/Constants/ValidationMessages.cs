namespace RapchieuPhim.API.Constants
{
    public static class ValidationMessages
    {
        #region 1. RÀNG BUỘC DỮ LIỆU ĐẦU VÀO (Bắt buộc / Độ dài)

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

        //  Ràng buộc dành riêng cho phân hệ quản lý Phim (Movies)
        public const string MovieTitleRequired = "Tiêu đề phim không được để trống.";
        public const string MovieDurationRequired = "Thời lượng phim không được để trống.";
        public const string MovieReleaseDateRequired = "Ngày khởi chiếu không được để trống.";
        public const string MovieEndDateRequired = "Ngày kết thúc không được để trống.";
        public const string MovieStatusRequired = "Trạng thái phim không được để trống.";

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

        // 🌟 CÁC HÀM TĨNH CHỨA BIẾN ĐỘNG (Ráp ID tự động)
        public static string UserNotFoundWithId(int id) => $"Không tìm thấy người dùng có ID: {id}.";
        public static string UserUpdateSuccessWithId(int id) => $"Đã cập nhật thành công tài khoản ID {id}.";

        public static string MovieNotFoundWithId(int id) => $"Không tìm thấy bộ phim có ID: {id}.";
        #endregion

        #region 4. CHU KỲ MÃ OTP & KHÔI PHỤC MẬT KHẨU (OTP & Forgot-Reset Flow)

        public const string OtpInvalidOrExpired = "Mã xác nhận không đúng hoặc đã hết hạn. Vui lòng yêu cầu mã mới.";
        public const string OtpValid = "Mã xác nhận hợp lệ.";
        public const string OtpSentSuccess = "Mã xác nhận đã được gửi đến email của bạn. Vui lòng kiểm tra hộp thư (cả mục Spam).";
        public const string IfEmailExistsOtpSent = "Nếu email tồn tại, mã xác nhận đã được gửi. Vui lòng kiểm tra hộp thư.";
        public const string ResetPasswordSuccess = "Đổi mật khẩu thành công! Vui lòng đăng nhập lại.";

        #endregion

        #region 5. PHÂN QUYỀN & BẢO MẬT TOKEN (Authorization & Token Claims)

        public const string TokenInvalidOrExpired = "Token không hợp lệ hoặc đã hết hạn.";
        public const string TokenInvalid = "Token không hợp lệ.";
        public const string InvalidRole = "Role chỉ được là Admin, Staff hoặc Customer.";
        public const string InvalidInternalRole = "Quyền tài khoản không hợp lệ. Chỉ được phép chọn quyền 'Admin' hoặc 'Staff'.";
        public const string RoleSelectionInvalid = "Quyền (Role) chọn không hợp lệ.";
        public const string OnlyCustomerRegistrationAllowed = "Chỉ cho phép đăng ký tài khoản Customer qua endpoint này.";

        // Phân cấp Super Admin
        public const string UnauthorizedRoleChange = "Quyền hạn bị từ chối! Chỉ Admin tối cao duy nhất mới có quyền thay đổi chức vụ (Phân quyền) của người khác.";
        public const string UnauthorizedDelete = "Quyền hạn bị từ chối! Chỉ duy nhất Admin cấp cao mới có quyền xóa tài khoản khỏi hệ thống.";

        #endregion

        #region 6. ĐĂNG NHẬP GOOGLE (Third-party Authentication)

        public const string GoogleAccountNotRegistered = "Tài khoản chưa đăng ký. Vui lòng bổ sung thông tin để hoàn tất.";
        public const string GoogleTokenInvalid = "Google token không hợp lệ hoặc đã hết hạn.";
        public const string RegistrationAdditionalInfo = "Vui lòng bổ sung thông tin để hoàn tất đăng ký.";

        #endregion

        #region 7. HỆ THỐNG & CẤU HÌNH GỐC (System & Configurations)

        public const string DataInvalid = "Dữ liệu không hợp lệ.";

        // Tài khoản Admin tối cao tối thượng được quyền làm hết mọi thứ
        public const string SuperAdminEmail = "admin@123.com";

        #endregion
    }
}