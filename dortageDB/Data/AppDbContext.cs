// Data/AppDbContext.cs
using dortageDB.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

public class AppDbContext : IdentityDbContext<AppUser, AppRole, int>
{
    public DbSet<TopraktarProfile> TopraktarProfiles => Set<TopraktarProfile>();
    public DbSet<Musteri> Musteriler => Set<Musteri>();
    public DbSet<Randevu> Randevular => Set<Randevu>();
    public DbSet<Satis> Satislar => Set<Satis>();

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);

        // AppUser
        b.Entity<AppUser>().HasIndex(x => x.TcNo).IsUnique();
        b.Entity<AppUser>().HasIndex(x => x.PhoneNumber).IsUnique();
        b.Entity<AppUser>().HasIndex(x => x.Email).IsUnique();

        // Musteri
        b.Entity<Musteri>().HasKey(x => x.IdMusteri);
        b.Entity<Musteri>().HasIndex(x => x.Telefon).IsUnique();
        b.Entity<Musteri>().HasIndex(x => x.TcNo).IsUnique();
        b.Entity<Musteri>().HasIndex(x => x.Eposta).IsUnique(false);

        // Profile
        b.Entity<TopraktarProfile>()
            .HasOne(tp => tp.User).WithOne(u => u.TopraktarProfile)
            .HasForeignKey<TopraktarProfile>(tp => tp.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Randevu
        b.Entity<Randevu>()
            .HasOne(r => r.Musteri).WithMany(m => m.Randevular)
            .HasForeignKey(r => r.MusteriId)
            .OnDelete(DeleteBehavior.Cascade);

        b.Entity<Randevu>()
            .HasOne(r => r.Topraktar).WithMany(u => u.Randevular)
            .HasForeignKey(r => r.TopraktarID);

        b.Entity<Randevu>().Property(r => r.RandevuDurum).HasConversion<string>();
        b.Entity<Randevu>().HasIndex(r => new { r.RandevuDurum, r.RandevuZaman });

        // Satis
        b.Entity<Satis>()
            .Property(s => s.ToplamSatisFiyati).HasPrecision(14, 2);
        b.Entity<Satis>()
            .Property(s => s.OdenecekKomisyon).HasPrecision(14, 2);

        b.Entity<Satis>()
            .HasOne(s => s.Musteri).WithMany(m => m.Satislar)
            .HasForeignKey(s => s.SatilanMusteriID);

        b.Entity<Satis>()
            .HasOne(s => s.Topraktar).WithMany(u => u.Satislar)
            .HasForeignKey(s => s.TopraktarID);

        b.Entity<Satis>().HasIndex(s => new { s.TopraktarID, s.SatilmaTarihi });
    }
}
