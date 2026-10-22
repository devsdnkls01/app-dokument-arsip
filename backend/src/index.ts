// index.ts - Hono Serverless API on Cloudflare Workers
import { Hono } from 'hono';
import { cors } from 'hono/cors';
import { 
  Env, 
  Category, 
  FormField, 
  CategoryWithFields, 
  CreateDocumentRequest, 
  UploadSignatureRequest, 
  LinkScanRequest,
  NextNumberRequest,
  AppVersionRecord,
  ApiResponse,
  DocumentRecord
} from './types';

const app = new Hono<{ Bindings: Env }>();

// Enable CORS for Desktop Client & Web Tools
app.use('*', cors({
  origin: '*',
  allowMethods: ['GET', 'POST', 'PUT', 'DELETE', 'OPTIONS'],
  allowHeaders: ['Content-Type', 'Authorization', 'X-Requested-With', 'X-App-Pin'],
}));

// Helper: Convert month number (1-12) to Roman Numerals (I - XII)
function getRomanMonth(monthIndex: number): string {
  const romans = ['I', 'II', 'III', 'IV', 'V', 'VI', 'VII', 'VIII', 'IX', 'X', 'XI', 'XII'];
  return romans[Math.max(0, Math.min(11, monthIndex))] || 'I';
}

// Helper: Generate SHA-1 Hex via Web Crypto API (Native in Cloudflare Workers)
async function generateSha1Hex(text: string): Promise<string> {
  const encoder = new TextEncoder();
  const data = encoder.encode(text);
  const hashBuffer = await crypto.subtle.digest('SHA-1', data);
  const hashArray = Array.from(new Uint8Array(hashBuffer));
  return hashArray.map(b => b.toString(16).padStart(2, '0')).join('');
}

// ----------------------------------------------------
// Middleware: Proteksi PIN Keamanan untuk Endpoint Sensitif
// ----------------------------------------------------
app.use('/api/*', async (c, next) => {
  // Hanya proteksi operasi POST/PUT/DELETE
  if (['POST', 'PUT', 'DELETE'].includes(c.req.method)) {
    const requiredPin = c.env.APP_PIN || '123456';
    const clientPin = c.req.header('X-App-Pin') || c.req.query('pin');

    if (clientPin !== requiredPin) {
      return c.json<ApiResponse>({
        success: false,
        error: 'Akses ditolak: PIN Keamanan API tidak valid. Harap masukkan PIN yang benar pada aplikasi.'
      }, 401);
    }
  }
  await next();
});

// ----------------------------------------------------
// 0. Health Check & Root Info
// ----------------------------------------------------
app.get('/', (c) => {
  return c.json<ApiResponse>({
    success: true,
    message: 'DocuFlow Engine API (Cloudflare Workers + D1) is running',
    data: {
      timestamp: new Date().toISOString(),
      version: '1.1.0'
    }
  });
});

app.get('/api/health', (c) => {
  return c.json<ApiResponse>({
    success: true,
    message: 'Healthy'
  });
});

// ----------------------------------------------------
// 1. GET /api/app/latest-version
// Cek versi terbaru aplikasi desktop (Distribusi via D1 & Cloudinary/Drive)
// ----------------------------------------------------
app.get('/api/app/latest-version', async (c) => {
  try {
    const latest = await c.env.DB
      .prepare('SELECT * FROM app_versions ORDER BY version_code DESC LIMIT 1')
      .first<AppVersionRecord>();

    return c.json<ApiResponse<AppVersionRecord>>({
      success: true,
      data: latest || undefined
    });
  } catch (error: any) {
    return c.json<ApiResponse>({
      success: false,
      error: error?.message || 'Gagal memeriksa versi aplikasi'
    }, 500);
  }
});

// ----------------------------------------------------
// 2. GET /api/categories
// Mengambil seluruh kategori surat beserta definisi field dinamisnya
// ----------------------------------------------------
app.get('/api/categories', async (c) => {
  try {
    const db = c.env.DB;

    const { results: categories } = await db
      .prepare('SELECT * FROM categories ORDER BY name ASC')
      .all<Category>();

    if (!categories || categories.length === 0) {
      return c.json<ApiResponse<CategoryWithFields[]>>({
        success: true,
        data: [],
        message: 'Belum ada kategori yang terdaftar'
      });
    }

    const { results: fields } = await db
      .prepare('SELECT * FROM form_fields ORDER BY display_order ASC')
      .all<FormField>();

    const categoriesWithFields: CategoryWithFields[] = categories.map((cat) => ({
      ...cat,
      fields: fields.filter((f) => f.category_id === cat.id)
    }));

    return c.json<ApiResponse<CategoryWithFields[]>>({
      success: true,
      data: categoriesWithFields
    });
  } catch (error: any) {
    return c.json<ApiResponse>({
      success: false,
      error: error?.message || 'Gagal memuat kategori dari D1'
    }, 500);
  }
});

