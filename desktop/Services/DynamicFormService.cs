using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MiniSoftware;
using SuratApp.Models;

namespace SuratApp.Services
{
    public class DynamicFormService
    {
        private readonly HttpClient _httpClient;
        private string _apiBaseUrl = "http://127.0.0.1:8787";
        private string _appPin = "123456";

        private readonly Dictionary<string, FrameworkElement> _registeredControls = new();
        private readonly Dictionary<string, FormFieldModel> _registeredFieldModels = new();

        public DynamicFormService(string? apiBaseUrl = null, string pin = "123456")
        {
            if (!string.IsNullOrEmpty(apiBaseUrl))
            {
                _apiBaseUrl = apiBaseUrl.TrimEnd('/');
            }
            _appPin = pin;

            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(_apiBaseUrl),
                Timeout = TimeSpan.FromSeconds(30)
            };
            ApplyPinHeader();
        }

        public void SetBaseUrl(string url)
        {
            _apiBaseUrl = url.TrimEnd('/');
            _httpClient.BaseAddress = new Uri(_apiBaseUrl);
        }

        public void SetPin(string pin)
        {
            _appPin = pin;
            ApplyPinHeader();
        }

        private void ApplyPinHeader()
        {
            _httpClient.DefaultRequestHeaders.Remove("X-App-Pin");
            _httpClient.DefaultRequestHeaders.Add("X-App-Pin", _appPin);
        }

        public string BaseUrl => _apiBaseUrl;

        // ====================================================
        // 1. DYNAMIC FORM UI GENERATION
        // ====================================================

        public void BuildDynamicForm(Panel container, IEnumerable<FormFieldModel> fields)
        {
            container.Children.Clear();
            _registeredControls.Clear();
            _registeredFieldModels.Clear();

            var sortedFields = fields.OrderBy(f => f.DisplayOrder).ToList();

            foreach (var field in sortedFields)
            {
                _registeredFieldModels[field.FieldKey] = field;

                var fieldCard = new StackPanel
                {
                    Margin = new Thickness(0, 0, 0, 14)
                };

                var labelPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 6) };
                
                var labelBlock = new TextBlock
                {
                    Text = field.Label,
                    FontWeight = FontWeights.Medium,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#334155"))
                };
                labelPanel.Children.Add(labelBlock);

                if (field.IsRequiredBool)
                {
                    var reqBlock = new TextBlock
                    {
                        Text = " *",
                        Foreground = Brushes.Red,
                        FontWeight = FontWeights.Bold
                    };
                    labelPanel.Children.Add(reqBlock);
                }

                fieldCard.Children.Add(labelPanel);

                FrameworkElement control;

                switch (field.FieldType.ToLowerInvariant())
                {
                    case "textarea":
                        var textBoxArea = new TextBox
                        {
                            Text = field.DefaultValue ?? string.Empty,
                            AcceptsReturn = true,
                            TextWrapping = TextWrapping.Wrap,
                            Height = 75,
                            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                            Style = (Style)Application.Current.Resources["ModernTextBox"]
                        };
                        control = textBoxArea;
                        break;

                    case "date":
                        var datePicker = new DatePicker
                        {
                            SelectedDate = DateTime.Today,
                            Height = 36,
                            VerticalContentAlignment = VerticalAlignment.Center
                        };
                        control = datePicker;
                        break;

                    case "select":
                        var comboBox = new ComboBox
                        {
                            Height = 36,
                            ItemsSource = field.ParsedOptions,
                            SelectedItem = field.DefaultValue ?? field.ParsedOptions.FirstOrDefault()
                        };
                        control = comboBox;
                        break;

                    default:
                        var textBox = new TextBox
                        {
                            Text = field.DefaultValue ?? string.Empty,
                            Height = 36,
                            Style = (Style)Application.Current.Resources["ModernTextBox"]
                        };
                        control = textBox;
                        break;
                }

