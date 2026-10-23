using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using ClientApp.Models;
using ClientApp.Services;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;
using MessageBox = System.Windows.MessageBox;
using MessageBoxButton = System.Windows.MessageBoxButton;
using MessageBoxImage = System.Windows.MessageBoxImage;
using MessageBoxResult = System.Windows.MessageBoxResult;
using Button = System.Windows.Controls.Button;

namespace ClientApp
{
    public partial class MainWindow : FluentWindow
    {
        private ObservableCollection<RabItemModel> _rabItems = new();
        private List<CategoryModel> _categories = new();
        private List<EntityRecordModel> _personnelRecords = new();
        private List<DocumentModel> _allDocuments = new();
        private CategoryModel? _currentSelectedCategory;
        private NextNumberResponse? _currentNumberInfo;
        private string? _editingDocumentId;
        private SystemBroadcastModel? _broadcast;

        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            DgRabGrid.ItemsSource = _rabItems;
            ApplySessionAndChameleonTheme();
            await LoadBroadcastAndPromosAsync();
            await LoadDashboardDataAsync();
        }

        #region Session & Adaptive Chameleon Theme
        private void ApplySessionAndChameleonTheme()
        {
            var session = App.ApiClient.Session;
            if (session == null) return;

            var tenant = session.Tenant;
            var user = session.User;
            var tier = session.Tier;

            // Sidebar Identity
            TxtSidebarInstitutionName.Text = tenant.InstitutionName;
            TxtSidebarUserName.Text = user.FullName;
            TxtSidebarUserEmail.Text = user.EmailUsername;
            TxtSidebarTenantPill();
            TxtHeaderTenantPill.Text = $"KODE: {tenant.InstitutionCode}";

            // Role Badge
            TxtSidebarUserRole.Text = user.IsAdmin ? "ADMIN" : "STAF";
            BrdRoleBadge.Background = user.IsAdmin ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#065F46")) : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#334155"));

            // Tier & Storage
            TxtSidebarTierName.Text = tier.DisplayName.ToUpper();
            int usedMb = (int)(tenant.StorageUsedBytes / (1024 * 1024));
            TxtSidebarStorageUsage.Text = $"{usedMb} MB / {tier.MaxStorageMb} MB";
            PbStorageUsage.Maximum = tier.MaxStorageMb;
            PbStorageUsage.Value = Math.Min(usedMb, tier.MaxStorageMb);

            // Chameleon Switcher
            var chameleon = ChameleonTerminologyService.GetTerms(tenant.InstitutionType);
            TxtSidebarChameleonType.Text = chameleon.InstitutionTypeDisplayName.ToUpper();
            TxtNavEntitiesTitle.Text = chameleon.PersonnelMenuName;
            TxtEntitiesMainTitle.Text = $"Daftar {chameleon.PersonnelMenuName}";
            TxtWorkspaceSdmLookupLabel.Text = $"2. DATA {chameleon.PersonnelMenuName.ToUpper()} (AUTO-FILL)";

            // Logo
            if (!string.IsNullOrEmpty(tenant.LogoUrl) && tenant.LogoUrl.StartsWith("data:image"))
            {
                try
                {
                    var base64 = tenant.LogoUrl.Substring(tenant.LogoUrl.IndexOf(',') + 1);
                    var bytes = Convert.FromBase64String(base64);
                    var bmp = new BitmapImage();
                    bmp.BeginInit();
                    bmp.StreamSource = new MemoryStream(bytes);
                    bmp.CacheOption = BitmapCacheOption.OnLoad;
                    bmp.EndInit();
                    ImgSidebarLogo.Source = bmp;
                }
                catch { }
            }

            // Role-based navigation visibility
            if (!user.IsAdmin)
            {
                NavTemplates.Visibility = Visibility.Collapsed;
                NavSettings.Visibility = Visibility.Collapsed;
            }
        }

        private void TxtSidebarTenantPill()
        {
            var code = App.ApiClient.Tenant?.InstitutionCode ?? "";
            Title = $"Sovereign Executive Suite — [{code}] {App.ApiClient.Tenant?.InstitutionName}";
        }
        #endregion

