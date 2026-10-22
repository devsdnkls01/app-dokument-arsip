# Sistem Otomasi Dokumen & Manajemen Arsip Digital (DocuFlow Pro)

Sistem otomasi pembuatan dokumen dan surat dinamis berbasis monorepo yang menggabungkan aplikasi desktop **C# (.NET 8 WPF)** mandiri (*Single-File Executable*) dengan backend serverless **Hono + Cloudflare Workers (D1 SQLite)** dan penyimpanan aset **Cloudinary**.

---

## 🏛️ 8 Modul Arsitektur Sistem

| No | Modul / Area Sistem | Fitur Implementasi | Manfaat Utama |
|---|---|---|---|
| **1** | **Aplikasi Desktop (.exe)** | • Skrip build lokal 1-klik ([`build.bat`](file:///b:/WEBSITE/.dokument-arsip/build.bat))<br>• Penyimpanan draf offline ([`OfflineDrafts.json`](file:///b:/WEBSITE/.dokument-arsip/desktop/Services/LocalDraftService.cs)) | Kontrol penuh build di PC sendiri tanpa antrean GitHub. Staf tetap bisa mengetik draf saat internet putus. |
| **2** | **Distribusi & Auto-Update** | • Tabel versi di Cloudflare D1 (`app_versions`)<br>• Banner notifikasi versi baru & link unduh langsung dari Cloudinary/Drive/LAN | Distribusi update tanpa perlu akun GitHub untuk guru / staf TU. |
| **3** | **Format & Menu Dinamis** | • Auto-scanner placeholder `{{...}}` langsung dari berkas template master `.docx` | Otomatis membuat draf form input tanpa perlu mengetik satu per satu di database. |
| **4** | **Backend & Auto-Numbering** | • Cloudflare Workers (Hono) 24/7 di Edge<br>• Penomoran surat berurutan secara atomic di D1<br>• Audit logging pembuat surat | Mencegah nomor surat ganda (*double number*) antar komputer staf. |
| **5** | **Validasi Keaslian QR Code** | • Pustaka QRCoder menyematkan gambar QR Code otomatis di dalam dokumen `.docx`<br>• Halaman verifikasi publik di `/verify/:docId` | Mencegah pemalsuan surat. Pihak luar cukup scan dengan kamera HP untuk cek keabsahan surat & scan basah. |
| **6** | **Arsip Dokumen & Buku Agenda** | • Pencarian instan per nomor & perihal surat<br>• Tombol **Ekspor Buku Agenda (Excel / CSV)** | Merekap agenda surat keluar bulanan secara otomatis tanpa buku manual. |
| **7** | **Arsip Scan Basah & Kompresi** | • Dropzone drag-and-drop berkas PDF/JPG<br>• Kompresi otomatis Cloudinary<br>• Label warna status (Merah = Belum scan, Hijau = Lengkap) | Memudahkan pemantauan surat yang belum diserahkan bukti fisik stempel basahnya. |
| **8** | **Keamanan & Deployment** | • Proteksi PIN API (`X-App-Pin`) di setiap request tulis<br>• Deploy 1-klik via Wrangler CLI ([`deploy.bat`](file:///b:/WEBSITE/.dokument-arsip/deploy.bat)) | GitHub hanya sebagai cadangan kode sumber. Deploy langsung cepat & aman dari akses luar tak berizin. |

---

## 📁 Struktur Monorepo

```text
.
├── .vscode/
│   ├── launch.json            # Profil F5: Desktop App, Backend Dev, Standalone Exe & Fullstack
│   ├── tasks.json             # Task build, publish, & dev backend
│   └── settings.json          # Konfigurasi path SDK dan lingkungan .NET
├── backend/
│   ├── migrations/
│   │   ├── 0001_init.sql      # Skema inti: categories, form_fields, documents
│   │   ├── 0002_features.sql  # Skema versi, number_sequences, audit_logs
│   │   ├── seed.sql           # Data awal kategori & fields SPT, SKK, SRP
│   │   └── seed_features.sql  # Data awal versi app & counter penomoran 2026
│   ├── src/
│   │   ├── index.ts           # Hono API, PIN guard, atomic counter, QR web page
│   │   └── types.ts           # Interface TypeScript & D1 Bindings
│   ├── wrangler.toml          # Binding D1 & konfigurasi Cloudflare Workers
│   └── package.json
├── desktop/
│   ├── Models/
│   │   └── AppModels.cs       # Model Category, FormField, Document, AppVersion
│   ├── Services/
│   │   ├── DynamicFormService.cs # Renderer form dinamis, MiniWord, API client, agenda
│   │   └── LocalDraftService.cs  # Cache draf offline, docx scanner & QR generator
│   ├── MainWindow.xaml        # UI 3 panel (Kategori & Riwayat, Form, Dropzone Scan)
│   ├── MainWindow.xaml.cs     # Logika bisnis interaksi UI
│   ├── App.xaml               # Theme, warna modern & style komponen
│   └── SuratApp.csproj        # .NET 8 WPF dengan MiniWord & QRCoder
├── build.bat                  # Skrip 1-klik kompilasi SuratApp.exe mandiri
├── deploy.bat                 # Skrip 1-klik migrasi D1 & deploy Worker ke Cloudflare
├── package.json               # Skrip monorepo root (npm run dev, build, dll.)
├── README.md
└── .gitignore
```

---

## 🚀 Panduan Eksekusi 1-Klik

### 1. Build File Executable (.exe) Mandiri di Komputer Lokal
Cukup klik dua kali pada file:
```text
build.bat
```
Skrip ini akan mengompilasi seluruh kode menjadi satu file `.exe` mandiri di `dist/desktop/SuratApp.exe` dan langsung membuka folder output di Windows Explorer.

### 2. Menjalankan Backend Dev Server Secara Lokal
Jalankan di root folder:
```bash
npm run dev
```
Backend aktif di `http://127.0.0.1:8787`.

### 3. Deploy Backend ke Cloudflare Edge (24 Jam)
Cukup klik dua kali pada file:
```text
deploy.bat
```
Skrip ini akan mengeksekusi migrasi tabel D1 remote dan mendeploy Worker langsung ke akun Cloudflare Anda.
*(Pastikan Anda telah menjalankan `npx wrangler login` sebelumnya).*

---

## 🔐 Keamanan & PIN API

Setiap operasi tulis (generate dokumen, alokasi nomor surat, upload scan) dilindungi oleh PIN Sekolah:
- **Default PIN Lokal**: `123456`
- PIN dapat diubah pada bar atas aplikasi desktop dan diatur pada environment Cloudflare Workers (`APP_PIN`).
