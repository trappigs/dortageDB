# Vekarer - Şifre Güncelleme Aracı
# Mail ve veritabanı şifrelerini sorar, appsettings.Secrets.json dosyasına yazar.
# Bu dosya git'e gönderilmez. Sunucuda, yayınlanan uygulama klasöründe çalıştırın.

$ErrorActionPreference = 'Stop'
$secretsPath = Join-Path $PSScriptRoot 'appsettings.Secrets.json'

function Read-Secret([string]$prompt) {
    while ($true) {
        $first = Read-Host -Prompt $prompt -AsSecureString
        $plain = [Runtime.InteropServices.Marshal]::PtrToStringBSTR(
            [Runtime.InteropServices.Marshal]::SecureStringToBSTR($first))
        if ([string]::IsNullOrEmpty($plain)) { return $null }

        $second = Read-Host -Prompt '  Tekrar girin' -AsSecureString
        $plain2 = [Runtime.InteropServices.Marshal]::PtrToStringBSTR(
            [Runtime.InteropServices.Marshal]::SecureStringToBSTR($second))
        if ($plain -eq $plain2) { return $plain }

        Write-Host '  Şifreler eşleşmedi, tekrar deneyin.' -ForegroundColor Red
    }
}

# Mevcut değerleri oku (boş bırakılan alan eskisini korur)
$mailPassword = $null
$dbPassword = $null
$adminSeedPassword = $null
if (Test-Path $secretsPath) {
    $existing = Get-Content $secretsPath -Raw -Encoding UTF8 | ConvertFrom-Json
    if ($existing.MailSettings) { $mailPassword = $existing.MailSettings.Password }
    $dbPassword = $existing.DbPassword
    $adminSeedPassword = $existing.AdminSeedPassword
}

Write-Host ''
Write-Host '=== Vekarer Şifre Güncelleme ===' -ForegroundColor Cyan
Write-Host 'Değiştirmek istemediğiniz alanı boş bırakıp Enter''a basın.'
Write-Host ''

$newMail = Read-Secret 'info@dortage.com mail şifresi'
if ($newMail) { $mailPassword = $newMail }

$newDb = Read-Secret 'Veritabanı (dortageUser) şifresi'
if ($newDb) { $dbPassword = $newDb }

$secrets = [ordered]@{
    MailSettings = [ordered]@{ Password = $mailPassword }
}
if ($dbPassword) { $secrets.DbPassword = $dbPassword }
if ($adminSeedPassword) { $secrets.AdminSeedPassword = $adminSeedPassword }

$json = $secrets | ConvertTo-Json -Depth 5
[IO.File]::WriteAllText($secretsPath, $json, (New-Object Text.UTF8Encoding $false))

Write-Host ''
Write-Host "Kaydedildi: $secretsPath" -ForegroundColor Green

# IIS altında çalışıyorsa web.config'e dokunarak uygulamayı yeniden başlat
$webConfig = Join-Path $PSScriptRoot 'web.config'
if (Test-Path $webConfig) {
    (Get-Item $webConfig).LastWriteTime = Get-Date
    Write-Host 'Uygulama yeniden başlatıldı (IIS).' -ForegroundColor Green
} else {
    Write-Host 'Yeni şifrelerin geçerli olması için uygulamayı yeniden başlatın.' -ForegroundColor Yellow
}
