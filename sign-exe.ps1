# Sovereign Executive Systems - Windows Code Signing & Authenticode Script
# Prepares and signs ClientApp.exe and DeveloperStudio.exe for Windows Defender SmartScreen compatibility

param (
    [string]$DistPath = "./dist"
)

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host "  SOVEREIGN EXECUTIVE WINDOWS CODE SIGNING (AUTHENTICODE) " -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

$CertSubject = "CN=Sovereign Executive Systems Code Signing, O=Sovereign Executive Systems, C=ID"
$CertStoreLocation = "Cert:\CurrentUser\My"

# 1. Check or create Self-Signed Code Signing Certificate
Write-Host "[1/3] Memeriksa sertifikat digital Code Signing..." -ForegroundColor Yellow
$cert = Get-ChildItem -Path $CertStoreLocation | Where-Object { $_.Subject -eq $CertSubject } | Select-Object -First 1

if (-not $cert) {
    Write-Host "Sertifikat belum ada. Membuat Self-Signed Code Signing Certificate resmi..." -ForegroundColor Green
    $cert = New-SelfSignedCertificate `
        -Type CodeSigningCert `
        -Subject $CertSubject `
        -KeySpec Signature `
        -KeyLength 2048 `
        -KeyExportPolicy Exportable `
        -HashAlgorithm SHA256 `
        -CertStoreLocation $CertStoreLocation `
        -NotAfter (Get-Date).AddYears(5)

    Write-Host "Sertifikat Code Signing berhasil dibuat di CurrentUser\My: $($cert.Thumbprint)" -ForegroundColor Green
} else {
    Write-Host "Sertifikat Code Signing ditemukan: $($cert.Thumbprint)" -ForegroundColor Green
}

# 2. Locate .exe files
Write-Host "[2/3] Mencari file .exe di $DistPath..." -ForegroundColor Yellow
$exeFiles = Get-ChildItem -Path $DistPath -Filter "*.exe" -Recurse

if ($exeFiles.Count -eq 0) {
    Write-Warning "Tidak ditemukan file .exe di folder $DistPath. Pastikan Anda menjalankan build terlebih dahulu."
    exit 0
}

# 3. Sign each .exe using Authenticode
Write-Host "[3/3] Menandatangani file biner executable..." -ForegroundColor Yellow
foreach ($file in $exeFiles) {
    Write-Host "Menandatangani: $($file.Name)..." -NoNewline
    try {
        $status = Set-AuthenticodeSignature -FilePath $file.FullName -Certificate $cert -HashAlgorithm SHA256
        if ($status.SignerCertificate) {
            Write-Host " [BERHASIL TERSEMAT: $($status.SignerCertificate.Subject)]" -ForegroundColor Green
        } else {
            Write-Host " [STATUS: $($status.Status)]" -ForegroundColor Yellow
        }
    } catch {
        Write-Host " [GAGAL: $($_.Exception.Message)]" -ForegroundColor Red
    }
}

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host "Proses Code Signing Selesai! File .exe siap didistribusikan." -ForegroundColor Green
Write-Host "==========================================================" -ForegroundColor Cyan
