namespace RapchieuPhim.API.Constants
{
    public static class ValidationMessages
    {
        public const string EmailRequired = "Email không được để trống.";
        public const string EmailInvalid = "Email không đúng định dạng.";
        public const string PasswordRequired = "Mật khẩu không được để trống.";
        public const string FullNameRequired = "Họ tên không được để trống.";
        public const string PasswordMinLength = "Mật khẩu phải có ít nhất 6 ký tự.";
        public const string ConfirmPasswordRequired = "Xác nhận mật khẩu không được để trống.";
        public const string DateOfBirthRequired = "Ngày sinh không được để trống.";
        public const string PhoneRequired = "Số điện thoại không được để trống.";
        public const string GoogleIdTokenRequired = "Google ID token không được để trống.";
        public const string RegistrationAdditionalInfo = "Vui lòng bổ sung thông tin để hoàn tất đăng ký.";
        public const string OtpRequired = "Mã xác nhận không được để trống.";
        public const string OtpLength = "Mã xác nhận gồm 6 chữ số.";
        public const string NewPasswordRequired = "Mật khẩu mới không được để trống.";

        // OTP flow
        public const string OtpInvalidOrExpired = "Mã xác nhận không đúng hoặc đã hết hạn. Vui lòng yêu cầu mã mới.";
        public const string OtpValid = "Mã xác nhận hợp lệ.";
        public const string OtpSentSuccess = "Mã xác nhận đã được gửi đến email của bạn. Vui lòng kiểm tra hộp thư (cả mục Spam).";
        public const string IfEmailExistsOtpSent = "Nếu email tồn tại, mã xác nhận đã được gửi. Vui lòng kiểm tra hộp thư.";

        // Auth / account messages
        public const string InvalidCredentials = "Email hoặc mật khẩu không chính xác.";
        public const string ConfirmPasswordMismatch = "Mật khẩu xác nhận không khớp.";
        public const string DateOfBirthInvalidFormat = "Ngày sinh không đúng định dạng. Vui lòng dùng định dạng yyyy-MM-dd (ví dụ: 2000-01-15).";
        public const string EmailAlreadyRegistered = "Email này đã được đăng ký. Vui lòng sử dụng email khác hoặc đăng nhập.";
        public const string InvalidRole = "Role chỉ được là Admin, Staff hoặc Customer.";
        public const string GoogleAccountNotRegistered = "Tài khoản chưa đăng ký. Vui lòng bổ sung thông tin để hoàn tất.";
        public const string AccountLocked = "Tài khoản của bạn đã bị khoá. Vui lòng liên hệ hỗ trợ.";
        public const string UserLocked = "Tài khoản đã bị khoá. Vui lòng liên hệ hỗ trợ.";
        public const string UserNotFound = "Không tìm thấy tài khoản.";
        public const string ResetPasswordSuccess = "Đổi mật khẩu thành công! Vui lòng đăng nhập lại.";
        public const string GoogleTokenInvalid = "Google token không hợp lệ hoặc đã hết hạn.";

        // General
        public const string DataInvalid = "Dữ liệu không hợp lệ.";
    }
}
