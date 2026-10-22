import { Hono, Context, Next } from 'hono';
import { cors } from 'hono/cors';
import { 
  Env, 
  TierConfig,
  License,
  SystemBroadcast,
  Tenant, 
  User,
  UserSessionPayload,
  CustomEntity,
  EntityAttribute,
  EntityRecord,
  GlobalPresetTemplate,
  Category, 
  FormField, 
  DocumentRecord,
  IncomingMail,
  ApiResponse,
  LoginRequest,
  RegisterTenantRequest,
  SaveDocumentRequest
} from './types';

type Variables = {
  session: UserSessionPayload;
  tenant: Tenant;
  tier: TierConfig;
};

const app = new Hono<{ Bindings: Env; Variables: Variables }>();

// Enable CORS
app.use('*', cors({
  origin: '*',
  allowMethods: ['GET', 'POST', 'PUT', 'DELETE', 'OPTIONS'],
  allowHeaders: ['Content-Type', 'Authorization', 'X-Requested-With', 'X-Developer-Master-Key'],
}));

// ========================================================================
// CRYPTO & JWT UTILITIES (Native Web Crypto API)
// ========================================================================

const PASSWORD_SALT = 'surat-enterprise-salt-v1';

async function hashPassword(password: string): Promise<string> {
  const encoder = new TextEncoder();
  const data = encoder.encode(`${PASSWORD_SALT}:${password}`);
  const hashBuffer = await crypto.subtle.digest('SHA-256', data);
  const hashArray = Array.from(new Uint8Array(hashBuffer));
  return hashArray.map(b => b.toString(16).padStart(2, '0')).join('');
}

async function generateSha1Hex(text: string): Promise<string> {
  const encoder = new TextEncoder();
  const data = encoder.encode(text);
  const hashBuffer = await crypto.subtle.digest('SHA-1', data);
  const hashArray = Array.from(new Uint8Array(hashBuffer));
  return hashArray.map(b => b.toString(16).padStart(2, '0')).join('');
}

function base64UrlEncode(str: string): string {
  return btoa(str).replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/, '');
}

function base64UrlDecode(str: string): string {
  str = str.replace(/-/g, '+').replace(/_/g, '/');
  while (str.length % 4) {
    str += '=';
  }
  return atob(str);
}

async function getHmacKey(secret: string): Promise<CryptoKey> {
  const enc = new TextEncoder();
  return await crypto.subtle.importKey(
    'raw',
    enc.encode(secret),
    { name: 'HMAC', hash: 'SHA-256' },
    false,
    ['sign', 'verify']
  );
}

async function createJwt(payload: UserSessionPayload, secret: string): Promise<string> {
  const header = { alg: 'HS256', typ: 'JWT' };
  const headerPart = base64UrlEncode(JSON.stringify(header));
  const payloadPart = base64UrlEncode(JSON.stringify(payload));
  const dataToSign = `${headerPart}.${payloadPart}`;

  const key = await getHmacKey(secret);
  const enc = new TextEncoder();
  const signatureBuffer = await crypto.subtle.sign('HMAC', key, enc.encode(dataToSign));
  const signaturePart = base64UrlEncode(String.fromCharCode(...new Uint8Array(signatureBuffer)));

  return `${dataToSign}.${signaturePart}`;
}

async function verifyJwt(token: string, secret: string): Promise<UserSessionPayload | null> {
  try {
    const parts = token.split('.');
    if (parts.length !== 3) return null;

    const [headerPart, payloadPart, signaturePart] = parts;
    const dataToVerify = `${headerPart}.${payloadPart}`;

    const key = await getHmacKey(secret);
    const enc = new TextEncoder();
    const sigStr = base64UrlDecode(signaturePart);
    const sigBytes = new Uint8Array(sigStr.length);
    for (let i = 0; i < sigStr.length; i++) {
      sigBytes[i] = sigStr.charCodeAt(i);
    }

    const isValid = await crypto.subtle.verify('HMAC', key, sigBytes, enc.encode(dataToVerify));
    if (!isValid) return null;

    const payload = JSON.parse(base64UrlDecode(payloadPart)) as UserSessionPayload;
    const now = Math.floor(Date.now() / 1000);
    if (payload.exp && payload.exp < now) {
      return null;
    }

    return payload;
  } catch {
    return null;
  }
}

// Helper: Hitung Masa Kerja dari TMT (Tahun dan Bulan)
function calculateTenure(tmtDateStr?: string | null): string {
  if (!tmtDateStr) return '-';
  try {
    const tmt = new Date(tmtDateStr);
    const now = new Date();
    if (isNaN(tmt.getTime())) return '-';

    let years = now.getFullYear() - tmt.getFullYear();
    let months = now.getMonth() - tmt.getMonth();
    if (months < 0) {
      years--;
      months += 12;
    }
    if (years < 0) return '0 Bulan';
    return `${years} Tahun ${months} Bulan`;
  } catch {
    return '-';
  }
}

// Helper: Bulan Romawi
function getRomanMonth(date: Date): string {
  const romans = ['I', 'II', 'III', 'IV', 'V', 'VI', 'VII', 'VIII', 'IX', 'X', 'XI', 'XII'];
  return romans[date.getMonth()];
}

// ------------------------------------------------------------------------
// 0. HEALTH CHECK & PUBLIC SYSTEM BROADCAST
// ------------------------------------------------------------------------
app.get('/', (c) => {
  return c.json<ApiResponse>({
    success: true,
    message: 'Sovereign Executive Closed-Ecosystem SaaS Engine is running 24/7 on Cloudflare D1',
    data: {
      version: '3.0.0-enterprise',
      edge: 'Cloudflare Workers + D1 SQLite'
    }
  });
});

app.get('/api/system/broadcast', async (c) => {
  const broadcast = await c.env.DB
    .prepare('SELECT * FROM system_broadcasts WHERE id = "sys-broadcast-main"')
    .first<SystemBroadcast>();

  const tiers = await c.env.DB
    .prepare('SELECT * FROM tier_configs ORDER BY max_users ASC')
    .all<TierConfig>();

  return c.json<ApiResponse>({
    success: true,
    data: {
      broadcast,
      tier_pricing: tiers.results || []
    }
  });
});

// ------------------------------------------------------------------------
// 1. PUBLIC ONBOARDING & AUTHENTICATION
// ------------------------------------------------------------------------

// A. Verifikasi Lisensi (Sebelum buka form registrasi)
app.post('/api/auth/verify-license', async (c) => {
  try {
    const body = await c.req.json<{ license_key: string }>();
    const licenseKey = body.license_key?.trim().toUpperCase();

    if (!licenseKey) {
      return c.json<ApiResponse>({ success: false, error: 'Kode lisensi wajib dimasukkan.' }, 400);
    }

    const license = await c.env.DB
      .prepare('SELECT l.*, t.display_name, t.price_label, t.max_users, t.max_storage_mb FROM licenses l JOIN tier_configs t ON l.tier_id = t.tier_id WHERE l.license_key = ?')
      .bind(licenseKey)
      .first<any>();

    if (!license) {
      return c.json<ApiResponse>({ success: false, error: 'Kode lisensi tidak valid atau tidak terdaftar di sistem pusat.' }, 404);
    }

    if (license.status === 'active') {
      return c.json<ApiResponse>({ success: false, error: 'Lisensi ini sudah aktif digunakan oleh instansi lain.' }, 409);
    }

    if (license.status === 'suspended') {
      return c.json<ApiResponse>({ success: false, error: 'Lisensi ini telah dibekukan (suspended) oleh developer.' }, 403);
    }

    return c.json<ApiResponse>({
      success: true,
      message: `Lisensi Valid: ${license.display_name} (Maks. ${license.custom_max_users || license.max_users} Pengguna)`,
      data: license
    });
  } catch (err: any) {
    return c.json<ApiResponse>({ success: false, error: err?.message || 'Gagal memverifikasi lisensi.' }, 500);
  }
});

