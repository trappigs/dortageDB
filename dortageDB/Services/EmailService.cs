using MailKit.Net.Smtp;
using MailKit.Security;
using MailKit;
using MimeKit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net;

namespace dortageDB.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task SendEmailAsync(
            string to,
            string subject = "Kayıt Başarılı",
            string htmlBody = "Kayıt işleminiz başarıyla tamamlanmıştır.")
        {
            var mailSettings = _configuration.GetSection("MailSettings");
            var mail = mailSettings["Mail"]?.Trim();
            var displayName = mailSettings["DisplayName"]?.Trim();
            var password = mailSettings["Password"]?.Trim();
            var host = mailSettings["Host"]?.Trim();
            var port = mailSettings.GetValue<int>("Port");

            _logger.LogInformation("Authenticating with Email: {Email}, Password Length: {Length}", mail, password?.Length ?? 0);

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(displayName, mail));
            message.To.Add(new MailboxAddress("", to));
            message.Subject = subject;

            // Ensure UTF-8 encoding is explicitly set in the HTML head
            var fullHtml = $@"<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <meta http-equiv='Content-Type' content='text/html; charset=utf-8'>
</head>
<body>
    {htmlBody}
</body>
</html>";

            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = fullHtml
            };
            message.Body = bodyBuilder.ToMessageBody();

            try 
            {
                using (var client = new SmtpClient())
                {
                    // Turkticaret.net: Port 587 with STARTTLS (Auto)
                    await client.ConnectAsync(host, port, SecureSocketOptions.Auto);

                    // Remove XOAUTH2 to prevent mechanism mismatch
                    client.AuthenticationMechanisms.Remove("XOAUTH2");

                    // Force AUTH LOGIN mechanism
                    if (!string.IsNullOrEmpty(mail) && !string.IsNullOrEmpty(password))
                    {
                        var credentials = new NetworkCredential(mail, password);
                        var sasl = new SaslMechanismLogin(credentials);
                        await client.AuthenticateAsync(sasl);
                    }
                    
                    await client.SendAsync(message);
                    await client.DisconnectAsync(true);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Email sending failed to {To}", to);
            }
        }
    }
}
