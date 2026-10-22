using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DeveloperStudio.Models;
using DeveloperStudio.Services;

namespace CoreProvisioningConsole.ViewModels
{
    public partial class LicensesViewModel : ObservableObject
    {
        private readonly MasterDevService _masterService;

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private string _searchText = string.Empty;

        [ObservableProperty]
        private string _selectedTierFilter = "Semua Tier";

        [ObservableProperty]
        private string _selectedStatusFilter = "Semua Status";

        [ObservableProperty]
        private DevLicenseModel? _selectedLicense;

        [ObservableProperty]
        private bool _isDetailsOpen;

        // KPI Metrics
        [ObservableProperty]
        private int _totalLicenses;

        [ObservableProperty]
        private int _claimedLicenses;

        [ObservableProperty]
        private int _unclaimedLicenses;

        [ObservableProperty]
        private string _tierDistribution = "0 Trial • 0 Std • 0 Pro";

        // Generate License State
        [ObservableProperty]
        private bool _isGenerateDialogOpen;

        [ObservableProperty]
        private string _genTierId = "standard";

        [ObservableProperty]
        private string _genBuyerNote = string.Empty;

        [ObservableProperty]
        private string _genCustomUsers = string.Empty;

        [ObservableProperty]
        private string _genCustomStorage = string.Empty;

        [ObservableProperty]
        private string? _lastGeneratedKey;

        [ObservableProperty]
        private bool _isGenerating;

        // Upgrade Tier State
        [ObservableProperty]
        private bool _isUpgradeDialogOpen;

        [ObservableProperty]
        private DevLicenseModel? _upgradeTargetLicense;

        [ObservableProperty]
        private string _upgradeNewTier = "professional";

        [ObservableProperty]
        private bool _isUpgrading;

        public ObservableCollection<DevLicenseModel> AllLicenses { get; } = new();
        public ObservableCollection<DevLicenseModel> FilteredLicenses { get; } = new();

        public List<string> TierFilterOptions { get; } = new()
        {
            "Semua Tier", "Trial", "Basic", "Standard", "Professional", "Enterprise"
        };

        public List<string> StatusFilterOptions { get; } = new()
        {
            "Semua Status", "Aktif / Terklaim", "Belum Diklaim", "Dibekukan"
        };

        public event Action<string, string>? ShowNotification;

        public LicensesViewModel(MasterDevService masterService)
        {
            _masterService = masterService;
        }

        partial void OnSearchTextChanged(string value) => ApplyFilter();
        partial void OnSelectedTierFilterChanged(string value) => ApplyFilter();
        partial void OnSelectedStatusFilterChanged(string value) => ApplyFilter();