// B. Registrasi Instansi & Akun Admin Baru (Chameleon Auto-Provisioning)
app.post('/api/auth/register-tenant', async (c) => {
  try {
    const body = await c.req.json<RegisterTenantRequest>();
    const licenseKey = body.license_key?.trim().toUpperCase();

    if (!licenseKey || !body.admin_full_name || !body.institution_name || !body.institution_type || !body.email || !body.password) {
      return c.json<ApiResponse>({ success: false, error: 'Semua kolom pendaftaran wajib diisi lengkap.' }, 400);
    }

    // 1. Validasi Lisensi
    const license = await c.env.DB
      .prepare('SELECT l.*, t.max_users FROM licenses l JOIN tier_configs t ON l.tier_id = t.tier_id WHERE l.license_key = ? AND l.status = "unclaimed"')
      .bind(licenseKey)
      .first<any>();

    if (!license) {
      return c.json<ApiResponse>({ success: false, error: 'Lisensi tidak valid atau sudah diklaim.' }, 400);
    }

    // 2. Buat ID & Kode Instansi Unik
    const tenantId = `tenant-${crypto.randomUUID()}`;
    const cleanPrefix = body.institution_name.replace(/[^A-Za-z0-9]/g, '').substring(0, 4).toUpperCase();
    const randomSuffix = Math.floor(100 + Math.random() * 900);
    const institutionCode = `${cleanPrefix}-${randomSuffix}`;

    // Rumus nomor surat default adaptif
    let defaultFormula = '[Kode_Klasifikasi]/[Nomor_Urut_3]/[Kode_Instansi]/[Bulan_Romawi]/[Tahun]';
    if (body.institution_type === 'village_gov') {
      defaultFormula = '[Kode_Klasifikasi]/[Nomor_Urut_3]/Pemdes-[Kode_Instansi]/[Tahun]';
    } else if (body.institution_type === 'corporate') {
      defaultFormula = '[Nomor_Urut_4]/[Kode_Instansi]-[Kode_Klasifikasi]/[Bulan_Romawi]/[Tahun]';
    }

    // 3. Simpan Tenant
    await c.env.DB
      .prepare(`
        INSERT INTO tenants (
          id, license_key, institution_code, institution_name, institution_type,
          institution_address, phone_number, email, logo_url, default_numbering_formula
        )
        VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
      `)
      .bind(
        tenantId, licenseKey, institutionCode, body.institution_name.trim(), body.institution_type,
        body.institution_address.trim(), body.phone_number.trim(), body.email.trim(), body.logo_url || null, defaultFormula
      )
      .run();

    // 4. Update status lisensi
    await c.env.DB
      .prepare('UPDATE licenses SET status = "active", assigned_tenant_id = ?, activated_at = CURRENT_TIMESTAMP WHERE license_key = ?')
      .bind(tenantId, licenseKey)
      .run();

    // 5. Buat Akun Admin Utama
    const userId = `usr-${crypto.randomUUID()}`;
    const passwordHash = await hashPassword(body.password);
    const initialSessionToken = `sess-${crypto.randomUUID()}`;

    await c.env.DB
      .prepare(`
        INSERT INTO users (id, tenant_id, email_username, password_hash, full_name, phone, role, active_session_token, active_device_info)
        VALUES (?, ?, ?, ?, ?, ?, 'admin', ?, ?)
      `)
      .bind(userId, tenantId, body.email.trim().toLowerCase(), passwordHash, body.admin_full_name.trim(), body.phone_number.trim(), initialSessionToken, body.device_info || 'ClientApp Desktop')
      .run();

    // 6. Chameleon Provisioning: Buat Entitas SDM Otomatis Sesuai Jenis Instansi
    const entityId = `ent-${crypto.randomUUID()}`;
    let entityName = 'Data Guru & Tendik (PTK)';
    let primaryIdLabel = 'NIP / NUPTK';

    if (body.institution_type === 'village_gov') {
      entityName = 'Data Perangkat & Aparatur Desa';
      primaryIdLabel = 'NIPD / NIP / NIK';
    } else if (body.institution_type === 'corporate') {
      entityName = 'Data Karyawan & Direksi';
      primaryIdLabel = 'NIK Karyawan / NPWP';
    } else if (body.institution_type === 'office_agency' || body.institution_type === 'foundation' || body.institution_type === 'healthcare') {
      entityName = 'Data Pegawai & Tenaga Medis';
      primaryIdLabel = 'NIP / STR / SIP';
    }

    await c.env.DB
      .prepare('INSERT INTO custom_entities (id, tenant_id, entity_slug, entity_display_name, is_system_default) VALUES (?, ?, "personnel", ?, 1)')
      .bind(entityId, tenantId, entityName)
      .run();

    // Kolom atribut SDM bawaan
    const attrQueries = [
      c.env.DB.prepare('INSERT INTO entity_attributes (tenant_id, entity_id, attr_key, attr_label, data_type, auto_calculate_tenure, sort_order) VALUES (?, ?, "nama_lengkap", "Nama Lengkap & Gelar", "text", 0, 1)').bind(tenantId, entityId),
      c.env.DB.prepare('INSERT INTO entity_attributes (tenant_id, entity_id, attr_key, attr_label, data_type, auto_calculate_tenure, sort_order) VALUES (?, ?, "primary_id", ?, "id_number", 0, 2)').bind(tenantId, entityId, primaryIdLabel),
      c.env.DB.prepare('INSERT INTO entity_attributes (tenant_id, entity_id, attr_key, attr_label, data_type, auto_calculate_tenure, sort_order) VALUES (?, ?, "jabatan", "Jabatan / Posisi", "text", 0, 3)').bind(tenantId, entityId),
      c.env.DB.prepare('INSERT INTO entity_attributes (tenant_id, entity_id, attr_key, attr_label, data_type, auto_calculate_tenure, sort_order) VALUES (?, ?, "tmt_date", "Terhitung Mulai Tanggal (TMT)", "date_tmt", 1, 4)').bind(tenantId, entityId)
    ];
    await c.env.DB.batch(attrQueries);

    // 7. Salin Global Preset Templates yang Cocok ke Kategori Instansi
    const presets = await c.env.DB
      .prepare('SELECT * FROM global_preset_templates WHERE target_institution_type = ? OR target_institution_type = "all"')
      .bind(body.institution_type)
      .all<GlobalPresetTemplate>();

    if (presets.results && presets.results.length > 0) {
      for (const p of presets.results) {
        const catId = `cat-${crypto.randomUUID()}`;
        await c.env.DB
          .prepare(`
            INSERT INTO categories (id, tenant_id, name, classification_code, description, numbering_formula, rich_content_json, has_rab_grid, rab_template_json, created_by)
            VALUES (?, ?, ?, '100', ?, ?, ?, ?, ?, ?)
          `)
          .bind(catId, tenantId, p.category_name, p.description || null, p.numbering_formula, p.rich_content_json, p.has_rab_grid, p.rab_preset_json || null, 'Sistem Preset')
          .run();

        // Salin fields
        if (p.form_fields_json) {
          try {
            const fields = JSON.parse(p.form_fields_json) as any[];
            for (const f of fields) {
              await c.env.DB
                .prepare('INSERT INTO form_fields (tenant_id, category_id, field_key, label, field_type, linked_entity_id, options_json, is_required, sort_order) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?)')
                .bind(tenantId, catId, f.field_key, f.label, f.field_type || 'text', f.linked_entity_id || null, f.options_json || null, f.is_required ? 1 : 0, f.sort_order || 0)
                .run();
            }
          } catch {}
        }
      }
    }

    // 8. Generate Token Sesi
    const jwtSecret = c.env.CLOUDINARY_API_SECRET || 'sovereign-executive-jwt-secret-2026';
    const now = Math.floor(Date.now() / 1000);
    const sessionPayload: UserSessionPayload = {
      user_id: userId,
      tenant_id: tenantId,
      email_username: body.email.trim().toLowerCase(),
      full_name: body.admin_full_name.trim(),
      role: 'admin',
      institution_code: institutionCode,
      institution_name: body.institution_name.trim(),
      institution_type: body.institution_type,
      session_token: initialSessionToken,
      tier_id: license.tier_id,
      iat: now,
      exp: now + (30 * 24 * 3600)
    };
    const token = await createJwt(sessionPayload, jwtSecret);

    return c.json<ApiResponse>({
      success: true,
      message: `Pendaftaran berhasil! Kode Instansi Anda: ${institutionCode}`,
      data: {
        token,
        institution_code: institutionCode,
        user: { id: userId, email_username: body.email, full_name: body.admin_full_name, role: 'admin' },
        tenant: { id: tenantId, institution_code: institutionCode, institution_name: body.institution_name, institution_type: body.institution_type }
      }
    });
  } catch (err: any) {
    return c.json<ApiResponse>({ success: false, error: err?.message || 'Gagal mendaftarkan instansi.' }, 500);
  }
});

