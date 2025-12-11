-- Belirtilen kullanıcı ID'lerini silme işlemi
-- Bu komutu veritabanı yönetim aracınızda çalıştırabilirsiniz.

-- 1. Kullanıcı ID'lerini tanımla
DECLARE @UserIds TABLE (Id INT);
INSERT INTO @UserIds (Id) VALUES (3), (4), (5), (6);

-- 2. Satislar tablosundan ilgili kayıtları sil
-- Bu vekarer tarafından yapılan satışları ve bu vekarerin müşterileriyle yapılan satışları siler.
DELETE FROM Satislar WHERE VekarerID IN (SELECT Id FROM @UserIds);
DELETE FROM Satislar WHERE SatilanMusteriID IN (SELECT IdMusteri FROM Musteriler WHERE VekarerID IN (SELECT Id FROM @UserIds));

-- 3. Randevular tablosundan ilgili kayıtları sil
-- Bu vekarere ait randevuları ve bu vekarerin müşterileriyle yapılan randevuları siler.
DELETE FROM Randevular WHERE VekarerID IN (SELECT Id FROM @UserIds);
DELETE FROM Randevular WHERE MusteriId IN (SELECT IdMusteri FROM Musteriler WHERE VekarerID IN (SELECT Id FROM @UserIds));

-- 4. Musteriler tablosundan ilgili kayıtları sil
-- Bu vekarerler tarafından eklenen müşterileri siler.
DELETE FROM Musteriler WHERE VekarerID IN (SELECT Id FROM @UserIds);

-- 5. VekarerProfiles tablosundan ilgili kayıtları sil
DELETE FROM VekarerProfiles WHERE UserId IN (SELECT Id FROM @UserIds);

-- 6. Referrals tablosundan ilgili kayıtları sil
DELETE FROM Referrals WHERE CreatedByUserId IN (SELECT Id FROM @UserIds);

-- 7. Identity tablolarından ilgili kayıtları sil (ASP.NET Core Identity varsayılan tabloları)
DELETE FROM AspNetUserRoles WHERE UserId IN (SELECT Id FROM @UserIds);
DELETE FROM AspNetUserClaims WHERE UserId IN (SELECT Id FROM @UserIds);
DELETE FROM AspNetUserLogins WHERE UserId IN (SELECT Id FROM @UserIds);
DELETE FROM AspNetUserTokens WHERE UserId IN (SELECT Id FROM @UserIds);

-- 8. Son olarak AspNetUsers tablosundan kullanıcıları sil
DELETE FROM AspNetUsers WHERE Id IN (SELECT Id FROM @UserIds);

-- İşlem tamamlandığında bir mesaj göster
SELECT 'Belirtilen kullanıcılar ve ilişkili veriler başarıyla silindi.' AS Sonuc;
