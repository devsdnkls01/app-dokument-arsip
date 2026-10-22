-- ========================================================================
-- PRODUCTION SEED DATA: TIERS, PROVISIONING PRESETS & INSTRUCTIONAL METADATA
-- PURE DATABASE BINDING (ZERO DUMMY NAMES / ZERO FICTITIOUS DATA)
-- ========================================================================

-- 1. TIER CONFIGURATIONS (ENTITLEMENT PACKAGES)
INSERT OR REPLACE INTO tier_configs (tier_id, display_name, price_label, max_users, max_templates, max_storage_mb, allow_rab_module, allow_direct_scanner, allow_qr_validation, has_watermark)
VALUES
('trial', 'Tingkat Evaluasi (Node Trial)', 'Evaluasi Terbatas', 3, 5, 250, 0, 0, 0, 1),
('basic', 'Tingkat Dasar (Basic Node)', 'Alokasi Terstandarisasi', 5, 15, 1000, 1, 1, 1, 0),
('standard', 'Tingkat Standard (Standard Node)', 'Alokasi Instansi Reguler', 15, 30, 2500, 1, 1, 1, 0),
('professional', 'Tingkat Profesional (Corporate Node)', 'Alokasi Skala Penuh', 50, 100, 10000, 1, 1, 1, 0),
('enterprise', 'Tingkat Enterprise (Dedicated Node)', 'Alokasi Tanpa Batas', 9999, 9999, 50000, 1, 1, 1, 0);

-- 2. ENTITLEMENT TOKENS (LICENSES READY FOR PROVISIONING)
INSERT OR REPLACE INTO licenses (license_key, tier_id, buyer_note, status, assigned_tenant_id)
VALUES
('ENT-TRL-2026-DEMO', 'trial', '[Alokasi Evaluasi Node]', 'unassigned_node', NULL),
('ENT-BSC-2026-NODE1', 'basic', '[Alokasi Instansi Tingkat Dasar]', 'unassigned_node', NULL),
('ENT-STD-2026-NODE2', 'standard', '[Alokasi Instansi Tingkat Standard]', 'unassigned_node', NULL),
('ENT-PRO-2026-NODE3', 'professional', '[Alokasi Instansi Tingkat Profesional]', 'unassigned_node', NULL),
('ENT-ENT-2026-NODE4', 'enterprise', '[Alokasi Instansi Tingkat Enterprise]', 'unassigned_node', NULL);

-- 3. SYSTEM BROADCASTS & EXECUTIVE BULLETIN
INSERT OR REPLACE INTO system_broadcasts (id, running_text, running_text_speed, running_text_bg_color, popup_enabled, popup_title, popup_image_url, popup_cta_text, popup_cta_link, target_tiers_json, app_latest_version, support_channel_uri)
VALUES
(
    'sys-broadcast-main',
    '[Area Informasi Buletin Sistem: Diatur Melalui Konsol Provisioning Pusat]',
    50,
    '#1E40AF',
    0,
    '[Judul Pengumuman Buletin Eksekutif]',
    NULL,
    '[ 💬 Hubungi Kami ]',
    '#',
    '["trial","basic","standard"]',
    '1.0.0',
    '628589584257'
);

-- 4. MASTER PRESET TEMPLATES (INSTRUCTIONAL METADATA PER INSTITUTION TYPE)
-- A. Paket Sekolah
INSERT OR REPLACE INTO global_preset_templates (id, target_institution_type, category_name, description, numbering_formula, rich_content_json, has_rab_grid, rab_preset_json, form_fields_json)
VALUES
(
    'preset-sch-tugas',
    'school',
    'Surat Perintah Tugas Kedinasan',
    '[Penjelasan Fungsi Format Naskah & Dasar Regulasi Penggunaan]',
    '[Kode_Klasifikasi]/[Nomor_Urut_3]/[Kode_Instansi]/[Bulan_Romawi]/[Tahun]',
    '{"title":"SURAT PERINTAH TUGAS","body":"Pimpinan Instansi memberikan tugas kedinasan kepada:\nNama: {{personnel.primary_name}}\nNomor Induk: {{personnel.primary_id_number}}\nMasa Tugas: {{personnel.calculated_tenure}}\n\nUraian Tugas Kedinasan: [Ketikkan Uraian Tugas]\nLokasi Pelaksanaan: [Ketikkan Lokasi Tugas]\nJangka Waktu Pelaksanaan: [Ketikkan Tanggal / Durasi Tugas]\n\nDemikian surat perintah tugas ini diterbitkan untuk dilaksanakan secara penuh tanggung jawab."}',
    0,
    NULL,
    '[]'
),
(
    'preset-sch-rab',
    'school',
    'Proposal Usulan Kegiatan & Rincian Anggaran (RAB)',
    '[Penjelasan Fungsi Format Naskah & Dasar Regulasi Penggunaan]',
    '421.5/[Nomor_Urut_3]/[Kode_Instansi]/ANGG/[Tahun]',
    '{"title":"PROPOSAL RENCANA ANGGARAN BIAYA (RAB)","body":"Bersama ini disampaikan usulan rincian rencana kebutuhan anggaran kegiatan kedinasan sebagaimana terlampir pada tabel spreadsheet terintegrasi di bawah ini."}',
    1,
    '[{"row_number":1,"item_name":"[Deskripsi Rincian Belanja Kegiatan 1]","volume":1,"unit":"Paket","unit_price":1000000},{"row_number":2,"item_name":"[Deskripsi Rincian Belanja Kegiatan 2]","volume":1,"unit":"Paket","unit_price":500000}]',
    '[]'
);