// C. Login Multi-User & Penegakan Single Active Session
app.post('/api/auth/login', async (c) => {
  try {
    const body = await c.req.json<LoginRequest>();
    const code = body.institution_code?.trim().toUpperCase();
    const emailUser = body.email_username?.trim().toLowerCase();
    const password = body.password;

    if (!code || !emailUser || !password) {
      return c.json<ApiResponse>({ success: false, error: 'Kode Instansi, Email/Username, dan Password wajib diisi.' }, 400);
    }

    // 1. Validasi Instansi
    const tenant = await c.env.DB
      .prepare('SELECT * FROM tenants WHERE UPPER(institution_code) = ? AND is_active = 1')
      .bind(code)
      .first<Tenant>();

    if (!tenant) {
      return c.json<ApiResponse>({ success: false, error: `Instansi dengan kode '${code}' tidak ditemukan atau sedang dinonaktifkan.` }, 401);
    }

    // 2. Cek Lisensi Tenant (Kill-switch check)
    const license = await c.env.DB
      .prepare('SELECT l.*, t.display_name, t.price_label, t.max_users, t.max_storage_mb, t.has_watermark FROM licenses l JOIN tier_configs t ON l.tier_id = t.tier_id WHERE l.license_key = ?')
      .bind(tenant.license_key)
      .first<any>();

    if (!license || license.status === 'suspended') {
      return c.json<ApiResponse>({ success: false, error: 'Akses instansi ini sedang dibekukan (suspended) oleh developer pusat. Silakan hubungi developer.' }, 403);
    }

    // 3. Validasi Password
    const passwordHash = await hashPassword(password);
    const user = await c.env.DB
      .prepare('SELECT * FROM users WHERE tenant_id = ? AND email_username = ? AND password_hash = ? AND is_active = 1')
      .bind(tenant.id, emailUser, passwordHash)
      .first<User>();

    if (!user) {
      return c.json<ApiResponse>({ success: false, error: 'Email/Username atau Password Anda salah.' }, 401);
    }

    // 4. Tegakkan SINGLE ACTIVE SESSION: Terbitkan Token Sesi Baru
    const newSessionToken = `sess-${crypto.randomUUID()}`;
    await c.env.DB
      .prepare('UPDATE users SET active_session_token = ?, active_device_info = ?, last_login = CURRENT_TIMESTAMP WHERE id = ?')
      .bind(newSessionToken, body.device_info || 'ClientApp Desktop', user.id)
      .run();

    // 5. Generate JWT Token
    const jwtSecret = c.env.CLOUDINARY_API_SECRET || 'sovereign-executive-jwt-secret-2026';
    const now = Math.floor(Date.now() / 1000);
    const sessionPayload: UserSessionPayload = {
      user_id: user.id,
      tenant_id: tenant.id,
      email_username: user.email_username,
      full_name: user.full_name,
      role: user.role,
      institution_code: tenant.institution_code,
      institution_name: tenant.institution_name,
      institution_type: tenant.institution_type,
      session_token: newSessionToken,
      tier_id: license.tier_id,
      iat: now,
      exp: now + (30 * 24 * 3600)
    };
    const token = await createJwt(sessionPayload, jwtSecret);

    return c.json<ApiResponse>({
      success: true,
      message: `Selamat datang, ${user.full_name}!`,
      data: {
        token,
        user: { id: user.id, email_username: user.email_username, full_name: user.full_name, role: user.role },
        tenant,
        tier: license
      }
    });
  } catch (err: any) {
    return c.json<ApiResponse>({ success: false, error: err?.message || 'Terjadi kesalahan login.' }, 500);
  }
});

// ========================================================================
// 2. MIDDLEWARE AUTENTIKASI CLIENT APP & SINGLE ACTIVE SESSION
// ========================================================================
app.use('/api/tenant/*', async (c, next) => {
  return await handleClientAuth(c, next);
});
app.use('/api/dashboard/*', async (c, next) => {
  return await handleClientAuth(c, next);
});
app.use('/api/users*', async (c, next) => {
  return await handleClientAuth(c, next);
});
app.use('/api/entities*', async (c, next) => {
  return await handleClientAuth(c, next);
});
app.use('/api/categories*', async (c, next) => {
  return await handleClientAuth(c, next);
});
app.use('/api/documents*', async (c, next) => {
  return await handleClientAuth(c, next);
});
app.use('/api/incoming-mails*', async (c, next) => {
  return await handleClientAuth(c, next);
});
app.use('/api/cloudinary/*', async (c, next) => {
  return await handleClientAuth(c, next);
});

async function handleClientAuth(c: Context<{ Bindings: Env; Variables: Variables }>, next: Next) {
  const authHeader = c.req.header('Authorization');
  if (!authHeader || !authHeader.startsWith('Bearer ')) {
    return c.json<ApiResponse>({ success: false, error: 'Autentikasi gagal: Token tidak disertakan.' }, 401);
  }

  const token = authHeader.substring(7).trim();
  const jwtSecret = c.env.CLOUDINARY_API_SECRET || 'sovereign-executive-jwt-secret-2026';
  const payload = await verifyJwt(token, jwtSecret);

  if (!payload) {
    return c.json<ApiResponse>({ success: false, error: 'Sesi Anda telah kedaluwarsa. Silakan masuk kembali.' }, 401);
  }

  // Cek SINGLE ACTIVE SESSION di database
  const user = await c.env.DB
    .prepare('SELECT * FROM users WHERE id = ? AND is_active = 1')
    .bind(payload.user_id)
    .first<User>();

  if (!user || user.active_session_token !== payload.session_token) {
    return c.json<ApiResponse>({
      success: false,
      error: 'Sesi Terminal Dialihkan. Akun Anda baru saja diaktifkan pada perangkat kerja lain. Demi keamanan otorisasi, sesi pada perangkat ini telah ditutup secara otomatis.'
    }, 401);
  }

  // Load Tenant & Tier
  const tenant = await c.env.DB
    .prepare('SELECT * FROM tenants WHERE id = ? AND is_active = 1')
    .bind(payload.tenant_id)
    .first<Tenant>();

  if (!tenant) {
    return c.json<ApiResponse>({ success: false, error: 'Sinkronisasi Layanan Instansi Ditangguhkan Sementara (Kode: SVR-403). Silakan hubungi Administrator Lisensi Pusat untuk pembaruan masa aktif node instansi Anda.' }, 403);
  }

  const license = await c.env.DB
    .prepare('SELECT t.* FROM licenses l JOIN tier_configs t ON l.tier_id = t.tier_id WHERE l.license_key = ? AND l.status IN ("active", "unassigned_node")')
    .bind(tenant.license_key)
    .first<TierConfig>();

  if (!license) {
    return c.json<ApiResponse>({ success: false, error: 'Sinkronisasi Layanan Instansi Ditangguhkan Sementara (Kode: SVR-403). Silakan hubungi Administrator Lisensi Pusat untuk pembaruan masa aktif node instansi Anda.' }, 403);
  }

  c.set('session', payload);
  c.set('tenant', tenant);
  c.set('tier', license);

  await next();
}

function requireAdmin(c: Context<{ Bindings: Env; Variables: Variables }>): boolean {
  const session = c.get('session') as UserSessionPayload;
  return session?.role === 'admin';
}

