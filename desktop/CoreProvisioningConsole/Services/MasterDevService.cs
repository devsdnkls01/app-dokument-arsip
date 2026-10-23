using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using DeveloperStudio.Models;

namespace DeveloperStudio.Services
{
    public class MasterDevService
    {
        private readonly HttpClient _httpClient;
        private string _baseUrl = "https://document-archive-backend.develzy.workers.dev";
        private const string MasterKey = "DEV-MASTER-GODMODE-2026";

        public MasterDevService(string? baseUrl = null)
        {
            if (!string.IsNullOrEmpty(baseUrl)) _baseUrl = baseUrl.TrimEnd('/');
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(_baseUrl),
                Timeout = TimeSpan.FromSeconds(30)
            };
            _httpClient.DefaultRequestHeaders.Add("X-Edge-Provision-Token", MasterKey);
            _httpClient.DefaultRequestHeaders.Add("X-Developer-Master-Key", MasterKey);
        }

        public string BaseUrl => _baseUrl;

        public void SetBaseUrl(string url)
        {
            _baseUrl = url.TrimEnd('/');
            _httpClient.BaseAddress = new Uri(_baseUrl);
        }

        public async Task<ApiResponse<DevOverviewModel>> GetOverviewAsync()
        {
            try
            {
                var res = await _httpClient.GetFromJsonAsync<ApiResponse<DevOverviewModel>>("/api/dev/overview");
                return res ?? new ApiResponse<DevOverviewModel> { Success = false, Error = "Respon kosong" };
            }
            catch (Exception ex)
            {
                return new ApiResponse<DevOverviewModel> { Success = false, Error = ex.Message };
            }
        }

        public async Task<ApiResponse<List<DevTierModel>>> GetTiersAsync()
        {
            try
            {
                var res = await _httpClient.GetFromJsonAsync<ApiResponse<List<DevTierModel>>>("/api/dev/tiers");
                return res ?? new ApiResponse<List<DevTierModel>> { Success = false, Error = "Respon kosong" };
            }
            catch (Exception ex)
            {
                return new ApiResponse<List<DevTierModel>> { Success = false, Error = ex.Message };
            }
        }

        public async Task<ApiResponse> UpdateTierAsync(string tierId, DevTierModel tier)
        {
            try
            {
                var res = await _httpClient.PutAsJsonAsync($"/api/dev/tiers/{tierId}", tier);
                var data = await res.Content.ReadFromJsonAsync<ApiResponse>();
                return data ?? new ApiResponse { Success = false, Error = "Respon kosong" };
            }
            catch (Exception ex)
            {
                return new ApiResponse { Success = false, Error = ex.Message };
            }
        }

        public async Task<ApiResponse<List<DevLicenseModel>>> GetLicensesAsync()
        {
            try
            {
                var res = await _httpClient.GetFromJsonAsync<ApiResponse<List<DevLicenseModel>>>("/api/dev/licenses");
                return res ?? new ApiResponse<List<DevLicenseModel>> { Success = false, Error = "Respon kosong" };
            }
            catch (Exception ex)
            {
                return new ApiResponse<List<DevLicenseModel>> { Success = false, Error = ex.Message };
            }
        }

        public async Task<ApiResponse<DevLicenseModel>> GenerateLicenseAsync(GenerateLicenseRequest req)
        {
            try
            {
                var res = await _httpClient.PostAsJsonAsync("/api/dev/licenses/generate", req);
                var data = await res.Content.ReadFromJsonAsync<ApiResponse<DevLicenseModel>>();
                return data ?? new ApiResponse<DevLicenseModel> { Success = false, Error = "Respon kosong" };
            }
            catch (Exception ex)
            {
                return new ApiResponse<DevLicenseModel> { Success = false, Error = ex.Message };
            }
        }

        public async Task<ApiResponse<DevLicenseModel>> UpgradeLicenseAsync(string licenseKey, UpgradeLicenseRequest req)
        {
            try
            {
                var res = await _httpClient.PutAsJsonAsync($"/api/dev/licenses/{Uri.EscapeDataString(licenseKey)}/upgrade", req);
                var data = await res.Content.ReadFromJsonAsync<ApiResponse<DevLicenseModel>>();
                return data ?? new ApiResponse<DevLicenseModel> { Success = false, Error = "Respon kosong" };
            }
            catch (Exception ex)
            {
                return new ApiResponse<DevLicenseModel> { Success = false, Error = ex.Message };
            }
        }

