# SOVEREIGN EXECUTIVE SUITE: ENTERPRISE MULTI-TENANT DOCUMENT AUTOMATION & CLOSED-ECOSYSTEM OFFICE SUITE

<p align="center">
  <img src="desktop/DesignSystem/Assets/logo.png" alt="Sovereign Executive Suite Logo" width="160" height="160" />
</p>

<p align="center">
  <strong>Ekosistem Tertutup (Closed-Ecosystem Desktop SaaS) Otomasi Persuratan, Proposal RAB, Manajemen SDM Adaptif, dan Manajemen Arsip Kedinasan Berbasis Cloud (Multi-Tenant)</strong>
</p>

<p align="center">
  <img src="https://img.shields.io/badge/.NET-8.0%20WPF-512BD4?style=flat-square&logo=dotnet&logoColor=white" alt=".NET 8.0" />
  <img src="https://img.shields.io/badge/UI%2FUX-WPF--UI%20Fluent%20Design-0078D4?style=flat-square&logo=windows11&logoColor=white" alt="WPF-UI Fluent" />
  <img src="https://img.shields.io/badge/Backend-Cloudflare%20Workers%20%2B%20Hono-F38020?style=flat-square&logo=cloudflare&logoColor=white" alt="Cloudflare Workers" />
  <img src="https://img.shields.io/badge/Database-Cloudflare%20D1%20SQLite%20Edge-0051C3?style=flat-square&logo=sqlite&logoColor=white" alt="Cloudflare D1" />
  <img src="https://img.shields.io/badge/Security-Authenticode%20Code%20Signed-059669?style=flat-square" alt="Authenticode Signed" />
</p>

---

## 🏛️ Gambaran Umum Ekosistem

**Sovereign Executive Suite** adalah rangkaian produk piranti lunak komersial skala enterprise (**B2B & B2G**) yang dirancang untuk instansi pemerintah desa, sekolah/institusi pendidikan, dinas, yayasan, korporasi swasta, hingga rumah sakit/klinik. Seluruh infrastruktur database dan penyimpanan berkas beroperasi terpusat di Cloudflare D1 dan Cloudinary, namun terisolasi secara ketat per instansi (`tenant_id`).

### 3 Pilar Utama Komponen Sistem:

1. **Backend Serverless Edge (`/backend`)**:
   - Ditenagai oleh **Cloudflare Workers** + **Hono** (TypeScript) berlatensi ultra-rendah di jaringan edge global Cloudflare.
   - Database terdistribusi **Cloudflare D1 (SQLite Edge)** dengan skema isolasi data *Multi-Tenant*.
   - Otentikasi sesi via **JWT (Web Crypto API)** dengan hashing kata sandi **PBKDF2/SHA-256 + Salt**.
   - Otentikasi tanda tangan berkas langsung terenkripsi ke **Cloudinary Storage**.

2. **Pustaka Bersama (`/desktop/DesignSystem`)**:
   - Class Library WPF (`net8.0-windows`) berdesain **Windows 11 Fluent Design** dengan backdrop material **Mica**.
   - Terpusat pada design tokens: `Tokens.xaml`, `Typography.xaml` (*Segoe UI Variable*), serta skala jarak 4/8/12/16/24/32 px.
   - Komponen visual modular: `StatusBadge`, `TierChip`, `KpiCard`, `PageHeader`, `EmptyState`, dan `FormSection`.

3. **Konsol Provisioning Pusat (`/desktop/CoreProvisioningConsole`)**:
   - Desktop console khusus Owner/Developer untuk menerbitkan dan mengelola token lisensi, batas tier dinamis, buletin eksekutif, teks berjalan (*running ticker*), direktori kuota instansi, dan fitur darurat *Reset Password Operator*.

4. **Aplikasi Desktop Instansi Klien (`/desktop/ClientApp`)**:
   - Aplikasi desktop mandiri untuk pengguna instansi pembeli lisensi dengan fitur *Adaptive Institution Chameleon*, editor in-app Word & Excel RAB, dan *Zero-Download RAM-Only Policy*.

---

## 🌟 Fitur Utama & Keunggulan Enterprise