        [RelayCommand]
        public async Task LoadLicensesAsync()
        {
            IsLoading = true;
            try
            {
                var res = await _masterService.GetLicensesAsync();
                if (res.Success && res.Data != null)
                {
                    AllLicenses.Clear();
                    foreach (var lic in res.Data)
                    {
                        AllLicenses.Add(lic);
                    }

                    UpdateKpiMetrics();
                    ApplyFilter();
                    ShowNotification?.Invoke("Data Lisensi Terkini", $"Berhasil memuat {AllLicenses.Count} lisensi dari Cloudflare D1.");
                }
                else
                {
                    ShowNotification?.Invoke("Gagal Memuat Lisensi", res.Error ?? "Terjadi kendala pada endpoint API.");
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

        private void UpdateKpiMetrics()
        {
            TotalLicenses = AllLicenses.Count;
            ClaimedLicenses = AllLicenses.Count(l => l.Status == "active");
            UnclaimedLicenses = AllLicenses.Count(l => l.Status == "unclaimed");

            int trial = AllLicenses.Count(l => l.TierId.Equals("trial", StringComparison.OrdinalIgnoreCase));
            int basic = AllLicenses.Count(l => l.TierId.Equals("basic", StringComparison.OrdinalIgnoreCase));
            int std = AllLicenses.Count(l => l.TierId.Equals("standard", StringComparison.OrdinalIgnoreCase));
            int pro = AllLicenses.Count(l => l.TierId.Equals("professional", StringComparison.OrdinalIgnoreCase));
            int ent = AllLicenses.Count(l => l.TierId.Equals("enterprise", StringComparison.OrdinalIgnoreCase));

            TierDistribution = $"{trial} Trial • {basic} Bsc • {std} Std • {pro} Pro • {ent} Ent";
        }

        public void ApplyFilter()
        {
            var query = AllLicenses.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                string s = SearchText.Trim().ToLowerInvariant();
                query = query.Where(l =>
                    (l.LicenseKey ?? "").ToLowerInvariant().Contains(s) ||
                    (l.BuyerNote ?? "").ToLowerInvariant().Contains(s) ||
                    (l.InstitutionName ?? "").ToLowerInvariant().Contains(s) ||
                    (l.TierId ?? "").ToLowerInvariant().Contains(s));
            }

            if (SelectedTierFilter != "Semua Tier")
            {
                query = query.Where(l => l.TierId.Equals(SelectedTierFilter, StringComparison.OrdinalIgnoreCase));
            }

            if (SelectedStatusFilter != "Semua Status")
            {
                string statusKey = SelectedStatusFilter switch
                {
                    "Aktif / Terklaim" => "active",
                    "Belum Diklaim" => "unclaimed",
                    "Dibekukan" => "suspended",
                    _ => ""
                };
                if (!string.IsNullOrEmpty(statusKey))
                {
                    query = query.Where(l => l.Status == statusKey);
                }
            }

            FilteredLicenses.Clear();
            foreach (var item in query)
            {
                FilteredLicenses.Add(item);
            }
        }

        [RelayCommand]
        public void OpenGenerateDialog()
        {
            GenTierId = "standard";
            GenBuyerNote = string.Empty;
            GenCustomUsers = string.Empty;
            GenCustomStorage = string.Empty;
            LastGeneratedKey = null;
            IsGenerateDialogOpen = true;
        }

        [RelayCommand]
        public void CloseGenerateDialog()
        {
            IsGenerateDialogOpen = false;
        }

        [RelayCommand]
        public async Task SubmitGenerateLicenseAsync()
        {
            if (string.IsNullOrWhiteSpace(GenBuyerNote))
            {
                ShowNotification?.Invoke("Validasi Formulir", "Catatan nama calon pembeli / instansi wajib diisi.");
                return;
            }

            int? customUsers = int.TryParse(GenCustomUsers, out int u) ? u : null;
            int? customStorage = int.TryParse(GenCustomStorage, out int s) ? s : null;

            var req = new GenerateLicenseRequest
            {
                TierId = GenTierId,
                BuyerNote = GenBuyerNote.Trim(),
                CustomMaxUsers = customUsers,
                CustomMaxStorageMb = customStorage
            };

            IsGenerating = true;
            try
            {
                var res = await _masterService.GenerateLicenseAsync(req);
                if (res.Success && res.Data != null)
                {
                    LastGeneratedKey = res.Data.LicenseKey;
                    Clipboard.SetText(res.Data.LicenseKey);

                    await LoadLicensesAsync();
                    ShowNotification?.Invoke("Lisensi Diterbitkan", $"Kunci {res.Data.LicenseKey} berhasil dibuat dan disalin ke clipboard!");
                }
                else
                {
                    ShowNotification?.Invoke("Gagal Menerbitkan", res.Error ?? "API menolak permintaan penerbitan.");
                }
            }
            catch (Exception ex)
            {
                ShowNotification?.Invoke("Kesalahan Sistem", ex.Message);
            }
            finally
            {
                IsGenerating = false;
            }
        }

        [RelayCommand]
        public void OpenUpgradeDialog(DevLicenseModel? license)
        {
            if (license == null) return;
            UpgradeTargetLicense = license;
            UpgradeNewTier = license.TierId.Equals("enterprise", StringComparison.OrdinalIgnoreCase) ? "enterprise" : "professional";
            IsUpgradeDialogOpen = true;
        }

        [RelayCommand]
        public void CloseUpgradeDialog()
        {
            IsUpgradeDialogOpen = false;
            UpgradeTargetLicense = null;
        }

        [RelayCommand]
        public async Task SubmitUpgradeTierAsync()
        {
            if (UpgradeTargetLicense == null) return;

            IsUpgrading = true;
            try
            {
                var req = new DeveloperStudio.Models.UpgradeLicenseRequest { TierId = UpgradeNewTier };
                var res = await _masterService.UpgradeLicenseAsync(UpgradeTargetLicense.LicenseKey, req);
                if (res.Success)
                {
                    ShowNotification?.Invoke("Upgrade Sukses", $"Lisensi {UpgradeTargetLicense.LicenseKey} berhasil dinaikkan ke tier '{UpgradeNewTier.ToUpperInvariant()}'!");
                    CloseUpgradeDialog();
                    await LoadLicensesAsync();
                }
                else
                {
                    ShowNotification?.Invoke("Gagal Upgrade", res.Error ?? "API gagal mengubah tier lisensi.");
                }
            }
            catch (Exception ex)
            {
                ShowNotification?.Invoke("Kesalahan Sistem", ex.Message);
            }
            finally
            {
                IsUpgrading = false;
            }
        }

        [RelayCommand]
        public void CopyLicenseKey(DevLicenseModel? license)
        {
            if (license == null) return;
            Clipboard.SetText(license.LicenseKey);
            ShowNotification?.Invoke("Tersalin", $"Kode lisensi '{license.LicenseKey}' telah disalin ke clipboard.");
        }

        [RelayCommand]
        public void CopyWhatsAppPackage()
        {
            if (string.IsNullOrWhiteSpace(LastGeneratedKey)) return;

            var msg = new StringBuilder();
            msg.AppendLine("Salam Hormat,");
            msg.AppendLine("Berikut ini kami lampirkan Kredensial Lisensi Resmi Aplikasi Sovereign Executive Suite:");
            msg.AppendLine();
            msg.AppendLine($"*KODE LISENSI*: `{LastGeneratedKey}`");
            msg.AppendLine($"*TIER PAKET*: {GenTierId.ToUpperInvariant()}");
            msg.AppendLine($"*ATAS NAMA*: {GenBuyerNote}");
            msg.AppendLine();
            msg.AppendLine("Petunjuk Aktivasi:");
            msg.AppendLine("1. Buka aplikasi ClientApp.exe di komputer Anda.");
            msg.AppendLine("2. Pilih menu 'Aktivasi Lisensi & Registrasi Baru'.");
            msg.AppendLine("3. Masukkan Kode Lisensi di atas, lalu lengkapi profil instansi Anda.");
            msg.AppendLine();
            msg.AppendLine("Terima kasih atas kepercayaan Anda.");

            Clipboard.SetText(msg.ToString());
            ShowNotification?.Invoke("Paket WA Tersalin", "Teks instruksi WhatsApp rapi telah tersalin ke clipboard.");
        }

        [RelayCommand]
        public void SelectLicense(DevLicenseModel? license)
        {
            SelectedLicense = license;
            IsDetailsOpen = license != null;
        }

        [RelayCommand]
        public void CloseDetails()
        {
            IsDetailsOpen = false;
            SelectedLicense = null;
        }

        [RelayCommand]
        public void ExportCsv()
        {
            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("Kode Lisensi,Tier,Catatan Pembeli,Instansi Terkait,Status,Dibuat Pada,Diaktifkan Pada");
                foreach (var l in FilteredLicenses)
                {
                    sb.AppendLine($"\"{l.LicenseKey}\",\"{l.TierId}\",\"{l.BuyerNote}\",\"{l.InstitutionName}\",\"{l.Status}\",\"{l.CreatedAt}\",\"{l.ActivatedAt}\"");
                }

                string exportPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), $"Laporan_Lisensi_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
                File.WriteAllText(exportPath, sb.ToString(), Encoding.UTF8);

                ShowNotification?.Invoke("Ekspor Berhasil", $"File CSV berhasil disimpan di Desktop:\n{Path.GetFileName(exportPath)}");
            }
            catch (Exception ex)
            {
                ShowNotification?.Invoke("Gagal Ekspor CSV", ex.Message);
            }
        }
    }
}
