using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using ClientApp.Models;
using ClientApp.Services;
using Wpf.Ui.Controls;
using MessageBox = System.Windows.MessageBox;
using MessageBoxButton = System.Windows.MessageBoxButton;
using MessageBoxImage = System.Windows.MessageBoxImage;

namespace ClientApp
{
    public partial class AuthWindow : FluentWindow
    {
        private LicenseVerifyResult? _verifiedLicense;
        private string? _selectedLogoBase64;

        public AuthWindow()
        {
            InitializeComponent();
        }

        private void OnTabChanged(object sender, RoutedEventArgs e)
        {
            if (LoginPanel == null || RegisterPanel == null) return;

            if (TabLoginRadio.IsChecked == true)
            {
                LoginPanel.Visibility = Visibility.Visible;
                RegisterPanel.Visibility = Visibility.Collapsed;
            }
            else
            {
                LoginPanel.Visibility = Visibility.Collapsed;
                RegisterPanel.Visibility = Visibility.Visible;
            }
        }



        #region Login Handling
        private async void OnBtnLoginClick(object sender, RoutedEventArgs e)
        {
            LoginAlertBox.Visibility = Visibility.Collapsed;
            var tenantCode = TxtLoginTenantCode.Text.Trim();
            var email = TxtLoginEmail.Text.Trim();
            var password = TxtLoginPassword.Password;

            if (string.IsNullOrWhiteSpace(tenantCode) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                ShowLoginError("Harap isi Kode Instansi, Email/Username, dan Kata Sandi.");
                return;
            }

            try
            {
                var result = await App.ApiClient.LoginAsync(tenantCode, email, password);
                if (result.Success && result.Data != null)
                {
                    var mainWin = new MainWindow();
                    mainWin.Show();
                    Close();
                }
                else
                {
                    ShowLoginError(result.Message ?? result.Error ?? "Kombinasi login tidak cocok.");
                }
            }
            catch (Exception ex)
            {
                ShowLoginError($"Gagal menghubungi server: {ex.Message}");
            }
        }

        private void ShowLoginError(string message)
        {
            LoginAlertText.Text = message;
            LoginAlertBox.Visibility = Visibility.Visible;
        }
        #endregion

