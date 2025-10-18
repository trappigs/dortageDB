using System.Net;
using System.Net.Mail;

namespace dortageDB.Services
{
    public class EmailService : IEmailService
    {
        //TODO: Implement email sending logic here
        public async Task SendEmailAsync(
            string to,
            string subject = "Kayıt Başarılı",
            string htmlBody = "Kayıt işleminiz başarıyla tamamlanmıştır.")
        {
            var mail = "info@dortage.com";
            var pw = "Dortage2024*";

            using (var client = new SmtpClient("smtp.yandex.com", 587))
            {
                client.Credentials = new NetworkCredential(mail, pw);
                client.EnableSsl = true;
                client.DeliveryMethod = SmtpDeliveryMethod.Network;
                client.Timeout = 20000;

                using (var message = new MailMessage())
                {
                    message.From = new MailAddress(mail);
                    message.To.Add(to);
                    message.Subject = subject;
                    message.Body = htmlBody;
                    message.IsBodyHtml = true;

                    await client.SendMailAsync(message);
                }
            }
        }
    }
}
