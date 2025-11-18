using dortageDB.Data;
using dortageDB.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace dortageDB.Controllers;

public class ProjeController : Controller
{
    private readonly AppDbContext _context;

    public ProjeController(AppDbContext context)
    {
        _context = context;
    }

    // GET: /Proje
    public async Task<IActionResult> Index(string? sehir, decimal? minFiyat, decimal? maxFiyat, bool? taksit, bool? takas, string? yatirimTuru)
    {
        var projelerQuery = _context.Projeler.Where(p => p.AktifMi);

        // Filtreler
        if (!string.IsNullOrEmpty(sehir))
        {
            projelerQuery = projelerQuery.Where(p => p.Sehir == sehir);
        }

        if (minFiyat.HasValue)
        {
            projelerQuery = projelerQuery.Where(p => p.MaxFiyat >= minFiyat.Value);
        }

        if (maxFiyat.HasValue)
        {
            projelerQuery = projelerQuery.Where(p => p.MinFiyat <= maxFiyat.Value);
        }

        if (taksit.HasValue && taksit.Value)
        {
            projelerQuery = projelerQuery.Where(p => p.TaksitImkani);
        }

        if (takas.HasValue && takas.Value)
        {
            projelerQuery = projelerQuery.Where(p => p.TakasImkani);
        }

        if (!string.IsNullOrEmpty(yatirimTuru))
        {
            projelerQuery = projelerQuery.Where(p => p.YatirimTuru == yatirimTuru);
        }

        // Önceliğe ve tarihe göre sırala
        var projeler = await projelerQuery
            .OrderByDescending(p => p.Oncelik)
            .ThenByDescending(p => p.KayitTarihi)
            .ToListAsync();

        // Şehir listesi (filtre için)
        ViewBag.Sehirler = await _context.Projeler
            .Where(p => p.AktifMi && p.Sehir != null)
            .Select(p => p.Sehir!)
            .Distinct()
            .OrderBy(s => s)
            .ToListAsync();

        // Mevcut filtreler
        ViewBag.SelectedSehir = sehir;
        ViewBag.MinFiyat = minFiyat;
        ViewBag.MaxFiyat = maxFiyat;
        ViewBag.Taksit = taksit;
        ViewBag.Takas = takas;
        ViewBag.YatirimTuru = yatirimTuru;

        return View(projeler);
    }

    // GET: /dikili (direkt slug URL)
    public async Task<IActionResult> Details(string? slug)
    {
        if (string.IsNullOrEmpty(slug))
        {
            return NotFound();
        }

        Proje? proje = null;

        // Önce slug'a göre ara
        proje = await _context.Projeler
            .FirstOrDefaultAsync(m => m.Slug == slug && m.AktifMi);

        // Slug bulunamazsa, eski ID formatı için sayısal kontrol yap (geriye dönük uyumluluk)
        if (proje == null && int.TryParse(slug, out int projeId))
        {
            proje = await _context.Projeler
                .FirstOrDefaultAsync(m => m.ProjeID == projeId && m.AktifMi);

            // Eğer ID ile bulunduysa ve slug varsa, slug URL'ine redirect et
            if (proje != null && !string.IsNullOrEmpty(proje.Slug))
            {
                return RedirectPermanent("/" + proje.Slug);
            }
        }

        if (proje == null)
        {
            return NotFound();
        }

        // İstatistikler
        var toplamSatis = await _context.Satislar
            .Where(s => s.ProjeID == proje.ProjeID)
            .CountAsync();

        var satisYuzdesi = proje.ToplamParsel > 0
            ? (double)proje.SatilanParsel / proje.ToplamParsel * 100
            : 0;

        ViewBag.SatisYuzdesi = Math.Round(satisYuzdesi, 1);
        ViewBag.KalanParsel = proje.ToplamParsel - proje.SatilanParsel;

        return View(proje);
    }
}
