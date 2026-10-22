-- ========================================================================
-- ENTERPRISE MULTI-TENANT DOCUMENT AUTOMATION & CLOSED-ECOSYSTEM SUITE
-- Cloudflare D1 SQLite Edge Schema (0001_init.sql)
-- ========================================================================

DROP TABLE IF EXISTS incoming_mails;
DROP TABLE IF EXISTS documents;
DROP TABLE IF EXISTS form_fields;
DROP TABLE IF EXISTS categories;
DROP TABLE IF EXISTS global_preset_templates;
DROP TABLE IF EXISTS entity_records;
DROP TABLE IF EXISTS entity_attributes;
DROP TABLE IF EXISTS custom_entities;
DROP TABLE IF EXISTS users;
DROP TABLE IF EXISTS tenants;
DROP TABLE IF EXISTS system_broadcasts;
DROP TABLE IF EXISTS licenses;
DROP TABLE IF EXISTS tier_configs;

-- 1. TIER CONFIGS (Pengaturan Paket & Harga Dinamis oleh Developer - Zero Hardcode)
CREATE TABLE tier_configs (
    tier_id TEXT PRIMARY KEY, -- 'trial', 'basic', 'standard', 'professional', 'enterprise'
    display_name TEXT NOT NULL,
    price_label TEXT NOT NULL,
    max_users INTEGER NOT NULL,
    max_templates INTEGER NOT NULL,
    max_storage_mb INTEGER NOT NULL,
    allow_rab_module INTEGER DEFAULT 1,
    allow_direct_scanner INTEGER DEFAULT 1,
    allow_qr_validation INTEGER DEFAULT 1,
    has_watermark INTEGER DEFAULT 0, -- 1 khusus untuk trial
    updated_at DATETIME DEFAULT CURRENT_TIMESTAMP
);

-- 2. LICENSES (Manajemen Lisensi Pembeli)
CREATE TABLE licenses (
    license_key TEXT PRIMARY KEY, -- contoh: 'ENT-PRO-2026-X991'
    tier_id TEXT NOT NULL,
    buyer_note TEXT,
    custom_max_users INTEGER,
    custom_max_storage_mb INTEGER,
    status TEXT DEFAULT 'unclaimed', -- 'unclaimed', 'active', 'suspended'
    assigned_tenant_id TEXT,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    activated_at DATETIME,
    FOREIGN KEY (tier_id) REFERENCES tier_configs(tier_id)
);

-- 3. SYSTEM BROADCASTS (Kontrol Iklan Pop-up, Running Text & Pengumuman oleh Developer)
CREATE TABLE system_broadcasts (
    id TEXT PRIMARY KEY,
    running_text TEXT NOT NULL,
    running_text_speed INTEGER DEFAULT 50,
    running_text_bg_color TEXT DEFAULT '#1E40AF',
    popup_enabled INTEGER DEFAULT 0,
    popup_title TEXT,
    popup_image_url TEXT,
    popup_cta_text TEXT,
    popup_cta_link TEXT,
    target_tiers_json TEXT DEFAULT '["trial","basic","standard"]',
    app_latest_version TEXT DEFAULT '1.0.0',
    maintenance_mode INTEGER DEFAULT 0,
    support_channel_uri TEXT DEFAULT '628589584257',
    updated_at DATETIME DEFAULT CURRENT_TIMESTAMP
);

-- 4. TENANTS (Data Instansi & Konfigurasi Chameleon)
CREATE TABLE tenants (
    id TEXT PRIMARY KEY, -- UUID
    license_key TEXT UNIQUE NOT NULL,
    institution_code TEXT UNIQUE NOT NULL, -- Digunakan saat login
    institution_name TEXT NOT NULL,
    institution_type TEXT NOT NULL, -- 'school', 'village_gov', 'corporate', 'office_agency', 'healthcare', 'foundation'
    institution_address TEXT NOT NULL,
    phone_number TEXT NOT NULL,
    email TEXT NOT NULL,
    logo_url TEXT,
    letterhead_config_json TEXT, -- Konfigurasi Kop Surat
    signature_url TEXT,
    stamp_url TEXT,
    default_numbering_formula TEXT NOT NULL, -- Rumus nomor default instansi
    storage_used_bytes INTEGER DEFAULT 0,
    is_active INTEGER DEFAULT 1,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (license_key) REFERENCES licenses(license_key)
);

