// types.ts - Full Data Models & Interfaces for Closed-Ecosystem SaaS Backend

export interface Env {
  DB: D1Database;
  CLOUDINARY_CLOUD_NAME: string;
  CLOUDINARY_API_KEY: string;
  CLOUDINARY_API_SECRET: string;
  DEVELOPER_MASTER_KEY?: string;
}

export interface TierConfig {
  tier_id: string; // 'trial', 'basic', 'standard', 'professional', 'enterprise'
  display_name: string;
  price_label: string;
  max_users: number;
  max_templates: number;
  max_storage_mb: number;
  allow_rab_module: number;
  allow_direct_scanner: number;
  allow_qr_validation: number;
  has_watermark: number;
  updated_at?: string;
}

export interface License {
  license_key: string;
  tier_id: string;
  buyer_note?: string | null;
  custom_max_users?: number | null;
  custom_max_storage_mb?: number | null;
  status: 'unclaimed' | 'active' | 'suspended';
  assigned_tenant_id?: string | null;
  created_at?: string;
  activated_at?: string | null;
}

export interface SystemBroadcast {
  id: string;
  running_text: string;
  running_text_speed: number;
  running_text_bg_color: string;
  popup_enabled: number;
  popup_title?: string | null;
  popup_image_url?: string | null;
  popup_cta_text?: string | null;
  popup_cta_link?: string | null;
  target_tiers_json: string;
  app_latest_version: string;
  maintenance_mode: number;
  support_channel_uri?: string | null;
  updated_at?: string;
}

export interface Tenant {
  id: string;
  license_key: string;
  institution_code: string;
  institution_name: string;
  institution_type: 'school' | 'village_gov' | 'corporate' | 'office_agency' | 'healthcare' | 'foundation';
  institution_address: string;
  phone_number: string;
  email: string;
  logo_url?: string | null;
  letterhead_config_json?: string | null;
  signature_url?: string | null;
  stamp_url?: string | null;
  default_numbering_formula: string;
  storage_used_bytes: number;
  is_active: number;
  created_at?: string;
}

export interface User {
  id: string;
  tenant_id: string;
  email_username: string;
  full_name: string;
  phone?: string | null;
  role: 'admin' | 'staff';
  active_session_token?: string | null;
  active_device_info?: string | null;
  is_active: number;
  last_login?: string | null;
  created_at?: string;
}

export interface UserSessionPayload {
  user_id: string;
  tenant_id: string;
  email_username: string;
  full_name: string;
  role: 'admin' | 'staff';
  institution_code: string;
  institution_name: string;
  institution_type: string;
  session_token: string;
  tier_id: string;
  iat: number;
  exp: number;
}

export interface CustomEntity {
  id: string;
  tenant_id: string;
  entity_slug: string;
  entity_display_name: string;
  is_system_default: number;
  attributes?: EntityAttribute[];
}

export interface EntityAttribute {
  id?: number;
  tenant_id: string;
  entity_id: string;
  attr_key: string;
  attr_label: string;
  data_type: 'text' | 'id_number' | 'date_tmt' | 'currency' | 'select';
  select_options_json?: string | null;
  auto_calculate_tenure: number;
  sort_order: number;
}

export interface EntityRecord {
  id: string;
  tenant_id: string;
  entity_id: string;
  primary_name: string;
  primary_id_number?: string | null;
  tmt_date?: string | null;
  contract_end_date?: string | null;
  values_json: string;
  created_at?: string;
}

export interface GlobalPresetTemplate {
  id: string;
  target_institution_type: string;
  category_name: string;
  description?: string | null;
  numbering_formula: string;
  rich_content_json: string;
  has_rab_grid: number;
  rab_preset_json?: string | null;
  form_fields_json: string;
}

export interface Category {
  id: string;
  tenant_id: string;
  name: string;
  classification_code?: string | null;
  description?: string | null;
  numbering_formula: string;
  rich_content_json: string;
  has_rab_grid: number;
  rab_template_json?: string | null;
  created_by?: string | null;
  created_at?: string;
  fields?: FormField[];
}

export interface FormField {
  id?: number;
  tenant_id: string;
  category_id: string;
  field_key: string;
  label: string;
  field_type: 'text' | 'multiline' | 'date' | 'select' | 'number' | 'currency' | 'entity_lookup';
  linked_entity_id?: string | null;
  options_json?: string | null;
  is_required: number;
  sort_order: number;
}

export interface DocumentRecord {
  id: string;
  tenant_id: string;
  category_id: string;
  category_name?: string;
  doc_sequence_number: number;
  doc_number_full: string;
  title_or_subject: string;
  recipient?: string | null;
  metadata_json: string;
  rendered_content_json: string;
  rab_data_json?: string | null;
  rab_total_amount: number;
  locked_archive_url?: string | null;
  scan_url?: string | null;
  approval_status: 'draft' | 'waiting_chief_approval' | 'revision_needed' | 'approved';
  approval_note?: string | null;
  archive_status: 'draft' | 'completed_digital' | 'verified_scan' | 'pending_deletion';
  deletion_requested_by_id?: string | null;
  deletion_requested_by_name?: string | null;
  deletion_reason?: string | null;
  deletion_requested_at?: string | null;
  created_by_user_id: string;
  created_by_name: string;
  created_at?: string;
  updated_at?: string;
}

export interface IncomingMail {
  id: string;
  tenant_id: string;
  agenda_number: string;
  sender_origin: string;
  original_mail_number: string;
  mail_date: string;
  received_date: string;
  subject: string;
  scan_url?: string | null;
  disposition_to?: string | null;
  disposition_instruction?: string | null;
  urgency_level: 'Biasa' | 'Penting' | 'Segera/Rahasia';
  created_by_name: string;
  created_at?: string;
}

export interface ApiResponse<T = any> {
  success: boolean;
  message?: string;
  data?: T;
  error?: string;
}

// Request Models
export interface LoginRequest {
  institution_code: string;
  email_username: string;
  password: string;
  device_info?: string;
}

export interface RegisterTenantRequest {
  license_key: string;
  admin_full_name: string;
  institution_name: string;
  institution_address: string;
  institution_type: 'school' | 'village_gov' | 'corporate' | 'office_agency' | 'healthcare' | 'foundation';
  phone_number: string;
  email: string;
  password: string;
  logo_url?: string;
  device_info?: string;
}

export interface SaveDocumentRequest {
  category_id: string;
  doc_sequence_number?: number;
  doc_number_full: string;
  title_or_subject: string;
  recipient?: string;
  metadata_json: Record<string, any> | string;
  rendered_content_json: string;
  rab_data_json?: string | null;
  rab_total_amount?: number;
  locked_archive_url?: string;
  is_draft?: boolean;
}
