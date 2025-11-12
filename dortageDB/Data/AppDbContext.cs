// Data/AppDbContext.cs
using dortageDB.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Reflection.Emit;

namespace dortageDB.Data
{
    public class AppDbContext : IdentityDbContext<AppUser, AppRole, int>
    {
        public DbSet<VisionerProfile> VisionerProfiles => Set<VisionerProfile>();
        public DbSet<Musteri> Musteriler => Set<Musteri>();
        public DbSet<Randevu> Randevular => Set<Randevu>();
        public DbSet<Satis> Satislar => Set<Satis>();
        public DbSet<Referral> Referrals => Set<Referral>();
        public DbSet<Proje> Projeler => Set<Proje>();
        public DbSet<EgitimVideo> EgitimVideolar => Set<EgitimVideo>();
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder b)
        {
            base.OnModelCreating(b);

            // ============================================
            // IDENTITY TABLES (AppUser, AppRole)
            // ============================================
            b.Entity<AppUser>(entity =>
            {
                entity.HasIndex(x => x.TcNo).IsUnique();
                entity.HasIndex(x => x.PhoneNumber).IsUnique();
                entity.HasIndex(x => x.Email).IsUnique();
                entity.HasIndex(x => x.UserName).IsUnique();
            });

            // ============================================
            // VİSİONER PROFILE
            // ============================================
            b.Entity<VisionerProfile>(entity =>
            {
                entity.ToTable("VisionerProfiles");
                entity.HasKey(tp => tp.UserId);

                entity.HasOne(tp => tp.User)
                    .WithOne(u => u.VisionerProfile)
                    .HasForeignKey<VisionerProfile>(tp => tp.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.Property(tp => tp.TotalCommission).HasPrecision(18, 2);
                entity.HasIndex(tp => tp.ReferralCode).IsUnique();
            });

            // ============================================
            // MUSTERI
            // ============================================
            b.Entity<Musteri>(entity =>
            {
                entity.HasKey(m => m.IdMusteri);

                // Visioner İlişkisi
                entity.HasOne(m => m.Visioner)
                    .WithMany(u => u.Musteriler)
                    .HasForeignKey(m => m.VisionerID)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(m => m.Telefon).IsUnique();
                entity.HasIndex(m => m.TcNo).IsUnique();
                entity.HasIndex(m => m.EklenmeTarihi);
                entity.HasIndex(m => m.VisionerID);

                entity.Property(m => m.EklenmeTarihi).HasDefaultValueSql("GETDATE()");
            });

            // ============================================
            // RANDEVU
            // ============================================
            b.Entity<Randevu>(entity =>
            {
                entity.HasKey(r => r.RandevuID);

                // Müşteri İlişkisi
                entity.HasOne(r => r.Musteri)
                    .WithMany(m => m.Randevular)
                    .HasForeignKey(r => r.MusteriId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Visioner İlişkisi
                entity.HasOne(r => r.Visioner)
                    .WithMany(u => u.Randevular)
                    .HasForeignKey(r => r.VisionerID)
                    .OnDelete(DeleteBehavior.Restrict);

                // Proje İlişkisi
                entity.HasOne(r => r.Proje)
                    .WithMany(p => p.Randevular)
                    .HasForeignKey(r => r.ProjeID)
                    .OnDelete(DeleteBehavior.SetNull);

                // Enum -> String dönüşümü
                entity.Property(r => r.RandevuDurum).HasConversion<string>();

                // İndeksler
                entity.HasIndex(r => new { VisionerID = r.VisionerID, r.RandevuZaman });
                entity.HasIndex(r => new { r.RandevuDurum, r.RandevuZaman });
                entity.HasIndex(r => r.MusteriId);

                // Varsayılan değerler
                entity.Property(r => r.OlusturulmaTarihi).HasDefaultValueSql("GETDATE()");
            });

            // ============================================
            // SATIS
            // ============================================
            b.Entity<Satis>(entity =>
            {
                entity.HasKey(s => s.SatisID);

                // Decimal Precision
                entity.Property(s => s.ToplamSatisFiyati).HasPrecision(14, 2);
                entity.Property(s => s.OdenecekKomisyon).HasPrecision(14, 2);

                // Müşteri İlişkisi
                entity.HasOne(s => s.Musteri)
                    .WithMany(m => m.Satislar)
                    .HasForeignKey(s => s.SatilanMusteriID)
                    .OnDelete(DeleteBehavior.Restrict);

                // Visioner İlişkisi
                entity.HasOne(s => s.Visioner)
                    .WithMany(u => u.Satislar)
                    .HasForeignKey(s => s.VisionerID)
                    .OnDelete(DeleteBehavior.Restrict);

                // Proje İlişkisi
                entity.HasOne(s => s.Proje)
                    .WithMany(p => p.Satislar)
                    .HasForeignKey(s => s.ProjeID)
                    .OnDelete(DeleteBehavior.SetNull);

                // İndeksler
                entity.HasIndex(s => new { VisionerID = s.VisionerID, s.SatilmaTarihi });
                entity.HasIndex(s => s.SatilanMusteriID);
                entity.HasIndex(s => s.SatilmaTarihi);

                // Varsayılan değerler
                entity.Property(s => s.OlusturulmaTarihi).HasDefaultValueSql("GETDATE()");
            });

            // ============================================
            // REFERRAL
            // ============================================
            b.Entity<Referral>(entity =>
            {
                entity.HasKey(r => r.Id);

                entity.Property(r => r.Code)
                    .HasMaxLength(32)
                    .IsRequired();

                entity.HasIndex(r => r.Code).IsUnique();
                entity.HasIndex(r => r.CreatedByUserId);
                entity.HasIndex(r => r.IsActive);

                entity.Property(r => r.CreatedAtUtc).HasDefaultValueSql("GETUTCDATE()");
            });

            // ============================================
            // PROJE
            // ============================================
            b.Entity<Proje>(entity =>
            {
                entity.HasKey(p => p.ProjeID);

                entity.Property(p => p.MinFiyat).HasPrecision(18, 2);
                entity.Property(p => p.MaxFiyat).HasPrecision(18, 2);

                entity.HasIndex(p => p.Sehir);
                entity.HasIndex(p => p.AktifMi);
                entity.HasIndex(p => new { p.AktifMi, p.Oncelik });

                entity.Property(p => p.KayitTarihi).HasDefaultValueSql("GETDATE()");
            });

            // ============================================
            // EGITIM VIDEO
            // ============================================
            b.Entity<EgitimVideo>(entity =>
            {
                entity.HasKey(e => e.VideoID);

                entity.HasIndex(e => e.Kategori);
                entity.HasIndex(e => new { e.Aktif, e.Sira });
                entity.HasIndex(e => e.OneEikan);

                entity.Property(e => e.EklenmeTarihi).HasDefaultValueSql("GETDATE()");
            });
        }
    }
}