// ------------------------------------------------------------------------
// 3. EXECUTIVE DASHBOARD & METRICS
// ------------------------------------------------------------------------
app.get('/api/dashboard/summary', async (c) => {
  const tenant = c.get('tenant') as Tenant;

  // 1. Total dokumen
  const docStats = await c.env.DB
    .prepare(`
      SELECT 
        COUNT(*) as total_docs,
        SUM(CASE WHEN archive_status = 'completed_digital' THEN 1 ELSE 0 END) as waiting_scan,
        SUM(CASE WHEN archive_status = 'verified_scan' THEN 1 ELSE 0 END) as verified_scan,
        SUM(CASE WHEN archive_status = 'pending_deletion' THEN 1 ELSE 0 END) as pending_deletion,
        SUM(rab_total_amount) as total_rab_amount
      FROM documents WHERE tenant_id = ?
    `)
    .bind(tenant.id)
    .first<any>();

  // 2. Contract & SK Expiry Watcher (Kontrak habis dalam 30 hari)
  const nowStr = new Date().toISOString().substring(0, 10);
  const futureStr = new Date(Date.now() + 30 * 24 * 3600 * 1000).toISOString().substring(0, 10);

  const expiringPersonnel = await c.env.DB
    .prepare(`
      SELECT primary_name, primary_id_number, contract_end_date 
      FROM entity_records 
      WHERE tenant_id = ? AND contract_end_date IS NOT NULL AND contract_end_date BETWEEN ? AND ?
      ORDER BY contract_end_date ASC LIMIT 5
    `)
    .bind(tenant.id, nowStr, futureStr)
    .all<any>();

  return c.json<ApiResponse>({
    success: true,
    data: {
      total_docs: docStats?.total_docs || 0,
      waiting_scan: docStats?.waiting_scan || 0,
      verified_scan: docStats?.verified_scan || 0,
      pending_deletion: docStats?.pending_deletion || 0,
      total_rab_amount: docStats?.total_rab_amount || 0,
      storage_used_bytes: tenant.storage_used_bytes || 0,
      expiring_personnel: expiringPersonnel.results || []
    }
  });
});

// ------------------------------------------------------------------------
// 4. MANAJEMEN PENGGUNA (BATAS TIER: max_users)
// ------------------------------------------------------------------------
app.get('/api/users', async (c) => {
  const tenant = c.get('tenant') as Tenant;
  const users = await c.env.DB
    .prepare('SELECT id, tenant_id, email_username, full_name, phone, role, is_active, last_login, created_at FROM users WHERE tenant_id = ? ORDER BY role ASC, full_name ASC')
    .bind(tenant.id)
    .all<User>();

  return c.json<ApiResponse<User[]>>({ success: true, data: users.results || [] });
});

app.post('/api/users', async (c) => {
  if (!requireAdmin(c)) return c.json<ApiResponse>({ success: false, error: 'Akses ditolak: Hanya Admin Instansi.' }, 403);

  const tenant = c.get('tenant') as Tenant;
  const tier = c.get('tier') as TierConfig;

  // Cek Kuota max_users
  const currentCount = await c.env.DB
    .prepare('SELECT COUNT(*) as cnt FROM users WHERE tenant_id = ?')
    .bind(tenant.id)
    .first<any>();

  if ((currentCount?.cnt || 0) >= tier.max_users) {
    return c.json<ApiResponse>({
      success: false,
      error: `Batas kuota pengguna telah tercapai (Maks. ${tier.max_users} Pengguna pada paket Anda). Tingkatkan lisensi Anda untuk menambah pengguna baru.`
    }, 400);
  }

  const body = await c.req.json<any>();
  const email = body.email_username?.trim().toLowerCase();
  const password = body.password?.trim();
  const fullName = body.full_name?.trim();
  const role = body.role === 'admin' ? 'admin' : 'staff';

  if (!email || !password || !fullName) {
    return c.json<ApiResponse>({ success: false, error: 'Email, Password, dan Nama Lengkap wajib diisi.' }, 400);
  }

  const existing = await c.env.DB
    .prepare('SELECT id FROM users WHERE tenant_id = ? AND email_username = ?')
    .bind(tenant.id, email)
    .first();

  if (existing) {
    return c.json<ApiResponse>({ success: false, error: 'Email/Username ini sudah terdaftar di instansi Anda.' }, 409);
  }

  const passwordHash = await hashPassword(password);
  const userId = `usr-${crypto.randomUUID()}`;

  await c.env.DB
    .prepare('INSERT INTO users (id, tenant_id, email_username, password_hash, full_name, phone, role) VALUES (?, ?, ?, ?, ?, ?, ?)')
    .bind(userId, tenant.id, email, passwordHash, fullName, body.phone || null, role)
    .run();

  return c.json<ApiResponse>({
    success: true,
    message: `Pengguna ${fullName} berhasil ditambahkan.`
  });
});

app.delete('/api/users/:id', async (c) => {
  if (!requireAdmin(c)) return c.json<ApiResponse>({ success: false, error: 'Akses ditolak: Hanya Admin Instansi.' }, 403);
  const session = c.get('session') as UserSessionPayload;
  const tenant = c.get('tenant') as Tenant;
  const targetId = c.req.param('id');

  if (targetId === session.user_id) {
    return c.json<ApiResponse>({ success: false, error: 'Anda tidak dapat menghapus akun Anda sendiri yang sedang aktif.' }, 400);
  }

  await c.env.DB
    .prepare('DELETE FROM users WHERE id = ? AND tenant_id = ?')
    .bind(targetId, tenant.id)
    .run();

  return c.json<ApiResponse>({ success: true, message: 'Pengguna berhasil dihapus.' });
});

// ------------------------------------------------------------------------
// 5. PENGATURAN PROFIL & KOP INSTANSI
// ------------------------------------------------------------------------
app.put('/api/tenant/settings', async (c) => {
  if (!requireAdmin(c)) return c.json<ApiResponse>({ success: false, error: 'Akses ditolak.' }, 403);
  const tenant = c.get('tenant') as Tenant;
  const body = await c.req.json<any>();

  await c.env.DB
    .prepare(`
      UPDATE tenants 
      SET institution_name = COALESCE(?, institution_name),
          institution_address = COALESCE(?, institution_address),
          phone_number = COALESCE(?, phone_number),
          logo_url = COALESCE(?, logo_url),
          signature_url = COALESCE(?, signature_url),
          stamp_url = COALESCE(?, stamp_url),
          letterhead_config_json = COALESCE(?, letterhead_config_json),
          default_numbering_formula = COALESCE(?, default_numbering_formula)
      WHERE id = ?
    `)
    .bind(
      body.institution_name?.trim() || null,
      body.institution_address?.trim() || null,
      body.phone_number?.trim() || null,
      body.logo_url || null,
      body.signature_url || null,
      body.stamp_url || null,
      body.letterhead_config_json || null,
      body.default_numbering_formula?.trim() || null,
      tenant.id
    )
    .run();

  const updated = await c.env.DB.prepare('SELECT * FROM tenants WHERE id = ?').bind(tenant.id).first<Tenant>();
  return c.json<ApiResponse>({ success: true, message: 'Pengaturan instansi berhasil disimpan.', data: updated });
});

// ------------------------------------------------------------------------
// 6. ADAPTIVE SDM & CUSTOM ENTITIES
// ------------------------------------------------------------------------
app.get('/api/entities', async (c) => {
  const tenant = c.get('tenant') as Tenant;

  const entities = await c.env.DB
    .prepare('SELECT * FROM custom_entities WHERE tenant_id = ?')
    .bind(tenant.id)
    .all<CustomEntity>();

  const attrs = await c.env.DB
    .prepare('SELECT * FROM entity_attributes WHERE tenant_id = ? ORDER BY sort_order ASC')
    .bind(tenant.id)
    .all<EntityAttribute>();

  const results = (entities.results || []).map(ent => ({
    ...ent,
    attributes: (attrs.results || []).filter(a => a.entity_id === ent.id)
  }));

  return c.json<ApiResponse>({ success: true, data: results });
});

app.post('/api/entities', async (c) => {
  if (!requireAdmin(c)) return c.json<ApiResponse>({ success: false, error: 'Akses ditolak.' }, 403);
  const tenant = c.get('tenant') as Tenant;
  const body = await c.req.json<any>();

  const id = `ent-${crypto.randomUUID()}`;
  await c.env.DB
    .prepare('INSERT INTO custom_entities (id, tenant_id, entity_slug, entity_display_name, is_system_default) VALUES (?, ?, ?, ?, 0)')
    .bind(id, tenant.id, body.entity_slug || 'custom', body.entity_display_name)
    .run();

  return c.json<ApiResponse>({ success: true, message: `Daftar '${body.entity_display_name}' berhasil dibuat.`, data: { id } });
});

