-- Migration: 0002_features.sql
-- Penyempurnaan: Versi Aplikasi, Penomoran Surat Otomatis (Anti-Duplikat), dan Audit Log

-- 1. Tabel Versi Aplikasi untuk Auto-Update Checker (Tanpa GitHub Releases)
CREATE TABLE IF NOT EXISTS app_versions (
    id TEXT PRIMARY KEY,
    version TEXT NOT NULL UNIQUE,          -- contoh: '1.1.0'
    version_code INTEGER NOT NULL UNIQUE,  -- contoh: 110 (untuk perbandingan numerik)
    download_url TEXT NOT NULL,            -- URL Cloudinary/Google Drive/LAN tempat .exe tersimpan
    file_size_bytes INTEGER,
    release_notes TEXT,                    -- Catatan pembaruan / changelog
    is_mandatory INTEGER DEFAULT 0,        -- 1 = Wajib update sebelum bisa pakai, 0 = Opsional
    released_at DATETIME DEFAULT CURRENT_TIMESTAMP
);

-- 2. Tabel Counter Penomoran Surat Otomatis (Atomic Sequence per Kategori & Tahun)
CREATE TABLE IF NOT EXISTS number_sequences (
    category_id TEXT NOT NULL,
    year INTEGER NOT NULL,
    current_number INTEGER NOT NULL DEFAULT 0,
    prefix_format TEXT DEFAULT '{num:3}/{code}/TU/{month_rom}/{year}', -- format penomoran
    updated_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (category_id, year),
    FOREIGN KEY (category_id) REFERENCES categories(id)
);

-- 3. Tabel Audit Log untuk Melacak Pembuat dan Perubahan Dokumen
CREATE TABLE IF NOT EXISTS audit_logs (
    id TEXT PRIMARY KEY,
    document_id TEXT,
    action TEXT NOT NULL,                 -- 'GENERATE_DOCX', 'LINK_SCAN', 'UPDATE_DRAFT'
    actor_name TEXT NOT NULL,             -- Nama staf / username komputer
    details TEXT,                         -- JSON rincian perubahan
    ip_address TEXT,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP
);

-- Tambahkan kolom QR verification hash & status di tabel documents jika belum ada
ALTER TABLE documents ADD COLUMN verification_token TEXT;
ALTER TABLE documents ADD COLUMN status TEXT DEFAULT 'DRAFT'; -- 'DRAFT', 'ISSUED_WAITING_SCAN', 'COMPLETED'

-- Indexing
CREATE INDEX IF NOT EXISTS idx_audit_doc ON audit_logs(document_id);
CREATE INDEX IF NOT EXISTS idx_app_version ON app_versions(version_code DESC);
