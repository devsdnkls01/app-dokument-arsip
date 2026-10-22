using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DeveloperStudio.Services;
using Wpf.Ui.Appearance;

namespace CoreProvisioningConsole.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly MasterDevService _masterService;

        public LicensesViewModel LicensesVM { get; }
        public TiersViewModel TiersVM { get; }
        public BroadcastViewModel BroadcastVM { get; }
        public TenantsViewModel TenantsVM { get; }
        public RescueViewModel RescueVM { get; }
        public PresetsViewModel PresetsVM { get; }

        [ObservableProperty]
        private string _activeViewTag = "licenses";

        [ObservableProperty]
        private string _serverStatusText = "Node Edge Cloudflare D1 Terhubung";

        [ObservableProperty]
        private bool _isServerOnline = true;

        [ObservableProperty]
        private string _authModeText = "Master Key Terverifikasi";

        [ObservableProperty]
        private string _appVersionText = "v3.0.0 Enterprise";

        [ObservableProperty]
        private bool _isDarkTheme;

        public event Action<string, string>? OnShowSnackbar;

        public MainViewModel(
            MasterDevService masterService,
            LicensesViewModel licensesVM,
            TiersViewModel tiersVM,
            BroadcastViewModel broadcastVM,
            TenantsViewModel tenantsVM,
            RescueViewModel rescueVM,
            PresetsViewModel presetsVM)
        {
            _masterService = masterService;
            LicensesVM = licensesVM;
            TiersVM = tiersVM;
            BroadcastVM = broadcastVM;
            TenantsVM = tenantsVM;
            RescueVM = rescueVM;
            PresetsVM = presetsVM;

            // Route notifications to main window snackbar
            LicensesVM.ShowNotification += TriggerNotification;
            TiersVM.ShowNotification += TriggerNotification;
            BroadcastVM.ShowNotification += TriggerNotification;
            TenantsVM.ShowNotification += TriggerNotification;
            RescueVM.ShowNotification += TriggerNotification;
            PresetsVM.ShowNotification += TriggerNotification;
        }

        private void TriggerNotification(string title, string message)
        {
            OnShowSnackbar?.Invoke(title, message);
        }

        [RelayCommand]
        public async Task InitializeAsync()
        {
            await LicensesVM.LoadLicensesAsync();
            await TiersVM.LoadTiersAsync();
            await BroadcastVM.LoadBroadcastAsync();
            await TenantsVM.LoadTenantsAsync();
            await RescueVM.LoadTenantsAsync();
            await PresetsVM.LoadPresetsAsync();
        }

        [RelayCommand]
        public void ToggleTheme()
        {
            IsDarkTheme = !IsDarkTheme;
            ApplicationThemeManager.Apply(IsDarkTheme ? ApplicationTheme.Dark : ApplicationTheme.Light);
        }

        [RelayCommand]
        public void Navigate(string? targetTag)
        {
            if (string.IsNullOrWhiteSpace(targetTag)) return;
            ActiveViewTag = targetTag;
        }
    }
}
