using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DeveloperStudio.Models;
using DeveloperStudio.Services;

namespace CoreProvisioningConsole.ViewModels
{
    public partial class RescueViewModel : ObservableObject
    {
        private readonly MasterDevService _masterService;

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private DevTenantModel? _selectedTenant;

        [ObservableProperty]
        private string _email = string.Empty;

        [ObservableProperty]
        private string _newPassword = string.Empty;

        [ObservableProperty]
        private string _confirmPassword = string.Empty;

        public ObservableCollection<DevTenantModel> Tenants { get; } = new();

        public event Action<string, string>? ShowNotification;

        public RescueViewModel(MasterDevService masterService)
        {
            _masterService = masterService;
        }

        [RelayCommand]
        public async Task LoadTenantsAsync()
        {
            try
            {
                var res = await _masterService.GetTenantsAsync();
                if (res.Success && res.Data != null)
                {
                    Tenants.Clear();
                    foreach (var t in res.Data)
                    {
                        Tenants.Add(t);
                    }
                }
            }
            catch { }
        }

        [RelayCommand]
        public async Task SubmitResetPasswordAsync()
        {
            if (SelectedTenant == null)
            {
                ShowNotification?.Invoke("Validasi Kredensial", "Silakan pilih instansi target terlebih dahulu.");
                return;
            }

            if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(NewPassword))
            {
                ShowNotification?.Invoke("Validasi Kredensial", "Email username dan kata sandi baru wajib diisi.");
                return;
            }

            if (NewPassword != ConfirmPassword)
            {
                ShowNotification?.Invoke("Validasi Kredensial", "Kata sandi baru dan konfirmasi kata sandi tidak cocok.");
                return;
            }

            if (NewPassword.Length < 6)
            {
                ShowNotification?.Invoke("Validasi Kredensial", "Kata sandi baru minimal 6 karakter.");
                return;
            }

            var req = new ResetPasswordRequest
            {
                TenantId = SelectedTenant.Id,
                EmailUsername = Email.Trim(),
                NewPassword = NewPassword
            };

            IsLoading = true;
            try
            {
                var res = await _masterService.ResetPasswordAsync(req);
                if (res.Success)
                {
                    ShowNotification?.Invoke("Kata Sandi Direset", $"Kata sandi untuk '{Email}' pada instansi '{SelectedTenant.InstitutionName}' berhasil diperbarui.");
                    Email = string.Empty;
                    NewPassword = string.Empty;
                    ConfirmPassword = string.Empty;
                }
                else
                {
                    ShowNotification?.Invoke("Gagal Reset", res.Error ?? "API menolak permohonan reset password.");
                }
            }
            catch (Exception ex)
            {
                ShowNotification?.Invoke("Kesalahan Sistem", ex.Message);
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}