app.post('/api/entities/:id/attributes', async (c) => {
  if (!requireAdmin(c)) return c.json<ApiResponse>({ success: false, error: 'Akses ditolak.' }, 403);
  const tenant = c.get('tenant') as Tenant;
  const entityId = c.req.param('id');
  const body = await c.req.json<any>();

  await c.env.DB
    .prepare('INSERT INTO entity_attributes (tenant_id, entity_id, attr_key, attr_label, data_type, select_options_json, auto_calculate_tenure, sort_order) VALUES (?, ?, ?, ?, ?, ?, ?, ?)')
    .bind(
      tenant.id, entityId, body.attr_key.trim(), body.attr_label.trim(), body.data_type || 'text',
      body.select_options_json || null, body.auto_calculate_tenure ? 1 : 0, body.sort_order || 0
    )
    .run();

  return c.json<ApiResponse>({ success: true, message: `Atribut '${body.attr_label}' berhasil ditambahkan.` });
});

app.get('/api/entities/:id/records', async (c) => {
  const tenant = c.get('tenant') as Tenant;
  const entityId = c.req.param('id');

  const records = await c.env.DB
    .prepare('SELECT * FROM entity_records WHERE tenant_id = ? AND entity_id = ? ORDER BY primary_name ASC')
    .bind(tenant.id, entityId)
    .all<EntityRecord>();

  // Pasang perhitungan masa kerja otomatis pada setiap record
  const enriched = (records.results || []).map(r => {
    let vals: any = {};
    try { vals = JSON.parse(r.values_json); } catch {}
    vals['masa_kerja'] = calculateTenure(r.tmt_date);
    return {
      ...r,
      values: vals,
      tenure_display: vals['masa_kerja']
    };
  });

  return c.json<ApiResponse>({ success: true, data: enriched });
});

app.post('/api/entities/:id/records', async (c) => {
  const tenant = c.get('tenant') as Tenant;
  const entityId = c.req.param('id');
  const body = await c.req.json<any>();

  const id = `rec-${crypto.randomUUID()}`;
  const valuesStr = typeof body.values === 'string' ? body.values : JSON.stringify(body.values || {});

  await c.env.DB
    .prepare('INSERT INTO entity_records (id, tenant_id, entity_id, primary_name, primary_id_number, tmt_date, contract_end_date, values_json) VALUES (?, ?, ?, ?, ?, ?, ?, ?)')
    .bind(id, tenant.id, entityId, body.primary_name, body.primary_id_number || null, body.tmt_date || null, body.contract_end_date || null, valuesStr)
    .run();

  return c.json<ApiResponse>({ success: true, message: 'Data berhasil disimpan.', data: { id } });
});

app.put('/api/entities/records/:recordId', async (c) => {
  const tenant = c.get('tenant') as Tenant;
  const recordId = c.req.param('recordId');
  const body = await c.req.json<any>();
  const valuesStr = typeof body.values === 'string' ? body.values : JSON.stringify(body.values || {});

  await c.env.DB
    .prepare('UPDATE entity_records SET primary_name = ?, primary_id_number = ?, tmt_date = ?, contract_end_date = ?, values_json = ? WHERE id = ? AND tenant_id = ?')
    .bind(body.primary_name, body.primary_id_number || null, body.tmt_date || null, body.contract_end_date || null, valuesStr, recordId, tenant.id)
    .run();

  return c.json<ApiResponse>({ success: true, message: 'Data berhasil diperbarui.' });
});

app.delete('/api/entities/records/:recordId', async (c) => {
  const tenant = c.get('tenant') as Tenant;
  const recordId = c.req.param('recordId');

  await c.env.DB
    .prepare('DELETE FROM entity_records WHERE id = ? AND tenant_id = ?')
    .bind(recordId, tenant.id)
    .run();

  return c.json<ApiResponse>({ success: true, message: 'Data berhasil dihapus.' });
});

// ------------------------------------------------------------------------
// 7. FORMAT KATEGORI & STUDIO TEMPLATE (BATAS TIER: max_templates)
// ------------------------------------------------------------------------
app.get('/api/categories', async (c) => {
  const tenant = c.get('tenant') as Tenant;

  const cats = await c.env.DB
    .prepare('SELECT * FROM categories WHERE tenant_id = ? ORDER BY name ASC')
    .bind(tenant.id)
    .all<Category>();

  const fields = await c.env.DB
    .prepare('SELECT * FROM form_fields WHERE tenant_id = ? ORDER BY sort_order ASC')
    .bind(tenant.id)
    .all<FormField>();

  const results = (cats.results || []).map(cat => ({
    ...cat,
    fields: (fields.results || []).filter(f => f.category_id === cat.id)
  }));

  return c.json<ApiResponse>({ success: true, data: results });
});

app.post('/api/categories', async (c) => {
  if (!requireAdmin(c)) return c.json<ApiResponse>({ success: false, error: 'Akses ditolak.' }, 403);
  const tenant = c.get('tenant') as Tenant;
  const tier = c.get('tier') as TierConfig;
  const session = c.get('session') as UserSessionPayload;
  const body = await c.req.json<any>();

  const categoryId = body.id || `cat-${crypto.randomUUID()}`;

  // Cek batas max_templates jika kategori baru
  if (!body.id) {
    const countCheck = await c.env.DB
      .prepare('SELECT COUNT(*) as cnt FROM categories WHERE tenant_id = ?')
      .bind(tenant.id)
      .first<any>();

    if ((countCheck?.cnt || 0) >= tier.max_templates) {
      return c.json<ApiResponse>({
        success: false,
        error: `Batas format surat telah tercapai (Maks. ${tier.max_templates} Kategori). Tingkatkan paket Anda untuk menambah format template baru.`
      }, 400);
    }
  }

  await c.env.DB
    .prepare(`
      INSERT INTO categories (id, tenant_id, name, classification_code, description, numbering_formula, rich_content_json, has_rab_grid, rab_template_json, created_by)
      VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
      ON CONFLICT(id) DO UPDATE SET
        name = excluded.name,
        classification_code = excluded.classification_code,
        description = excluded.description,
        numbering_formula = excluded.numbering_formula,
        rich_content_json = excluded.rich_content_json,
        has_rab_grid = excluded.has_rab_grid,
        rab_template_json = excluded.rab_template_json
    `)
    .bind(
      categoryId, tenant.id, body.name, body.classification_code || '100', body.description || null,
      body.numbering_formula || tenant.default_numbering_formula, body.rich_content_json || '{}',
      body.has_rab_grid ? 1 : 0, body.rab_template_json || null, session.full_name
    )
    .run();

  // Reset & replace form fields
  await c.env.DB.prepare('DELETE FROM form_fields WHERE tenant_id = ? AND category_id = ?').bind(tenant.id, categoryId).run();

  if (body.fields && Array.isArray(body.fields)) {
    for (const f of body.fields) {
      await c.env.DB
        .prepare('INSERT INTO form_fields (tenant_id, category_id, field_key, label, field_type, linked_entity_id, options_json, is_required, sort_order) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?)')
        .bind(tenant.id, categoryId, f.field_key, f.label, f.field_type || 'text', f.linked_entity_id || null, f.options_json || null, f.is_required ? 1 : 0, f.sort_order || 0)
        .run();
    }
  }

  return c.json<ApiResponse>({ success: true, message: `Format surat '${body.name}' berhasil disimpan.`, data: { id: categoryId } });
});

app.delete('/api/categories/:id', async (c) => {
  if (!requireAdmin(c)) return c.json<ApiResponse>({ success: false, error: 'Akses ditolak.' }, 403);
  const tenant = c.get('tenant') as Tenant;
  const id = c.req.param('id');

  await c.env.DB.prepare('DELETE FROM form_fields WHERE tenant_id = ? AND category_id = ?').bind(tenant.id, id).run();
  await c.env.DB.prepare('DELETE FROM categories WHERE tenant_id = ? AND id = ?').bind(tenant.id, id).run();

  return c.json<ApiResponse>({ success: true, message: 'Format surat berhasil dihapus.' });
});

