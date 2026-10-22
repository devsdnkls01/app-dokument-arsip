using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SuratApp.Models
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

    public class CategoryModel
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("code")]
        public string Code { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("template_cloudinary_url")]
        public string TemplateCloudinaryUrl { get; set; } = string.Empty;

        [JsonPropertyName("fields")]
        public List<FormFieldModel> Fields { get; set; } = new();

        public override string ToString() => $"{Code} - {Name}";
    }

    public class FormFieldModel
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("category_id")]
        public string CategoryId { get; set; } = string.Empty;

        [JsonPropertyName("field_key")]
        public string FieldKey { get; set; } = string.Empty;

        [JsonPropertyName("label")]
        public string Label { get; set; } = string.Empty;

        [JsonPropertyName("field_type")]
        public string FieldType { get; set; } = "text";

        [JsonPropertyName("options")]
        public string? Options { get; set; }

        [JsonPropertyName("default_value")]
        public string? DefaultValue { get; set; }

        [JsonPropertyName("placeholder")]
        public string? Placeholder { get; set; }

        [JsonPropertyName("is_required")]
        public int IsRequired { get; set; } = 1;

        [JsonPropertyName("display_order")]
        public int DisplayOrder { get; set; }

        [JsonIgnore]
        public bool IsRequiredBool => IsRequired == 1;

        [JsonIgnore]
        public List<string> ParsedOptions
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Options)) return new List<string>();
                try
                {
                    return JsonSerializer.Deserialize<List<string>>(Options) ?? new List<string>();
                }
                catch
                {
                    return new List<string>();
                }
            }
        }
    }

    public class DocumentModel
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("category_id")]
        public string CategoryId { get; set; } = string.Empty;

        [JsonPropertyName("category_name")]
        public string? CategoryName { get; set; }

        [JsonPropertyName("document_number")]
        public string DocumentNumber { get; set; } = string.Empty;

        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("form_data")]
        public string FormData { get; set; } = "{}";

        [JsonPropertyName("docx_cloudinary_url")]
        public string DocxCloudinaryUrl { get; set; } = string.Empty;

        [JsonPropertyName("docx_public_id")]
        public string DocxPublicId { get; set; } = string.Empty;

        [JsonPropertyName("scan_cloudinary_url")]
        public string? ScanCloudinaryUrl { get; set; }

        [JsonPropertyName("scan_public_id")]
        public string? ScanPublicId { get; set; }

        [JsonPropertyName("scan_uploaded_at")]
        public string? ScanUploadedAt { get; set; }

        [JsonPropertyName("verification_token")]
        public string? VerificationToken { get; set; }

        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("created_at")]
        public string? CreatedAt { get; set; }

        [JsonIgnore]
        public bool HasScan => !string.IsNullOrEmpty(ScanCloudinaryUrl);

        [JsonIgnore]
        public string ScanStatusText => HasScan ? "✓ Scan Basah Terarsip" : "⚠️ Belum Ada Scan Basah";

        // Indikator Warna Sesuai Spesifikasi: Merah = Belum ada scan basah, Hijau = Lengkap
        [JsonIgnore]
        public string ScanBadgeBg => HasScan ? "#DCFCE7" : "#FEE2E2";

        [JsonIgnore]
        public string ScanBadgeFg => HasScan ? "#166534" : "#991B1B";
    }

    public class NextNumberResult
    {
        [JsonPropertyName("number")]
        public string Number { get; set; } = string.Empty;

        [JsonPropertyName("raw_number")]
        public int RawNumber { get; set; }

        [JsonPropertyName("category_code")]
        public string CategoryCode { get; set; } = string.Empty;

        [JsonPropertyName("year")]
        public int Year { get; set; }
    }

    public class AppVersionInfo
    {
        [JsonPropertyName("version")]
        public string Version { get; set; } = "1.0.0";

        [JsonPropertyName("version_code")]
        public int VersionCode { get; set; } = 100;

        [JsonPropertyName("download_url")]
        public string DownloadUrl { get; set; } = string.Empty;

        [JsonPropertyName("release_notes")]
        public string? ReleaseNotes { get; set; }

        [JsonPropertyName("is_mandatory")]
        public int IsMandatory { get; set; }
    }

    public class CloudinarySignatureData
    {
        [JsonPropertyName("cloudName")]
        public string CloudName { get; set; } = string.Empty;

        [JsonPropertyName("apiKey")]
        public string ApiKey { get; set; } = string.Empty;

        [JsonPropertyName("timestamp")]
        public long Timestamp { get; set; }

        [JsonPropertyName("folder")]
        public string Folder { get; set; } = string.Empty;

        [JsonPropertyName("signature")]
        public string Signature { get; set; } = string.Empty;

        [JsonPropertyName("uploadUrl")]
        public string UploadUrl { get; set; } = string.Empty;
    }

    public class CloudinaryUploadResult
    {
        [JsonPropertyName("public_id")]
        public string PublicId { get; set; } = string.Empty;

        [JsonPropertyName("secure_url")]
        public string SecureUrl { get; set; } = string.Empty;

        [JsonPropertyName("url")]
        public string Url { get; set; } = string.Empty;

        [JsonPropertyName("bytes")]
        public long Bytes { get; set; }

        [JsonPropertyName("format")]
        public string? Format { get; set; }
    }
}
