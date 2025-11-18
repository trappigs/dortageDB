namespace dortageDB.Entities;

public class SeoSetting
{
    public int Id { get; set; }

    /// <summary>
    /// Sayfa URL yolu (örn: "/", "/proje", "/hakkimizda")
    /// </summary>
    public required string PagePath { get; set; }

    /// <summary>
    /// Sayfa başlığı
    /// </summary>
    public string? PageTitle { get; set; }

    /// <summary>
    /// Meta açıklama
    /// </summary>
    public string? MetaDescription { get; set; }

    /// <summary>
    /// OG başlık (boşsa PageTitle kullanılır)
    /// </summary>
    public string? OgTitle { get; set; }

    /// <summary>
    /// OG açıklama (boşsa MetaDescription kullanılır)
    /// </summary>
    public string? OgDescription { get; set; }

    /// <summary>
    /// OG görsel URL
    /// </summary>
    public string? OgImage { get; set; }

    /// <summary>
    /// OG type (varsayılan: website)
    /// </summary>
    public string? OgType { get; set; }

    /// <summary>
    /// Yazar (varsayılan: Vekarer)
    /// </summary>
    public string? Author { get; set; }

    /// <summary>
    /// Robots meta tag (varsayılan: index, follow)
    /// </summary>
    public string? Robots { get; set; }

    /// <summary>
    /// Canonical URL (boşsa mevcut URL kullanılır)
    /// </summary>
    public string? CanonicalUrl { get; set; }

    /// <summary>
    /// Twitter başlık (boşsa OgTitle kullanılır)
    /// </summary>
    public string? TwitterTitle { get; set; }

    /// <summary>
    /// Twitter açıklama (boşsa OgDescription kullanılır)
    /// </summary>
    public string? TwitterDescription { get; set; }

    /// <summary>
    /// Aktif mi?
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Oluşturulma tarihi
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    /// <summary>
    /// Güncellenme tarihi
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
}
