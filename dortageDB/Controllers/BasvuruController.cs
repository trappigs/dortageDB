using dortageDB.Data;
using dortageDB.Entities;
using dortageDB.Services;
using dortageDB.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace dortageDB.Controllers
{
    public class BasvuruController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IEmailService _emailService;

        public BasvuruController(AppDbContext context, IWebHostEnvironment webHostEnvironment, IEmailService emailService)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
            _emailService = emailService;
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Submit(BasvuruCreateVM model)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return Json(new { success = false, message = "Lütfen tüm zorunlu alanları doldurunuz.", errors = errors });
            }

            if (model.CvDosyasi != null)
            {
                if (model.CvDosyasi.Length > 5 * 1024 * 1024) // 5MB
                {
                    return Json(new { success = false, message = "Dosya boyutu 5MB'dan büyük olamaz." });
                }

                // 1. Uzantı Kontrolü (Hızlı Kontrol)
                var ext = Path.GetExtension(model.CvDosyasi.FileName).ToLowerInvariant();
                if (ext != ".pdf")
                {
                    return Json(new { success = false, message = "Sadece PDF dosyası yükleyebilirsiniz." });
                }

                // 2. İmza (Magic Number) Kontrolü (Güvenli Kontrol)
                try
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        await model.CvDosyasi.CopyToAsync(memoryStream);
                        var bytes = memoryStream.ToArray();
                        
                        // PDF Magic Number: %PDF (Hex: 25 50 44 46)
                        if (bytes.Length < 4 || 
                            bytes[0] != 0x25 || 
                            bytes[1] != 0x50 || 
                            bytes[2] != 0x44 || 
                            bytes[3] != 0x46)
                        {
                            return Json(new { success = false, message = "Geçersiz dosya formatı. Lütfen gerçek bir PDF dosyası yükleyiniz." });
                        }
                    }
                    // Stream pozisyonunu başa sar (Create işleminde tekrar kullanılacak)
                    model.CvDosyasi.OpenReadStream().Position = 0;
                }
                catch
                {
                    return Json(new { success = false, message = "Dosya doğrulaması sırasında bir hata oluştu." });
                }
            }

            string uniqueFileName = null;
            if (model.CvDosyasi != null)
            {
                string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "cvs");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }
                uniqueFileName = Guid.NewGuid().ToString() + "_" + model.CvDosyasi.FileName;
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await model.CvDosyasi.CopyToAsync(fileStream);
                }
            }

            var basvuru = new Basvuru
            {
                AdSoyad = model.AdSoyad,
                Telefon = model.Telefon,
                Email = model.Email,
                Il = model.Il,
                Ilce = model.Ilce,
                Meslek = model.Meslek,
                EgitimDurumu = model.EgitimDurumu,
                GayrimenkulTecrubesi = model.GayrimenkulTecrubesi,
                NeredenDuydunuz = model.NeredenDuydunuz,
                KendiniziTanitin = model.KendiniziTanitin,
                Beklentiniz = model.Beklentiniz,
                CvDosyaYolu = uniqueFileName,
                SosyalMedyaLink = model.SosyalMedyaLink,
                KvkkOnay = model.KvkkOnay,
                BasvuruTarihi = DateTime.Now,
                Durum = BasvuruDurum.Bekliyor
            };

            _context.Basvurular.Add(basvuru);
            await _context.SaveChangesAsync();

            // Send Email
            try
            {
                // HTML Encode inputs to prevent Injection
                var encodedAdSoyad = System.Net.WebUtility.HtmlEncode(model.AdSoyad);
                var encodedTelefon = System.Net.WebUtility.HtmlEncode(model.Telefon);
                var encodedEmail = System.Net.WebUtility.HtmlEncode(model.Email);
                var encodedIl = System.Net.WebUtility.HtmlEncode(model.Il);
                var encodedIlce = System.Net.WebUtility.HtmlEncode(model.Ilce);
                var encodedMeslek = System.Net.WebUtility.HtmlEncode(model.Meslek);
                var encodedEgitim = System.Net.WebUtility.HtmlEncode(model.EgitimDurumu);
                var encodedTecrube = System.Net.WebUtility.HtmlEncode(model.GayrimenkulTecrubesi);
                var encodedNereden = System.Net.WebUtility.HtmlEncode(model.NeredenDuydunuz);
                var encodedTanitim = System.Net.WebUtility.HtmlEncode(model.KendiniziTanitin);
                var encodedBeklenti = System.Net.WebUtility.HtmlEncode(model.Beklentiniz ?? "-");
                var encodedSosyal = System.Net.WebUtility.HtmlEncode(model.SosyalMedyaLink ?? "-");

                // 1. Başvuru sahibine onay maili
                string subject = "Başvurunuz Alındı - Vekarer";
                string body = $@"
                    <h3>Sayın {encodedAdSoyad},</h3>
                    <p>Vekarer'e yaptığınız başvuru başarıyla alınmıştır.</p>
                    <p>Başvurunuz ekibimiz tarafından incelendikten sonra sizinle iletişime geçilecektir.</p>
                    <br>
                    <p>Saygılarımızla,</p>
                    <p>Vekarer Ekibi</p>";

                await _emailService.SendEmailAsync(model.Email, subject, body);

                // 2. Yöneticiye (info@dortage.com) bildirim maili
                string adminSubject = "Yeni Referans Kodu Talebi (Başvuru)";
                string adminBody = $@"
                    <h2>Yeni Başvuru Alındı</h2>
                    <p><strong>Ad Soyad:</strong> {encodedAdSoyad}</p>
                    <p><strong>Telefon:</strong> {encodedTelefon}</p>
                    <p><strong>Email:</strong> {encodedEmail}</p>
                    <p><strong>Şehir:</strong> {encodedIl} / {encodedIlce}</p>
                    <p><strong>Meslek:</strong> {encodedMeslek}</p>
                    <p><strong>Eğitim:</strong> {encodedEgitim}</p>
                    <p><strong>Tecrübe:</strong> {encodedTecrube}</p>
                    <p><strong>Referans Kaynağı:</strong> {encodedNereden}</p>
                    <hr>
                    <p><strong>Kendini Tanıtma:</strong><br>{encodedTanitim}</p>
                    <p><strong>Beklenti:</strong><br>{encodedBeklenti}</p>
                    <p><strong>Sosyal Medya:</strong> {encodedSosyal}</p>
                    <p><strong>CV:</strong> {(uniqueFileName != null ? "Yüklendi (" + uniqueFileName + ")" : "Yüklenmedi")}</p>
                ";

                var attachments = new List<string>();
                if (uniqueFileName != null)
                {
                    string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "cvs");
                    string cvPath = Path.Combine(uploadsFolder, uniqueFileName);
                    attachments.Add(cvPath);
                }

                await _emailService.SendEmailAsync("info@dortage.com", adminSubject, adminBody, attachments);
            }
            catch (Exception)
            {
                // Email sending failed, but application is saved. 
                // We might want to log this error, but for now we won't fail the request.
            }

            return Json(new { success = true, message = "Başvurunuz başarıyla alınmıştır. En kısa sürede sizinle iletişime geçilecektir." });
        }
    }
}
