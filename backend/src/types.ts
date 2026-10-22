// types.ts - Tipe Data & Interface untuk Backend Cloudflare Workers D1

export interface Env {
  DB: D1Database;
  CLOUDINARY_CLOUD_NAME: string;
  CLOUDINARY_API_KEY: string;
  CLOUDINARY_API_SECRET: string;
  APP_PIN?: string; // PIN Pengamanan API (default: '123456')
}

export interface Category {
  id: string;
  name: string;
  code: string;
  description?: string;
  template_cloudinary_url: string;
  created_at?: string;
  updated_at?: string;
}

export interface FormField {
  id: string;
  category_id: string;
  field_key: string;
  label: string;
  field_type: 'text' | 'textarea' | 'date' | 'number' | 'select';
  options?: string | null;
  default_value?: string | null;
  placeholder?: string | null;
  is_required: number;
  display_order: number;
  created_at?: string;
}

export interface CategoryWithFields extends Category {
  fields: FormField[];
}

export interface DocumentRecord {
  id: string;
  category_id: string;
  category_name?: string;
  category_code?: string;
  document_number: string;
  title: string;
  form_data: string;
  docx_cloudinary_url: string;
  docx_public_id: string;
  scan_cloudinary_url?: string | null;
  scan_public_id?: string | null;
  scan_uploaded_at?: string | null;
  verification_token?: string | null;
  status?: string;
  created_by?: string;
  created_at?: string;
  updated_at?: string;
}

export interface CreateDocumentRequest {
  category_id: string;
  document_number: string;
  title: string;
  form_data: Record<string, any>;
  docx_cloudinary_url: string;
  docx_public_id: string;
  verification_token?: string;
  created_by?: string;
}

export interface NextNumberRequest {
  category_id: string;
  year?: number;
  actor_name?: string;
}

export interface AppVersionRecord {
  id: string;
  version: string;
  version_code: number;
  download_url: string;
  file_size_bytes?: number;
  release_notes?: string;
  is_mandatory: number;
  released_at?: string;
}

export interface UploadSignatureRequest {
  folder: 'archives/docx' | 'archives/scans' | 'templates' | string;
  tags?: string;
  resource_type?: 'auto' | 'raw' | 'image';
}

export interface LinkScanRequest {
  document_id: string;
  scan_cloudinary_url: string;
  scan_public_id: string;
  actor_name?: string;
}

export interface ApiResponse<T = any> {
  success: boolean;
  message?: string;
  data?: T;
  error?: string;
}
