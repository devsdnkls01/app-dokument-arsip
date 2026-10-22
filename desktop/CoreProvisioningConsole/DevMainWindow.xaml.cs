using System;
using System.Windows;
using System.Windows.Controls;
using CoreProvisioningConsole.ViewModels;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace DeveloperStudio
{
    public partial class DevMainWindow : FluentWindow
    {
        private readonly MainViewModel _viewModel;

        public DevMainWindow()
        {
            InitializeComponent();

            _viewModel = App.MainVM;
            DataContext = _viewModel;

            _viewModel.OnShowSnackbar += (title, message) =>
            {
                Dispatcher.Invoke(() =>
                {
                    var snackbar = new Snackbar(MainSnackbarPresenter)
                    {
                        Title = title,
                        Content = message,
                        Timeout = TimeSpan.FromSeconds(4)
                    };
                    snackbar.Show();
                });
            };

            Loaded += async (s, e) =>
            {
                await _viewModel.InitializeAsync();
            };
        }

        private void OnNavSelectionChanged(NavigationView sender, RoutedEventArgs args)
        {
            if (sender.SelectedItem is NavigationViewItem item && item.Tag is string tag)
            {
                SwitchView(tag);
            }
        }

        private void SwitchView(string tag)
        {
            if (ViewLicenses == null) return;

            ViewLicenses.Visibility = tag == "licenses" ? Visibility.Visible : Visibility.Collapsed;
            ViewTiers.Visibility = tag == "tiers" ? Visibility.Visible : Visibility.Collapsed;
            ViewBroadcast.Visibility = tag == "broadcast" ? Visibility.Visible : Visibility.Collapsed;
            ViewTenants.Visibility = tag == "tenants" ? Visibility.Visible : Visibility.Collapsed;
            ViewRescue.Visibility = tag == "rescue" ? Visibility.Visible : Visibility.Collapsed;
            ViewPresets.Visibility = tag == "presets" ? Visibility.Visible : Visibility.Collapsed;
        }

        private void OnToggleThemeClick(object sender, RoutedEventArgs e)
        {
            var current = ApplicationThemeManager.GetAppTheme();
            var next = current == ApplicationTheme.Dark ? ApplicationTheme.Light : ApplicationTheme.Dark;
            ApplicationThemeManager.Apply(next);
        }
    }
}
