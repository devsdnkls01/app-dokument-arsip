using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;
using SuratApp.Models;
using SuratApp.Services;

namespace SuratApp
{
    public partial class MainWindow : Window
    {
        private readonly DynamicFormService _formService;
        private CategoryModel? _selectedCategory;
        private DocumentModel? _selectedDocument;
        private string? _stagedScanFilePath;
        private AppVersionInfo? _latestVersion;
        private const string CurrentAppVersion = "1.0.0";

        public MainWindow()
        {
            InitializeComponent();
            _formService = new DynamicFormService(TxtApiUrl.Text.Trim(), TxtAppPin.Password);

            Loaded += async (s, e) =>
            {
                await InitializeDataAsync();
            };
        }

        // =========================================================
        // INITIALIZATION & CONNECTION
        // =========================================================

        private async void BtnConnect_Click(object sender, RoutedEventArgs e)
        {
            _formService.SetBaseUrl(TxtApiUrl.Text.Trim());
            _formService.SetPin(TxtAppPin.Password);
            await InitializeDataAsync();
        }

        private async Task InitializeDataAsync()
        {
            SetStatus("Menghubungkan ke API Worker...");
            ShowProgress(true);

            try
            {
                // 1. Cek Pembaruan Versi Aplikasi dari D1
                await CheckAppVersionAsync();

                // 2. Muat Kategori
                var categories = await _formService.GetCategoriesAsync();
                ListCategories.ItemsSource = categories;

                if (categories.Count > 0)
                {
                    ListCategories.SelectedIndex = 0;
                }

                // 3. Muat Riwayat Dokumen
                await RefreshDocumentsListAsync();

                SetStatus($"Terhubung. {categories.Count} kategori dimuat.");
            }
            catch (Exception ex)
            {
                SetStatus($"Koneksi gagal: {ex.Message}");
                MessageBox.Show(
                    $"Gagal terhubung ke Worker API:\n{ex.Message}\n\nPastikan backend sedang aktif di {TxtApiUrl.Text}.",
                    "Koneksi Gagal", 
                    MessageBoxButton.OK, 
                    MessageBoxImage.Warning
                );
            }
            finally
            {
                ShowProgress(false);
            }
        }

        private async Task CheckAppVersionAsync()
        {
            _latestVersion = await _formService.CheckLatestVersionAsync();
            if (_latestVersion != null && _latestVersion.Version != CurrentAppVersion)
            {
                TxtUpdateMsg.Text = $"Versi {_latestVersion.Version} telah rilis: {_latestVersion.ReleaseNotes}";
                BannerUpdate.Visibility = Visibility.Visible;
            }
            else
            {
                BannerUpdate.Visibility = Visibility.Collapsed;
            }
        }

        private void BtnDownloadUpdate_Click(object sender, RoutedEventArgs e)
        {
            if (_latestVersion != null && !string.IsNullOrEmpty(_latestVersion.DownloadUrl))
            {
                OpenUrlOrFile(_latestVersion.DownloadUrl);
            }
        }

        private void BtnDismissUpdate_Click(object sender, RoutedEventArgs e)
        {
            BannerUpdate.Visibility = Visibility.Collapsed;
        }

        private async Task RefreshDocumentsListAsync(string? query = null)
        {
            try
            {
                var docs = await _formService.GetDocumentsAsync(query);
                ListDocuments.ItemsSource = docs;
            }
            catch (Exception ex)
            {
                SetStatus($"Gagal refresh riwayat: {ex.Message}");
            }
        }

        private async void BtnRefreshDocs_Click(object sender, RoutedEventArgs e)
        {
            ShowProgress(true);
            await RefreshDocumentsListAsync(TxtSearchDoc.Text.Trim());
            ShowProgress(false);
        }

        private async void TxtSearchDoc_TextChanged(object sender, TextChangedEventArgs e)
        {
            var q = TxtSearchDoc.Text.Trim();
            await RefreshDocumentsListAsync(string.IsNullOrEmpty(q) ? null : q);
        }

        // =========================================================
        // PANEL 1: SELECTION HANDLERS & TEMPLATE AUTO-SCANNER
        // =========================================================