// ----------------------------------------------------
// 3. POST /api/documents/next-number
// Penomoran Surat Otomatis & Atomic (Anti-Duplikat Antar Staf)
// ----------------------------------------------------
app.post('/api/documents/next-number', async (c) => {
  try {
    const body = await c.req.json<NextNumberRequest>();
    if (!body.category_id) {
      return c.json<ApiResponse>({
        success: false,
        error: 'category_id wajib disertakan'
      }, 400);
    }

    const currentYear = body.year || new Date().getFullYear();
    const currentMonthRom = getRomanMonth(new Date().getMonth());

    // Ambil kode kategori
    const category = await c.env.DB
      .prepare('SELECT code FROM categories WHERE id = ?')
      .bind(body.category_id)
      .first<{ code: string }>();

    const categoryCode = category?.code || 'SRT';

    // Pastikan row sequence ada di database
    await c.env.DB
      .prepare(`
        INSERT OR IGNORE INTO number_sequences (category_id, year, current_number, prefix_format)
        VALUES (?, ?, 0, '{num:3}/{code}/TU/{month_rom}/{year}')
      `)
      .bind(body.category_id, currentYear)
      .run();

    // Increment secara atomic
    await c.env.DB
      .prepare(`
        UPDATE number_sequences 
        SET current_number = current_number + 1, updated_at = CURRENT_TIMESTAMP
        WHERE category_id = ? AND year = ?
      `)
      .bind(body.category_id, currentYear)
      .run();

    // Ambil nomor yang baru di-increment
    const seq = await c.env.DB
      .prepare('SELECT current_number, prefix_format FROM number_sequences WHERE category_id = ? AND year = ?')
      .bind(body.category_id, currentYear)
      .first<{ current_number: number; prefix_format: string }>();

    const numValue = seq?.current_number || 1;
    const format = seq?.prefix_format || '{num:3}/{code}/TU/{month_rom}/{year}';

    // Format penomoran: {num:3} -> '001', {code} -> 'SPT', {month_rom} -> 'X', {year} -> '2026'
    const formattedNumber = format
      .replace('{num:3}', String(numValue).padStart(3, '0'))
      .replace('{num}', String(numValue))
      .replace('{code}', categoryCode)
      .replace('{month_rom}', currentMonthRom)
      .replace('{year}', String(currentYear));

    // Catat ke audit log
    await c.env.DB
      .prepare(`
        INSERT INTO audit_logs (id, action, actor_name, details, ip_address)
        VALUES (?, 'GENERATE_NUMBER', ?, ?, ?)
      `)
      .bind(
        `log-${Date.now()}`,
        body.actor_name || 'desktop-client',
        JSON.stringify({ number: formattedNumber, category_id: body.category_id }),
        c.req.header('CF-Connecting-IP') || '127.0.0.1'
      )
      .run();

    return c.json<ApiResponse>({
      success: true,
      data: {
        number: formattedNumber,
        raw_number: numValue,
        category_code: categoryCode,
        year: currentYear
      }
    });
  } catch (error: any) {
    return c.json<ApiResponse>({
      success: false,
      error: error?.message || 'Gagal menghasilkan nomor surat otomatis'
    }, 500);
  }
});

