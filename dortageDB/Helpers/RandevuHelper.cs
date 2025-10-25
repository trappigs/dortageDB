using dortageDB.Entities;

namespace dortageDB.Helpers
{
    public static class RandevuHelper
    {
        public static string GetDurumText(RandevuDurum durum)
        {
            return durum switch
            {
                RandevuDurum.OnayBekliyor => "Onay Bekliyor",
                RandevuDurum.GorusmeBekleniyor => "Görüşme Bekleniyor",
                RandevuDurum.KararBekleniyor => "Görüşüldü - Karar Bekleniyor",
                RandevuDurum.Olumsuz => "Görüşüldü - Olumsuz",
                RandevuDurum.KaporaAlindi => "Görüşüldü - Kapora Alındı",
                RandevuDurum.OdemeAlindi => "Görüşüldü - Ödeme Alındı",
                RandevuDurum.Gerceklesmedi => "Gerçekleşmedi",
                RandevuDurum.Iptal => "İptal",
                _ => "Bilinmeyen"
            };
        }

        public static string GetDurumClass(RandevuDurum durum)
        {
            return durum switch
            {
                RandevuDurum.OnayBekliyor => "status-pending",
                RandevuDurum.GorusmeBekleniyor => "status-confirmed",
                RandevuDurum.KararBekleniyor => "status-in-progress",
                RandevuDurum.Olumsuz => "status-cancelled",
                RandevuDurum.KaporaAlindi => "status-success",
                RandevuDurum.OdemeAlindi => "status-completed",
                RandevuDurum.Gerceklesmedi => "status-no-show",
                RandevuDurum.Iptal => "status-cancelled",
                _ => "status-pending"
            };
        }

        public static string GetDurumIcon(RandevuDurum durum)
        {
            return durum switch
            {
                RandevuDurum.OnayBekliyor => "⏳",
                RandevuDurum.GorusmeBekleniyor => "📅",
                RandevuDurum.KararBekleniyor => "⏱️",
                RandevuDurum.Olumsuz => "❌",
                RandevuDurum.KaporaAlindi => "💰",
                RandevuDurum.OdemeAlindi => "✅",
                RandevuDurum.Gerceklesmedi => "⊘",
                RandevuDurum.Iptal => "✗",
                _ => "?"
            };
        }
    }
}
