using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace RapchieuPhim.API.Services
{
    public interface IEmailService
    {
        Task SendOtpAsync(string toEmail, string toName, string otpCode);
    }

    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;

        public EmailService(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendOtpAsync(string toEmail, string toName, string otpCode)
        {
            var smtpHost    = _config["Email:SmtpHost"]!;
            var smtpPort    = int.Parse(_config["Email:SmtpPort"] ?? "587");
            var senderEmail = _config["Email:SenderEmail"]!;
            var senderName  = _config["Email:SenderName"] ?? "Rạp Chiếu Phim";
            var appPassword = _config["Email:AppPassword"]!;

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(senderName, senderEmail));
            message.To.Add(new MailboxAddress(toName, toEmail));
            message.Subject = "[Rạp Chiếu Phim] Mã xác nhận đặt lại mật khẩu";

            message.Body = new TextPart("html")
            {
                Text = $@"
                <div style='font-family: Arial, sans-serif; max-width: 500px; margin: 0 auto;'>
                    <div style='background: #c0392b; padding: 20px; text-align: center;'>
                        <h2 style='color: white; margin: 0;'>🎬 RẠP CHIẾU PHIM</h2>
                    </div>
                    <div style='padding: 30px; background: #f9f9f9;'>
                        <p>Xin chào <strong>{toName}</strong>,</p>
                        <p>Bạn vừa yêu cầu đặt lại mật khẩu. Đây là mã xác nhận của bạn:</p>
                        <div style='text-align: center; margin: 30px 0;'>
                            <span style='
                                font-size: 36px;
                                font-weight: bold;
                                letter-spacing: 10px;
                                color: #c0392b;
                                background: #fff;
                                padding: 15px 30px;
                                border: 2px dashed #c0392b;
                                border-radius: 8px;
                            '>{otpCode}</span>
                        </div>
                        <p>⏰ Mã có hiệu lực trong <strong>5 phút</strong>.</p>
                        <p>Nếu bạn không yêu cầu đặt lại mật khẩu, hãy bỏ qua email này.</p>
                    </div>
                    <div style='background: #333; padding: 15px; text-align: center;'>
                        <p style='color: #aaa; margin: 0; font-size: 12px;'>© 2024 Rạp Chiếu Phim. Không trả lời email này.</p>
                    </div>
                </div>"
            };

            using var client = new SmtpClient();
            await client.ConnectAsync(smtpHost, smtpPort, SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(senderEmail, appPassword);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }
    }
}