// ------------------------------------------------------------------------
// 8. GENERATOR NOMOR SURAT CERDAS & DETEKTOR CELAH NOMOR KOSONG
// ------------------------------------------------------------------------
app.get('/api/documents/next-number/:categoryId', async (c) => {
  const tenant = c.get('tenant') as Tenant;
  const categoryId = c.req.param('categoryId');

  const cat = await c.env.DB
    .prepare('SELECT * FROM categories WHERE id = ? AND tenant_id = ?')
    .bind(categoryId, tenant.id)
    .first<Category>();

  const formula = cat?.numbering_formula || tenant.default_numbering_formula;
  const classCode = cat?.classification_code || '100';

  // Cari semua sequence yang pernah ada di kategori ini
  const existingSeqs = await c.env.DB
    .prepare('SELECT doc_sequence_number FROM documents WHERE tenant_id = ? AND category_id = ? ORDER BY doc_sequence_number ASC')
    .bind(tenant.id, categoryId)
    .all<{ doc_sequence_number: number }>();

  const seqList = (existingSeqs.results || []).map(s => s.doc_sequence_number);
  let nextSeq = seqList.length > 0 ? Math.max(...seqList) + 1 : 1;
  let hasGap = false;
  let gapNumber: number | null = null;

  // Cek apakah ada nomor yang terlompat (gap)
  for (let i = 1; i < nextSeq; i++) {
    if (!seqList.includes(i)) {
      hasGap = true;
      gapNumber = i;
      break;
    }
  }

  // Rakit format tampilan nomor
  const now = new Date();
  const yearStr = now.getFullYear().toString();
  const monthNum = (now.getMonth() + 1).toString().padStart(2, '0');
  const monthRom = getRomanMonth(now);
  const seq3 = nextSeq.toString().padStart(3, '0');
  const seq4 = nextSeq.toString().padStart(4, '0');

  let formatted = formula
    .replace(/\[Kode_Klasifikasi\]/g, classCode)
    .replace(/\[Nomor_Urut_3\]/g, seq3)
    .replace(/\[Nomor_Urut_4\]/g, seq4)
    .replace(/\[Kode_Instansi\]/g, tenant.institution_code)
    .replace(/\[Bulan_Romawi\]/g, monthRom)
    .replace(/\[Bulan_Angka\]/g, monthNum)
    .replace(/\[Tahun\]/g, yearStr);

  return c.json<ApiResponse>({
    success: true,
    data: {
      next_sequence: nextSeq,
      has_gap: hasGap,
      gap_sequence: gapNumber,
      formula: formula,
      formatted_number: formatted
    }
  });
});

// ------------------------------------------------------------------------
// 9. DOKUMEN & ZERO-DOWNLOAD OFFICE SUITE
// ------------------------------------------------------------------------
app.get('/api/documents', async (c) => {
  const tenant = c.get('tenant') as Tenant;
  const q = c.req.query('q')?.trim();
  const status = c.req.query('status')?.trim();

  let sql = `
    SELECT d.*, c.name as category_name
    FROM documents d
    LEFT JOIN categories c ON d.category_id = c.id
    WHERE d.tenant_id = ?
  `;
  const params: any[] = [tenant.id];

  if (q) {
    sql += ' AND (d.doc_number_full LIKE ? OR d.title_or_subject LIKE ? OR d.created_by_name LIKE ?)';
    params.push(`%${q}%`, `%${q}%`, `%${q}%`);
  }

  if (status) {
    sql += ' AND d.archive_status = ?';
    params.push(status);
  }

  sql += ' ORDER BY d.created_at DESC LIMIT 100';

  const docs = await c.env.DB.prepare(sql).bind(...params).all<DocumentRecord>();
  return c.json<ApiResponse<DocumentRecord[]>>({ success: true, data: docs.results || [] });
});

app.post('/api/documents', async (c) => {
  const tenant = c.get('tenant') as Tenant;
  const session = c.get('session') as UserSessionPayload;
  const body = await c.req.json<SaveDocumentRequest>();

  const docId = `doc-${crypto.randomUUID()}`;
  const metadataStr = typeof body.metadata_json === 'string' ? body.metadata_json : JSON.stringify(body.metadata_json || {});
  const archiveStatus = body.is_draft ? 'draft' : 'completed_digital';

  // Tentukan sequence number
  let seq = body.doc_sequence_number;
  if (!seq) {
    const maxSeq = await c.env.DB
      .prepare('SELECT MAX(doc_sequence_number) as max_seq FROM documents WHERE tenant_id = ? AND category_id = ?')
      .bind(tenant.id, body.category_id)
      .first<any>();
    seq = (maxSeq?.max_seq || 0) + 1;
  }

  await c.env.DB
    .prepare(`
      INSERT INTO documents (
        id, tenant_id, category_id, doc_sequence_number, doc_number_full,
        title_or_subject, recipient, metadata_json, rendered_content_json,
        rab_data_json, rab_total_amount, locked_archive_url, archive_status,
        created_by_user_id, created_by_name
      )
      VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
    `)
    .bind(
      docId, tenant.id, body.category_id, seq, body.doc_number_full,
      body.title_or_subject, body.recipient || null, metadataStr, body.rendered_content_json,
      body.rab_data_json || null, body.rab_total_amount || 0, body.locked_archive_url || null,
      archiveStatus, session.user_id, session.full_name
    )
    .run();

  return c.json<ApiResponse>({
    success: true,
    message: `Surat Nomor '${body.doc_number_full}' berhasil disimpan ke Cloud!`,
    data: { id: docId, doc_number_full: body.doc_number_full }
  });
});

app.put('/api/documents/:id', async (c) => {
  const tenant = c.get('tenant') as Tenant;
  const docId = c.req.param('id');
  const body = await c.req.json<any>();
  const metadataStr = typeof body.metadata_json === 'string' ? body.metadata_json : JSON.stringify(body.metadata_json || {});

  await c.env.DB
    .prepare(`
      UPDATE documents 
      SET title_or_subject = COALESCE(?, title_or_subject),
          recipient = COALESCE(?, recipient),
          metadata_json = COALESCE(?, metadata_json),
          rendered_content_json = COALESCE(?, rendered_content_json),
          rab_data_json = COALESCE(?, rab_data_json),
          rab_total_amount = COALESCE(?, rab_total_amount),
          updated_at = CURRENT_TIMESTAMP
      WHERE id = ? AND tenant_id = ?
    `)
    .bind(
      body.title_or_subject || null, body.recipient || null, metadataStr,
      body.rendered_content_json || null, body.rab_data_json || null,
      body.rab_total_amount || 0, docId, tenant.id
    )
    .run();

  return c.json<ApiResponse>({ success: true, message: 'Revisi dokumen berhasil disimpan ke Cloud.' });
});

// Tautkan berkas scan basah
app.post('/api/documents/:id/link-scan', async (c) => {
  const tenant = c.get('tenant') as Tenant;
  const docId = c.req.param('id');
  const body = await c.req.json<{ scan_url: string }>();

  await c.env.DB
    .prepare('UPDATE documents SET scan_url = ?, archive_status = "verified_scan", updated_at = CURRENT_TIMESTAMP WHERE id = ? AND tenant_id = ?')
    .bind(body.scan_url, docId, tenant.id)
    .run();

  return c.json<ApiResponse>({ success: true, message: 'Scan tanda tangan & stempel basah berhasil ditautkan. Status: Terverifikasi Lengkap.' });
});

// Alur Pengajuan Hapus Dokumen oleh Staf
app.post('/api/documents/:id/request-delete', async (c) => {
  const session = c.get('session') as UserSessionPayload;
  const tenant = c.get('tenant') as Tenant;
  const docId = c.req.param('id');
  const body = await c.req.json<{ reason: string }>();

  await c.env.DB
    .prepare(`
      UPDATE documents 
      SET archive_status = 'pending_deletion',
          deletion_requested_by_id = ?,
          deletion_requested_by_name = ?,
          deletion_reason = ?,
          deletion_requested_at = CURRENT_TIMESTAMP
      WHERE id = ? AND tenant_id = ?
    `)
    .bind(session.user_id, session.full_name, body.reason || 'Pengajuan hapus oleh staf', docId, tenant.id)
    .run();

  return c.json<ApiResponse>({ success: true, message: 'Pengajuan penghapusan dokumen telah dikirim ke Admin Instansi untuk persetujuan.' });
});

// Alur Persetujuan Hapus oleh Admin Instansi
app.post('/api/documents/:id/resolve-delete', async (c) => {
  if (!requireAdmin(c)) return c.json<ApiResponse>({ success: false, error: 'Akses ditolak.' }, 403);
  const tenant = c.get('tenant') as Tenant;
  const docId = c.req.param('id');
  const body = await c.req.json<{ approve: boolean }>();

  if (body.approve) {
    await c.env.DB.prepare('DELETE FROM documents WHERE id = ? AND tenant_id = ?').bind(docId, tenant.id).run();
    return c.json<ApiResponse>({ success: true, message: 'Dokumen telah dihapus permanen oleh Admin.' });
  } else {
    await c.env.DB
      .prepare('UPDATE documents SET archive_status = "completed_digital", deletion_reason = NULL WHERE id = ? AND tenant_id = ?')
      .bind(docId, tenant.id)
      .run();
    return c.json<ApiResponse>({ success: true, message: 'Pengajuan hapus ditolak. Dokumen dipulihkan ke arsip digital.' });
  }
});

