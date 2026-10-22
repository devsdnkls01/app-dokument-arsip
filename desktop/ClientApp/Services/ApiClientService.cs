using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using ClientApp.Models;

namespace ClientApp.Services
{
    public class ApiClientService
    {
        private readonly HttpClient _httpClient;
        private string _baseUrl = "http://127.0.0.1:8787";
        private ClientSession? _session;

        public ApiClientService(string? baseUrl = null)
        {
            if (!string.IsNullOrEmpty(baseUrl))
            {
                _baseUrl = baseUrl.TrimEnd('/');
            }

            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(_baseUrl),
                Timeout = TimeSpan.FromSeconds(30)
            };

            _session = SessionService.GetSession();
            ApplySessionHeaders();
        }

        public string BaseUrl => _baseUrl;
        public ClientSession? Session => _session;
        public TenantModel? Tenant => _session?.Tenant;
        public UserModel? User => _session?.User;
        public TierModel? Tier => _session?.Tier;
        public bool IsAdmin => _session?.User.IsAdmin ?? false;

        public void SetBaseUrl(string url)
        {
            _baseUrl = url.TrimEnd('/');
            _httpClient.BaseAddress = new Uri(_baseUrl);
        }

        public void SetSession(ClientSession session)
        {
            _session = session;
            SessionService.SaveSession(session);
            ApplySessionHeaders();
        }

        public void ClearSession()
        {
            _session = null;
            SessionService.ClearSession();
            _httpClient.DefaultRequestHeaders.Authorization = null;
        }

