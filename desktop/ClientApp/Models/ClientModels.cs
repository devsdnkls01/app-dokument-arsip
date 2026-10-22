using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ClientApp.Models
{
    public class ApiResponse<T>
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("message")]
        public string? Message { get; set; }

        [JsonPropertyName("data")]
        public T? Data { get; set; }

        [JsonPropertyName("error")]
        public string? Error { get; set; }
    }

    public class ApiResponse : ApiResponse<object>
    {
    }

    public class TierModel
    {
        [JsonPropertyName("tier_id")]
        public string TierId { get; set; } = "trial";

        [JsonPropertyName("display_name")]
        public string DisplayName { get; set; } = string.Empty;

        [JsonPropertyName("price_label")]
        public string PriceLabel { get; set; } = string.Empty;

        [JsonPropertyName("max_users")]
        public int MaxUsers { get; set; } = 3;

        [JsonPropertyName("max_templates")]
        public int MaxTemplates { get; set; } = 5;

        [JsonPropertyName("max_storage_mb")]
        public int MaxStorageMb { get; set; } = 250;

        [JsonPropertyName("allow_rab_module")]
        public int AllowRabModule { get; set; } = 1;

        [JsonPropertyName("allow_direct_scanner")]
        public int AllowDirectScanner { get; set; } = 1;

        [JsonPropertyName("allow_qr_validation")]
        public int AllowQrValidation { get; set; } = 1;

        [JsonPropertyName("has_watermark")]
        public int HasWatermark { get; set; } = 0;

        [JsonIgnore]
        public bool IsTrial => TierId.Equals("trial", StringComparison.OrdinalIgnoreCase);
    }

    public class TenantModel
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("license_key")]
        public string LicenseKey { get; set; } = string.Empty;

        [JsonPropertyName("institution_code")]
        public string InstitutionCode { get; set; } = string.Empty;

        [JsonPropertyName("institution_name")]
        public string InstitutionName { get; set; } = string.Empty;

        [JsonPropertyName("institution_type")]
        public string InstitutionType { get; set; } = "school"; // 'school', 'village_gov', 'corporate', 'office_agency', etc.

        [JsonPropertyName("institution_address")]
        public string InstitutionAddress { get; set; } = string.Empty;

        [JsonPropertyName("phone_number")]
        public string PhoneNumber { get; set; } = string.Empty;

        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;

        [JsonPropertyName("logo_url")]
        public string? LogoUrl { get; set; }

        [JsonPropertyName("letterhead_config_json")]
        public string? LetterheadConfigJson { get; set; }

        [JsonPropertyName("signature_url")]
        public string? SignatureUrl { get; set; }

        [JsonPropertyName("stamp_url")]
        public string? StampUrl { get; set; }

        [JsonPropertyName("default_numbering_formula")]
        public string DefaultNumberingFormula { get; set; } = string.Empty;

        [JsonPropertyName("storage_used_bytes")]
        public long StorageUsedBytes { get; set; } = 0;

        [JsonPropertyName("is_active")]
        public int IsActive { get; set; } = 1;

        [JsonIgnore]
        public double StorageUsedMb => Math.Round(StorageUsedBytes / (1024.0 * 1024.0), 2);
    }

    public class UserModel
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("tenant_id")]
        public string TenantId { get; set; } = string.Empty;

        [JsonPropertyName("email_username")]
        public string EmailUsername { get; set; } = string.Empty;

        [JsonPropertyName("full_name")]
        public string FullName { get; set; } = string.Empty;

        [JsonPropertyName("phone")]
        public string? Phone { get; set; }

        [JsonPropertyName("role")]
        public string Role { get; set; } = "staff"; // 'admin' or 'staff'

        [JsonPropertyName("is_active")]
        public int IsActive { get; set; } = 1;

        [JsonPropertyName("last_login")]
        public string? LastLogin { get; set; }

        [JsonIgnore]
        public bool IsAdmin => string.Equals(Role, "admin", StringComparison.OrdinalIgnoreCase);

        [JsonIgnore]
        public string RoleBadgeText => IsAdmin ? "ADMIN INSTANSI" : "STAF / OPERATOR";

        [JsonIgnore]
        public string RoleBadgeBg => IsAdmin ? "#1E293B" : "#F1F5F9";

        [JsonIgnore]
        public string RoleBadgeFg => IsAdmin ? "#F8FAFC" : "#334155";
    }

    public class ClientSession
    {
        [JsonPropertyName("token")]
        public string Token { get; set; } = string.Empty;

        [JsonPropertyName("user")]
        public UserModel User { get; set; } = new();

        [JsonPropertyName("tenant")]
        public TenantModel Tenant { get; set; } = new();

        [JsonPropertyName("tier")]
        public TierModel Tier { get; set; } = new();

        [JsonPropertyName("saved_at")]
        public DateTime SavedAt { get; set; } = DateTime.UtcNow;
    }

    public class CustomEntityModel
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("tenant_id")]
        public string TenantId { get; set; } = string.Empty;

        [JsonPropertyName("entity_slug")]
        public string EntitySlug { get; set; } = string.Empty;

        [JsonPropertyName("entity_display_name")]
        public string EntityDisplayName { get; set; } = string.Empty;

        [JsonPropertyName("is_system_default")]
        public int IsSystemDefault { get; set; } = 1;

        [JsonPropertyName("attributes")]
        public List<EntityAttributeModel> Attributes { get; set; } = new();

        public override string ToString() => EntityDisplayName;
    }

    public class EntityAttributeModel
    {
        [JsonPropertyName("id")]
        public int? Id { get; set; }

        [JsonPropertyName("tenant_id")]
        public string TenantId { get; set; } = string.Empty;

        [JsonPropertyName("entity_id")]
        public string EntityId { get; set; } = string.Empty;

        [JsonPropertyName("attr_key")]
        public string AttrKey { get; set; } = string.Empty;

        [JsonPropertyName("attr_label")]
        public string AttrLabel { get; set; } = string.Empty;

        [JsonPropertyName("data_type")]
        public string DataType { get; set; } = "text"; // 'text', 'id_number', 'date_tmt', 'currency', 'select'

        [JsonPropertyName("select_options_json")]
        public string? SelectOptionsJson { get; set; }

        [JsonPropertyName("auto_calculate_tenure")]
        public int AutoCalculateTenure { get; set; } = 0;

        [JsonPropertyName("sort_order")]
        public int SortOrder { get; set; } = 0;
    }

    public class EntityRecordModel
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("tenant_id")]
        public string TenantId { get; set; } = string.Empty;

        [JsonPropertyName("entity_id")]
        public string EntityId { get; set; } = string.Empty;

        [JsonPropertyName("primary_name")]
        public string PrimaryName { get; set; } = string.Empty;

        [JsonPropertyName("primary_id_number")]
        public string? PrimaryIdNumber { get; set; }

        [JsonPropertyName("tmt_date")]
        public string? TmtDate { get; set; }

        [JsonPropertyName("contract_end_date")]
        public string? ContractEndDate { get; set; }

        [JsonPropertyName("values_json")]
        public string ValuesJson { get; set; } = "{}";

        [JsonPropertyName("tenure_display")]
        public string? TenureDisplay { get; set; }

        [JsonIgnore]
        public string? CalculatedTenure => TenureDisplay;

        [JsonPropertyName("values")]
        public Dictionary<string, object>? Values { get; set; }

        public override string ToString() => $"{PrimaryName} - {PrimaryIdNumber}";
    }

    public class CategoryModel
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("tenant_id")]
        public string TenantId { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("classification_code")]
        public string? ClassificationCode { get; set; } = "100";

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("numbering_formula")]
        public string NumberingFormula { get; set; } = string.Empty;

        [JsonPropertyName("rich_content_json")]
        public string RichContentJson { get; set; } = "{}";

        [JsonPropertyName("has_rab_grid")]
        public int HasRabGrid { get; set; } = 0;

        [JsonPropertyName("rab_template_json")]
        public string? RabTemplateJson { get; set; }

        [JsonPropertyName("fields")]
        public List<FormFieldModel> Fields { get; set; } = new();

        [JsonIgnore]
        public bool HasRabBool => HasRabGrid == 1;

        public override string ToString() => Name;
    }

    public class FormFieldModel
    {
        [JsonPropertyName("id")]
        public int? Id { get; set; }

        [JsonPropertyName("tenant_id")]
        public string TenantId { get; set; } = string.Empty;

        [JsonPropertyName("category_id")]
        public string CategoryId { get; set; } = string.Empty;

        [JsonPropertyName("field_key")]
        public string FieldKey { get; set; } = string.Empty;

        [JsonPropertyName("label")]
        public string Label { get; set; } = string.Empty;

        [JsonPropertyName("field_type")]
        public string FieldType { get; set; } = "text"; // 'text', 'multiline', 'date', 'select', 'number', 'currency', 'entity_lookup'

        [JsonPropertyName("linked_entity_id")]
        public string? LinkedEntityId { get; set; }

        [JsonPropertyName("options_json")]
        public string? OptionsJson { get; set; }

        [JsonPropertyName("is_required")]
        public int IsRequired { get; set; } = 1;

        [JsonPropertyName("sort_order")]
        public int SortOrder { get; set; } = 0;

        [JsonIgnore]
        public bool IsRequiredBool => IsRequired == 1;
    }

    public class DocumentModel
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("tenant_id")]
        public string TenantId { get; set; } = string.Empty;

        [JsonPropertyName("category_id")]
        public string CategoryId { get; set; } = string.Empty;

        [JsonPropertyName("category_name")]
        public string? CategoryName { get; set; }

        [JsonPropertyName("doc_sequence_number")]
        public int DocSequenceNumber { get; set; }

        [JsonPropertyName("doc_number_full")]
        public string DocNumberFull { get; set; } = string.Empty;

        [JsonPropertyName("title_or_subject")]
        public string TitleOrSubject { get; set; } = string.Empty;

        [JsonPropertyName("recipient")]
        public string? Recipient { get; set; }

        [JsonPropertyName("metadata_json")]
        public string MetadataJson { get; set; } = "{}";

        [JsonPropertyName("rendered_content_json")]
        public string RenderedContentJson { get; set; } = "{}";

        [JsonPropertyName("rab_data_json")]
        public string? RabDataJson { get; set; }

        [JsonPropertyName("rab_total_amount")]
        public double RabTotalAmount { get; set; } = 0;

        [JsonPropertyName("locked_archive_url")]
        public string? LockedArchiveUrl { get; set; }

        [JsonPropertyName("scan_url")]
        public string? ScanUrl { get; set; }

        [JsonPropertyName("approval_status")]
        public string ApprovalStatus { get; set; } = "approved"; // 'draft', 'waiting_chief_approval', 'revision_needed', 'approved'

        [JsonPropertyName("archive_status")]
        public string ArchiveStatus { get; set; } = "completed_digital"; // 'draft', 'completed_digital', 'verified_scan', 'pending_deletion'

        [JsonPropertyName("deletion_reason")]
        public string? DeletionReason { get; set; }

        [JsonPropertyName("created_by_user_id")]
        public string CreatedByUserId { get; set; } = string.Empty;

        [JsonPropertyName("created_by_name")]
        public string CreatedByName { get; set; } = string.Empty;

        [JsonPropertyName("created_at")]
        public string? CreatedAt { get; set; }

        [JsonIgnore]
        public bool HasScan => !string.IsNullOrEmpty(ScanUrl) && ArchiveStatus == "verified_scan";

        [JsonIgnore]
        public string StatusBadgeText => ArchiveStatus switch
        {
            "draft" => "Draf Belum Selesai",
            "completed_digital" => "⚠️ Arsip Digital (Menunggu Scan Basah)",
            "verified_scan" => "✓ Terverifikasi Lengkap (Digital + Scan)",
            "pending_deletion" => "⛔ Menunggu Persetujuan Hapus Admin",
            _ => ArchiveStatus
        };

        [JsonIgnore]
        public string ArchiveStatusLabel => StatusBadgeText;

        [JsonIgnore]
        public string ArchiveStatusBadgeBg => StatusBadgeBg;

        [JsonIgnore]
        public string ArchiveStatusBadgeFg => StatusBadgeFg;

        [JsonIgnore]
        public string StatusBadgeBg => ArchiveStatus switch
        {
            "draft" => "#F1F5F9",
            "completed_digital" => "#FEF3C7",
            "verified_scan" => "#DCFCE7",
            "pending_deletion" => "#FEE2E2",
            _ => "#F1F5F9"
        };

        [JsonIgnore]
        public string StatusBadgeFg => ArchiveStatus switch
        {
            "draft" => "#475569",
            "completed_digital" => "#92400E",
            "verified_scan" => "#166534",
            "pending_deletion" => "#B91C1C",
            _ => "#334155"
        };

        [JsonIgnore]
        public string FormattedTotalRab => RabTotalAmount > 0 
            ? string.Format(new System.Globalization.CultureInfo("id-ID"), "Rp {0:N0},-", RabTotalAmount) 
            : "-";
    }

    public class RabItemModel
    {
        private double? _subTotal;

        public int RowNumber { get; set; }
        public string ItemName { get; set; } = string.Empty;
        public double Volume { get; set; } = 1;
        public string Unit { get; set; } = "Paket";
        public double UnitPrice { get; set; } = 0;
        public double SubTotal { get => _subTotal ?? (Volume * UnitPrice); set => _subTotal = value; }

        [JsonIgnore]
        public int ItemNo { get => RowNumber; set => RowNumber = value; }

        [JsonIgnore]
        public string Description { get => ItemName; set => ItemName = value; }

        public string FormattedSubTotal => string.Format(new System.Globalization.CultureInfo("id-ID"), "Rp {0:N0}", SubTotal);
        public string FormattedUnitPrice => string.Format(new System.Globalization.CultureInfo("id-ID"), "Rp {0:N0}", UnitPrice);
    }

    public class IncomingMailModel
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("tenant_id")]
        public string TenantId { get; set; } = string.Empty;

        [JsonPropertyName("agenda_number")]
        public string AgendaNumber { get; set; } = string.Empty;

        [JsonPropertyName("sender_origin")]
        public string SenderOrigin { get; set; } = string.Empty;

        [JsonPropertyName("original_mail_number")]
        public string OriginalMailNumber { get; set; } = string.Empty;

        [JsonPropertyName("mail_date")]
        public string MailDate { get; set; } = string.Empty;

        [JsonPropertyName("received_date")]
        public string ReceivedDate { get; set; } = string.Empty;

        [JsonPropertyName("subject")]
        public string Subject { get; set; } = string.Empty;

        [JsonPropertyName("scan_url")]
        public string? ScanUrl { get; set; }

        [JsonPropertyName("disposition_to")]
        public string? DispositionTo { get; set; }

        [JsonPropertyName("disposition_instruction")]
        public string? DispositionInstruction { get; set; }

        [JsonPropertyName("urgency_level")]
        public string UrgencyLevel { get; set; } = "Biasa";

        [JsonPropertyName("created_by_name")]
        public string CreatedByName { get; set; } = string.Empty;

        [JsonPropertyName("created_at")]
        public string? CreatedAt { get; set; }
    }

    public class SystemBroadcastModel
    {
        [JsonPropertyName("running_text")]
        public string RunningText { get; set; } = string.Empty;

        [JsonPropertyName("running_text_speed")]
        public int RunningTextSpeed { get; set; } = 50;

        [JsonPropertyName("running_text_bg_color")]
        public string RunningTextBgColor { get; set; } = "#1E40AF";

        [JsonPropertyName("popup_enabled")]
        public int PopupEnabled { get; set; } = 0;

        [JsonPropertyName("popup_title")]
        public string? PopupTitle { get; set; }

        [JsonPropertyName("popup_image_url")]
        public string? PopupImageUrl { get; set; }

        [JsonPropertyName("popup_cta_text")]
        public string? PopupCtaText { get; set; }

        [JsonPropertyName("popup_cta_link")]
        public string? PopupCtaLink { get; set; }

        [JsonPropertyName("support_channel_uri")]
        public string? SupportChannelUri { get; set; }

        [JsonPropertyName("target_tiers")]
        public List<string> TargetTiers { get; set; } = new();
    }

    public class LicenseVerifyResult
    {
        [JsonPropertyName("license_key")]
        public string LicenseKey { get; set; } = string.Empty;

        [JsonPropertyName("tier_id")]
        public string TierId { get; set; } = string.Empty;

        [JsonPropertyName("effective_max_users")]
        public int EffectiveMaxUsers { get; set; }

        [JsonPropertyName("effective_max_storage_mb")]
        public int EffectiveMaxStorageMb { get; set; }

        [JsonPropertyName("tier")]
        public TierModel Tier { get; set; } = new();
    }

    public class RegisterTenantRequest
    {
        [JsonPropertyName("license_key")]
        public string LicenseKey { get; set; } = string.Empty;

        [JsonPropertyName("admin_name")]
        public string AdminName { get; set; } = string.Empty;

        [JsonPropertyName("institution_name")]
        public string InstitutionName { get; set; } = string.Empty;

        [JsonPropertyName("institution_address")]
        public string InstitutionAddress { get; set; } = string.Empty;

        [JsonPropertyName("institution_type")]
        public string InstitutionType { get; set; } = "school";

        [JsonPropertyName("phone_number")]
        public string PhoneNumber { get; set; } = string.Empty;

        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;

        [JsonPropertyName("password")]
        public string Password { get; set; } = string.Empty;

        [JsonPropertyName("logo_url")]
        public string? LogoUrl { get; set; }
    }

    public class NextNumberResponse
    {
        [JsonPropertyName("sequence_number")]
        public int SequenceNumber { get; set; }

        [JsonPropertyName("doc_number_full")]
        public string DocNumberFull { get; set; } = string.Empty;

        [JsonPropertyName("is_gap_fill")]
        public bool IsGapFill { get; set; }

        [JsonPropertyName("has_gap")]
        public bool HasGap { get; set; }

        [JsonPropertyName("available_gaps")]
        public List<int> AvailableGaps { get; set; } = new();
    }

    public class DashboardSummaryModel
    {
        [JsonPropertyName("total_documents")]
        public int TotalDocuments { get; set; }

        [JsonPropertyName("total_rab_amount")]
        public double TotalRabAmount { get; set; }

        [JsonPropertyName("waiting_scan_count")]
        public int WaitingScanCount { get; set; }

        [JsonPropertyName("verified_scan_count")]
        public int VerifiedScanCount { get; set; }

        [JsonPropertyName("expiring_personnel")]
        public List<EntityRecordModel> ExpiringPersonnel { get; set; } = new();

        [JsonPropertyName("pending_deletions")]
        public List<DocumentModel> PendingDeletions { get; set; } = new();
    }

    public class CreateDocumentRequest
    {
        [JsonPropertyName("category_id")]
        public string CategoryId { get; set; } = string.Empty;

        [JsonPropertyName("title_or_subject")]
        public string TitleOrSubject { get; set; } = string.Empty;

        [JsonPropertyName("recipient")]
        public string? Recipient { get; set; }

        [JsonPropertyName("rendered_content_json")]
        public string RenderedContentJson { get; set; } = string.Empty;

        [JsonPropertyName("rab_data_json")]
        public string? RabDataJson { get; set; }

        [JsonPropertyName("rab_total_amount")]
        public double RabTotalAmount { get; set; }

        [JsonPropertyName("archive_status")]
        public string ArchiveStatus { get; set; } = "draft";
    }

    public class UpdateDocumentRequest
    {
        [JsonPropertyName("title_or_subject")]
        public string TitleOrSubject { get; set; } = string.Empty;

        [JsonPropertyName("recipient")]
        public string? Recipient { get; set; }

        [JsonPropertyName("rendered_content_json")]
        public string RenderedContentJson { get; set; } = string.Empty;

        [JsonPropertyName("rab_data_json")]
        public string? RabDataJson { get; set; }

        [JsonPropertyName("rab_total_amount")]
        public double RabTotalAmount { get; set; }

        [JsonPropertyName("archive_status")]
        public string ArchiveStatus { get; set; } = "draft";
    }

    public class CreateIncomingMailRequest
    {
        [JsonPropertyName("agenda_number")]
        public string AgendaNumber { get; set; } = string.Empty;

        [JsonPropertyName("sender_origin")]
        public string SenderOrigin { get; set; } = string.Empty;

        [JsonPropertyName("original_mail_number")]
        public string OriginalMailNumber { get; set; } = string.Empty;

        [JsonPropertyName("subject")]
        public string Subject { get; set; } = string.Empty;

        [JsonPropertyName("urgency_level")]
        public string UrgencyLevel { get; set; } = "Biasa";

        [JsonPropertyName("disposition_to")]
        public string? DispositionTo { get; set; }

        [JsonPropertyName("disposition_instruction")]
        public string? DispositionInstruction { get; set; }
    }

    public class CreateEntityRecordRequest
    {
        [JsonPropertyName("primary_name")]
        public string PrimaryName { get; set; } = string.Empty;

        [JsonPropertyName("primary_id_number")]
        public string? PrimaryIdNumber { get; set; }

        [JsonPropertyName("tmt_date")]
        public string? TmtDate { get; set; }

        [JsonPropertyName("contract_end_date")]
        public string? ContractEndDate { get; set; }

        [JsonPropertyName("values_json")]
        public string ValuesJson { get; set; } = "{}";
    }

    public class CreateAttributeRequest
    {
        [JsonPropertyName("attr_key")]
        public string AttrKey { get; set; } = string.Empty;

        [JsonPropertyName("attr_label")]
        public string AttrLabel { get; set; } = string.Empty;

        [JsonPropertyName("data_type")]
        public string DataType { get; set; } = "text";
    }

    public class CreateUserRequest
    {
        [JsonPropertyName("full_name")]
        public string FullName { get; set; } = string.Empty;

        [JsonPropertyName("email_username")]
        public string EmailUsername { get; set; } = string.Empty;

        [JsonPropertyName("password")]
        public string Password { get; set; } = string.Empty;

        [JsonPropertyName("role")]
        public string Role { get; set; } = "staff";

        [JsonPropertyName("phone")]
        public string? Phone { get; set; }
    }

    public class UpdateTenantSettingsRequest
    {
        [JsonPropertyName("institution_address")]
        public string InstitutionAddress { get; set; } = string.Empty;

        [JsonPropertyName("phone_number")]
        public string PhoneNumber { get; set; } = string.Empty;

        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;
    }
}

