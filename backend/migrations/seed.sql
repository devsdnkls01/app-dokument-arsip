-- Seed Data: seed.sql
-- Kategori contoh dan form fields dinamis untuk pengujian

-- 1. Insert Kategori
INSERT OR REPLACE INTO categories (id, name, code, description, template_cloudinary_url)
VALUES 
(
    'cat-st-001',
    'Surat Perintah Tugas (SPT)',
    'SPT',
    'Surat penugasan resmi untuk dinas luar atau perjalanan dinas pegawai.',
    'https://res.cloudinary.com/demo/raw/upload/v1/templates/template_surat_tugas.docx'
),
(
    'cat-skk-002',
    'Surat Keterangan Kerja (SKK)',
    'SKK',
    'Surat keterangan masa kerja dan posisi aktif karyawan/staf.',
    'https://res.cloudinary.com/demo/raw/upload/v1/templates/template_skk.docx'
),
(
    'cat-spd-003',
    'Surat Rekomendasi / Pengantar',
    'SRP',
    'Surat rekomendasi resmi untuk keperluan administratif dan perizinan.',
    'https://res.cloudinary.com/demo/raw/upload/v1/templates/template_rekomendasi.docx'
);

-- 2. Insert Fields Dinamis untuk Surat Perintah Tugas (SPT)
INSERT OR REPLACE INTO form_fields (id, category_id, field_key, label, field_type, options, default_value, placeholder, is_required, display_order)
VALUES
('fld-01', 'cat-st-001', 'nomor_surat', 'Nomor Surat', 'text', NULL, '094/ST/ORG/2026', 'Contoh: 094/ST/ORG/2026', 1, 1),
('fld-02', 'cat-st-001', 'nama_pegawai', 'Nama Lengkap Pegawai', 'text', NULL, '', 'Nama lengkap dan gelar', 1, 2),
('fld-03', 'cat-st-001', 'nip', 'NIP / NIK Pegawai', 'text', NULL, '', 'Nomor Identitas Pegawai', 1, 3),
('fld-04', 'cat-st-001', 'pangkat_golongan', 'Pangkat / Golongan', 'text', NULL, 'Penata Muda / III a', 'Golongan ruang', 0, 4),
('fld-05', 'cat-st-001', 'jabatan', 'Jabatan', 'text', NULL, '', 'Jabatan struktural / fungsional', 1, 5),
('fld-06', 'cat-st-001', 'unit_kerja', 'Unit Kerja / Divisi', 'text', NULL, 'Biro Perencanaan & TI', 'Unit kerja penempatan', 1, 6),
('fld-07', 'cat-st-001', 'tujuan_tugas', 'Kota / Lokasi Tujuan', 'text', NULL, '', 'Contoh: Bandung, Jawa Barat', 1, 7),
('fld-08', 'cat-st-001', 'keperluan_tugas', 'Maksud & Keperluan Penugasan', 'textarea', NULL, '', 'Rincian agenda tugas kedinasan...', 1, 8),
('fld-09', 'cat-st-001', 'tgl_mulai', 'Tanggal Mulai', 'date', NULL, '', 'Pilih tanggal awal penugasan', 1, 9),
('fld-10', 'cat-st-001', 'tgl_selesai', 'Tanggal Selesai', 'date', NULL, '', 'Pilih tanggal akhir penugasan', 1, 10),
('fld-11', 'cat-st-001', 'transportasi', 'Moda Transportasi', 'select', '["Kendaraan Dinas", "Kereta Api", "Pesawat Udara", "Transportasi Darat Umum"]', 'Kendaraan Dinas', '', 1, 11),
('fld-12', 'cat-st-001', 'beban_anggaran', 'Beban Anggaran', 'select', '["DIPA Satker 2026", "Swadaya Lembaga", "Pihak Pengundang"]', 'DIPA Satker 2026', '', 1, 12),
('fld-13', 'cat-st-001', 'kota_terbit', 'Kota Penerbitan', 'text', NULL, 'Jakarta', 'Kota surat diterbitkan', 1, 13),
('fld-14', 'cat-st-001', 'tgl_surat', 'Tanggal Surat Ditandatangani', 'date', NULL, '', 'Tanggal surat', 1, 14),
('fld-15', 'cat-st-001', 'nama_pejabat', 'Nama Pejabat Berwenang', 'text', NULL, 'Dr. H. Hendra Wijaya, M.Si', 'Nama pejabat penandatangan', 1, 15),
('fld-16', 'cat-st-001', 'jabatan_pejabat', 'Jabatan Pejabat', 'text', NULL, 'Kepala Biro Administrasi & Umum', 'Jabatan penandatangan', 1, 16);

-- 3. Insert Fields Dinamis untuk Surat Keterangan Kerja (SKK)
INSERT OR REPLACE INTO form_fields (id, category_id, field_key, label, field_type, options, default_value, placeholder, is_required, display_order)
VALUES
('fld-20', 'cat-skk-002', 'nomor_surat', 'Nomor Surat SKK', 'text', NULL, '421/SKK/HRD/2026', 'Contoh: 421/SKK/HRD/2026', 1, 1),
('fld-21', 'cat-skk-002', 'nama_karyawan', 'Nama Karyawan', 'text', NULL, '', 'Nama lengkap karyawan', 1, 2),
('fld-22', 'cat-skk-002', 'nomor_induk', 'ID Karyawan / NIK', 'text', NULL, '', 'Nomor induk karyawan', 1, 3),
('fld-23', 'cat-skk-002', 'jabatan_terakhir', 'Jabatan Terakhir', 'text', NULL, '', 'Posisi jabatan', 1, 4),
('fld-24', 'cat-skk-002', 'tgl_bergabung', 'Tanggal Mulai Bekerja', 'date', NULL, '', 'Tanggal bergabung', 1, 5),
('fld-25', 'cat-skk-002', 'status_kerja', 'Status Karyawan', 'select', '["Karyawan Tetap", "Karyawan Kontrak PKWT", "Tenaga Ahli Konsultan"]', 'Karyawan Tetap', '', 1, 6),
('fld-26', 'cat-skk-002', 'keperluan_surat', 'Keperluan Surat', 'text', NULL, 'Pembukaan Rekening Bank / KPR', 'Maksud pembuatan surat', 1, 7),
('fld-27', 'cat-skk-002', 'tgl_surat', 'Tanggal Surat', 'date', NULL, '', 'Tanggal terbit', 1, 8),
('fld-28', 'cat-skk-002', 'nama_hrd', 'Nama Manajer HRD', 'text', NULL, 'Siti Rahmawati, S.Psi', 'Penandatangan', 1, 9);
