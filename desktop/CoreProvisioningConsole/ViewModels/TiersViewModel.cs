using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DeveloperStudio.Models;
using DeveloperStudio.Services;

namespace CoreProvisioningConsole.ViewModels
{
    public partial class TiersViewModel : ObservableObject
    {
        private readonly MasterDevService _masterService;

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private DevTierModel? _selectedTier;

        public ObservableCollection<DevTierModel> Tiers { get; } = new();

        public event Action<string, string>? ShowNotification;

        public TiersViewModel(MasterDevService masterService)
        {
            _masterService = masterService;
        }

        [RelayCommand]
        public async Task LoadTiersAsync()
        {
            IsLoading = true;
            try
            {
                var res = await _masterService.GetTiersAsync();
                if (res.Success && res.Data != null)
                {
                    Tiers.Clear();
                    foreach (var tier in res.Data)
                    {
                        Tiers.Add(tier);
                    }
                    ShowNotification?.Invoke("Konfigurasi Paket", $"Memuat {Tiers.Count} skema paket tier dari database edge.");
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

        [RelayCommand]
        public async Task SaveTierAsync(DevTierModel? tier)
        {
            if (tier == null) return;

            try
            {
                var res = await _masterService.UpdateTierAsync(tier.TierId, tier);
                if (res.Success)
                {
                    ShowNotification?.Invoke("Paket Diperbarui", $"Konfigurasi '{tier.DisplayName}' berhasil disimpan secara langsung ke Cloudflare D1.");
                    await LoadTiersAsync();
                }
                else
                {
                    ShowNotification?.Invoke("Gagal Menyimpan", res.Error ?? "API menolak pembaharuan.");
                }
            }
            catch (Exception ex)
            {
                ShowNotification?.Invoke("Kesalahan Sistem", ex.Message);
            }
        }
    }
}
