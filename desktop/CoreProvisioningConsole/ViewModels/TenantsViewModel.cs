using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DeveloperStudio.Models;
using DeveloperStudio.Services;

namespace CoreProvisioningConsole.ViewModels
{
    public partial class TenantsViewModel : ObservableObject
    {
        private readonly MasterDevService _masterService;

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private string _searchText = string.Empty;

        public ObservableCollection<DevTenantModel> AllTenants { get; } = new();
        public ObservableCollection<DevTenantModel> FilteredTenants { get; } = new();

        public event Action<string, string>? ShowNotification;

        public TenantsViewModel(MasterDevService masterService)
        {
            _masterService = masterService;
        }

        partial void OnSearchTextChanged(string value) => ApplyFilter();

        [RelayCommand]
        public async Task LoadTenantsAsync()
        {
            IsLoading = true;
            try
            {
                var res = await _masterService.GetTenantsAsync();
                if (res.Success && res.Data != null)
                {
                    AllTenants.Clear();
                    foreach (var t in res.Data)
                    {
                        AllTenants.Add(t);
                    }
                    ApplyFilter();
                    ShowNotification?.Invoke("Data Instansi", $"Berhasil memuat {AllTenants.Count} instansi klien terdaftar.");
                }
            }
            catch (Exception ex)
            {
                ShowNotification?.Invoke("Kesalahan Jaringan", ex.Message);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void ApplyFilter()
        {
            var query = AllTenants.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                string s = SearchText.Trim().ToLowerInvariant();
                query = query.Where(t =>
                    (t.InstitutionName ?? "").ToLowerInvariant().Contains(s) ||
                    (t.InstitutionCode ?? "").ToLowerInvariant().Contains(s) ||
                    (t.InstitutionType ?? "").ToLowerInvariant().Contains(s) ||
                    (t.LicenseKey ?? "").ToLowerInvariant().Contains(s));
            }

            FilteredTenants.Clear();
            foreach (var item in query)
            {
                FilteredTenants.Add(item);
            }
        }

        [RelayCommand]
        public async Task ToggleTenantStatusAsync(DevTenantModel? tenant)
        {
            if (tenant == null) return;
            bool newStatus = !tenant.IsActiveBool;

            try
            {
                var res = await _masterService.SetTenantStatusAsync(tenant.Id, newStatus);
                if (res.Success)
                {
                    tenant.IsActiveBool = newStatus;
                    ApplyFilter();
                    string stateLabel = newStatus ? "Diaktifkan Normal" : "Ditempatkan dalam Mode Standby / Suspended";
                    ShowNotification?.Invoke("Status Layanan Berubah", $"Instansi '{tenant.InstitutionName}' berhasil {stateLabel}.");
                }
                else
                {
                    ShowNotification?.Invoke("Gagal Mengubah Status", res.Error ?? "API menolak perintah status.");
                }
            }
            catch (Exception ex)
            {
                ShowNotification?.Invoke("Kesalahan Sistem", ex.Message);
            }
        }
    }
}