-- B. Paket Pemerintah Desa / Dinas
INSERT OR REPLACE INTO global_preset_templates (id, target_institution_type, category_name, description, numbering_formula, rich_content_json, has_rab_grid, rab_preset_json, form_fields_json)
VALUES
(
    'preset-des-pengantar',
    'village_gov',
    'Surat Keterangan Pengantar Kedinasan',
    '[Penjelasan Fungsi Format Naskah & Dasar Regulasi Penggunaan]',
    '[Kode_Klasifikasi]/[Nomor_Urut_3]/[Kode_Instansi]/[Tahun]',
    '{"title":"SURAT KETERANGAN PENGANTAR","body":"Yang bertanda tangan di bawah ini Kepala Instansi, menerangkan bahwa:\nNama: {{personnel.primary_name}}\nNomor Identitas: {{personnel.primary_id_number}}\n\nKeterangan Pokok: [Ketikkan Keterangan Kedinasan Sesuai Permohonan]\n\nDemikian surat keterangan ini diberikan sebagai pengantar resmi."}',
    0,
    NULL,
    '[]'
),
(
    'preset-des-sppd',
    'village_gov',
    'Surat Perintah Perjalanan Dinas (SPPD)',
    '[Penjelasan Fungsi Format Naskah & Dasar Regulasi Penggunaan]',
    '090/[Nomor_Urut_3]/[Kode_Instansi]/SPPD/[Tahun]',
    '{"title":"SURAT PERINTAH PERJALANAN DINAS","body":"Pejabat Berwenang memerintahkan perjalanan dinas kepada:\nNama Personel: {{personnel.primary_name}}\nNomor Induk: {{personnel.primary_id_number}}\n\nMaksud Perjalanan Dinas: [Ketikkan Dasar dan Tujuan Perjalanan]\nTempat Berangkat: [Kedudukan Instansi]\nTempat Tujuan: [Kota/Lokasi Tujuan Dinas]"}',
    0,
    NULL,
    '[]'
);

-- C. Paket Korporasi / Perusahaan Swasta
INSERT OR REPLACE INTO global_preset_templates (id, target_institution_type, category_name, description, numbering_formula, rich_content_json, has_rab_grid, rab_preset_json, form_fields_json)
VALUES
(
    'preset-corp-sk',
    'corporate',
    'Surat Keputusan Pengangkatan & Penempatan Kerja',
    '[Penjelasan Fungsi Format Naskah & Dasar Regulasi Penggunaan]',
    '[Nomor_Urut_4]/[Kode_Instansi]-[Kode_Klasifikasi]/[Bulan_Romawi]/[Tahun]',
    '{"title":"SURAT KEPUTUSAN DIREKSI","body":"Direksi Perusahaan memutuskan penempatan kerja terhadap:\nNama: {{personnel.primary_name}}\nNomor Pokok Karyawan: {{personnel.primary_id_number}}\nMasa Kerja: {{personnel.calculated_tenure}}\n\nPenempatan Divisi / Unit Kerja: [Ketikkan Divisi / Job Title]"}',
    0,
    NULL,
    '[]'
),
(
    'preset-corp-quote',
    'corporate',
    'Surat Penawaran Harga Komersial (Commercial Quotation)',
    '[Penjelasan Fungsi Format Naskah & Dasar Regulasi Penggunaan]',
    'QUO/[Nomor_Urut_4]/[Kode_Instansi]/[Bulan_Angka]/[Tahun]',
    '{"title":"SURAT PENAWARAN HARGA KOMERSIAL","body":"Dengan hormat,\nBerikut ini rincian penawaran harga pengadaan barang/jasa sebagaimana tertera pada matriks spreadsheet terlampir."}',
    1,
    '[{"row_number":1,"item_name":"[Deskripsi Layanan / Item Produk Pengadaan]","volume":1,"unit":"Lot","unit_price":2500000}]',
    '[]'
);

-- D. Paket Perkantoran Umum / Organisasi / Kesehatan
INSERT OR REPLACE INTO global_preset_templates (id, target_institution_type, category_name, description, numbering_formula, rich_content_json, has_rab_grid, rab_preset_json, form_fields_json)
VALUES
(
    'preset-gen-memo',
    'office_agency',
    'Nota Dinas & Memorandum Internal',
    '[Penjelasan Fungsi Format Naskah & Dasar Regulasi Penggunaan]',
    'ND/[Nomor_Urut_3]/[Kode_Instansi]/[Bulan_Romawi]/[Tahun]',
    '{"title":"NOTA DINAS INTERNAL","body":"Kepada Yth: [Ketikkan Pejabat / Unit Tujuan]\nDari: [Ketikkan Pengirim]\nHal: [Ketikkan Pokok Nota Dinas]\n\nSehubungan dengan koordinasi kedinasan, bersama ini disampaikan pokok naskah dinas sebagai berikut:\n[Ketikkan Isi Penjelasan Lengkap]"}',
    0,
    NULL,
    '[]'
);
