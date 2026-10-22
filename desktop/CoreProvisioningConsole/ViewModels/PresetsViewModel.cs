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
    public partial class PresetsViewModel : ObservableObject
    {
        private readonly MasterDevService _masterService;

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private string _selectedTypeFilter = "Semua Tipe";

        public ObservableCollection<DevPresetModel> AllPresets { get; } = new();
        public ObservableCollection<DevPresetModel> FilteredPresets { get; } = new();

        public ObservableCollection<string> TypeFilterOptions { get; } = new()
        {
            "Semua Tipe", "school", "village_gov", "corporate", "office_agency", "all"
        };

        public event Action<string, string>? ShowNotification;

        public PresetsViewModel(MasterDevService masterService)
        {
            _masterService = masterService;
        }

        partial void OnSelectedTypeFilterChanged(string value) => ApplyFilter();

        [RelayCommand]
        public async Task LoadPresetsAsync()
        {
            IsLoading = true;
            try
            {
                var res = await _masterService.GetPresetsAsync();
                if (res.Success && res.Data != null)
                {
                    AllPresets.Clear();
                    foreach (var p in res.Data)
                    {
                        AllPresets.Add(p);
                    }
                    ApplyFilter();
                    ShowNotification?.Invoke("Pustaka Template", $"Memuat {AllPresets.Count} format master preset kedinasan.");
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
            var query = AllPresets.AsEnumerable();
            if (SelectedTypeFilter != "Semua Tipe")
            {
                query = query.Where(p => p.TargetInstitutionType.Equals(SelectedTypeFilter, StringComparison.OrdinalIgnoreCase));
            }

            FilteredPresets.Clear();
            foreach (var item in query)
            {
                FilteredPresets.Add(item);
            }
        }
    }
}