-- 5. USERS (Akun Pengguna Multi-Role & Single Active Session)
CREATE TABLE users (
    id TEXT PRIMARY KEY, -- UUID
    tenant_id TEXT NOT NULL,
    email_username TEXT NOT NULL, -- Email menjadi username utama
    password_hash TEXT NOT NULL,
    full_name TEXT NOT NULL,
    phone TEXT,
    role TEXT NOT NULL DEFAULT 'staff', -- 'admin' atau 'staff'
    active_session_token TEXT, -- Kunci Single Active Session
    active_device_info TEXT,
    is_active INTEGER DEFAULT 1,
    last_login DATETIME,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (tenant_id) REFERENCES tenants(id) ON DELETE CASCADE,
    UNIQUE(tenant_id, email_username)
);

-- 6. CUSTOM ENTITIES & ATTRIBUTES & RECORDS (Database SDM & Entitas Dinamis)
CREATE TABLE custom_entities (
    id TEXT PRIMARY KEY,
    tenant_id TEXT NOT NULL,
    entity_slug TEXT NOT NULL, -- 'personnel', 'students', 'citizens', 'vendors'
    entity_display_name TEXT NOT NULL,
    is_system_default INTEGER DEFAULT 1,
    FOREIGN KEY (tenant_id) REFERENCES tenants(id) ON DELETE CASCADE
);

CREATE TABLE entity_attributes (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    tenant_id TEXT NOT NULL,
    entity_id TEXT NOT NULL,
    attr_key TEXT NOT NULL, -- Menjadi token {{personnel.attr_key}}
    attr_label TEXT NOT NULL,
    data_type TEXT NOT NULL, -- 'text', 'id_number', 'date_tmt', 'currency', 'select'
    select_options_json TEXT,
    auto_calculate_tenure INTEGER DEFAULT 0,
    sort_order INTEGER DEFAULT 0,
    FOREIGN KEY (entity_id) REFERENCES custom_entities(id) ON DELETE CASCADE,
    FOREIGN KEY (tenant_id) REFERENCES tenants(id) ON DELETE CASCADE
);

CREATE TABLE entity_records (
    id TEXT PRIMARY KEY,
    tenant_id TEXT NOT NULL,
    entity_id TEXT NOT NULL,
    primary_name TEXT NOT NULL,
    primary_id_number TEXT, -- NIP / NIK / NUPTK
    tmt_date TEXT, -- YYYY-MM-DD
    contract_end_date TEXT, -- Untuk contract expiry watcher
    values_json TEXT NOT NULL, -- Seluruh data atribut dalam JSON
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (entity_id) REFERENCES custom_entities(id) ON DELETE CASCADE,
    FOREIGN KEY (tenant_id) REFERENCES tenants(id) ON DELETE CASCADE
);

-- 7. GLOBAL PRESET TEMPLATES (Pustaka Master Template Developer)
CREATE TABLE global_preset_templates (
    id TEXT PRIMARY KEY,
    target_institution_type TEXT NOT NULL, -- 'school', 'village_gov', 'corporate', 'office_agency', 'all'
    category_name TEXT NOT NULL,
    description TEXT,
    numbering_formula TEXT NOT NULL,
    rich_content_json TEXT NOT NULL,
    has_rab_grid INTEGER DEFAULT 0,
    rab_preset_json TEXT,
    form_fields_json TEXT NOT NULL
);

-- 8. CATEGORIES & FORM FIELDS (Format Surat Milik Instansi)
CREATE TABLE categories (
    id TEXT PRIMARY KEY,
    tenant_id TEXT NOT NULL,
    name TEXT NOT NULL,
    classification_code TEXT, -- contoh '421.2', '140', 'HRD-SK'
    description TEXT,
    numbering_formula TEXT NOT NULL,
    rich_content_json TEXT NOT NULL,
    has_rab_grid INTEGER DEFAULT 0,
    rab_template_json TEXT,
    created_by TEXT,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (tenant_id) REFERENCES tenants(id) ON DELETE CASCADE
);

