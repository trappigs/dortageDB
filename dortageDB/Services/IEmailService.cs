namespace dortageDB.Services
{
    public interface IEmailService
    {
        Task SendEmailAsync(
            string to,
            string subject = "Kayıt Başarılı",
            string htmlBody = "Kayıt işleminiz başarıyla tamamlanmıştır.",
            List<string> attachmentPaths = null);
    }
}
