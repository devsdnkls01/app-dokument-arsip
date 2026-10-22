using System;
using System.Windows;
using ClientApp.Services;

namespace ClientApp
{
    public partial class App : Application
    {
        public static ApiClientService ApiClient { get; private set; } = null!;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            AppDomain.CurrentDomain.UnhandledException += (s, args) =>
            {
                var ex = args.ExceptionObject as Exception;
                MessageBox.Show($"Terjadi kesalahan sistem: {ex?.Message}", "Kesalahan Fatal", MessageBoxButton.OK, MessageBoxImage.Error);
            };

            DispatcherUnhandledException += (s, args) =>
            {
                MessageBox.Show($"Terjadi kesalahan: {args.Exception.Message}", "Pemberitahuan Sistem", MessageBoxButton.OK, MessageBoxImage.Warning);
                args.Handled = true;
            };

            ApiClient = new ApiClientService();

            if (SessionService.IsLoggedIn())
            {
                var mainWin = new MainWindow();
                mainWin.Show();
            }
            else
            {
                var authWin = new AuthWindow();
                authWin.Show();
            }
        }
    }
}