        private void ListCategories_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selectedCategory = ListCategories.SelectedItem as CategoryModel;
            if (_selectedCategory == null) return;

            TxtSelectedCategoryTitle.Text = _selectedCategory.Name;
            TxtSelectedCategoryDesc.Text = _selectedCategory.Description ?? $"Kode: {_selectedCategory.Code}";

            _formService.BuildDynamicForm(DynamicFormContainer, _selectedCategory.Fields);
            SetStatus($"Kategori '{_selectedCategory.Name}' siap ({_selectedCategory.Fields.Count} kolom).");
        }

        private void ListDocuments_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selectedDocument = ListDocuments.SelectedItem as DocumentModel;
            if (_selectedDocument == null) return;

            TxtTargetDocNumber.Text = _selectedDocument.DocumentNumber;
            TxtTargetDocId.Text = $"ID: {_selectedDocument.Id}";
            
            if (_selectedDocument.HasScan)
            {
                TxtExistingScanStatus.Text = "Status: ✓ Lengkap (Scan Basah Terarsip)";
                TxtExistingScanStatus.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#059669"));
            }
            else
            {
                TxtExistingScanStatus.Text = "Status: ⚠️ Belum Ada Bukti Scan Basah";
                TxtExistingScanStatus.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DC2626"));
            }
        }

        // Auto-Scanner: Memindai tag {{variabel}} langsung dari master .docx baru
        private void BtnScanNewDocx_Click(object sender, RoutedEventArgs e)
        {
            var openDlg = new OpenFileDialog
            {
                Filter = "Word Document (*.docx)|*.docx",
                Title = "Pilih Master Template .docx untuk Dipindai Otomatis"
            };

            if (openDlg.ShowDialog() == true)
            {
                try
                {
                    var placeholders = LocalDraftService.ScanPlaceholdersFromDocx(openDlg.FileName);
                    if (placeholders.Count == 0)
                    {
                        MessageBox.Show("Tidak ditemukan tag placeholder {{variabel}} dalam dokumen ini.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }

                    var newCat = new CategoryModel
                    {
                        Id = $"custom-{DateTime.Now:yyyyMMddHHmm}",
                        Name = Path.GetFileNameWithoutExtension(openDlg.FileName),
                        Code = "CUSTOM",
                        Description = $"Template lokal: {openDlg.FileName}",
                        TemplateCloudinaryUrl = openDlg.FileName
                    };

                    int order = 1;
                    foreach (var p in placeholders)
                    {
                        var isDate = p.Contains("tgl", StringComparison.OrdinalIgnoreCase) || p.Contains("date", StringComparison.OrdinalIgnoreCase);
                        var isArea = p.Contains("keperluan", StringComparison.OrdinalIgnoreCase) || p.Contains("alamat", StringComparison.OrdinalIgnoreCase);

                        newCat.Fields.Add(new FormFieldModel
                        {
                            Id = $"fld-{order}",
                            CategoryId = newCat.Id,
                            FieldKey = p,
                            Label = ToTitleCase(p.Replace("_", " ")),
                            FieldType = isDate ? "date" : (isArea ? "textarea" : "text"),
                            IsRequired = 1,
                            DisplayOrder = order++
                        });
                    }

                    var currentList = (ListCategories.ItemsSource as List<CategoryModel>) ?? new List<CategoryModel>();
                    var updated = new List<CategoryModel>(currentList) { newCat };
                    ListCategories.ItemsSource = updated;
                    ListCategories.SelectedItem = newCat;

                    MessageBox.Show(
                        $"Berhasil memindai {placeholders.Count} placeholder variabel:\n\n" + string.Join(", ", placeholders),
                        "Auto-Scanner Sukses", 
                        MessageBoxButton.OK, 
                        MessageBoxImage.Information
                    );
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Gagal memindai dokumen: {ex.Message}", "Kesalahan", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // =========================================================
        // PANEL 2: AUTO NUMBERING, DRAF OFFLINE & GENERATOR
        // =========================================================

        private async void BtnAutoNumber_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedCategory == null)
            {
                MessageBox.Show("Pilih kategori surat terlebih dahulu.", "Peringatan", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                ShowProgress(true);
                SetStatus("Mengambil nomor urut otomatis dari database server...");

                var nextNum = await _formService.GetNextDocumentNumberAsync(_selectedCategory.Id);
                
                // Cari kontrol nomor_surat di form dinamis dan isi otomatis
                var values = new Dictionary<string, object>
                {
                    { "nomor_surat", nextNum.Number }
                };
                _formService.PopulateValues(values);

                SetStatus($"Nomor surat otomatis berhasil didapatkan: {nextNum.Number}");
                MessageBox.Show($"Nomor surat resmi berhasil dialokasikan secara atomic:\n\n{nextNum.Number}", "Penomoran Otomatis", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Gagal mengambil nomor otomatis:\n{ex.Message}", "Kesalahan", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                ShowProgress(false);
            }
        }

        private void BtnSaveDraft_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedCategory == null) return;

            var (_, _, values) = _formService.ExtractAndValidateForm();
            string docNumber = values.TryGetValue("nomor_surat", out var no) ? no.ToString() ?? "DRAF" : "DRAF-TANPA-NOMOR";

            LocalDraftService.SaveDraft(_selectedCategory.Id, _selectedCategory.Name, docNumber, values);
            SetStatus($"Draf untuk surat '{docNumber}' berhasil disimpan ke penyimpanan offline.");
            MessageBox.Show($"Draf berhasil disimpan secara lokal ke komputer ini.\nAnda dapat melanjutkan pengisian kapan saja meskipun tidak terhubung internet.", "Draf Tersimpan", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private async void BtnGenerateAndArchive_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedCategory == null)
            {
                MessageBox.Show("Pilih kategori dokumen terlebih dahulu.", "Peringatan", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var (isValid, errors, values) = _formService.ExtractAndValidateForm();
            if (!isValid)
            {
                MessageBox.Show(string.Join("\n", errors), "Validasi Form Gagal", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string docNumber = values.TryGetValue("nomor_surat", out var no) ? no.ToString() ?? "SURAT" : $"DOC-{DateTime.Now:yyyyMMddHHmm}";
            string docTitle = $"{_selectedCategory.Name} ({docNumber})";
            string docId = $"doc-{DateTime.Now:yyyyMMddHHmmss}-{Guid.NewGuid():N}".Substring(0, 24);
            string verificationUrl = $"{_formService.BaseUrl}/verify/{docId}";

            ShowProgress(true);
            SetStatus("1/3 Membuat berkas .docx lokal & menyematkan QR Code validasi keaslian...");

            try
            {
                string templateSource = GetTemplatePathForCategory(_selectedCategory);
                string outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GeneratedDocuments");

                // Generate docx lokal dengan QR Code
                string generatedDocxPath = await _formService.GenerateDocumentLocalAsync(
                    templateSource, 
                    outputDir, 
                    docNumber, 
                    values,
                    ChkEnableQr.IsChecked == true ? verificationUrl : null
                );

                // Upload Cloudinary
                SetStatus("2/3 Mengunggah berkas .docx ke Cloudinary (archives/docx)...");
                var signatureData = await _formService.GetUploadSignatureAsync("archives/docx");
                var uploadResult = await _formService.UploadToCloudinaryDirectAsync(generatedDocxPath, signatureData);

                // Catat ke D1
                SetStatus("3/3 Mencatat riwayat dokumen ke database D1...");
                var createdDoc = await _formService.SaveDocumentToBackendAsync(
                    _selectedCategory.Id,
                    docNumber,
                    docTitle,
                    values,
                    uploadResult.SecureUrl,
                    uploadResult.PublicId,
                    verificationUrl
                );

                await RefreshDocumentsListAsync();

                _selectedDocument = createdDoc;
                TxtTargetDocNumber.Text = createdDoc.DocumentNumber;
                TxtTargetDocId.Text = $"ID: {createdDoc.Id}";
                TxtExistingScanStatus.Text = "Status: ⚠️ Belum Ada Bukti Scan Basah";
                TxtExistingScanStatus.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DC2626"));

                SetStatus("Selesai! Dokumen berhasil digenerate, disematkan QR, dan diarsipkan.");

                var result = MessageBox.Show(
                    $"Dokumen nomor '{docNumber}' berhasil dibuat dan diarsipkan!\n\nURL Validasi QR:\n{verificationUrl}\n\nIngin membuka file .docx lokal sekarang?",
                    "Generate Berhasil",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Information
                );

                if (result == MessageBoxResult.Yes)
                {
                    OpenUrlOrFile(generatedDocxPath);
                }
            }
            catch (Exception ex)
            {
                SetStatus($"Terjadi kesalahan: {ex.Message}");
                MessageBox.Show($"Proses gagal:\n{ex.Message}", "Kesalahan", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                ShowProgress(false);
            }
        }

        // =========================================================
        // PANEL 3: PHYSICAL SCAN DROPZONE & QR VERIFIKASI
        // =========================================================

        private void DropZone_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Copy;
                DropZoneBorder.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2563EB"));
                DropZoneBorder.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EFF6FF"));
            }
            e.Handled = true;
        }

        private void DropZone_DragLeave(object sender, DragEventArgs e)
        {
            ResetDropZoneVisual();
        }

        private void DropZone_Drop(object sender, DragEventArgs e)
        {
            ResetDropZoneVisual();

            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files != null && files.Length > 0)
                {
                    StageScanFile(files[0]);
                }
            }
        }

        private void BtnBrowseScanFile_Click(object sender, RoutedEventArgs e)
        {
            var openDlg = new OpenFileDialog
            {
                Filter = "Berkas Scan (*.pdf;*.jpg;*.jpeg;*.png)|*.pdf;*.jpg;*.jpeg;*.png",
                Title = "Pilih Berkas Scan Fisik Berstempel Basah"
            };

            if (openDlg.ShowDialog() == true)
            {
                StageScanFile(openDlg.FileName);
            }
        }

        private void StageScanFile(string filePath)
        {
            var ext = Path.GetExtension(filePath).ToLowerInvariant();
            var validExts = new[] { ".pdf", ".jpg", ".jpeg", ".png" };

            if (!validExts.Contains(ext))
            {
                MessageBox.Show("Harap pilih berkas berformat PDF, JPG, JPEG, atau PNG.", "Format Tidak Didukung", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            _stagedScanFilePath = filePath;
            var fileInfo = new FileInfo(filePath);
            var sizeKb = fileInfo.Length / 1024;

            TxtDroppedFileName.Text = $"📄 {Path.GetFileName(filePath)} ({sizeKb} KB)";
            SetStatus($"Berkas scan fisik '{Path.GetFileName(filePath)}' siap diunggah.");
        }

        private async void BtnUploadScan_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedDocument == null)
            {
                MessageBox.Show("Pilih dokumen target dari 'Riwayat Dokumen' terlebih dahulu.", "Peringatan", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrEmpty(_stagedScanFilePath) || !File.Exists(_stagedScanFilePath))
            {
                MessageBox.Show("Pilih atau tarik file scan fisik ke dropzone terlebih dahulu.", "Peringatan", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            ShowProgress(true);
            SetStatus("Mengunggah & mengompresi otomatis berkas scan basah ke Cloudinary...");

            try
            {
                var signatureData = await _formService.GetUploadSignatureAsync("archives/scans");
                var uploadResult = await _formService.UploadToCloudinaryDirectAsync(_stagedScanFilePath, signatureData);

                SetStatus("Menautkan URL scan basah ke catatan D1...");
                var updatedDoc = await _formService.LinkScanDocumentAsync(
                    _selectedDocument.Id,
                    uploadResult.SecureUrl,
                    uploadResult.PublicId
                );

                _selectedDocument = updatedDoc;
                TxtExistingScanStatus.Text = "Status: ✓ Lengkap (Scan Basah Terarsip)";
                TxtExistingScanStatus.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#059669"));

                await RefreshDocumentsListAsync(TxtSearchDoc.Text.Trim());

                SetStatus("Scan basah berhasil diarsipkan & ditautkan!");
                MessageBox.Show(
                    $"Berkas scan stempel basah berhasil ditautkan ke surat nomor '{_selectedDocument.DocumentNumber}'!\n\nStatus arsip fisik kini: LENGKAP",
                    "Arsip Lengkap",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );

                _stagedScanFilePath = null;
                TxtDroppedFileName.Text = string.Empty;
            }
            catch (Exception ex)
            {
                SetStatus($"Gagal menautkan scan: {ex.Message}");
                MessageBox.Show($"Unggah scan gagal:\n{ex.Message}", "Kesalahan", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                ShowProgress(false);
            }
        }

        private void LnkViewDocx_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedDocument != null && !string.IsNullOrEmpty(_selectedDocument.DocxCloudinaryUrl))
            {
                OpenUrlOrFile(_selectedDocument.DocxCloudinaryUrl);
            }
        }

        private void LnkViewScan_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedDocument != null && !string.IsNullOrEmpty(_selectedDocument.ScanCloudinaryUrl))
            {
                OpenUrlOrFile(_selectedDocument.ScanCloudinaryUrl);
            }
            else
            {
                MessageBox.Show("Dokumen ini belum memiliki berkas scan fisik stempel basah.", "Informasi", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void LnkVerifyPublic_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedDocument != null)
            {
                var verifyUrl = $"{_formService.BaseUrl}/verify/{_selectedDocument.Id}";
                OpenUrlOrFile(verifyUrl);
            }
        }

        // =========================================================
        // EKSPOR BUKU AGENDA SURAT KELUAR (EXCEL / CSV)
        // =========================================================

        private async void BtnExportAgenda_Click(object sender, RoutedEventArgs e)
        {
            var saveDlg = new SaveFileDialog
            {
                Filter = "File Excel / CSV (*.csv)|*.csv",
                FileName = $"Buku_Agenda_Surat_Keluar_{DateTime.Now:yyyyMM}.csv",
                Title = "Simpan Rekapitulasi Buku Agenda Surat Keluar"
            };

            if (saveDlg.ShowDialog() == true)
            {
                try
                {
                    ShowProgress(true);
                    SetStatus("Mengekspor data buku agenda...");

                    var filePath = await _formService.ExportAgendaBookCsvAsync(saveDlg.FileName);

                    SetStatus($"Buku agenda berhasil diekspor ke: {Path.GetFileName(filePath)}");

                    var result = MessageBox.Show(
                        $"Laporan buku agenda surat keluar berhasil diekspor!\n\nLokasi:\n{filePath}\n\nIngin membuka file di Excel sekarang?",
                        "Ekspor Berhasil",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Information
                    );

                    if (result == MessageBoxResult.Yes)
                    {
                        OpenUrlOrFile(filePath);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Gagal ekspor agenda:\n{ex.Message}", "Kesalahan", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    ShowProgress(false);
                }
            }
        }

        // =========================================================
        // UTILITIES
        // =========================================================

        private string GetTemplatePathForCategory(CategoryModel category)
        {
            if (File.Exists(category.TemplateCloudinaryUrl))
            {
                return category.TemplateCloudinaryUrl;
            }

            string localTemplate = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Templates", $"{category.Code}.docx");
            if (File.Exists(localTemplate))
            {
                return localTemplate;
            }

            return category.TemplateCloudinaryUrl;
        }

        private void SetStatus(string message) => TxtStatusBar.Text = message;

        private void ShowProgress(bool show)
        {
            FormProgressBar.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
            FormProgressBar.IsIndeterminate = show;
        }

        private void ResetDropZoneVisual()
        {
            DropZoneBorder.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94A3B8"));
            DropZoneBorder.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F8FAFC"));
        }

        private void OpenUrlOrFile(string pathOrUrl)
        {
            try
            {
                Process.Start(new ProcessStartInfo { FileName = pathOrUrl, UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Tidak dapat membuka tautan/file: {ex.Message}", "Perhatian", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private static string ToTitleCase(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            var words = text.Split(' ');
            for (int i = 0; i < words.Length; i++)
            {
                if (words[i].Length > 0)
                {
                    words[i] = char.ToUpper(words[i][0]) + words[i].Substring(1).ToLower();
                }
            }
            return string.Join(" ", words);
        }
    }
}