                _registeredControls[field.FieldKey] = control;
                fieldCard.Children.Add(control);
                container.Children.Add(fieldCard);
            }
        }

        public void PopulateValues(Dictionary<string, object> values)
        {
            foreach (var kvp in values)
            {
                if (_registeredControls.TryGetValue(kvp.Key, out var control))
                {
                    var strVal = kvp.Value?.ToString() ?? string.Empty;
                    if (control is TextBox tb)
                    {
                        tb.Text = strVal;
                    }
                    else if (control is DatePicker dp)
                    {
                        if (DateTime.TryParse(strVal, out var dt))
                        {
                            dp.SelectedDate = dt;
                        }
                    }
                    else if (control is ComboBox cb)
                    {
                        cb.SelectedItem = strVal;
                    }
                }
            }
        }

        // ====================================================
        // 2. FORM VALUE EXTRACTION & VALIDATION
        // ====================================================

        public (bool IsValid, List<string> Errors, Dictionary<string, object> Values) ExtractAndValidateForm()
        {
            var errors = new List<string>();
            var values = new Dictionary<string, object>();

            foreach (var kvp in _registeredControls)
            {
                var key = kvp.Key;
                var control = kvp.Value;
                var field = _registeredFieldModels[key];

                object? rawValue = null;

                if (control is TextBox tb)
                {
                    rawValue = tb.Text?.Trim();
                }
                else if (control is DatePicker dp)
                {
                    rawValue = dp.SelectedDate.HasValue ? dp.SelectedDate.Value.ToString("dd MMMM yyyy") : string.Empty;
                }
                else if (control is ComboBox cb)
                {
                    rawValue = cb.SelectedItem?.ToString() ?? string.Empty;
                }

                string strValue = rawValue?.ToString() ?? string.Empty;

                if (field.IsRequiredBool && string.IsNullOrWhiteSpace(strValue))
                {
                    errors.Add($"Field '{field.Label}' wajib diisi.");
                }

                values[key] = strValue;
            }

            return (errors.Count == 0, errors, values);
        }

        // ====================================================
        // 3. LOCAL DOCX ENGINE (MINIWORD) DENGAN QR CODE
        // ====================================================

        public async Task<string> GenerateDocumentLocalAsync(
            string templateSourcePathOrUrl, 
            string outputDirectory, 
            string documentNumber,
            Dictionary<string, object> values,
            string? verificationUrl = null)
        {
            Directory.CreateDirectory(outputDirectory);

            var sanitizedDocNo = string.Join("_", documentNumber.Split(Path.GetInvalidFileNameChars()));
            var outputFilePath = Path.Combine(outputDirectory, $"Dokumen_{sanitizedDocNo}_{DateTime.Now:yyyyMMddHHmmss}.docx");

            string localTemplatePath;

            if (templateSourcePathOrUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                templateSourcePathOrUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                var tempTemplatePath = Path.Combine(Path.GetTempPath(), $"master_template_{Guid.NewGuid():N}.docx");
                using var client = new HttpClient();
                var templateBytes = await client.GetByteArrayAsync(templateSourcePathOrUrl);
                await File.WriteAllBytesAsync(tempTemplatePath, templateBytes);
                localTemplatePath = tempTemplatePath;
            }
            else
            {
                localTemplatePath = templateSourcePathOrUrl;
            }

            if (!File.Exists(localTemplatePath))
            {
                throw new FileNotFoundException($"Master template .docx tidak ditemukan pada path: {localTemplatePath}");
            }

            var mergedValues = new Dictionary<string, object>(values);

            // Tambahkan URL validasi ke placeholder jika ada
            if (!string.IsNullOrEmpty(verificationUrl))
            {
                mergedValues["verification_url"] = verificationUrl;
                // QR Code Image byte array
                var qrBytes = LocalDraftService.GenerateQrCodePng(verificationUrl);
                mergedValues["qr_code"] = new MiniWordPicture { Bytes = qrBytes, Width = 90, Height = 90 };
            }

            await Task.Run(() =>
            {
                MiniWord.SaveAsByTemplate(outputFilePath, localTemplatePath, mergedValues);
            });

            return outputFilePath;
        }

        // ====================================================
        // 4. BACKEND API INTERACTIONS
        // ====================================================

        public async Task<AppVersionInfo?> CheckLatestVersionAsync()
        {
            try
            {
                var res = await _httpClient.GetFromJsonAsync<ApiResponse<AppVersionInfo>>("/api/app/latest-version");
                return res?.Data;
            }
            catch
            {
                return null;
            }
        }

        public async Task<NextNumberResult> GetNextDocumentNumberAsync(string categoryId)
        {
            var payload = new { category_id = categoryId, actor_name = Environment.UserName };
            var response = await _httpClient.PostAsJsonAsync("/api/documents/next-number", payload);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<NextNumberResult>>();

            if (result != null && result.Success && result.Data != null)
            {
                return result.Data;
            }
            throw new Exception(result?.Error ?? "Gagal mengambil nomor surat otomatis dari server");
        }

        public async Task<List<CategoryModel>> GetCategoriesAsync()
        {
            var res = await _httpClient.GetFromJsonAsync<ApiResponse<List<CategoryModel>>>("/api/categories");
            if (res != null && res.Success && res.Data != null)
            {
                return res.Data;
            }
            throw new Exception(res?.Error ?? "Gagal memuat kategori dari server");
        }

        public async Task<List<DocumentModel>> GetDocumentsAsync(string? searchQuery = null)
        {
            var url = string.IsNullOrEmpty(searchQuery) ? "/api/documents" : $"/api/documents?q={Uri.EscapeDataString(searchQuery)}";
            var res = await _httpClient.GetFromJsonAsync<ApiResponse<List<DocumentModel>>>(url);
            if (res != null && res.Success && res.Data != null)
            {
                return res.Data;
            }
            throw new Exception(res?.Error ?? "Gagal memuat riwayat dokumen");
        }

        public async Task<string> ExportAgendaBookCsvAsync(string targetFilePath)
        {
            var res = await _httpClient.GetFromJsonAsync<ApiResponse<List<JsonElement>>>("/api/documents/agenda");
            if (res == null || !res.Success || res.Data == null)
            {
                throw new Exception(res?.Error ?? "Gagal mengambil data buku agenda dari server");
            }

            var sb = new StringBuilder();
            // UTF-8 BOM for Microsoft Excel compatibility
            sb.AppendLine("No,Nomor Dokumen,Kategori,Perihal,Tanggal Pembuatan,Operator,Status Arsip Scan,Link Arsip Scan");

            int no = 1;
            foreach (var item in res.Data)
            {
                var docNum = item.GetProperty("document_number").GetString() ?? "";
                var catName = item.TryGetProperty("category_name", out var c) ? c.GetString() ?? "" : "";
                var title = item.GetProperty("title").GetString() ?? "";
                var date = item.GetProperty("tanggal_buat").GetString() ?? "";
                var op = item.GetProperty("operator").GetString() ?? "";
                var scanUrl = item.TryGetProperty("scan_cloudinary_url", out var s) ? s.GetString() ?? "" : "";
                var hasScan = !string.IsNullOrEmpty(scanUrl) ? "Lengkap" : "Belum Ada Scan Basah";

                sb.AppendLine($"\"{no}\",\"{EscapeCsv(docNum)}\",\"{EscapeCsv(catName)}\",\"{EscapeCsv(title)}\",\"{EscapeCsv(date)}\",\"{EscapeCsv(op)}\",\"{hasScan}\",\"{scanUrl}\"");
                no++;
            }

            await File.WriteAllTextAsync(targetFilePath, sb.ToString(), Encoding.UTF8);
            return targetFilePath;
        }

        public async Task<CloudinarySignatureData> GetUploadSignatureAsync(string folder)
        {
            var payload = new { folder };
            var response = await _httpClient.PostAsJsonAsync("/api/upload-signature-url", payload);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<CloudinarySignatureData>>();

            if (result != null && result.Success && result.Data != null)
            {
                return result.Data;
            }
            throw new Exception(result?.Error ?? "Gagal mendapatkan signature Cloudinary");
        }

        public async Task<CloudinaryUploadResult> UploadToCloudinaryDirectAsync(
            string filePath, 
            CloudinarySignatureData signatureData)
        {
            using var form = new MultipartFormDataContent();
            var fileBytes = await File.ReadAllBytesAsync(filePath);
            var fileContent = new ByteArrayContent(fileBytes);
            fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse(GetMimeType(filePath));

            form.Add(fileContent, "file", Path.GetFileName(filePath));
            form.Add(new StringContent(signatureData.ApiKey), "api_key");
            form.Add(new StringContent(signatureData.Timestamp.ToString()), "timestamp");
            form.Add(new StringContent(signatureData.Signature), "signature");
            form.Add(new StringContent(signatureData.Folder), "folder");

            using var cloudClient = new HttpClient();
            var response = await cloudClient.PostAsync(signatureData.UploadUrl, form);
            var responseJson = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Cloudinary upload failed ({response.StatusCode}): {responseJson}");
            }

            var uploadResult = JsonSerializer.Deserialize<CloudinaryUploadResult>(responseJson);
            if (uploadResult == null || string.IsNullOrEmpty(uploadResult.SecureUrl))
            {
                throw new Exception("Format respon Cloudinary tidak valid: " + responseJson);
            }

            return uploadResult;
        }

        public async Task<DocumentModel> SaveDocumentToBackendAsync(
            string categoryId, 
            string docNumber, 
            string title, 
            Dictionary<string, object> formData, 
            string docxUrl, 
            string docxPublicId,
            string? verificationToken = null)
        {
            var payload = new
            {
                category_id = categoryId,
                document_number = docNumber,
                title = title,
                form_data = formData,
                docx_cloudinary_url = docxUrl,
                docx_public_id = docxPublicId,
                verification_token = verificationToken,
                created_by = Environment.UserName
            };

            var response = await _httpClient.PostAsJsonAsync("/api/documents", payload);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<DocumentModel>>();

            if (result != null && result.Success && result.Data != null)
            {
                return result.Data;
            }

            throw new Exception(result?.Error ?? "Gagal menyimpan catatan dokumen ke D1");
        }

        public async Task<DocumentModel> LinkScanDocumentAsync(string documentId, string scanUrl, string scanPublicId)
        {
            var payload = new
            {
                document_id = documentId,
                scan_cloudinary_url = scanUrl,
                scan_public_id = scanPublicId,
                actor_name = Environment.UserName
            };

            var response = await _httpClient.PostAsJsonAsync("/api/documents/link-scan", payload);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<DocumentModel>>();

            if (result != null && result.Success && result.Data != null)
            {
                return result.Data;
            }

            throw new Exception(result?.Error ?? "Gagal menautkan arsip scan fisik ke dokumen");
        }

        private static string EscapeCsv(string val) => val.Replace("\"", "\"\"");

        private static string GetMimeType(string fileName)
        {
            var ext = Path.GetExtension(fileName).ToLowerInvariant();
            return ext switch
            {
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".pdf" => "application/pdf",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                _ => "application/octet-stream"
            };
        }
    }
}