// ----------------------------------------------------
// 4. POST /api/upload-signature-url
// Signed upload Cloudinary (HMAC SHA-1) dengan kompresi otomatis
// ----------------------------------------------------
app.post('/api/upload-signature-url', async (c) => {
  try {
    const body = await c.req.json<UploadSignatureRequest>().catch(() => ({} as UploadSignatureRequest));
    const folder = body.folder || 'archives/docx';
    const timestamp = Math.round(new Date().getTime() / 1000);

    const cloudName = c.env.CLOUDINARY_CLOUD_NAME || 'demo';
    const apiKey = c.env.CLOUDINARY_API_KEY || '';
    const apiSecret = c.env.CLOUDINARY_API_SECRET || '';

    if (!apiKey || !apiSecret) {
      return c.json<ApiResponse>({
        success: false,
        error: 'CLOUDINARY_API_KEY atau CLOUDINARY_API_SECRET belum dikonfigurasi di environment backend.'
      }, 500);
    }

    const paramsToSign: Record<string, string | number> = {
      folder,
      timestamp
    };

    if (body.tags) {
      paramsToSign.tags = body.tags;
    }

    const sortedKeys = Object.keys(paramsToSign).sort();
    const stringToSign = sortedKeys
      .map((key) => `${key}=${paramsToSign[key]}`)
      .join('&') + apiSecret;

    const signature = await generateSha1Hex(stringToSign);

    return c.json<ApiResponse>({
      success: true,
      data: {
        cloudName,
        apiKey,
        timestamp,
        folder,
        signature,
        uploadUrl: `https://api.cloudinary.com/v1_1/${cloudName}/auto/upload`
      }
    });
  } catch (error: any) {
    return c.json<ApiResponse>({
      success: false,
      error: error?.message || 'Gagal membuat signature Cloudinary'
    }, 500);
  }
});

// ----------------------------------------------------
// 5. POST /api/documents
// Mencatat riwayat surat hasil generate ke D1
// ----------------------------------------------------
app.post('/api/documents', async (c) => {
  try {
    const body = await c.req.json<CreateDocumentRequest>();

    if (!body.category_id || !body.document_number || !body.docx_cloudinary_url) {
      return c.json<ApiResponse>({
        success: false,
        error: 'Parameter category_id, document_number, dan docx_cloudinary_url wajib diisi.'
      }, 400);
    }

    const docId = `doc-${Date.now()}-${Math.random().toString(36).substring(2, 7)}`;
    const formDataJson = typeof body.form_data === 'string' ? body.form_data : JSON.stringify(body.form_data || {});
    const title = body.title || `Dokumen ${body.document_number}`;
    const createdBy = body.created_by || 'desktop-client';
    const verificationToken = body.verification_token || `ver-${Math.random().toString(36).substring(2, 10)}`;

    await c.env.DB
      .prepare(`
        INSERT INTO documents (
          id, category_id, document_number, title, form_data, 
          docx_cloudinary_url, docx_public_id, verification_token, status, created_by
        ) VALUES (?, ?, ?, ?, ?, ?, ?, ?, 'ISSUED_WAITING_SCAN', ?)
      `)
      .bind(
        docId,
        body.category_id,
        body.document_number,
        title,
        formDataJson,
        body.docx_cloudinary_url,
        body.docx_public_id || '',
        verificationToken,
        createdBy
      )
      .run();

    // Catat ke audit log
    await c.env.DB
      .prepare(`
        INSERT INTO audit_logs (id, document_id, action, actor_name, details, ip_address)
        VALUES (?, ?, 'GENERATE_DOCX', ?, ?, ?)
      `)
      .bind(
        `log-${Date.now()}`,
        docId,
        createdBy,
        JSON.stringify({ docNumber: body.document_number, url: body.docx_cloudinary_url }),
        c.req.header('CF-Connecting-IP') || '127.0.0.1'
      )
      .run();

    const createdDoc = await c.env.DB
      .prepare('SELECT * FROM documents WHERE id = ?')
      .bind(docId)
      .first<DocumentRecord>();

    return c.json<ApiResponse<DocumentRecord>>({
      success: true,
      message: 'Dokumen berhasil dicatat ke database arsip',
      data: createdDoc || undefined
    }, 201);
  } catch (error: any) {
    return c.json<ApiResponse>({
      success: false,
      error: error?.message || 'Gagal menyimpan dokumen ke D1'
    }, 500);
  }
});