CREATE TABLE form_fields (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    tenant_id TEXT NOT NULL,
    category_id TEXT NOT NULL,
    field_key TEXT NOT NULL,
    label TEXT NOT NULL,
    field_type TEXT NOT NULL, -- 'text', 'multiline', 'date', 'select', 'number', 'currency', 'entity_lookup'
    linked_entity_id TEXT,
    options_json TEXT,
    is_required INTEGER DEFAULT 1,
    sort_order INTEGER DEFAULT 0,
    FOREIGN KEY (category_id) REFERENCES categories(id) ON DELETE CASCADE,
    FOREIGN KEY (tenant_id) REFERENCES tenants(id) ON DELETE CASCADE
);

-- 9. DOCUMENTS (Buku Agenda, Proposal RAB, Siklus Hidup & Approval Hapus)
CREATE TABLE documents (
    id TEXT PRIMARY KEY, -- UUID
    tenant_id TEXT NOT NULL,
    category_id TEXT NOT NULL,
    doc_sequence_number INTEGER NOT NULL, -- Nomor urut murni untuk smart gap detection
    doc_number_full TEXT NOT NULL,
    title_or_subject TEXT NOT NULL,
    recipient TEXT,
    metadata_json TEXT NOT NULL,
    rendered_content_json TEXT NOT NULL, -- Konten dokumen in-app
    rab_data_json TEXT,
    rab_total_amount REAL DEFAULT 0,
    locked_archive_url TEXT,
    scan_url TEXT,
    approval_status TEXT DEFAULT 'approved', -- 'draft', 'waiting_chief_approval', 'revision_needed', 'approved'
    approval_note TEXT,
    archive_status TEXT DEFAULT 'completed_digital', -- 'draft', 'completed_digital', 'verified_scan', 'pending_deletion'
    deletion_requested_by_id TEXT,
    deletion_requested_by_name TEXT,
    deletion_reason TEXT,
    deletion_requested_at DATETIME,
    created_by_user_id TEXT NOT NULL,
    created_by_name TEXT NOT NULL,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (category_id) REFERENCES categories(id),
    FOREIGN KEY (tenant_id) REFERENCES tenants(id) ON DELETE CASCADE
);

-- 10. INCOMING MAILS (Surat Masuk & Disposisi Digital)
CREATE TABLE incoming_mails (
    id TEXT PRIMARY KEY,
    tenant_id TEXT NOT NULL,
    agenda_number TEXT NOT NULL,
    sender_origin TEXT NOT NULL,
    original_mail_number TEXT NOT NULL,
    mail_date TEXT NOT NULL,
    received_date TEXT NOT NULL,
    subject TEXT NOT NULL,
    scan_url TEXT,
    disposition_to TEXT,
    disposition_instruction TEXT,
    urgency_level TEXT DEFAULT 'Biasa', -- 'Biasa', 'Penting', 'Segera/Rahasia'
    created_by_name TEXT NOT NULL,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (tenant_id) REFERENCES tenants(id) ON DELETE CASCADE
);

-- INDEXING MULTI-TENANT UNTUK PERFORMA TINGGI
CREATE INDEX idx_tenants_code ON tenants(institution_code);
CREATE INDEX idx_licenses_tier ON licenses(tier_id, status);
CREATE INDEX idx_users_tenant_auth ON users(tenant_id, email_username, is_active);
CREATE INDEX idx_entity_records_lookup ON entity_records(tenant_id, entity_id);
CREATE INDEX idx_categories_tenant ON categories(tenant_id);
CREATE INDEX idx_documents_tenant_seq ON documents(tenant_id, category_id, doc_sequence_number DESC);
CREATE INDEX idx_documents_archive_status ON documents(tenant_id, archive_status);
CREATE INDEX idx_incoming_tenant ON incoming_mails(tenant_id, received_date DESC);