        public async Task<ApiResponse<List<DevTenantModel>>> GetTenantsAsync()
        {
            try
            {
                var res = await _httpClient.GetFromJsonAsync<ApiResponse<List<DevTenantModel>>>("/api/dev/tenants");
                return res ?? new ApiResponse<List<DevTenantModel>> { Success = false, Error = "Respon kosong" };
            }
            catch (Exception ex)
            {
                return new ApiResponse<List<DevTenantModel>> { Success = false, Error = ex.Message };
            }
        }

        public async Task<ApiResponse> SetTenantStatusAsync(string tenantId, bool isActive)
        {
            try
            {
                var body = new { is_active = isActive ? 1 : 0 };
                var res = await _httpClient.PutAsJsonAsync($"/api/dev/tenants/{tenantId}/status", body);
                var data = await res.Content.ReadFromJsonAsync<ApiResponse>();
                return data ?? new ApiResponse { Success = false, Error = "Respon kosong" };
            }
            catch (Exception ex)
            {
                return new ApiResponse { Success = false, Error = ex.Message };
            }
        }

        public async Task<ApiResponse> ResetPasswordAsync(ResetPasswordRequest req)
        {
            try
            {
                var res = await _httpClient.PostAsJsonAsync("/api/dev/users/reset-password", req);
                var data = await res.Content.ReadFromJsonAsync<ApiResponse>();
                return data ?? new ApiResponse { Success = false, Error = "Respon kosong" };
            }
            catch (Exception ex)
            {
                return new ApiResponse { Success = false, Error = ex.Message };
            }
        }

        public async Task<ApiResponse<DevBroadcastModel>> GetBroadcastAsync()
        {
            try
            {
                var res = await _httpClient.GetFromJsonAsync<ApiResponse<DevBroadcastModel>>("/api/dev/broadcast");
                return res ?? new ApiResponse<DevBroadcastModel> { Success = false, Error = "Respon kosong" };
            }
            catch (Exception ex)
            {
                return new ApiResponse<DevBroadcastModel> { Success = false, Error = ex.Message };
            }
        }

        public async Task<ApiResponse> UpdateBroadcastAsync(DevBroadcastModel bcast)
        {
            try
            {
                var res = await _httpClient.PutAsJsonAsync("/api/dev/broadcast", bcast);
                var data = await res.Content.ReadFromJsonAsync<ApiResponse>();
                return data ?? new ApiResponse { Success = false, Error = "Respon kosong" };
            }
            catch (Exception ex)
            {
                return new ApiResponse { Success = false, Error = ex.Message };
            }
        }

        public async Task<ApiResponse<List<DevPresetModel>>> GetPresetsAsync()
        {
            try
            {
                var res = await _httpClient.GetFromJsonAsync<ApiResponse<List<DevPresetModel>>>("/api/dev/presets");
                return res ?? new ApiResponse<List<DevPresetModel>> { Success = false, Error = "Respon kosong" };
            }
            catch (Exception ex)
            {
                return new ApiResponse<List<DevPresetModel>> { Success = false, Error = ex.Message };
            }
        }

        public async Task<ApiResponse<DevPresetModel>> CreatePresetAsync(DevPresetModel preset)
        {
            try
            {
                var res = await _httpClient.PostAsJsonAsync("/api/dev/presets", preset);
                var data = await res.Content.ReadFromJsonAsync<ApiResponse<DevPresetModel>>();
                return data ?? new ApiResponse<DevPresetModel> { Success = false, Error = "Respon kosong" };
            }
            catch (Exception ex)
            {
                return new ApiResponse<DevPresetModel> { Success = false, Error = ex.Message };
            }
        }

        public async Task<ApiResponse> DeletePresetAsync(string id)
        {
            try
            {
                var res = await _httpClient.DeleteAsync($"/api/dev/presets/{id}");
                var data = await res.Content.ReadFromJsonAsync<ApiResponse>();
                return data ?? new ApiResponse { Success = false, Error = "Respon kosong" };
            }
            catch (Exception ex)
            {
                return new ApiResponse { Success = false, Error = ex.Message };
            }
        }
    }
}
