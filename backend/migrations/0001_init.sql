-- Migration: 0001_init.sql
-- Inisialisasi skema database D1 SQLite untuk Sistem Otomasi Dokumen & Arsip

-- 1. Tabel Kategori Surat / Dokumen
CREATE TABLE IF NOT EXISTS categories (
    id TEXT PRIMARY KEY,
    name TEXT NOT NULL,
    code TEXT NOT NULL UNIQUE,
    description TEXT,
    template_cloudinary_url TEXT NOT NULL,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME DEFAULT CURRENT_TIMESTAMP
);

-- 2. Tabel Definisi Field Dinamis per Kategori
CREATE TABLE IF NOT EXISTS form_fields (
    id TEXT PRIMARY KEY,
    category_id TEXT NOT NULL,
    field_key TEXT NOT NULL,              -- nama variabel placeholder di docx, contoh: {{nama_pegawai}} -> "nama_pegawai"
    label TEXT NOT NULL,                  -- label tampilan di form UI
    field_type TEXT NOT NULL,             -- text, textarea, date, number, select
    options TEXT,                         -- JSON array string untuk pilihan tipe select, contoh: ["Pilihan A","Pilihan B"]
    default_value TEXT,                   -- nilai default jika ada
    placeholder TEXT,                     -- petunjuk input
    is_required INTEGER DEFAULT 1,        -- 1 = true, 0 = false
    display_order INTEGER DEFAULT 0,      -- urutan tampil di UI WPF
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (category_id) REFERENCES categories(id) ON DELETE CASCADE
);

-- 3. Tabel Dokumen yang Telah Di-generate dan Arsipnya
CREATE TABLE IF NOT EXISTS documents (
    id TEXT PRIMARY KEY,
    category_id TEXT NOT NULL,
    document_number TEXT NOT NULL,        -- nomor surat resmi
    title TEXT NOT NULL,                  -- perihal / judul dokumen
    form_data TEXT NOT NULL,              -- JSON serialized values dari form dinamis
    docx_cloudinary_url TEXT NOT NULL,    -- URL file .docx hasil generate di Cloudinary (archives/docx/)
    docx_public_id TEXT NOT NULL,
    scan_cloudinary_url TEXT,             -- URL file scan basah (PDF/JPG) di Cloudinary (archives/scans/)
    scan_public_id TEXT,
    scan_uploaded_at DATETIME,
    created_by TEXT DEFAULT 'system',
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (category_id) REFERENCES categories(id)
);

-- Indexing untuk query cepat
CREATE INDEX IF NOT EXISTS idx_fields_category ON form_fields(category_id, display_order);
CREATE INDEX IF NOT EXISTS idx_documents_category ON documents(category_id);
CREATE INDEX IF NOT EXISTS idx_documents_number ON documents(document_number);
CREATE INDEX IF NOT EXISTS idx_documents_created ON documents(created_at DESC);