        #region License Verification & Registration Handling
        private async void OnVerifyLicenseClick(object sender, RoutedEventArgs e)
        {
            LicenseVerifiedBox.Visibility = Visibility.Collapsed;
            LicenseErrorBox.Visibility = Visibility.Collapsed;
            RegistrationFormBox.IsEnabled = false;
            RegistrationFormBox.Opacity = 0.5;

            var key = TxtLicenseKeyInput.Text.Trim();
            if (string.IsNullOrEmpty(key))
            {
                TxtLicenseErrorDetails.Text = "Harap masukkan kode lisensi.";
                LicenseErrorBox.Visibility = Visibility.Visible;
                return;
            }

            try
            {
                var res = await App.ApiClient.VerifyLicenseAsync(key);
                if (res.Success && res.Data != null)
                {
                    _verifiedLicense = res.Data;
                    TxtLicenseBadgeTitle.Text = $"✅ LISENSI VALID: {_verifiedLicense.Tier.DisplayName.ToUpper()}";
                    TxtLicenseBadgeDetails.Text = $"Maks. {_verifiedLicense.EffectiveMaxUsers} Pengguna • Kuota Cloud {_verifiedLicense.EffectiveMaxStorageMb} MB • {(_verifiedLicense.Tier.AllowRabModule == 1 ? "Modul RAB Aktif" : "Tanpa Modul RAB")}";
                    LicenseVerifiedBox.Visibility = Visibility.Visible;

                    RegistrationFormBox.IsEnabled = true;
                    RegistrationFormBox.Opacity = 1.0;
                }
                else
                {
                    TxtLicenseErrorDetails.Text = res.Message ?? "Kode lisensi tidak valid atau sudah digunakan.";
                    LicenseErrorBox.Visibility = Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                TxtLicenseErrorDetails.Text = $"Koneksi error: {ex.Message}";
                LicenseErrorBox.Visibility = Visibility.Visible;
            }
        }

        private void OnSelectLogoClick(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Filter = "File Gambar (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg",
                Title = "Pilih Logo Resmi Instansi"
            };

            if (dlg.ShowDialog() == true)
            {
                try
                {
                    var fileInfo = new FileInfo(dlg.FileName);
                    TxtLogoFileName.Text = $"{fileInfo.Name} ({fileInfo.Length / 1024} KB)";

                    var compressedBytes = ImageCompressionService.CompressForDocumentArchive(dlg.FileName, 600, 75);
                    _selectedLogoBase64 = "data:image/jpeg;base64," + Convert.ToBase64String(compressedBytes);

                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.StreamSource = new MemoryStream(compressedBytes);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    ImgLogoPreview.Source = bitmap;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Gagal memuat logo: {ex.Message}", "Peringatan", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        private async void OnRegisterSubmitClick(object sender, RoutedEventArgs e)
        {
            if (_verifiedLicense == null)
            {
                MessageBox.Show("Harap verifikasi kode lisensi terlebih dahulu.", "Peringatan", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var adminName = TxtRegAdminName.Text.Trim();
            var instName = TxtRegInstitutionName.Text.Trim();
            var address = TxtRegAddress.Text.Trim();
            var phone = TxtRegPhone.Text.Trim();
            var email = TxtRegEmail.Text.Trim();
            var pass = TxtRegPassword.Password;
            var confirmPass = TxtRegConfirmPassword.Password;

            var instTypeItem = CmbRegInstitutionType.SelectedItem as ComboBoxItem;
            var instType = instTypeItem?.Tag?.ToString() ?? "school";

            if (string.IsNullOrWhiteSpace(adminName) || string.IsNullOrWhiteSpace(instName) ||
                string.IsNullOrWhiteSpace(address) || string.IsNullOrWhiteSpace(phone) ||
                string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(pass))
            {
                MessageBox.Show("Seluruh kolom wajib diisi lengkap.", "Validasi Formulir", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (pass != confirmPass)
            {
                MessageBox.Show("Kata sandi dan konfirmasi kata sandi tidak cocok.", "Validasi Formulir", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (pass.Length < 6)
            {
                MessageBox.Show("Kata sandi minimal 6 karakter.", "Validasi Formulir", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var req = new RegisterTenantRequest
            {
                LicenseKey = _verifiedLicense.LicenseKey,
                AdminName = adminName,
                InstitutionName = instName,
                InstitutionAddress = address,
                InstitutionType = instType,
                PhoneNumber = phone,
                Email = email,
                Password = pass,
                LogoUrl = _selectedLogoBase64
            };

            try
            {
                var res = await App.ApiClient.RegisterTenantAsync(req);
                if (res.Success && res.Data != null)
                {
                    MessageBox.Show($"Pendaftaran instansi '{instName}' berhasil!\nKode Instansi Anda: {res.Data.Tenant.InstitutionCode}\n\nSelamat datang di Sovereign Executive Suite.", "Registrasi Sukses", MessageBoxButton.OK, MessageBoxImage.Information);

                    var mainWin = new MainWindow();
                    mainWin.Show();
                    Close();
                }
                else
                {
                    MessageBox.Show(res.Message ?? res.Error ?? "Gagal mendaftarkan instansi.", "Registrasi Gagal", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Terjadi kesalahan: {ex.Message}", "Kesalahan Sistem", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnContactHelpdeskClick(object sender, RoutedEventArgs e)
        {
            string tCode = TxtLoginTenantCode.Text.Trim();
            SupportGatewayService.OpenSupportChannel(SupportIntent.TechnicalAssistance, string.IsNullOrWhiteSpace(tCode) ? null : $"Instansi Kode: {tCode}", App.ApiClient);
        }

        private void OnOrderLicenseClick(object sender, RoutedEventArgs e)
        {
            SupportGatewayService.OpenSupportChannel(SupportIntent.NewLicenseOrder, "Permohonan Kode Lisensi Baru", App.ApiClient);
        }
        #endregion
    }
}