// ----------------------------------------------------
// 6. POST /api/documents/link-scan
// Menautkan berkas fisik hasil scan basah (PDF/JPG) ke dokumen
// ----------------------------------------------------
app.post('/api/documents/link-scan', async (c) => {
  try {
    const body = await c.req.json<LinkScanRequest>();

    if (!body.document_id || !body.scan_cloudinary_url) {
      return c.json<ApiResponse>({
        success: false,
        error: 'document_id dan scan_cloudinary_url wajib disediakan'
      }, 400);
    }

    const nowIso = new Date().toISOString();
    const actor = body.actor_name || 'desktop-client';

    const result = await c.env.DB
      .prepare(`
        UPDATE documents 
        SET scan_cloudinary_url = ?, 
            scan_public_id = ?, 
            scan_uploaded_at = ?,
            status = 'COMPLETED',
            updated_at = CURRENT_TIMESTAMP
        WHERE id = ?
      `)
      .bind(
        body.scan_cloudinary_url,
        body.scan_public_id || '',
        nowIso,
        body.document_id
      )
      .run();

    if (result.meta?.changes === 0) {
      return c.json<ApiResponse>({
        success: false,
        error: `Dokumen dengan ID ${body.document_id} tidak ditemukan`
      }, 404);
    }

    // Catat ke audit log
    await c.env.DB
      .prepare(`
        INSERT INTO audit_logs (id, document_id, action, actor_name, details, ip_address)
        VALUES (?, ?, 'LINK_SCAN', ?, ?, ?)
      `)
      .bind(
        `log-${Date.now()}`,
        body.document_id,
        actor,
        JSON.stringify({ scanUrl: body.scan_cloudinary_url }),
        c.req.header('CF-Connecting-IP') || '127.0.0.1'
      )
      .run();

    const updatedDoc = await c.env.DB
      .prepare('SELECT * FROM documents WHERE id = ?')
      .bind(body.document_id)
      .first<DocumentRecord>();

    return c.json<ApiResponse<DocumentRecord>>({
      success: true,
      message: 'Berkas scan stempel basah berhasil ditautkan ke arsip dokumen.',
      data: updatedDoc || undefined
    });
  } catch (error: any) {
    return c.json<ApiResponse>({
      success: false,
      error: error?.message || 'Gagal menautkan berkas scan'
    }, 500);
  }
});

// ----------------------------------------------------
// 7. GET /api/documents (Dengan Filter & Search)
// ----------------------------------------------------
app.get('/api/documents', async (c) => {
  try {
    const q = c.req.query('q')?.trim();

    let query = `
      SELECT d.*, c.name as category_name, c.code as category_code
      FROM documents d
      LEFT JOIN categories c ON d.category_id = c.id
    `;

    if (q) {
      query += ` WHERE d.document_number LIKE '%${q}%' OR d.title LIKE '%${q}%' `;
    }

    query += ` ORDER BY d.created_at DESC LIMIT 100 `;

    const { results } = await c.env.DB.prepare(query).all<DocumentRecord>();

    return c.json<ApiResponse<DocumentRecord[]>>({
      success: true,
      data: results || []
    });
  } catch (error: any) {
    return c.json<ApiResponse>({
      success: false,
      error: error?.message || 'Gagal mengambil riwayat dokumen'
    }, 500);
  }
});

// ----------------------------------------------------
// 8. GET /api/documents/agenda
// Ekspor Data Buku Agenda Surat Keluar
// ----------------------------------------------------
app.get('/api/documents/agenda', async (c) => {
  try {
    const { results } = await c.env.DB
      .prepare(`
        SELECT 
          d.document_number,
          d.title,
          c.name as category_name,
          d.form_data,
          d.created_at as tanggal_buat,
          d.docx_cloudinary_url,
          d.scan_cloudinary_url,
          d.status,
          d.created_by as operator
        FROM documents d
        LEFT JOIN categories c ON d.category_id = c.id
        ORDER BY d.created_at DESC
      `)
      .all<any>();

    return c.json<ApiResponse>({
      success: true,
      data: results || []
    });
  } catch (error: any) {
    return c.json<ApiResponse>({
      success: false,
      error: error?.message || 'Gagal mengekspor data agenda'
    }, 500);
  }
});

