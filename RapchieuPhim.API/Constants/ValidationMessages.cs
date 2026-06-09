
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
    }
}
