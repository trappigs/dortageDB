namespace dortageDB.ViewModels
{
    public class RandevuListItemVM
    {
        public int RandevuID { get; set; }
        public string MusteriAdSoyad { get; set; } = null!;
        public string VisionerAdSoyad { get; set; } = null!;
        public DateTime RandevuZaman { get; set; }
        public string Durum { get; set; } = null!;
        public string Bolge { get; set; } = null!;
    }
}
