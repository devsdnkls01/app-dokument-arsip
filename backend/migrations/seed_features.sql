-- Seed Data Tambahan untuk Versi Aplikasi dan Penomoran Otomatis

INSERT OR REPLACE INTO app_versions (id, version, version_code, download_url, file_size_bytes, release_notes, is_mandatory)
VALUES (
    'ver-1.0.0',
    '1.0.0',
    100,
    'https://res.cloudinary.com/demo/raw/upload/v1/releases/SuratApp-v1.0.0.exe',
    73414951,
    'Rilis versi perdana: Form dinamis, engine MiniWord lokal, penomoran otomatis, validasi QR Code, dan arsip digital Cloudinary.',
    0
);

-- Inisialisasi sequence penomoran surat 2026
INSERT OR REPLACE INTO number_sequences (category_id, year, current_number, prefix_format)
VALUES 
('cat-st-001', 2026, 0, '{num:3}/SPT/TU/{month_rom}/{year}'),
('cat-skk-002', 2026, 0, '{num:3}/SKK/HRD/{month_rom}/{year}'),
('cat-spd-003', 2026, 0, '{num:3}/SRP/TU/{month_rom}/{year}');