// ------------------------------------------------------------------------
// 10. SURAT MASUK & LEMBAR DISPOSISI
// ------------------------------------------------------------------------
app.get('/api/incoming-mails', async (c) => {
  const tenant = c.get('tenant') as Tenant;
  const mails = await c.env.DB
    .prepare('SELECT * FROM incoming_mails WHERE tenant_id = ? ORDER BY received_date DESC LIMIT 100')
    .bind(tenant.id)
    .all<IncomingMail>();

  return c.json<ApiResponse<IncomingMail[]>>({ success: true, data: mails.results || [] });
});

app.post('/api/incoming-mails', async (c) => {
  const tenant = c.get('tenant') as Tenant;
  const session = c.get('session') as UserSessionPayload;
  const body = await c.req.json<any>();

  const id = `mail-${crypto.randomUUID()}`;
  await c.env.DB
    .prepare(`
      INSERT INTO incoming_mails (
        id, tenant_id, agenda_number, sender_origin, original_mail_number,
        mail_date, received_date, subject, scan_url, disposition_to,
        disposition_instruction, urgency_level, created_by_name
      )
      VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
    `)
    .bind(
      id, tenant.id, body.agenda_number, body.sender_origin, body.original_mail_number,
      body.mail_date, body.received_date, body.subject, body.scan_url || null,
      body.disposition_to || null, body.disposition_instruction || null,
      body.urgency_level || 'Biasa', session.full_name
    )
    .run();

  return c.json<ApiResponse>({ success: true, message: 'Surat masuk berhasil dicatat ke agenda.', data: { id } });
});

app.delete('/api/incoming-mails/:id', async (c) => {
  if (!requireAdmin(c)) return c.json<ApiResponse>({ success: false, error: 'Akses ditolak.' }, 403);
  const tenant = c.get('tenant') as Tenant;
  const id = c.req.param('id');

  await c.env.DB.prepare('DELETE FROM incoming_mails WHERE id = ? AND tenant_id = ?').bind(id, tenant.id).run();
  return c.json<ApiResponse>({ success: true, message: 'Surat masuk berhasil dihapus.' });
});

// ------------------------------------------------------------------------
// 11. SIGNATURE CLOUDINARY TERKUNCI & VALIDASI KUOTA STORAGE
// ------------------------------------------------------------------------
app.post('/api/cloudinary/sign', async (c) => {
  const tenant = c.get('tenant') as Tenant;
  const tier = c.get('tier') as TierConfig;
  const body = await c.req.json<any>();

  // Cek batas kuota storage
  const maxStorageBytes = tier.max_storage_mb * 1024 * 1024;
  if (tenant.storage_used_bytes >= maxStorageBytes) {
    return c.json<ApiResponse>({
      success: false,
      error: 'Kapasitas Alokasi Instansi Mencapai Batas Maksimal. Silakan tingkatkan Tingkat Lisensi (Tier Entitlement) instansi Anda untuk menambah alokasi pengguna dan ruang arsip.'
    }, 400);
  }

  const subfolder = body.subfolder?.replace(/^\/+|\/+$/g, '') || 'archives/scans';
  const targetFolder = `tenants/${tenant.id}/${subfolder}`;
  const timestamp = Math.round(new Date().getTime() / 1000);

  const toSign = `folder=${targetFolder}&timestamp=${timestamp}${c.env.CLOUDINARY_API_SECRET}`;
  const signature = await generateSha1Hex(toSign);

  return c.json<ApiResponse>({
    success: true,
    data: {
      cloudName: c.env.CLOUDINARY_CLOUD_NAME,
      apiKey: c.env.CLOUDINARY_API_KEY,
      timestamp,
      folder: targetFolder,
      signature,
      uploadUrl: `https://api.cloudinary.com/v1_1/${c.env.CLOUDINARY_CLOUD_NAME}/auto/upload`
    }
  });
});

// ========================================================================
// 12. CORE PROVISIONING CONSOLE API (SYSTEM PROVISIONING ROUTE)
// ========================================================================
const PROVISION_TOKEN = 'DEV-MASTER-GODMODE-2026';

const provMiddleware = async (c: Context<{ Bindings: Env; Variables: Variables }>, next: Next) => {
  if (c.req.path.includes('/contact-gateway')) {
    return next();
  }

  const token = c.req.header('X-Edge-Provision-Token') || c.req.header('X-Developer-Master-Key');
  const configuredToken = c.env.DEVELOPER_MASTER_KEY || PROVISION_TOKEN;

  if (!token || token !== configuredToken) {
    return c.json<ApiResponse>({
      success: false,
      error: 'Otorisasi Layanan Ditolak: Token Provisioning Node tidak valid.'
    }, 403);
  }

  await next();
};

app.use('/api/v1/sys-prov/*', provMiddleware);
app.use('/api/dev/*', provMiddleware);

// Overview Developer
app.get('/api/dev/overview', async (c) => {
  const tenants = await c.env.DB.prepare('SELECT COUNT(*) as cnt FROM tenants').first<any>();
  const users = await c.env.DB.prepare('SELECT COUNT(*) as cnt FROM users').first<any>();
  const licenses = await c.env.DB.prepare('SELECT COUNT(*) as cnt FROM licenses').first<any>();
  const docs = await c.env.DB.prepare('SELECT COUNT(*) as cnt FROM documents').first<any>();

  return c.json<ApiResponse>({
    success: true,
    data: {
      total_tenants: tenants?.cnt || 0,
      total_users: users?.cnt || 0,
      total_licenses: licenses?.cnt || 0,
      total_documents: docs?.cnt || 0
    }
  });
});

// Tiers & Dynamic Pricing
app.get('/api/dev/tiers', async (c) => {
  const tiers = await c.env.DB.prepare('SELECT * FROM tier_configs ORDER BY max_users ASC').all<TierConfig>();
  return c.json<ApiResponse>({ success: true, data: tiers.results || [] });
});

app.put('/api/dev/tiers/:tierId', async (c) => {
  const tierId = c.req.param('tierId');
  const body = await c.req.json<any>();

  await c.env.DB
    .prepare(`
      UPDATE tier_configs 
      SET display_name = COALESCE(?, display_name),
          price_label = COALESCE(?, price_label),
          max_users = COALESCE(?, max_users),
          max_templates = COALESCE(?, max_templates),
          max_storage_mb = COALESCE(?, max_storage_mb),
          allow_rab_module = COALESCE(?, allow_rab_module),
          allow_direct_scanner = COALESCE(?, allow_direct_scanner),
          allow_qr_validation = COALESCE(?, allow_qr_validation),
          has_watermark = COALESCE(?, has_watermark),
          updated_at = CURRENT_TIMESTAMP
      WHERE tier_id = ?
    `)
    .bind(
      body.display_name || null, body.price_label || null, body.max_users || null,
      body.max_templates || null, body.max_storage_mb || null,
      body.allow_rab_module !== undefined ? (body.allow_rab_module ? 1 : 0) : null,
      body.allow_direct_scanner !== undefined ? (body.allow_direct_scanner ? 1 : 0) : null,
      body.allow_qr_validation !== undefined ? (body.allow_qr_validation ? 1 : 0) : null,
      body.has_watermark !== undefined ? (body.has_watermark ? 1 : 0) : null,
      tierId
    )
    .run();

  return c.json<ApiResponse>({ success: true, message: `Tier '${tierId}' berhasil diperbarui.` });
});

// Lisensi & Generator
app.get('/api/dev/licenses', async (c) => {
  const list = await c.env.DB
    .prepare(`
      SELECT l.*, t.display_name as tier_name, tn.institution_name, tn.institution_code
      FROM licenses l
      JOIN tier_configs t ON l.tier_id = t.tier_id
      LEFT JOIN tenants tn ON l.assigned_tenant_id = tn.id
      ORDER BY l.created_at DESC
    `)
    .all<any>();

  return c.json<ApiResponse>({ success: true, data: list.results || [] });
});

