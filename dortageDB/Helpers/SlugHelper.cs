using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace dortageDB.Helpers;

public static class SlugHelper
{
    public static string GenerateSlug(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        // Türkçe karakterleri dönüştür
        text = text.Replace("İ", "i");
        text = text.Replace("ı", "i");
        text = text.Replace("Ğ", "g");
        text = text.Replace("ğ", "g");
        text = text.Replace("Ü", "u");
        text = text.Replace("ü", "u");
        text = text.Replace("Ş", "s");
        text = text.Replace("ş", "s");
        text = text.Replace("Ö", "o");
        text = text.Replace("ö", "o");
        text = text.Replace("Ç", "c");
        text = text.Replace("ç", "c");

        // Küçük harfe çevir
        text = text.ToLowerInvariant();

        // Diğer aksanlı karakterleri temizle
        text = RemoveDiacritics(text);

        // Geçersiz karakterleri tire ile değiştir
        text = Regex.Replace(text, @"[^a-z0-9\s-]", "");

        // Birden fazla boşluğu tek boşluğa çevir
        text = Regex.Replace(text, @"\s+", " ").Trim();

        // Boşlukları tire ile değiştir
        text = Regex.Replace(text, @"\s", "-");

        // Birden fazla tireyi tek tireye çevir
        text = Regex.Replace(text, @"-+", "-");

        // Baştaki ve sondaki tireleri temizle
        text = text.Trim('-');

        return text;
    }

    private static string RemoveDiacritics(string text)
    {
        var normalizedString = text.Normalize(NormalizationForm.FormD);
        var stringBuilder = new StringBuilder();

        foreach (var c in normalizedString)
        {
            var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
            if (unicodeCategory != UnicodeCategory.NonSpacingMark)
            {
                stringBuilder.Append(c);
            }
        }

        return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
    }
}
