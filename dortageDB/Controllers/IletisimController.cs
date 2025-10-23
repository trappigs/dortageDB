using Microsoft.AspNetCore.Mvc;
using dortageDB.ViewModels;

namespace dortageDB.Controllers;

public class IletisimController : Controller
{
    private readonly ILogger<IletisimController> _logger;

    public IletisimController(ILogger<IletisimController> logger)
    {
        _logger = logger;
    }

    // GET: Iletisim/Index
    public IActionResult Index()
    {
        return View();
    }

    // POST: Iletisim/Index
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Index(IletisimVM model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            // İletişim formunu logla (ileride email gönderimi eklenebilir)
            _logger.LogInformation($"📧 Yeni iletişim mesajı - Ad: {model.AdSoyad}, Email: {model.Email}, Konu: {model.Konu}");

            TempData["SuccessMessage"] = "Mesajınız başarıyla gönderildi. En kısa sürede size dönüş yapacağız.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError($"❌ İletişim formu hatası: {ex.Message}");
            TempData["ErrorMessage"] = "Bir hata oluştu. Lütfen tekrar deneyin.";
            return View(model);
        }
    }
}