        private void ApplySessionHeaders()
        {
            if (_session != null && !string.IsNullOrEmpty(_session.Token))
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _session.Token);
            }
            else
            {
                _httpClient.DefaultRequestHeaders.Authorization = null;
            }
        }

        // =========================================================
        // 1. PUBLIC AUTH & ONBOARDING
        // =========================================================

        public async Task<ApiResponse<LicenseVerifyResult>> VerifyLicenseAsync(string licenseKey)
        {
            try
            {
                var res = await _httpClient.PostAsJsonAsync("/api/auth/verify-license", new { license_key = licenseKey });
                var data = await res.Content.ReadFromJsonAsync<ApiResponse<LicenseVerifyResult>>();
                return data ?? new ApiResponse<LicenseVerifyResult> { Success = false, Error = "Respon kosong dari server." };
            }
            catch (Exception ex)
            {
                return new ApiResponse<LicenseVerifyResult> { Success = false, Error = ex.Message };
            }
        }

        public async Task<ApiResponse<ClientSession>> RegisterTenantAsync(RegisterTenantRequest req)
        {
            try
            {
                var payload = new
                {
                    license_key = req.LicenseKey,
                    admin_name = req.AdminName,
                    institution_name = req.InstitutionName,
                    institution_address = req.InstitutionAddress,
                    institution_type = req.InstitutionType,
                    phone_number = req.PhoneNumber,
                    email = req.Email,
                    password = req.Password,
                    logo_url = req.LogoUrl,
                    device_info = Environment.MachineName
                };

                var res = await _httpClient.PostAsJsonAsync("/api/auth/register-tenant", payload);
                var regResult = await res.Content.ReadFromJsonAsync<ApiResponse<JsonElement>>();

                if (regResult != null && regResult.Success)
                {
                    string institutionCode = regResult.Data.GetProperty("institution_code").GetString() ?? "";
                    // Auto login immediately
                    var loginRes = await LoginAsync(institutionCode, req.Email, req.Password);
                    if (loginRes.Success && loginRes.Data != null)
                    {
                        var session = new ClientSession
                        {
                            Token = loginRes.Data.Token,
                            User = loginRes.Data.User,
                            Tenant = loginRes.Data.Tenant,
                            Tier = loginRes.Data.Tier,
                            SavedAt = DateTime.UtcNow
                        };
                        return new ApiResponse<ClientSession> { Success = true, Data = session };
                    }
                }

                return new ApiResponse<ClientSession> { Success = false, Error = regResult?.Error ?? "Registrasi gagal." };
            }
            catch (Exception ex)
            {
                return new ApiResponse<ClientSession> { Success = false, Error = ex.Message };
            }
        }

        public class LoginDataPayload
        {
            public string Token { get; set; } = string.Empty;
            public UserModel User { get; set; } = new();
            public TenantModel Tenant { get; set; } = new();
            public TierModel Tier { get; set; } = new();
        }

        public async Task<ApiResponse<LoginDataPayload>> LoginAsync(string institutionCode, string emailUsername, string password)
        {
            try
            {
                var payload = new
                {
                    institution_code = institutionCode,
                    email_username = emailUsername,
                    password = password,
                    device_info = Environment.MachineName
                };

                var res = await _httpClient.PostAsJsonAsync("/api/auth/login", payload);
                var result = await res.Content.ReadFromJsonAsync<ApiResponse<LoginDataPayload>>();

                if (result != null && result.Success && result.Data != null)
                {
                    var session = new ClientSession
                    {
                        Token = result.Data.Token,
                        User = result.Data.User,
                        Tenant = result.Data.Tenant,
                        Tier = result.Data.Tier,
                        SavedAt = DateTime.UtcNow
                    };
                    SetSession(session);
                }

                return result ?? new ApiResponse<LoginDataPayload> { Success = false, Error = "Respon login kosong." };
            }
            catch (Exception ex)
            {
                return new ApiResponse<LoginDataPayload> { Success = false, Error = ex.Message };
            }
        }

        public async Task<ApiResponse<SystemBroadcastModel>> GetSystemBroadcastAsync()
        {
            try
            {
                var res = await _httpClient.GetFromJsonAsync<ApiResponse<JsonElement>>("/api/system/broadcast");
                if (res != null && res.Success)
                {
                    var b = res.Data.GetProperty("broadcast");
                    var broadcast = JsonSerializer.Deserialize<SystemBroadcastModel>(b.GetRawText()) ?? new SystemBroadcastModel();
                    return new ApiResponse<SystemBroadcastModel> { Success = true, Data = broadcast };
                }
                return new ApiResponse<SystemBroadcastModel> { Success = false, Error = res?.Error ?? "Gagal memuat siaran." };
            }
            catch (Exception ex)
            {
                return new ApiResponse<SystemBroadcastModel> { Success = false, Error = ex.Message };
            }
        }

        // =========================================================
        // 2. DASHBOARD
        // =========================================================

        public async Task<ApiResponse<DashboardSummaryModel>> GetDashboardSummaryAsync()
        {
            try
            {
                var res = await _httpClient.GetFromJsonAsync<ApiResponse<DashboardSummaryModel>>("/api/dashboard/summary");
                return res ?? new ApiResponse<DashboardSummaryModel> { Success = false, Error = "Respon dashboard kosong." };
            }
            catch (Exception ex)
            {
                return new ApiResponse<DashboardSummaryModel> { Success = false, Error = ex.Message };
            }
        }

        // =========================================================
        // 3. CATEGORIES & DOCUMENT TEMPLATES
        // =========================================================

        public async Task<ApiResponse<List<CategoryModel>>> GetCategoriesAsync()
        {
            try
            {
                var res = await _httpClient.GetFromJsonAsync<ApiResponse<List<CategoryModel>>>("/api/categories");
                return res ?? new ApiResponse<List<CategoryModel>> { Success = false, Error = "Respon kategori kosong." };
            }
            catch (Exception ex)
            {
                return new ApiResponse<List<CategoryModel>> { Success = false, Error = ex.Message };
            }
        }

        public async Task<ApiResponse> UpdateCategoryAsync(string id, CategoryModel cat)
        {
            try
            {
                var res = await _httpClient.PutAsJsonAsync($"/api/categories/{id}", cat);
                var data = await res.Content.ReadFromJsonAsync<ApiResponse>();
                return data ?? new ApiResponse { Success = false, Error = "Respon kosong." };
            }
            catch (Exception ex)
            {
                return new ApiResponse { Success = false, Error = ex.Message };
            }
        }

        public async Task<ApiResponse<NextNumberResponse>> GetNextDocumentNumberAsync(string categoryId)
        {
            try
            {
                var res = await _httpClient.GetFromJsonAsync<ApiResponse<NextNumberResponse>>($"/api/documents/next-number/{categoryId}");
                return res ?? new ApiResponse<NextNumberResponse> { Success = false, Error = "Respon nomor kosong." };
            }
            catch (Exception ex)
            {
                return new ApiResponse<NextNumberResponse> { Success = false, Error = ex.Message };
            }
        }

        // =========================================================
        // 4. DOCUMENTS & ARCHIVES
        // =========================================================

        public async Task<ApiResponse<List<DocumentModel>>> GetDocumentsAsync()
        {
            try
            {
                var res = await _httpClient.GetFromJsonAsync<ApiResponse<List<DocumentModel>>>("/api/documents");
                return res ?? new ApiResponse<List<DocumentModel>> { Success = false, Error = "Respon dokumen kosong." };
            }
            catch (Exception ex)
            {
                return new ApiResponse<List<DocumentModel>> { Success = false, Error = ex.Message };
            }
        }

        public async Task<ApiResponse<DocumentModel>> CreateDocumentAsync(CreateDocumentRequest req)
        {
            try
            {
                var res = await _httpClient.PostAsJsonAsync("/api/documents", req);
                var data = await res.Content.ReadFromJsonAsync<ApiResponse<DocumentModel>>();
                return data ?? new ApiResponse<DocumentModel> { Success = false, Error = "Respon simpan dokumen kosong." };
            }
            catch (Exception ex)
            {
                return new ApiResponse<DocumentModel> { Success = false, Error = ex.Message };
            }
        }

        public async Task<ApiResponse> UpdateDocumentAsync(string id, UpdateDocumentRequest req)
        {
            try
            {
                var res = await _httpClient.PutAsJsonAsync($"/api/documents/{id}", req);
                var data = await res.Content.ReadFromJsonAsync<ApiResponse>();
                return data ?? new ApiResponse { Success = false, Error = "Respon update dokumen kosong." };
            }
            catch (Exception ex)
            {
                return new ApiResponse { Success = false, Error = ex.Message };
            }
        }

        public async Task<ApiResponse> LinkDocumentScanAsync(string docId, string scanUrl)
        {
            try
            {
                var res = await _httpClient.PostAsJsonAsync($"/api/documents/{docId}/link-scan", new { scan_url = scanUrl });
                var data = await res.Content.ReadFromJsonAsync<ApiResponse>();
                return data ?? new ApiResponse { Success = false, Error = "Respon link scan kosong." };
            }
            catch (Exception ex)
            {
                return new ApiResponse { Success = false, Error = ex.Message };
            }
        }

        public async Task<ApiResponse> RequestDocumentDeletionAsync(string docId, string reason)
        {
            try
            {
                var res = await _httpClient.PostAsJsonAsync($"/api/documents/{docId}/request-delete", new { reason = reason });
                var data = await res.Content.ReadFromJsonAsync<ApiResponse>();
                return data ?? new ApiResponse { Success = false, Error = "Respon ajukan hapus kosong." };
            }
            catch (Exception ex)
            {
                return new ApiResponse { Success = false, Error = ex.Message };
            }
        }

        public async Task<ApiResponse> ResolveDocumentDeletionAsync(string docId, bool approve)
        {
            try
            {
                var res = await _httpClient.PostAsJsonAsync($"/api/documents/{docId}/resolve-delete", new { approve = approve });
                var data = await res.Content.ReadFromJsonAsync<ApiResponse>();
                return data ?? new ApiResponse { Success = false, Error = "Respon resolusi hapus kosong." };
            }
            catch (Exception ex)
            {
                return new ApiResponse { Success = false, Error = ex.Message };
            }
        }

        // =========================================================
        // 5. INCOMING MAILS
        // =========================================================

        public async Task<ApiResponse<List<IncomingMailModel>>> GetIncomingMailsAsync()
        {
            try
            {
                var res = await _httpClient.GetFromJsonAsync<ApiResponse<List<IncomingMailModel>>>("/api/incoming-mails");
                return res ?? new ApiResponse<List<IncomingMailModel>> { Success = false, Error = "Respon surat masuk kosong." };
            }
            catch (Exception ex)
            {
                return new ApiResponse<List<IncomingMailModel>> { Success = false, Error = ex.Message };
            }
        }

        public async Task<ApiResponse> CreateIncomingMailAsync(CreateIncomingMailRequest req)
        {
            try
            {
                var res = await _httpClient.PostAsJsonAsync("/api/incoming-mails", req);
                var data = await res.Content.ReadFromJsonAsync<ApiResponse>();
                return data ?? new ApiResponse { Success = false, Error = "Respon surat masuk kosong." };
            }
            catch (Exception ex)
            {
                return new ApiResponse { Success = false, Error = ex.Message };
            }
        }

        // =========================================================
        // 6. ADAPTIVE SDM & ENTITIES
        // =========================================================

        public async Task<ApiResponse<List<CustomEntityModel>>> GetEntitiesAsync()
        {
            try
            {
                var res = await _httpClient.GetFromJsonAsync<ApiResponse<List<CustomEntityModel>>>("/api/entities");
                return res ?? new ApiResponse<List<CustomEntityModel>> { Success = false, Error = "Respon entitas kosong." };
            }
            catch (Exception ex)
            {
                return new ApiResponse<List<CustomEntityModel>> { Success = false, Error = ex.Message };
            }
        }

        public async Task<ApiResponse<List<EntityRecordModel>>> GetEntityRecordsAsync(string entityId)
        {
            try
            {
                var res = await _httpClient.GetFromJsonAsync<ApiResponse<List<EntityRecordModel>>>($"/api/entities/{entityId}/records");
                return res ?? new ApiResponse<List<EntityRecordModel>> { Success = false, Error = "Respon data SDM kosong." };
            }
            catch (Exception ex)
            {
                return new ApiResponse<List<EntityRecordModel>> { Success = false, Error = ex.Message };
            }
        }

        public async Task<ApiResponse> CreateEntityRecordAsync(string entityId, CreateEntityRecordRequest req)
        {
            try
            {
                var res = await _httpClient.PostAsJsonAsync($"/api/entities/{entityId}/records", req);
                var data = await res.Content.ReadFromJsonAsync<ApiResponse>();
                return data ?? new ApiResponse { Success = false, Error = "Respon buat data SDM kosong." };
            }
            catch (Exception ex)
            {
                return new ApiResponse { Success = false, Error = ex.Message };
            }
        }

        public async Task<ApiResponse> CreateAttributeAsync(string entityId, CreateAttributeRequest req)
        {
            try
            {
                var res = await _httpClient.PostAsJsonAsync($"/api/entities/{entityId}/attributes", req);
                var data = await res.Content.ReadFromJsonAsync<ApiResponse>();
                return data ?? new ApiResponse { Success = false, Error = "Respon buat atribut kosong." };
            }
            catch (Exception ex)
            {
                return new ApiResponse { Success = false, Error = ex.Message };
            }
        }

        // =========================================================
        // 7. USER MANAGEMENT & SETTINGS
        // =========================================================

        public async Task<ApiResponse<List<UserModel>>> GetUsersAsync()
        {
            try
            {
                var res = await _httpClient.GetFromJsonAsync<ApiResponse<List<UserModel>>>("/api/users");
                return res ?? new ApiResponse<List<UserModel>> { Success = false, Error = "Respon user kosong." };
            }
            catch (Exception ex)
            {
                return new ApiResponse<List<UserModel>> { Success = false, Error = ex.Message };
            }
        }

        public async Task<ApiResponse> CreateUserAsync(CreateUserRequest req)
        {
            try
            {
                var res = await _httpClient.PostAsJsonAsync("/api/users", req);
                var data = await res.Content.ReadFromJsonAsync<ApiResponse>();
                return data ?? new ApiResponse { Success = false, Error = "Respon buat user kosong." };
            }
            catch (Exception ex)
            {
                return new ApiResponse { Success = false, Error = ex.Message };
            }
        }

        public async Task<ApiResponse> UpdateTenantSettingsAsync(UpdateTenantSettingsRequest req)
        {
            try
            {
                var res = await _httpClient.PutAsJsonAsync("/api/tenant/settings", req);
                var data = await res.Content.ReadFromJsonAsync<ApiResponse>();
                return data ?? new ApiResponse { Success = false, Error = "Respon simpan profil kosong." };
            }
            catch (Exception ex)
            {
                return new ApiResponse { Success = false, Error = ex.Message };
            }
        }
    }
}