### 1. Adaptive Institution Chameleon
Antarmuka dan peristilahan sistem secara dinamis beradaptasi 100% mengikuti jenis instansi pemegang lisensi:
- **Sekolah / Pendidikan**: Istilah SDM berubah menjadi *Dewan Guru & Tenaga Kependidikan*, template surat mencakup Keterangan Siswa Aktif, Panggilan Wali Murid, SPPD Pengawas.
- **Pemerintah Desa / Kelurahan**: Istilah SDM menjadi *Perangkat & Staf Kantor Desa*, template mencakup Surat Keterangan Usaha (SKU), Domisili, Pengantar SKCK, Keterangan Tidak Mampu (SKTM).
- **Perusahaan / PT**: Istilah SDM menjadi *Karyawan & Divisi*, template mencakup Surat Penawaran Kerjasama, Perjanjian Kerja Waktu Tertentu (PKWT), Undangan RUPS, Memo Internal.
- **Rumah Sakit / Faskes**: Istilah SDM menjadi *Tenaga Medis & Dokter*, template mencakup Rujukan, Keterangan Rawat Inap, Surat Balasan Diagnosa.
- **Yayasan / Lembaga Sosial**: Istilah SDM menjadi *Pengurus & Relawan*, template mencakup Tanda Terima Hibah, Mandat Program, Laporan Pertanggungjawaban.

### 2. In-Memory Direct RAM Stream (Zero-Download Policy)
- Berkas template Word (`.docx`) dan lembar kerja RAB (`.xlsx`) diolah langsung di dalam aliran memori RAM (*MemoryStream*).
- Menjamin perlindungan dokumen kedinasan rahasia tanpa pernah meninggalkan jejak file mentah sementara di folder unduhan lokal pengguna (*No temp file leaks*).

### 3. Tier Entitlement & Kapasitas Dinamis
Hak akses dan limitasi fitur dikendalikan secara real-time dari Cloud Edge D1:
- **Trial**: Evaluasi terbatas (3 pengguna, 250 MB storage).
- **Basic**: Instansi tingkat dasar (5 pengguna, 1 GB storage).
- **Standard**: Instansi tingkat reguler (15 pengguna, 2.5 GB storage).
- **Professional**: Instansi korporat skala penuh (50 pengguna, 10 GB storage).
- **Enterprise**: Kapasitas tanpa batas dedikasi penuh (Kustom pengguna & storage besar).

### 4. Stealth Contact & Support Gateway
Sistem mengarahkan permohonan upgrade tier, kuota penuh, dan bantuan darurat ke saluran resmi WhatsApp Pusat (`628589584257`) secara terenkripsi di balik tombol berwibawa `[ 💬 Hubungi Kami ]` tanpa mengekspos deretan nomor telanjang di layar.

### 5. Windows Authenticode Code Signing
Seluruh file biner eksekusi (`.exe`) yang dirilis secara otomatis ditandatangani menggunakan sertifikat digital resmi:
`CN=Sovereign Executive Systems Code Signing, O=Sovereign Executive Systems, C=ID`.

---

## 📁 Struktur Monorepo

```text
.
├── backend/                             # Serverless Cloudflare Workers + Hono
│   ├── migrations/
│   │   └── 0001_init.sql                # Skema 8 tabel D1 Edge (tenants, licenses, tiers, users, docs)
│   ├── src/
│   │   ├── index.ts                     # RESTful Hono API, Auth JWT, RBAC Middleware
│   │   └── types.ts                     # Definisi tipe data & kontrak request/response
│   ├── wrangler.toml                    # Konfigurasi binding Cloudflare D1
│   └── package.json
│
├── database/
│   └── seed.sql                         # Master data: paket tier, lisensi aktif, preset template
│
├── desktop/
│   ├── DesignSystem/                    # [SHARED CLASS LIBRARY] Fluent Design System
│   │   ├── Assets/                      # Logo resmi (logo.png, app.ico)
│   │   ├── Controls/                    # StatusBadge, TierChip, KpiCard, PageHeader, EmptyState
│   │   ├── Converters/                  # Value converters data binding
│   │   ├── Tokens.xaml                  # Palet warna enterprise, radius, elevasi & spasi
│   │   ├── Typography.xaml              # Hirarki font Segoe UI Variable
│   │   └── Components.xaml              # Kamus gabungan resource dictionary
│   │
│   ├── CoreProvisioningConsole/         # [KONSOL MASTER/DEVELOPER] Pusat Kendali Lisensi
│   │   ├── Assets/                      # Ikon & logo ter-embed
│   │   ├── Models/                      # DevLicenseModel, DevTenantModel, DevBroadcastModel
│   │   ├── Services/                    # MasterDevService, DevDialogService
│   │   ├── ViewModels/                  # MVVM CommunityToolkit ViewModels
│   │   ├── Views/                       # LicensesView, TiersView, BroadcastView, TenantsView, RescueView
│   │   ├── DevMainWindow.xaml           # FluentWindow dengan Mica backdrop & TitleBar modern
│   │   └── CoreProvisioningConsole.csproj
│   │
│   └── ClientApp/                       # [APLIKASI INSTANSI KLIEN] Sovereign Suite Desktop
│       ├── Assets/                      # Logo resmi ter-embed
│       ├── Services/                    # ChameleonTerminologyService, ApiService, SessionService
│       ├── Models/                      # ClientModels
│       ├── AuthWindow.xaml              # Layar login & aktivasi lisensi split-screen
│       ├── MainWindow.xaml              # Workspace dinamis, editor in-app, agenda persuratan
│       └── ClientApp.csproj
│
├── dist/                                # Output direktori file executable siap rilis
│   ├── ClientApp/                       # ClientApp.exe (Authenticode Signed)
│   └── CoreProvisioningConsole/         # CoreProvisioningConsole.exe (Authenticode Signed)
│
├── build-exe.bat                        # Skrip rilis otomatis single-file .exe kedua aplikasi
├── sign-exe.ps1                         # Skrip otomatis tanda tangan digital Authenticode
├── init-local-db.bat                    # Skrip inisialisasi database lokal Cloudflare D1
└── README.md
```

