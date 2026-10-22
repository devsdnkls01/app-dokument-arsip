using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DeveloperStudio.Models;
using DeveloperStudio.Services;

namespace CoreProvisioningConsole.ViewModels
{
    public partial class BroadcastViewModel : ObservableObject
    {
        private readonly MasterDevService _masterService;

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private string _runningText = string.Empty;

        [ObservableProperty]
        private string _runningTextBgColor = "#1E40AF";

        [ObservableProperty]
        private string _appLatestVersion = "1.0.0";

        [ObservableProperty]
        private bool _popupEnabled;

        [ObservableProperty]
        private string _popupTitle = string.Empty;

        [ObservableProperty]
        private string _popupCtaText = "Hubungi Kami";

        [ObservableProperty]
        private string _popupCtaLink = "#";

        [ObservableProperty]
        private string _supportChannelUri = "628589584257";

        public event Action<string, string>? ShowNotification;

        public BroadcastViewModel(MasterDevService masterService)
        {
            _masterService = masterService;
        }

        [RelayCommand]
        public async Task LoadBroadcastAsync()
        {
            IsLoading = true;
            try
            {
                var res = await _masterService.GetBroadcastAsync();
                if (res.Success && res.Data != null)
                {
                    var b = res.Data;
                    RunningText = b.RunningText;
                    RunningTextBgColor = b.RunningTextBgColor;
                    AppLatestVersion = b.AppLatestVersion;
                    PopupEnabled = b.PopupEnabled == 1;
                    PopupTitle = b.PopupTitle ?? string.Empty;
                    PopupCtaText = b.PopupCtaText ?? "Hubungi Kami";
                    PopupCtaLink = b.PopupCtaLink ?? "#";
                    SupportChannelUri = !string.IsNullOrWhiteSpace(b.SupportChannelUri) ? b.SupportChannelUri : "628589584257";

                    ShowNotification?.Invoke("Siaran Dimuat", "Pengaturan buletin, teks berjalan, dan gateway kontak berhasil disinkronkan.");
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
        public async Task SaveBroadcastAsync()
        {
            var bcast = new DevBroadcastModel
            {
                RunningText = RunningText.Trim(),
                RunningTextBgColor = RunningTextBgColor.Trim(),
                AppLatestVersion = AppLatestVersion.Trim(),
                PopupEnabled = PopupEnabled ? 1 : 0,
                PopupTitle = PopupTitle.Trim(),
                PopupCtaText = PopupCtaText.Trim(),
                PopupCtaLink = PopupCtaLink.Trim(),
                SupportChannelUri = SupportChannelUri.Trim(),
                TargetTiers = new List<string> { "trial", "basic", "standard" }
            };

            try
            {
                var res = await _masterService.UpdateBroadcastAsync(bcast);
                if (res.Success)
                {
                    ShowNotification?.Invoke("Pengaturan Tersimpan", "Perubahan buletin dan nomor WhatsApp Gateway berhasil disiarkan ke seluruh aplikasi klien.");
                }
                else
                {
                    ShowNotification?.Invoke("Gagal Menyimpan", res.Error ?? "API menolak pembaruan siaran.");
                }
            }
            catch (Exception ex)
            {
                ShowNotification?.Invoke("Kesalahan Sistem", ex.Message);
            }
        }
    }
}
