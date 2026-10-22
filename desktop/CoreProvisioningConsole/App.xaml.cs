using System;
using System.Windows;
using CoreProvisioningConsole.ViewModels;
using DeveloperStudio.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Wpf.Ui.Appearance;

namespace DeveloperStudio
{
    public partial class App : Application
    {
        private static IHost _host = null!;

        public static MasterDevService MasterService => _host.Services.GetRequiredService<MasterDevService>();
        public static MainViewModel MainVM => _host.Services.GetRequiredService<MainViewModel>();

        public App()
        {
            _host = Host.CreateDefaultBuilder()
                .ConfigureServices((context, services) =>
                {
                    // Services
                    services.AddSingleton<MasterDevService>();

                    // ViewModels
                    services.AddSingleton<LicensesViewModel>();
                    services.AddSingleton<TiersViewModel>();
                    services.AddSingleton<BroadcastViewModel>();
                    services.AddSingleton<TenantsViewModel>();
                    services.AddSingleton<RescueViewModel>();
                    services.AddSingleton<PresetsViewModel>();
                    services.AddSingleton<MainViewModel>();

                    // Views
                    services.AddTransient<DevMainWindow>();
                })
                .Build();
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            ApplicationThemeManager.Apply(ApplicationTheme.Light);

            AppDomain.CurrentDomain.UnhandledException += (s, args) =>
            {
                var ex = args.ExceptionObject as Exception;
                MessageBox.Show($"Kesalahan Sistem Provisioning: {ex?.Message}", "Peringatan Sistem", MessageBoxButton.OK, MessageBoxImage.Error);
            };

            var mainWindow = _host.Services.GetRequiredService<DevMainWindow>();
            mainWindow.Show();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _host.Dispose();
            base.OnExit(e);
        }
    }
}