---

## 🚀 Panduan Menjalankan Secara Lokal

### 1. Prasyarat Sistem
- **Windows 10 / 11** (Skala DPI 100%–200%).
- **.NET 8.0 SDK** (x64).
- **Node.js v18+** & npm.
- **Cloudflare Wrangler CLI** (`npm i -g wrangler`).

### 2. Inisialisasi Database D1 Lokal
Jalankan file batch berikut untuk membuat dan mengisi database lokal:
```cmd
init-local-db.bat
```

### 3. Menjalankan Backend API Dev Server
```cmd
npm run dev
```
Server berjalan di alamat: `http://127.0.0.1:8787`.

### 4. Mengompilasi & Menerbitkan Executable Mandiri (1-Klik)
Untuk menghasilkan file `.exe` rilis resmi (*Single-File Self-Contained*) yang otomatis ditandatangani dengan sertifikat digital Authenticode, jalankan:
```cmd
build-exe.bat
```
File executable siap pakai langsung tersedia di folder:
- **`dist/CoreProvisioningConsole/CoreProvisioningConsole.exe`** (Konsol Lisensi)
- **`dist/ClientApp/ClientApp.exe`** (Aplikasi Instansi Klien)

---

## 📦 Unduh Biner Siap Pakai Langsung dari GitHub (Tanpa Compile)

Proyek ini telah dilengkapi dengan **GitHub Actions CI/CD Pipeline Otomatis** (`.github/workflows/desktop-build.yml`). Setiap pembaruan kode akan otomatis mengompilasi, memaketkan, dan menandatangani file `.exe` dengan sertifikat digital resmi Authenticode.

### Cara Mengunduh File `.exe` di Komputer Rumah / Kantor:
1. Buka repositori GitHub: **[devsdnkls01/app-dokument-arsip](https://github.com/devsdnkls01/app-dokument-arsip)**
2. Klik tab **"Actions"** di menu navigasi atas repository.
3. Klik workflow run terbaru pada workflow **"Build & Package Desktop Applications"**.
4. Gulir ke bawah ke bagian **Artifacts**:
   - 📥 **`ClientApp-win-x64`**: File `ClientApp.exe` siap pakai untuk instansi.
   - 📥 **`CoreProvisioningConsole-win-x64`**: File `CoreProvisioningConsole.exe` khusus developer.
   - 📥 **`SovereignExecutiveSuite-Complete-Package`**: Paket lengkap berisi kedua aplikasi.
5. Ekstrak file `.zip` dan jalankan aplikasi secara langsung!

---

## 🔑 Data Uji Coba & Lisensi Siap Pakai

Berikut data lisensi dan kredensial uji coba yang telah terpasang pada master seed database:

### 1. Token Lisensi untuk Instansi Baru
- **Lisensi Standard**: `ENT-STD-2026-NODE2` (Tier Standard, Alokasi Instansi Reguler)
- **Lisensi Profesional**: `ENT-PRO-2026-NODE3` (Tier Professional, Alokasi Korporat Penuh)
- **Lisensi Enterprise**: `ENT-ENT-2026-NODE4` (Tier Enterprise, Alokasi Dedicated Uncapped)
- **Lisensi Evaluasi**: `ENT-TRL-2026-DEMO` (Tier Trial, Evaluasi Terbatas)

### 2. Kredensial Akses Konsol Master
- **Kunci Akses Master**: `DEV-MASTER-GODMODE-2026` (Terverifikasi otomatis di `CoreProvisioningConsole`).

---

## 🛡️ Lisensi & Hak Cipta
Hak Cipta © 2026 **Sovereign Executive Systems**. Seluruh hak cipta dilindungi undang-undang.
Dilarang mendistribusikan, merekayasa balik, atau memodifikasi biner piranti lunak tanpa izin tertulis dari pemilik hak cipta.