        #region Broadcast, Running Text & Pop-up Promo
        private async Task LoadBroadcastAndPromosAsync()
        {
            try
            {
                var res = await App.ApiClient.GetSystemBroadcastAsync();
                if (res.Success && res.Data != null)
                {
                    _broadcast = res.Data;
                    if (!string.IsNullOrEmpty(_broadcast.RunningText))
                    {
                        TxtBroadcastRunningTicker.Text = _broadcast.RunningText;
                    }

                    if (!string.IsNullOrEmpty(_broadcast.RunningTextBgColor))
                    {
                        try
                        {
                            BrdRunningTextTicker.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(_broadcast.RunningTextBgColor));
                        }
                        catch { }
                    }

                    // Check Popup Promo for current tier
                    var currentTier = App.ApiClient.Tier?.TierId ?? "trial";
                    if (_broadcast.PopupEnabled == 1 && _broadcast.TargetTiers.Contains(currentTier, StringComparer.OrdinalIgnoreCase))
                    {
                        TxtPromoTitle.Text = _broadcast.PopupTitle ?? "PENAWARAN KHUSUS UPGRADE LISENSI";
                        BtnPromoCta.Content = !string.IsNullOrWhiteSpace(_broadcast.PopupCtaText) ? _broadcast.PopupCtaText : "[ 💬 Hubungi Kami ]";
                        ModalBroadcastPromo.Visibility = Visibility.Visible;
                    }
                }
            }
            catch { }
        }

        private void OnClosePromoModalClick(object sender, RoutedEventArgs e)
        {
            ModalBroadcastPromo.Visibility = Visibility.Collapsed;
        }

        private void OnPromoCtaClick(object sender, RoutedEventArgs e)
        {
            ModalBroadcastPromo.Visibility = Visibility.Collapsed;
            SupportGatewayService.OpenSupportChannel(SupportIntent.UpgradeTier, "Pengajuan Peningkatan Paket melalui Buletin Eksekutif", App.ApiClient);
        }

        private void OnShowUpgradePricingClick(object sender, RoutedEventArgs e)
        {
            SupportGatewayService.OpenSupportChannel(SupportIntent.UpgradeTier, null, App.ApiClient);
        }
        #endregion

        #region Navigation Switching
        private async void OnNavMenuChanged(object sender, RoutedEventArgs e)
        {
            if (ViewDashboard == null) return;

            ViewDashboard.Visibility = Visibility.Collapsed;
            ViewWorkspace.Visibility = Visibility.Collapsed;
            ViewArchive.Visibility = Visibility.Collapsed;
            ViewIncoming.Visibility = Visibility.Collapsed;
            ViewEntities.Visibility = Visibility.Collapsed;
            ViewTemplates.Visibility = Visibility.Collapsed;
            ViewSettings.Visibility = Visibility.Collapsed;

            if (NavDashboard.IsChecked == true)
            {
                TxtHeaderTitle.Text = "Executive Dashboard";
                TxtHeaderSubtitle.Text = "Ringkasan eksekutif tata naskah dinas, peringatan kontrak, dan persetujuan arsip.";
                ViewDashboard.Visibility = Visibility.Visible;
                await LoadDashboardDataAsync();
            }
            else if (NavWorkspace.IsChecked == true)
            {
                TxtHeaderTitle.Text = "Ruang Kerja Surat & Proposal RAB";
                TxtHeaderSubtitle.Text = "Penyusunan naskah kedinasan WYSIWYG dan Rencana Anggaran Biaya berbasis spreadsheet cerdas.";
                ViewWorkspace.Visibility = Visibility.Visible;
                await LoadWorkspaceDataAsync();
            }
            else if (NavArchive.IsChecked == true)
            {
                TxtHeaderTitle.Text = "Buku Agenda Surat Keluar & Arsip Cloud";
                TxtHeaderSubtitle.Text = "Kelola siklus hidup dokumen, verifikasi scan basah, re-edit in-app, dan penghapusan berjenjang.";
                ViewArchive.Visibility = Visibility.Visible;
                await LoadArchiveDataAsync();
            }
            else if (NavIncoming.IsChecked == true)
            {
                TxtHeaderTitle.Text = "Surat Masuk & Lembar Disposisi Digital";
                TxtHeaderSubtitle.Text = "Pencatatan surat masuk dari instansi eksternal dan pencetakan lembar disposisi instruksi pimpinan.";
                ViewIncoming.Visibility = Visibility.Visible;
                await LoadIncomingMailsAsync();
            }
            else if (NavEntities.IsChecked == true)
            {
                var terms = ChameleonTerminologyService.GetTerms(App.ApiClient.Tenant?.InstitutionType ?? "school");
                TxtHeaderTitle.Text = terms.PersonnelMenuName;
                TxtHeaderSubtitle.Text = $"Database {terms.PersonnelMenuName} dengan perhitungan masa kerja otomatis dan atribut kustom.";
                ViewEntities.Visibility = Visibility.Visible;
                await LoadEntitiesDataAsync();
            }
            else if (NavTemplates.IsChecked == true)
            {
                TxtHeaderTitle.Text = "Studio Format Surat & Builder Penomoran";
                TxtHeaderSubtitle.Text = "Desain format template naskah dinas dan rakit rumus nomor surat otomatis menggunakan blok token.";
                ViewTemplates.Visibility = Visibility.Visible;
                await LoadTemplateStudioDataAsync();
            }
            else if (NavSettings.IsChecked == true)
            {
                TxtHeaderTitle.Text = "Pengaturan Instansi & Manajemen Pengguna";
                TxtHeaderSubtitle.Text = "Konfigurasi kop surat, logo resmi, dan akun staf yang dibatasi oleh tier lisensi.";
                ViewSettings.Visibility = Visibility.Visible;
                await LoadSettingsDataAsync();
            }
        }

        private async void OnRefreshGlobalDataClick(object sender, RoutedEventArgs e)
        {
            ApplySessionAndChameleonTheme();
            if (NavDashboard.IsChecked == true) await LoadDashboardDataAsync();
            else if (NavWorkspace.IsChecked == true) await LoadWorkspaceDataAsync();
            else if (NavArchive.IsChecked == true) await LoadArchiveDataAsync();
            else if (NavIncoming.IsChecked == true) await LoadIncomingMailsAsync();
            else if (NavEntities.IsChecked == true) await LoadEntitiesDataAsync();
            else if (NavTemplates.IsChecked == true) await LoadTemplateStudioDataAsync();
            else if (NavSettings.IsChecked == true) await LoadSettingsDataAsync();
        }

        private void OnLogoutClick(object sender, RoutedEventArgs e)
        {
            var res = MessageBox.Show("Apakah Anda yakin ingin keluar dari sistem?", "Konfirmasi Keluar", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (res == MessageBoxResult.Yes)
            {
                App.ApiClient.ClearSession();
                var authWin = new AuthWindow();
                authWin.Show();
                Close();
            }
        }
        #endregion

        #region 1. Dashboard View Logic
        private async Task LoadDashboardDataAsync()
        {
            try
            {
                var res = await App.ApiClient.GetDashboardSummaryAsync();
                if (res.Success && res.Data != null)
                {
                    var data = res.Data;
                    TxtDashTotalDocs.Text = data.TotalDocuments.ToString();
                    TxtDashTotalBudget.Text = $"Rp {data.TotalRabAmount:N0}";
                    TxtDashWaitingScan.Text = data.WaitingScanCount.ToString();
                    TxtDashVerifiedScan.Text = data.VerifiedScanCount.ToString();

                    LvDashExpiringPersonnel.ItemsSource = data.ExpiringPersonnel;
                    LvDashPendingDeletions.ItemsSource = data.PendingDeletions;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Gagal memuat data dashboard: {ex.Message}", "Error Dashboard", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private async void OnApproveDeleteDocClick(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string docId)
            {
                var confirm = MessageBox.Show("Setujui penghapusan permanen dokumen ini?", "Konfirmasi Hapus", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (confirm == MessageBoxResult.Yes)
                {
                    var res = await App.ApiClient.ResolveDocumentDeletionAsync(docId, true);
                    if (res.Success)
                    {
                        MessageBox.Show("Dokumen telah dihapus permanen.", "Sukses", MessageBoxButton.OK, MessageBoxImage.Information);
                        await LoadDashboardDataAsync();
                    }
                }
            }
        }

        private async void OnRejectDeleteDocClick(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string docId)
            {
                var res = await App.ApiClient.ResolveDocumentDeletionAsync(docId, false);
                if (res.Success)
                {
                    MessageBox.Show("Pengajuan hapus ditolak. Dokumen tetap dipertahankan.", "Informasi", MessageBoxButton.OK, MessageBoxImage.Information);
                    await LoadDashboardDataAsync();
                }
            }
        }
        #endregion

        #region 2. Document Workspace & Dual-Mode Office Suite
        private async Task LoadWorkspaceDataAsync()
        {
            try
            {
                // Load Categories
                var catRes = await App.ApiClient.GetCategoriesAsync();
                if (catRes.Success && catRes.Data != null)
                {
                    _categories = catRes.Data;
                    CmbWorkspaceCategory.ItemsSource = _categories;
                    CmbWorkspaceCategory.DisplayMemberPath = "Name";
                    if (_categories.Count > 0 && CmbWorkspaceCategory.SelectedIndex < 0)
                    {
                        CmbWorkspaceCategory.SelectedIndex = 0;
                    }
                }

                // Load SDM / Personnel Records for Auto-fill lookup
                var entRes = await App.ApiClient.GetEntitiesAsync();
                if (entRes.Success && entRes.Data != null)
                {
                    var personnelEnt = entRes.Data.FirstOrDefault(x => x.EntitySlug == "personnel") ?? entRes.Data.FirstOrDefault();
                    if (personnelEnt != null)
                    {
                        var recRes = await App.ApiClient.GetEntityRecordsAsync(personnelEnt.Id);
                        if (recRes.Success && recRes.Data != null)
                        {
                            _personnelRecords = recRes.Data;
                            CmbWorkspacePersonnelLookup.ItemsSource = _personnelRecords;
                            CmbWorkspacePersonnelLookup.DisplayMemberPath = "PrimaryName";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Gagal memuat ruang kerja: {ex.Message}", "Error Workspace", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private async void OnWorkspaceCategoryChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CmbWorkspaceCategory.SelectedItem is CategoryModel cat)
            {
                _currentSelectedCategory = cat;
                try
                {
                    var numRes = await App.ApiClient.GetNextDocumentNumberAsync(cat.Id);
                    if (numRes.Success && numRes.Data != null)
                    {
                        _currentNumberInfo = numRes.Data;
                        TxtWorkspaceGeneratedDocNumber.Text = _currentNumberInfo.DocNumberFull;

                        if (_currentNumberInfo.IsGapFill)
                        {
                            TxtNumberGapMessage.Text = $"⚠️ Nomor {_currentNumberInfo.SequenceNumber} adalah celah nomor kosong yang sebelumnya terhapus. Nomor ini digunakan untuk menjaga urutan kronologis.";
                            BrdNumberGapAlert.Visibility = Visibility.Visible;
                        }
                        else
                        {
                            BrdNumberGapAlert.Visibility = Visibility.Collapsed;
                        }
                    }

                    // Populate Rich Template Content
                    if (!string.IsNullOrEmpty(cat.RichContentJson))
                    {
                        TxtWordRichDocumentContent.Text = cat.RichContentJson;
                    }

                    // Reset RAB from database template
                    _rabItems.Clear();
                    if (cat.HasRabGrid == 1 && !string.IsNullOrEmpty(cat.RabTemplateJson) && cat.RabTemplateJson.StartsWith("["))
                    {
                        try
                        {
                            var presetItems = System.Text.Json.JsonSerializer.Deserialize<List<RabItemModel>>(cat.RabTemplateJson);
                            if (presetItems != null)
                            {
                                foreach (var item in presetItems) _rabItems.Add(item);
                            }
                        }
                        catch { }
                    }
                    CalculateRabTotals();
                }
                catch (Exception ex)
                {
                    TxtWorkspaceGeneratedDocNumber.Text = "Error nomor otomatis: " + ex.Message;
                }
            }
        }

        private void OnWorkspacePersonnelSelected(object sender, SelectionChangedEventArgs e)
        {
            if (CmbWorkspacePersonnelLookup.SelectedItem is EntityRecordModel p)
            {
                var content = TxtWordRichDocumentContent.Text;
                content = content.Replace("{{personnel.nama}}", p.PrimaryName);
                content = content.Replace("{{personnel.nip}}", p.PrimaryIdNumber);
                content = content.Replace("{{personnel.tmt}}", p.TmtDate ?? "-");
                content = content.Replace("{{personnel.masa_kerja}}", p.CalculatedTenure ?? "-");
                TxtWordRichDocumentContent.Text = content;
            }
        }

        private void OnOfficeModeChanged(object sender, RoutedEventArgs e)
        {
            if (WordCanvasContainer == null || RabCanvasContainer == null) return;

            if (TabOfficeWord.IsChecked == true)
            {
                WordRibbonBar.Visibility = Visibility.Visible;
                RabRibbonBar.Visibility = Visibility.Collapsed;
                WordCanvasContainer.Visibility = Visibility.Visible;
                RabCanvasContainer.Visibility = Visibility.Collapsed;
            }
            else
            {
                WordRibbonBar.Visibility = Visibility.Collapsed;
                RabRibbonBar.Visibility = Visibility.Visible;
                WordCanvasContainer.Visibility = Visibility.Collapsed;
                RabCanvasContainer.Visibility = Visibility.Visible;
            }
        }

        #region Word Formatting & Inserts
        private void OnWordBoldClick(object sender, RoutedEventArgs e)
        {
            var sel = TxtWordRichDocumentContent.SelectedText;
            if (!string.IsNullOrEmpty(sel))
            {
                TxtWordRichDocumentContent.SelectedText = $"**{sel}**";
            }
        }

        private void OnWordItalicClick(object sender, RoutedEventArgs e)
        {
            var sel = TxtWordRichDocumentContent.SelectedText;
            if (!string.IsNullOrEmpty(sel))
            {
                TxtWordRichDocumentContent.SelectedText = $"*{sel}*";
            }
        }

        private void OnWordUnderlineClick(object sender, RoutedEventArgs e)
        {
            var sel = TxtWordRichDocumentContent.SelectedText;
            if (!string.IsNullOrEmpty(sel))
            {
                TxtWordRichDocumentContent.SelectedText = $"<u>{sel}</u>";
            }
        }

        private void OnWordAlignLeftClick(object sender, RoutedEventArgs e)
        {
            TxtWordRichDocumentContent.TextAlignment = TextAlignment.Left;
        }

        private void OnWordAlignCenterClick(object sender, RoutedEventArgs e)
        {
            TxtWordRichDocumentContent.TextAlignment = TextAlignment.Center;
        }

        private void OnWordAlignRightClick(object sender, RoutedEventArgs e)
        {
            TxtWordRichDocumentContent.TextAlignment = TextAlignment.Right;
        }

        private void OnInsertKopSuratClick(object sender, RoutedEventArgs e)
        {
            var tenant = App.ApiClient.Tenant;
            var kop = $"======================================================================\n" +
                      $"{tenant?.InstitutionName?.ToUpper() ?? "INSTANSI"}\n" +
                      $"{tenant?.InstitutionAddress ?? "Alamat Resmi Instansi"}\n" +
                      $"Telp: {tenant?.PhoneNumber ?? "-"} | Email: {tenant?.Email ?? "-"}\n" +
                      $"======================================================================\n\n";

            TxtWordRichDocumentContent.Text = kop + TxtWordRichDocumentContent.Text;
        }

        private void OnInsertSignatureBlockClick(object sender, RoutedEventArgs e)
        {
            var terms = ChameleonTerminologyService.GetTerms(App.ApiClient.Tenant?.InstitutionType ?? "school");
            var dateStr = DateTime.Now.ToString("dd MMMM yyyy", new System.Globalization.CultureInfo("id-ID"));
            var sig = $"\n\n                                  Ditetapkan di: Jakarta\n" +
                      $"                                  Pada tanggal : {dateStr}\n\n" +
                      $"                                  {terms.LeaderTitle.ToUpper()},\n\n\n\n" +
                      $"                                  ( {App.ApiClient.User?.FullName ?? "Nama Pimpinan"} )\n" +
                      $"                                  {terms.IdentityNumberLabel}: 19800512 200501 1 003\n";

            TxtWordRichDocumentContent.Text += sig;
        }

        private void OnInsertQrValidationClick(object sender, RoutedEventArgs e)
        {
            var qr = $"\n[ 🏁 QR CODE VALIDASI RESMI DIGITAL ]\nKode Keabsahan Naskah: DOC-VAL-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}\n";
            TxtWordRichDocumentContent.Text += qr;
        }

        private void OnInsertVariableClick(object sender, RoutedEventArgs e)
        {
            TxtWordRichDocumentContent.SelectedText = "{{nama_variabel}}";
        }

        private void OnRunSpellCheckClick(object sender, RoutedEventArgs e)
        {
            var original = TxtWordRichDocumentContent.Text;
            var corrected = SpellCheckService.CheckAndCorrect(original, out int correctionsCount);
            TxtWordRichDocumentContent.Text = corrected;
            MessageBox.Show($"Pemeriksaan ejaan baku selesai.\nSebanyak {correctionsCount} kata tidak baku kedinasan berhasil diperbaiki.", "Pemeriksa Ejaan KBBI", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        #endregion

        #region Excel RAB Click-to-Formula
        private void OnAddRabRowClick(object sender, RoutedEventArgs e)
        {
            int nextNo = _rabItems.Count + 1;
            _rabItems.Add(new RabItemModel { ItemNo = nextNo, Description = "Barang / Jasa Baru", Volume = 1, Unit = "Unit", UnitPrice = 0, SubTotal = 0 });
            CalculateRabTotals();
        }

        private void OnRabMultiplyClick(object sender, RoutedEventArgs e)
        {
            foreach (var item in _rabItems)
            {
                item.SubTotal = item.Volume * item.UnitPrice;
            }
            DgRabGrid.Items.Refresh();
            CalculateRabTotals();
        }

        private void OnRabSumTotalClick(object sender, RoutedEventArgs e)
        {
            OnRabMultiplyClick(sender, e);
        }

        private void OnRabCalculateTaxClick(object sender, RoutedEventArgs e)
        {
            OnRabMultiplyClick(sender, e);
            double currentSum = _rabItems.Where(x => !x.Description.StartsWith("PPN")).Sum(x => x.SubTotal);
            double ppnAmount = Math.Round(currentSum * 0.11);

            var existingPpn = _rabItems.FirstOrDefault(x => x.Description.StartsWith("PPN 11%"));
            if (existingPpn != null)
            {
                existingPpn.SubTotal = ppnAmount;
            }
            else
            {
                _rabItems.Add(new RabItemModel
                {
                    ItemNo = _rabItems.Count + 1,
                    Description = "PPN 11% (Pajak Pertambahan Nilai)",
                    Volume = 1,
                    Unit = "Kegiatan",
                    UnitPrice = ppnAmount,
                    SubTotal = ppnAmount
                });
            }
            DgRabGrid.Items.Refresh();
            CalculateRabTotals();
        }

        private void OnRabFormatTerbilangClick(object sender, RoutedEventArgs e)
        {
            CalculateRabTotals();
            MessageBox.Show($"Format angka dan kalimat terbilang diperbarui:\n{TxtRabTerbilangSentence.Text}", "Terbilang Otomatis", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void CalculateRabTotals()
        {
            double grandTotal = _rabItems.Sum(x => x.SubTotal);
            TxtRabGrandTotal.Text = $"Rp {grandTotal:N0}";
            TxtRabTerbilangSentence.Text = TerbilangHelper.ToTerbilang((long)grandTotal);
        }
        #endregion

        #region Saving & Archiving Documents
        private async void OnSaveDraftDocClick(object sender, RoutedEventArgs e)
        {
            await SaveDocumentAsync("draft");
        }

        private async void OnSaveAndArchiveDocClick(object sender, RoutedEventArgs e)
        {
            await SaveDocumentAsync("completed_digital");
        }

        private async Task SaveDocumentAsync(string archiveStatus)
        {
            if (_currentSelectedCategory == null)
            {
                MessageBox.Show("Pilih kategori format surat terlebih dahulu.", "Peringatan", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var subject = TxtWorkspaceDocSubject.Text.Trim();
            if (string.IsNullOrEmpty(subject))
            {
                MessageBox.Show("Harap isi Perihal / Judul Surat.", "Peringatan", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var recipient = TxtWorkspaceDocRecipient.Text.Trim();
            var content = TxtWordRichDocumentContent.Text;
            double rabTotal = _rabItems.Sum(x => x.SubTotal);
            var rabJson = System.Text.Json.JsonSerializer.Serialize(_rabItems);

            var req = new CreateDocumentRequest
            {
                CategoryId = _currentSelectedCategory.Id,
                TitleOrSubject = subject,
                Recipient = recipient,
                RenderedContentJson = content,
                RabDataJson = rabJson,
                RabTotalAmount = rabTotal,
                ArchiveStatus = archiveStatus
            };

            try
            {
                if (string.IsNullOrEmpty(_editingDocumentId))
                {
                    var res = await App.ApiClient.CreateDocumentAsync(req);
                    if (res.Success && res.Data != null)
                    {
                        MessageBox.Show($"Dokumen berhasil disimpan!\nNomor Surat: {res.Data.DocNumberFull}\nStatus: {res.Data.ArchiveStatusLabel}", "Sukses", MessageBoxButton.OK, MessageBoxImage.Information);
                        _editingDocumentId = res.Data.Id;
                    }
                    else
                    {
                        MessageBox.Show(res.Message ?? "Gagal menyimpan dokumen.", "Gagal", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                else
                {
                    var updateReq = new UpdateDocumentRequest
                    {
                        TitleOrSubject = subject,
                        Recipient = recipient,
                        RenderedContentJson = content,
                        RabDataJson = rabJson,
                        RabTotalAmount = rabTotal,
                        ArchiveStatus = archiveStatus
                    };
                    var res = await App.ApiClient.UpdateDocumentAsync(_editingDocumentId, updateReq);
                    if (res.Success)
                    {
                        MessageBox.Show("Perubahan dokumen berhasil diperbarui.", "Sukses", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saat menyimpan dokumen: {ex.Message}", "Kesalahan Sistem", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        #endregion

        #region Zero-Download Backstage Print Modal
        private void OnOpenPrintBackstageClick(object sender, RoutedEventArgs e)
        {
            // Filter physical printers only! Anti-Virtual PDF
            var printers = PrinterFilterService.GetPhysicalPrinters();
            CmbPhysicalPrinters.ItemsSource = printers;
            if (printers.Count > 0) CmbPhysicalPrinters.SelectedIndex = 0;

            // Set Preview Body
            TxtPrintPreviewBody.Text = TxtWordRichDocumentContent.Text;

            // If RAB has items, append RAB table to print preview
            if (_rabItems.Count > 0)
            {
                TxtPrintPreviewBody.Text += "\n\nLAMPIRAN RENCANA ANGGARAN BIAYA (RAB):\n";
                foreach (var item in _rabItems)
                {
                    TxtPrintPreviewBody.Text += $"{item.ItemNo}. {item.Description} ({item.Volume} {item.Unit}) = Rp {item.SubTotal:N0}\n";
                }
                TxtPrintPreviewBody.Text += $"\nTOTAL ANGGARAN: {TxtRabGrandTotal.Text}\n{TxtRabTerbilangSentence.Text}\n";
            }

            // Trial watermark check
            var tier = App.ApiClient.Tier;
            if (tier != null && (tier.HasWatermark == 1 || tier.IsTrial))
            {
                TxtPrintDemoWatermark.Visibility = Visibility.Visible;
            }
            else
            {
                TxtPrintDemoWatermark.Visibility = Visibility.Collapsed;
            }

            ModalPrintBackstage.Visibility = Visibility.Visible;
        }

        private void OnClosePrintBackstageClick(object sender, RoutedEventArgs e)
        {
            ModalPrintBackstage.Visibility = Visibility.Collapsed;
        }

        private void OnExecutePhysicalPrintClick(object sender, RoutedEventArgs e)
        {
            var printerName = CmbPhysicalPrinters.SelectedItem?.ToString();
            if (string.IsNullOrEmpty(printerName) || !HardwareSpoolerFilter.VerifyPhysicalDevice(printerName))
            {
                MessageBox.Show("Perangkat Cetak Tidak Kompatibel. Demi menjaga integritas keaslian dokumen kedinasan, sistem hanya mendukung pencetakan langsung ke perangkat keras printer fisik (Hardware Spooler Only).", "Peringatan Perangkat Cetak", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int copies = 1;
            int.TryParse(TxtPrintCopies.Text, out copies);
            copies = Math.Max(1, copies);

            try
            {
                ZeroDownloadMemoryService.PrintDirectToHardware(TxtPrintPreviewBody.Text, printerName, copies);
                MessageBox.Show($"Perintah cetak ({copies} rangkap) berhasil dikirim ke perangkat fisik: {printerName}", "Pencetakan Berhasil", MessageBoxButton.OK, MessageBoxImage.Information);
                ModalPrintBackstage.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Pencetakan gagal: {ex.Message}", "Error Cetak", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        #endregion
        #endregion

        #region 3. Archive Manager Logic
        private async Task LoadArchiveDataAsync()
        {
            try
            {
                var res = await App.ApiClient.GetDocumentsAsync();
                if (res.Success && res.Data != null)
                {
                    _allDocuments = res.Data;
                    ApplyArchiveFilters();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Gagal memuat arsip: {ex.Message}", "Error Arsip", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void ApplyArchiveFilters()
        {
            var query = TxtArchiveSearchQuery?.Text?.Trim().ToLower() ?? "";
            var statusItem = CmbArchiveStatusFilter?.SelectedItem as ComboBoxItem;
            var statusTag = statusItem?.Tag?.ToString();

            var filtered = _allDocuments.AsEnumerable();

            if (!string.IsNullOrEmpty(query))
            {
                filtered = filtered.Where(x =>
                    x.DocNumberFull.ToLower().Contains(query) ||
                    x.TitleOrSubject.ToLower().Contains(query) ||
                    (x.Recipient != null && x.Recipient.ToLower().Contains(query))
                );
            }

            if (!string.IsNullOrEmpty(statusTag))
            {
                filtered = filtered.Where(x => x.ArchiveStatus.Equals(statusTag, StringComparison.OrdinalIgnoreCase));
            }

            DgArchiveDocs.ItemsSource = filtered.ToList();
        }

        private void OnArchiveSearchChanged(object sender, TextChangedEventArgs e)
        {
            ApplyArchiveFilters();
        }

        private void OnArchiveStatusFilterChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyArchiveFilters();
        }

        private void OnArchivePrintDocClick(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is DocumentModel doc)
            {
                TxtWordRichDocumentContent.Text = doc.RenderedContentJson;
                OnOpenPrintBackstageClick(sender, e);
            }
        }

        private void OnArchiveReEditDocClick(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is DocumentModel doc)
            {
                _editingDocumentId = doc.Id;
                TxtWorkspaceDocSubject.Text = doc.TitleOrSubject;
                TxtWorkspaceDocRecipient.Text = doc.Recipient ?? "";
                TxtWorkspaceGeneratedDocNumber.Text = doc.DocNumberFull;
                TxtWordRichDocumentContent.Text = doc.RenderedContentJson;

                // Load RAB if present
                if (!string.IsNullOrEmpty(doc.RabDataJson) && doc.RabDataJson.StartsWith("["))
                {
                    try
                    {
                        var items = System.Text.Json.JsonSerializer.Deserialize<List<RabItemModel>>(doc.RabDataJson);
                        if (items != null)
                        {
                            _rabItems.Clear();
                            foreach (var item in items) _rabItems.Add(item);
                            CalculateRabTotals();
                        }
                    }
                    catch { }
                }

                // Switch view to Workspace
                NavWorkspace.IsChecked = true;
            }
        }

        private async void OnArchiveUploadScanClick(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is DocumentModel doc)
            {
                var dlg = new OpenFileDialog
                {
                    Filter = "File Scan Dokumen (*.jpg;*.jpeg;*.png)|*.jpg;*.jpeg;*.png",
                    Title = $"Pilih File Scan Tanda Tangan & Stempel Basah untuk {doc.DocNumberFull}"
                };

                if (dlg.ShowDialog() == true)
                {
                    try
                    {
                        var compressed = ImageCompressionService.CompressForDocumentArchive(dlg.FileName, 1200, 75);
                        var dataUrl = "data:image/jpeg;base64," + Convert.ToBase64String(compressed);

                        var res = await App.ApiClient.LinkDocumentScanAsync(doc.Id, dataUrl);
                        if (res.Success)
                        {
                            MessageBox.Show($"Bukti scan tanda tangan & stempel basah berhasil ditautkan!\nStatus arsip berubah menjadi: TERVERIFIKASI SCAN (HIJAU)", "Sukses Scan", MessageBoxButton.OK, MessageBoxImage.Information);
                            await LoadArchiveDataAsync();
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Gagal upload scan: {ex.Message}", "Error Scan", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void OnDirectScanHardwareClick(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Modul TWAIN/WIA Scanner Kantor aktif. Menginisialisasi koneksi perangkat keras scanner fisik melalui USB/LAN...\n\n(Catatan: Untuk demo cepat, Anda juga dapat menggunakan tombol 'Upload Scan' di setiap baris dokumen).", "Scanner Perangkat Keras", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private async void OnArchiveDeleteDocClick(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is DocumentModel doc)
            {
                var isAdmin = App.ApiClient.User?.IsAdmin ?? false;
                if (isAdmin)
                {
                    var confirm = MessageBox.Show($"Sebagai Admin Instansi, apakah Anda yakin ingin menghapus permanen dokumen {doc.DocNumberFull}?", "Konfirmasi Hapus Admin", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                    if (confirm == MessageBoxResult.Yes)
                    {
                        var res = await App.ApiClient.ResolveDocumentDeletionAsync(doc.Id, true);
                        if (res.Success)
                        {
                            MessageBox.Show("Dokumen berhasil dihapus permanen.", "Sukses", MessageBoxButton.OK, MessageBoxImage.Information);
                            await LoadArchiveDataAsync();
                        }
                    }
                }
                else
                {
                    // Staf deletion approval workflow
                    var reason = ModernDialogService.Prompt("Ketikkan alasan penghapusan dokumen untuk diajukan ke Admin Instansi:", "Pengajuan Penghapusan Dokumen (Staf)", "Koreksi data naskah");
                    if (!string.IsNullOrWhiteSpace(reason))
                    {
                        var res = await App.ApiClient.RequestDocumentDeletionAsync(doc.Id, reason);
                        if (res.Success)
                        {
                            MessageBox.Show("Pengajuan penghapusan berhasil dikirim ke Admin Instansi.\nStatus dokumen berubah menjadi: MENUNGGU PERSETUJUAN HAPUS (MERAH)", "Pengajuan Berhasil", MessageBoxButton.OK, MessageBoxImage.Information);
                            await LoadArchiveDataAsync();
                        }
                    }
                }
            }
        }
        #endregion

        #region 4. Incoming Mails Logic
        private async Task LoadIncomingMailsAsync()
        {
            try
            {
                var res = await App.ApiClient.GetIncomingMailsAsync();
                if (res.Success && res.Data != null)
                {
                    DgIncomingMails.ItemsSource = res.Data;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Gagal memuat surat masuk: {ex.Message}", "Error Surat Masuk", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private async void OnSaveIncomingMailClick(object sender, RoutedEventArgs e)
        {
            var agenda = TxtIncAgendaNo.Text.Trim();
            var origin = TxtIncSenderOrigin.Text.Trim();
            var mailNo = TxtIncMailNo.Text.Trim();
            var subject = TxtIncSubject.Text.Trim();
            var urgencyItem = CmbIncUrgency.SelectedItem as ComboBoxItem;
            var urgency = urgencyItem?.Content?.ToString() ?? "Biasa";
            var dispTo = TxtIncDispositionTo.Text.Trim();
            var instr = TxtIncInstruction.Text.Trim();

            if (string.IsNullOrEmpty(origin) || string.IsNullOrEmpty(subject))
            {
                MessageBox.Show("Pengirim dan Perihal surat masuk wajib diisi.", "Peringatan", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var req = new CreateIncomingMailRequest
            {
                AgendaNumber = agenda,
                SenderOrigin = origin,
                OriginalMailNumber = mailNo,
                Subject = subject,
                UrgencyLevel = urgency,
                DispositionTo = dispTo,
                DispositionInstruction = instr
            };

            var res = await App.ApiClient.CreateIncomingMailAsync(req);
            if (res.Success)
            {
                MessageBox.Show("Surat masuk berhasil dicatat dalam Buku Agenda Surat Masuk.", "Sukses", MessageBoxButton.OK, MessageBoxImage.Information);
                TxtIncAgendaNo.Clear();
                TxtIncSenderOrigin.Clear();
                TxtIncMailNo.Clear();
                TxtIncSubject.Clear();
                TxtIncDispositionTo.Clear();
                TxtIncInstruction.Clear();
                await LoadIncomingMailsAsync();
            }
        }

        private void OnPrintDispositionSheetClick(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is IncomingMailModel mail)
            {
                var tenant = App.ApiClient.Tenant;
                var terms = ChameleonTerminologyService.GetTerms(tenant?.InstitutionType ?? "school");
                var sheet = $"======================================================================\n" +
                            $"                   LEMBAR DISPOSISI KEDINASAN\n" +
                            $"                   {tenant?.InstitutionName?.ToUpper() ?? "INSTANSI"}\n" +
                            $"======================================================================\n\n" +
                            $"No. Agenda     : {mail.AgendaNumber}\n" +
                            $"Asal Surat     : {mail.SenderOrigin}\n" +
                            $"Nomor Surat    : {mail.OriginalMailNumber}\n" +
                            $"Tanggal Terima : {mail.ReceivedDate}\n" +
                            $"Tingkat Urgensi: {mail.UrgencyLevel.ToUpper()}\n" +
                            $"Perihal        : {mail.Subject}\n\n" +
                            $"----------------------------------------------------------------------\n" +
                            $"DITERUSKAN KEPADA Sdr : {mail.DispositionTo}\n\n" +
                            $"INSTRUKSI / PETUNJUK {terms.LeaderTitle.ToUpper()}:\n" +
                            $"{mail.DispositionInstruction}\n\n" +
                            $"                                  Tanda Tangan Pimpinan:\n\n\n" +
                            $"                                  ( .............................. )\n";

                TxtPrintPreviewBody.Text = sheet;
                OnOpenPrintBackstageClick(sender, e);
            }
        }
        #endregion

        #region 5. Adaptive SDM & Entities Logic
        private async Task LoadEntitiesDataAsync()
        {
            try
            {
                var entRes = await App.ApiClient.GetEntitiesAsync();
                if (entRes.Success && entRes.Data != null)
                {
                    var personnelEnt = entRes.Data.FirstOrDefault(x => x.EntitySlug == "personnel") ?? entRes.Data.FirstOrDefault();
                    if (personnelEnt != null)
                    {
                        var recRes = await App.ApiClient.GetEntityRecordsAsync(personnelEnt.Id);
                        if (recRes.Success && recRes.Data != null)
                        {
                            DgPersonnelRecords.ItemsSource = recRes.Data;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Gagal memuat data SDM: {ex.Message}", "Error SDM", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private async void OnAddPersonnelRecordClick(object sender, RoutedEventArgs e)
        {
            var terms = ChameleonTerminologyService.GetTerms(App.ApiClient.Tenant?.InstitutionType ?? "school");
            var name = ModernDialogService.Prompt($"Ketikkan Nama Lengkap {terms.PersonnelMenuName} Beserta Gelar:", "Tambah Personel", "");
            if (string.IsNullOrWhiteSpace(name)) return;

            var idNum = ModernDialogService.Prompt($"Ketikkan {terms.IdentityNumberLabel}:", "Nomor Identitas", "");
            var tmt = ModernDialogService.Prompt("Ketikkan Tanggal TMT (Format: YYYY-MM-DD):", "Tanggal TMT", DateTime.Now.ToString("yyyy-MM-dd"));

            var entRes = await App.ApiClient.GetEntitiesAsync();
            var personnelEnt = entRes.Data?.FirstOrDefault(x => x.EntitySlug == "personnel") ?? entRes.Data?.FirstOrDefault();
            if (personnelEnt == null) return;

            var req = new CreateEntityRecordRequest
            {
                PrimaryName = name,
                PrimaryIdNumber = idNum,
                TmtDate = tmt,
                ValuesJson = "{}"
            };

            var res = await App.ApiClient.CreateEntityRecordAsync(personnelEnt.Id, req);
            if (res.Success)
            {
                MessageBox.Show($"Data {terms.PersonnelMenuName} berhasil ditambahkan!\nMasa kerja otomatis dihitung oleh sistem.", "Sukses", MessageBoxButton.OK, MessageBoxImage.Information);
                await LoadEntitiesDataAsync();
            }
        }

        private async void OnAddCustomAttributeClick(object sender, RoutedEventArgs e)
        {
            var label = ModernDialogService.Prompt("Ketikkan Nama Kolom Kebutuhan Data Instansi (Contoh Fungsi: Masa Berlaku Sertifikasi / Unit Kerja):", "Tambah Kolom Spesifik", "");
            if (string.IsNullOrWhiteSpace(label)) return;

            var entRes = await App.ApiClient.GetEntitiesAsync();
            var personnelEnt = entRes.Data?.FirstOrDefault(x => x.EntitySlug == "personnel") ?? entRes.Data?.FirstOrDefault();
            if (personnelEnt == null) return;

            var attrKey = label.ToLower().Replace(" ", "_").Replace(".", "");
            var req = new CreateAttributeRequest
            {
                AttrKey = attrKey,
                AttrLabel = label,
                DataType = "text"
            };

            var res = await App.ApiClient.CreateAttributeAsync(personnelEnt.Id, req);
            if (res.Success)
            {
                MessageBox.Show($"Kolom spesifik '{label}' berhasil dibuat!\nToken variabel baru siap pakai di template: {{{{personnel.{attrKey}}}}}", "Atribut Tersedia", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        #endregion

        #region 6. Template Studio & Numbering Formula Logic
        private async Task LoadTemplateStudioDataAsync()
        {
            try
            {
                var res = await App.ApiClient.GetCategoriesAsync();
                if (res.Success && res.Data != null)
                {
                    LbTemplateCategories.ItemsSource = res.Data;
                    LbTemplateCategories.DisplayMemberPath = "Name";
                    if (res.Data.Count > 0) LbTemplateCategories.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Gagal memuat template: {ex.Message}", "Error Template", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void OnTemplateCategorySelected(object sender, SelectionChangedEventArgs e)
        {
            if (LbTemplateCategories.SelectedItem is CategoryModel cat)
            {
                TxtFormulaEditor.Text = cat.NumberingFormula;
                TxtTemplateBodyContent.Text = cat.RichContentJson;
                UpdateFormulaPreview();
            }
        }

        private void OnAddTokenToFormulaClick(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string token)
            {
                TxtFormulaEditor.Text += token;
                UpdateFormulaPreview();
            }
        }

        private void OnFormulaEditorChanged(object sender, TextChangedEventArgs e)
        {
            UpdateFormulaPreview();
        }

        private void UpdateFormulaPreview()
        {
            if (TxtFormulaLivePreview == null || TxtFormulaEditor == null) return;
            var formula = TxtFormulaEditor.Text;
            var code = App.ApiClient.Tenant?.InstitutionCode ?? "INST-01";
            var now = DateTime.Now;

            var rom = new[] { "", "I", "II", "III", "IV", "V", "VI", "VII", "VIII", "IX", "X", "XI", "XII" }[now.Month];
            var preview = formula
                .Replace("[Nomor_Urut_3]", "005")
                .Replace("[Nomor_Urut_4]", "0005")
                .Replace("[Kode_Klasifikasi]", "421.2")
                .Replace("[Kode_Instansi]", code)
                .Replace("[Bulan_Romawi]", rom)
                .Replace("[Bulan_Angka]", now.Month.ToString("D2"))
                .Replace("[Tahun]", now.Year.ToString());

            TxtFormulaLivePreview.Text = preview;
        }

        private async void OnSaveTemplateChangesClick(object sender, RoutedEventArgs e)
        {
            if (LbTemplateCategories.SelectedItem is CategoryModel cat)
            {
                cat.NumberingFormula = TxtFormulaEditor.Text;
                cat.RichContentJson = TxtTemplateBodyContent.Text;

                var res = await App.ApiClient.UpdateCategoryAsync(cat.Id, cat);
                if (res.Success)
                {
                    MessageBox.Show("Perubahan rumus penomoran dan isi template berhasil disimpan!", "Sukses", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }
        #endregion

        #region 7. Settings & Users Logic
        private async Task LoadSettingsDataAsync()
        {
            try
            {
                var userRes = await App.ApiClient.GetUsersAsync();
                if (userRes.Success && userRes.Data != null)
                {
                    DgSettingsUsers.ItemsSource = userRes.Data;
                    var maxUsers = App.ApiClient.Tier?.MaxUsers ?? 3;
                    TxtSettingsUserQuotaLabel.Text = $"Kapasitas Akun: {userRes.Data.Count} / {maxUsers} Pengguna Aktif";
                }

                var tenant = App.ApiClient.Tenant;
                if (tenant != null)
                {
                    TxtSettingsAddress.Text = tenant.InstitutionAddress;
                    TxtSettingsPhone.Text = tenant.PhoneNumber;
                    TxtSettingsEmail.Text = tenant.Email;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Gagal memuat pengaturan: {ex.Message}", "Error Pengaturan", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private async void OnAddStaffUserClick(object sender, RoutedEventArgs e)
        {
            var fullName = ModernDialogService.Prompt("Ketikkan Nama Lengkap Pejabat/Pengguna Beserta Gelar Resmi:", "Tambah Akun Pengguna", "");
            if (string.IsNullOrWhiteSpace(fullName)) return;

            var email = ModernDialogService.Prompt("Ketikkan Alamat Email Aktif Sebagai ID Otorisasi Login:", "Email Otorisasi", "");
            if (string.IsNullOrWhiteSpace(email)) return;

            var pass = ModernDialogService.Prompt("Ketikkan Kata Sandi Awal Pengguna:", "Kata Sandi", "");
            if (string.IsNullOrWhiteSpace(pass)) return;

            var req = new CreateUserRequest
            {
                FullName = fullName,
                EmailUsername = email,
                Password = pass,
                Role = "staff"
            };

            var res = await App.ApiClient.CreateUserAsync(req);
            if (res.Success)
            {
                MessageBox.Show($"Akun staf '{fullName}' berhasil dibuat!", "Sukses", MessageBoxButton.OK, MessageBoxImage.Information);
                await LoadSettingsDataAsync();
            }
            else
            {
                MessageBox.Show(res.Message ?? "Gagal menambah user. Mungkin kuota pengguna paket telah habis.", "Peringatan", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private async void OnSaveInstitutionSettingsClick(object sender, RoutedEventArgs e)
        {
            var addr = TxtSettingsAddress.Text.Trim();
            var phone = TxtSettingsPhone.Text.Trim();
            var email = TxtSettingsEmail.Text.Trim();

            var req = new UpdateTenantSettingsRequest
            {
                InstitutionAddress = addr,
                PhoneNumber = phone,
                Email = email
            };

            var res = await App.ApiClient.UpdateTenantSettingsAsync(req);
            if (res.Success)
            {
                MessageBox.Show("Pengaturan profil instansi berhasil diperbarui!", "Sukses", MessageBoxButton.OK, MessageBoxImage.Information);
                if (App.ApiClient.Tenant != null)
                {
                    App.ApiClient.Tenant.InstitutionAddress = addr;
                    App.ApiClient.Tenant.PhoneNumber = phone;
                    App.ApiClient.Tenant.Email = email;
                }
            }
        }

        private void OnToggleThemeClick(object sender, RoutedEventArgs e)
        {
            var current = ApplicationThemeManager.GetAppTheme();
            var next = current == ApplicationTheme.Dark ? ApplicationTheme.Light : ApplicationTheme.Dark;
            ApplicationThemeManager.Apply(next);
        }
        #endregion
    }
}
