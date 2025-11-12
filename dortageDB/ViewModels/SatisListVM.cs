namespace dortageDB.ViewModels
{
    public class SatisListItemVM
    {
        public int SatisID { get; set; }
        public string MusteriAdSoyad { get; set; } = null!;
        public string VisionerAdSoyad { get; set; } = null!;
        public DateTime SatilmaTarihi { get; set; }
        public decimal ToplamSatisFiyati { get; set; }
        public bool Taksit { get; set; }
        public decimal OdenecekKomisyon { get; set; }
    }
}