app.post('/api/dev/licenses/generate', async (c) => {
  const body = await c.req.json<any>();
  const tierId = body.tier_id || 'standard';

  // Format kode lisensi: ENT-[TIER]-[RANDOM4]
  const randomCode = Math.random().toString(36).substring(2, 6).toUpperCase();
  const licenseKey = `ENT-${tierId.toUpperCase()}-2026-${randomCode}`;

  await c.env.DB
    .prepare('INSERT INTO licenses (license_key, tier_id, buyer_note, custom_max_users, custom_max_storage_mb, status) VALUES (?, ?, ?, ?, ?, "unclaimed")')
    .bind(licenseKey, tierId, body.buyer_note || null, body.custom_max_users || null, body.custom_max_storage_mb || null)
    .run();

  return c.json<ApiResponse>({
    success: true,
    message: 'Lisensi baru berhasil dibuat.',
    data: { license_key: licenseKey, tier_id: tierId }
  });
});

app.put('/api/dev/licenses/:key/upgrade', async (c) => {
  const key = c.req.param('key');
  const body = await c.req.json<any>();

  await c.env.DB
    .prepare('UPDATE licenses SET tier_id = ?, custom_max_users = ?, custom_max_storage_mb = ? WHERE license_key = ?')
    .bind(body.tier_id, body.custom_max_users || null, body.custom_max_storage_mb || null, key)
    .run();

  return c.json<ApiResponse>({ success: true, message: `Lisensi '${key}' berhasil di-upgrade ke tier '${body.tier_id}' secara instan.` });
});

// Tenant Directory & Kill-Switch
app.get('/api/dev/tenants', async (c) => {
  const list = await c.env.DB
    .prepare(`
      SELECT tn.*, l.tier_id, tc.display_name as tier_name, tc.max_users, tc.max_storage_mb,
             (SELECT COUNT(*) FROM users u WHERE u.tenant_id = tn.id) as user_count,
             (SELECT COUNT(*) FROM documents d WHERE d.tenant_id = tn.id) as document_count
      FROM tenants tn
      JOIN licenses l ON tn.license_key = l.license_key
      JOIN tier_configs tc ON l.tier_id = tc.tier_id
      ORDER BY tn.created_at DESC
    `)
    .all<any>();

  return c.json<ApiResponse>({ success: true, data: list.results || [] });
});

app.put('/api/dev/tenants/:id/status', async (c) => {
  const tenantId = c.req.param('id');
  const body = await c.req.json<{ is_active: boolean }>();

  const activeVal = body.is_active ? 1 : 0;
  const statusStr = body.is_active ? 'active' : 'suspended';

  await c.env.DB.prepare('UPDATE tenants SET is_active = ? WHERE id = ?').bind(activeVal, tenantId).run();
  await c.env.DB.prepare('UPDATE licenses SET status = ? WHERE assigned_tenant_id = ?').bind(statusStr, tenantId).run();

  return c.json<ApiResponse>({
    success: true,
    message: body.is_active ? 'Instansi berhasil diaktifkan kembali.' : 'Instansi berhasil dibekukan (kill-switch diaktifkan).'
  });
});

// Password Rescue
app.post('/api/dev/users/reset-password', async (c) => {
  const body = await c.req.json<{ email_username: string; new_password: string }>();
  if (!body.email_username || !body.new_password) {
    return c.json<ApiResponse>({ success: false, error: 'Email dan password baru wajib diisi.' }, 400);
  }

  const hash = await hashPassword(body.new_password);
  const res = await c.env.DB
    .prepare('UPDATE users SET password_hash = ?, active_session_token = NULL WHERE email_username = ?')
    .bind(hash, body.email_username.trim().toLowerCase())
    .run();

  if (!res.meta.changes) {
    return c.json<ApiResponse>({ success: false, error: 'Pengguna dengan email tersebut tidak ditemukan.' }, 404);
  }

  return c.json<ApiResponse>({ success: true, message: `Password pengguna '${body.email_username}' berhasil direset.` });
});

// Broadcast Controller
app.get('/api/dev/broadcast', async (c) => {
  const b = await c.env.DB.prepare('SELECT * FROM system_broadcasts WHERE id = "sys-broadcast-main"').first<SystemBroadcast>();
  return c.json<ApiResponse>({ success: true, data: b });
});

app.put('/api/dev/broadcast', async (c) => {
  const body = await c.req.json<any>();

  await c.env.DB
    .prepare(`
      UPDATE system_broadcasts 
      SET running_text = COALESCE(?, running_text),
          running_text_speed = COALESCE(?, running_text_speed),
          running_text_bg_color = COALESCE(?, running_text_bg_color),
          popup_enabled = COALESCE(?, popup_enabled),
          popup_title = COALESCE(?, popup_title),
          popup_image_url = COALESCE(?, popup_image_url),
          popup_cta_text = COALESCE(?, popup_cta_text),
          popup_cta_link = COALESCE(?, popup_cta_link),
          target_tiers_json = COALESCE(?, target_tiers_json),
          app_latest_version = COALESCE(?, app_latest_version),
          maintenance_mode = COALESCE(?, maintenance_mode),
          support_channel_uri = COALESCE(?, support_channel_uri),
          updated_at = CURRENT_TIMESTAMP
      WHERE id = 'sys-broadcast-main'
    `)
    .bind(
      body.running_text || null, body.running_text_speed || null, body.running_text_bg_color || null,
      body.popup_enabled !== undefined ? (body.popup_enabled ? 1 : 0) : null,
      body.popup_title || null, body.popup_image_url || null, body.popup_cta_text || null,
      body.popup_cta_link || null, body.target_tiers_json || null, body.app_latest_version || null,
      body.maintenance_mode !== undefined ? (body.maintenance_mode ? 1 : 0) : null,
      body.support_channel_uri || null
    )
    .run();

  return c.json<ApiResponse>({ success: true, message: 'Iklan, siaran, dan nomor gateway dukungan berhasil diperbarui.' });
});

// ------------------------------------------------------------------------
// STEALTH SUPPORT & UPGRADE CONTACT GATEWAY (ZERO CLIENT PHONE VISIBILITY)
// ------------------------------------------------------------------------
app.get('/api/v1/sys-prov/contact-gateway', async (c) => {
  const intent = (c.req.query('intent') || 'support').toLowerCase();
  const tenantName = c.req.query('tenant_name') || 'Instansi';
  const tenantCode = c.req.query('tenant_code') || '-';
  const tierName = c.req.query('tier_name') || 'Standard';
  const customNote = c.req.query('note') || '';

  const broadcast = await c.env.DB
    .prepare('SELECT support_channel_uri FROM system_broadcasts WHERE id = "sys-broadcast-main"')
    .first<{ support_channel_uri?: string }>();

  let phone = broadcast?.support_channel_uri?.trim() || '628589584257';
  phone = phone.replace(/[^0-9]/g, '');
  if (phone.startsWith('0')) {
    phone = '62' + phone.substring(1);
  }

  let message = '';
  switch (intent) {
    case 'upgrade':
      message = `Halo Layanan Lisensi Pusat, kami dari ${tenantName} (Kode: ${tenantCode} | Paket Saat Ini: ${tierName}) bermaksud mengajukan peningkatan paket lisensi (Upgrade Entitlement).`;
      break;
    case 'purchase':
      message = `Halo Tim Layanan, instansi kami bermaksud melakukan pemesanan Kode Lisensi (Entitlement Key) baru.`;
      break;
    case 'bulletin':
      message = `Halo Layanan Pelanggan Pusat, kami ingin menanyakan informasi lebih lanjut terkait pengumuman/layanan sistem.`;
      break;
    case 'support':
    case 'reset':
    default:
      message = `Halo Layanan Teknis, kami memerlukan bantuan otorisasi akses untuk instansi ${tenantName} (Kode: ${tenantCode}).`;
      break;
  }

  if (customNote) {
    message += ` Rincian: ${customNote}`;
  }

  const waUrl = `https://wa.me/${phone}?text=${encodeURIComponent(message)}`;

  if (c.req.header('Accept')?.includes('application/json')) {
    return c.json<ApiResponse>({
      success: true,
      message: 'Support gateway target resolved',
      data: { redirect_url: waUrl, phone_target: phone }
    });
  }

  return c.redirect(waUrl, 302);
});

export default app;