// ----------------------------------------------------
// 9. GET /verify/:docId
// Halaman Publik Validasi Keaslian Surat (Dibuka saat QR Code dipindai HP)
// ----------------------------------------------------
app.get('/verify/:docId', async (c) => {
  const docId = c.req.param('docId');

  const doc = await c.env.DB
    .prepare(`
      SELECT d.*, c.name as category_name
      FROM documents d
      LEFT JOIN categories c ON d.category_id = c.id
      WHERE d.id = ? OR d.document_number = ?
    `)
    .bind(docId, docId)
    .first<DocumentRecord>();

  if (!doc) {
    return c.html(`
      <!DOCTYPE html>
      <html lang="id">
      <head>
        <meta charset="utf-8">
        <meta name="viewport" content="width=device-width, initial-scale=1">
        <title>Verifikasi Surat Tidak Ditemukan</title>
        <style>
          body { font-family: system-ui, -apple-system, sans-serif; background: #f8fafc; display: flex; align-items: center; justify-content: center; min-height: 100vh; margin: 0; padding: 20px; box-sizing: border-box; }
          .card { background: white; border-radius: 12px; padding: 32px; max-width: 460px; width: 100%; box-shadow: 0 10px 25px rgba(0,0,0,0.06); text-align: center; }
          .icon { font-size: 48px; margin-bottom: 16px; }
          h2 { color: #dc2626; margin: 0 0 8px 0; }
          p { color: #64748b; font-size: 14px; line-height: 1.5; }
        </style>
      </head>
      <body>
        <div class="card">
          <div class="icon">⚠️</div>
          <h2>Dokumen Tidak Ditemukan</h2>
          <p>Tanda bukti digital dengan nomor/ID <strong>${docId}</strong> tidak terdaftar dalam basis data arsip resmi.</p>
        </div>
      </body>
      </html>
    `, 404);
  }

  const hasScan = !!doc.scan_cloudinary_url;

  return c.html(`
    <!DOCTYPE html>
    <html lang="id">
    <head>
      <meta charset="utf-8">
      <meta name="viewport" content="width=device-width, initial-scale=1">
      <title>Verifikasi Keaslian Surat - ${doc.document_number}</title>
      <style>
        body { font-family: system-ui, -apple-system, sans-serif; background: #f1f5f9; display: flex; align-items: center; justify-content: center; min-height: 100vh; margin: 0; padding: 20px; box-sizing: border-box; }
        .card { background: #ffffff; border-radius: 16px; max-width: 520px; width: 100%; box-shadow: 0 20px 30px rgba(0,0,0,0.07); overflow: hidden; border: 1px solid #e2e8f0; }
        .header { background: #059669; color: white; padding: 24px; text-align: center; }
        .header .badge { display: inline-block; background: rgba(255,255,255,0.2); padding: 4px 12px; border-radius: 20px; font-size: 12px; font-weight: 600; margin-bottom: 8px; }
        .header h1 { font-size: 20px; margin: 0; font-weight: 700; }
        .content { padding: 24px; }
        .row { display: flex; justify-content: space-between; padding: 10px 0; border-bottom: 1px dashed #e2e8f0; font-size: 13px; }
        .row:last-child { border-bottom: none; }
        .label { color: #64748b; font-weight: 500; }
        .value { color: #0f172a; font-weight: 600; text-align: right; max-width: 60%; }
        .status-box { background: ${hasScan ? '#dcfce7' : '#fef3c7'}; border: 1px solid ${hasScan ? '#86efac' : '#fde68a'}; border-radius: 8px; padding: 12px; margin-top: 20px; text-align: center; }
        .status-box .title { font-weight: 700; font-size: 13px; color: ${hasScan ? '#166534' : '#92400e'}; }
        .btn-view { display: block; background: #2563eb; color: white; text-align: center; padding: 12px; border-radius: 8px; text-decoration: none; font-weight: 600; font-size: 13px; margin-top: 16px; }
        .footer { background: #f8fafc; padding: 12px; text-align: center; font-size: 11px; color: #94a3b8; border-top: 1px solid #e2e8f0; }
      </style>
    </head>
    <body>
      <div class="card">
        <div class="header">
          <div class="badge">✓ RESMI TERVALIDASI</div>
          <h1>Sistem Verifikasi Dokumen</h1>
        </div>
        <div class="content">
          <div class="row">
            <span class="label">Nomor Dokumen:</span>
            <span class="value">${doc.document_number}</span>
          </div>
          <div class="row">
            <span class="label">Kategori Surat:</span>
            <span class="value">${doc.category_name || '-'}</span>
          </div>
          <div class="row">
            <span class="label">Perihal / Judul:</span>
            <span class="value">${doc.title}</span>
          </div>
          <div class="row">
            <span class="label">Diterbitkan Pada:</span>
            <span class="value">${doc.created_at || '-'}</span>
          </div>
          <div class="row">
            <span class="label">ID Verifikasi:</span>
            <span class="value" style="font-family: monospace;">${doc.id}</span>
          </div>

          <div class="status-box">
            <div class="title">${hasScan ? '✓ Berkas Fisik Berstempel Basah Telah Terarsip' : '⏳ Menunggu Bukti Berkas Fisik Basah'}</div>
          </div>

          ${hasScan ? `<a href="${doc.scan_cloudinary_url}" target="_blank" class="btn-view">📄 Buka Bukti Scan Stempel Basah Resmi</a>` : ''}
        </div>
        <div class="footer">
          DocuFlow Verification Engine • Terenkripsi Cloudflare Edge D1
        </div>
      </div>
    </body>
    </html>
  `);
});

export default app;
