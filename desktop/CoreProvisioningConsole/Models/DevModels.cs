using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace DeveloperStudio.Models
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

    public class DevOverviewModel
    {
        [JsonPropertyName("total_tenants")]
        public int TotalTenants { get; set; }

        [JsonPropertyName("active_tenants")]
        public int ActiveTenants { get; set; }

        [JsonPropertyName("suspended_tenants")]
        public int SuspendedTenants { get; set; }

        [JsonPropertyName("total_licenses")]
        public int TotalLicenses { get; set; }

        [JsonPropertyName("claimed_licenses")]
        public int ClaimedLicenses { get; set; }

        [JsonPropertyName("unclaimed_licenses")]
        public int UnclaimedLicenses { get; set; }

        [JsonPropertyName("total_storage_mb")]
        public double TotalStorageMb { get; set; }

        [JsonPropertyName("total_documents")]
        public int TotalDocuments { get; set; }
    }

    public class DevTierModel
    {
        [JsonPropertyName("tier_id")]
        public string TierId { get; set; } = string.Empty;

        [JsonPropertyName("display_name")]
        public string DisplayName { get; set; } = string.Empty;

        [JsonPropertyName("price_label")]
        public string PriceLabel { get; set; } = string.Empty;

        [JsonPropertyName("max_users")]
        public int MaxUsers { get; set; }

        [JsonPropertyName("max_templates")]
        public int MaxTemplates { get; set; }

        [JsonPropertyName("max_storage_mb")]
        public int MaxStorageMb { get; set; }

        [JsonPropertyName("allow_rab_module")]
        public int AllowRabModule { get; set; }

        [JsonPropertyName("allow_direct_scanner")]
        public int AllowDirectScanner { get; set; }

        [JsonPropertyName("allow_qr_validation")]
        public int AllowQrValidation { get; set; }

        [JsonPropertyName("has_watermark")]
        public int HasWatermark { get; set; }
    }

    public class DevLicenseModel
    {
        [JsonPropertyName("license_key")]
        public string LicenseKey { get; set; } = string.Empty;

        [JsonPropertyName("tier_id")]
        public string TierId { get; set; } = string.Empty;

        [JsonPropertyName("tier_name")]
        public string? TierName { get; set; }

        [JsonPropertyName("buyer_note")]
        public string? BuyerNote { get; set; }

        [JsonPropertyName("custom_max_users")]
        public int? CustomMaxUsers { get; set; }

        [JsonPropertyName("custom_max_storage_mb")]
        public int? CustomMaxStorageMb { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; } = "unclaimed";

        [JsonPropertyName("assigned_tenant_id")]
        public string? AssignedTenantId { get; set; }

        [JsonPropertyName("institution_name")]
        public string? InstitutionName { get; set; }

        [JsonPropertyName("created_at")]
        public string? CreatedAt { get; set; }

        [JsonPropertyName("activated_at")]
        public string? ActivatedAt { get; set; }

        [JsonIgnore]
        public string StatusLabel => Status switch
        {
            "active" => "AKTIF / TERKLAIM",
            "suspended" => "DIBEKUKAN",
            _ => "BELUM DIKLAIM"
        };

        [JsonIgnore]
        public DesignSystem.Controls.BadgeVariant StatusBadgeVariant => Status switch
        {
            "active" => DesignSystem.Controls.BadgeVariant.Success,
            "suspended" => DesignSystem.Controls.BadgeVariant.Danger,
            _ => DesignSystem.Controls.BadgeVariant.Info
        };
    }

    public class DevTenantModel
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
        public string InstitutionType { get; set; } = string.Empty;

        [JsonPropertyName("institution_address")]
        public string InstitutionAddress { get; set; } = string.Empty;

        [JsonPropertyName("phone_number")]
        public string PhoneNumber { get; set; } = string.Empty;

        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;

        [JsonPropertyName("storage_used_bytes")]
        public long StorageUsedBytes { get; set; }

        [JsonPropertyName("is_active")]
        public int IsActive { get; set; }

        [JsonPropertyName("created_at")]
        public string? CreatedAt { get; set; }

        [JsonPropertyName("tier_id")]
        public string? TierId { get; set; }

        [JsonPropertyName("tier_name")]
        public string? TierName { get; set; }

        [JsonPropertyName("user_count")]
        public int UserCount { get; set; }

        [JsonPropertyName("max_users")]
        public int MaxUsers { get; set; }

        [JsonPropertyName("max_storage_mb")]
        public int MaxStorageMb { get; set; }

        [JsonPropertyName("admin_name")]
        public string? AdminName { get; set; }

        [JsonIgnore]
        public bool IsActiveBool
        {
            get => IsActive == 1;
            set => IsActive = value ? 1 : 0;
        }

        [JsonIgnore]
        public int StorageUsedMb => (int)(StorageUsedBytes / (1024 * 1024));

        [JsonIgnore]
        public string StorageUsageFormatted => $"{StorageUsedMb} MB / {MaxStorageMb} MB";

        [JsonIgnore]
        public double StorageUsagePercent => MaxStorageMb > 0 ? Math.Min(100.0, (double)StorageUsedMb / MaxStorageMb * 100.0) : 0;

        [JsonIgnore]
        public string StatusLabel => IsActive == 1 ? "OPERASIONAL AKTIF" : "STANDBY / DIBEKUKAN";

        [JsonIgnore]
        public DesignSystem.Controls.BadgeVariant StatusBadgeVariant => IsActive == 1 ? DesignSystem.Controls.BadgeVariant.Success : DesignSystem.Controls.BadgeVariant.Danger;

        [JsonIgnore]
        public string ActionLabel => IsActive == 1 ? "Tangguhkan (Standby)" : "Aktifkan Node";
    }

    public class DevBroadcastModel
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = "default";

        [JsonPropertyName("running_text")]
        public string RunningText { get; set; } = string.Empty;

        [JsonPropertyName("running_text_speed")]
        public int RunningTextSpeed { get; set; } = 50;

        [JsonPropertyName("running_text_bg_color")]
        public string RunningTextBgColor { get; set; } = "#1E40AF";

        [JsonPropertyName("popup_enabled")]
        public int PopupEnabled { get; set; }

        [JsonPropertyName("popup_title")]
        public string? PopupTitle { get; set; }

        [JsonPropertyName("popup_image_url")]
        public string? PopupImageUrl { get; set; }

        [JsonPropertyName("popup_cta_text")]
        public string? PopupCtaText { get; set; }

        [JsonPropertyName("popup_cta_link")]
        public string? PopupCtaLink { get; set; }

        [JsonPropertyName("target_tiers")]
        public List<string> TargetTiers { get; set; } = new();

        [JsonPropertyName("app_latest_version")]
        public string AppLatestVersion { get; set; } = "1.0.0";

        [JsonPropertyName("maintenance_mode")]
        public int MaintenanceMode { get; set; }

        [JsonPropertyName("support_channel_uri")]
        public string SupportChannelUri { get; set; } = "628589584257";
    }

    public class DevPresetModel
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("target_institution_type")]
        public string TargetInstitutionType { get; set; } = "all";

        [JsonPropertyName("category_name")]
        public string CategoryName { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("numbering_formula")]
        public string NumberingFormula { get; set; } = string.Empty;

        [JsonPropertyName("rich_content_json")]
        public string RichContentJson { get; set; } = string.Empty;

        [JsonPropertyName("has_rab_grid")]
        public int HasRabGrid { get; set; }
    }

    public class GenerateLicenseRequest
    {
        [JsonPropertyName("tier_id")]
        public string TierId { get; set; } = "standard";

        [JsonPropertyName("buyer_note")]
        public string? BuyerNote { get; set; }

        [JsonPropertyName("custom_max_users")]
        public int? CustomMaxUsers { get; set; }

        [JsonPropertyName("custom_max_storage_mb")]
        public int? CustomMaxStorageMb { get; set; }
    }

    public class UpgradeLicenseRequest
    {
        [JsonPropertyName("tier_id")]
        public string TierId { get; set; } = "professional";

        [JsonPropertyName("custom_max_users")]
        public int? CustomMaxUsers { get; set; }

        [JsonPropertyName("custom_max_storage_mb")]
        public int? CustomMaxStorageMb { get; set; }
    }

    public class ResetPasswordRequest
    {
        [JsonPropertyName("user_id")]
        public string? UserId { get; set; }

        [JsonPropertyName("email_username")]
        public string? EmailUsername { get; set; }

        [JsonPropertyName("tenant_id")]
        public string? TenantId { get; set; }

        [JsonPropertyName("new_password")]
        public string NewPassword { get; set; } = "admin123";
    }
}
