using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;
using QRCoder;
using SuratApp.Models;

namespace SuratApp.Services
{
    public class LocalDraft
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string CategoryId { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string DocumentNumber { get; set; } = string.Empty;
        public Dictionary<string, object> FormValues { get; set; } = new();
        public DateTime SavedAt { get; set; } = DateTime.Now;

        public override string ToString() => $"[DRAF] {DocumentNumber} ({SavedAt:dd/MM/yyyy HH:mm})";
    }

    public static class LocalDraftService
    {
        private static readonly string DraftFilePath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, 
            "OfflineDrafts.json"
        );

        public static List<LocalDraft> LoadDrafts(string? categoryId = null)
        {
            if (!File.Exists(DraftFilePath)) return new List<LocalDraft>();

            try
            {
                var json = File.ReadAllText(DraftFilePath);
                var all = JsonSerializer.Deserialize<List<LocalDraft>>(json) ?? new List<LocalDraft>();
                if (!string.IsNullOrEmpty(categoryId))
                {
                    return all.Where(d => d.CategoryId == categoryId).OrderByDescending(d => d.SavedAt).ToList();
                }
                return all.OrderByDescending(d => d.SavedAt).ToList();
            }
            catch
            {
                return new List<LocalDraft>();
            }
        }

        public static void SaveDraft(string categoryId, string categoryName, string docNumber, Dictionary<string, object> values)
        {
            var drafts = LoadDrafts();
            var existing = drafts.FirstOrDefault(d => d.DocumentNumber == docNumber && d.CategoryId == categoryId);

            if (existing != null)
            {
                existing.FormValues = values;
                existing.SavedAt = DateTime.Now;
            }
            else
            {
                drafts.Add(new LocalDraft
                {
                    CategoryId = categoryId,
                    CategoryName = categoryName,
                    DocumentNumber = docNumber,
                    FormValues = values,
                    SavedAt = DateTime.Now
                });
            }

            var json = JsonSerializer.Serialize(drafts, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(DraftFilePath, json);
        }

        public static void DeleteDraft(string draftId)
        {
            var drafts = LoadDrafts();
            drafts.RemoveAll(d => d.Id == draftId);
            var json = JsonSerializer.Serialize(drafts, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(DraftFilePath, json);
        }

        // ====================================================
        // AUTO-SCANNER PLACEHOLDER {{variabel}} DARI BERKAS .DOCX
        // ====================================================

        public static List<string> ScanPlaceholdersFromDocx(string docxFilePath)
        {
            var detected = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (!File.Exists(docxFilePath)) return new List<string>();

            try
            {
                using var archive = ZipFile.OpenRead(docxFilePath);
                var docEntry = archive.GetEntry("word/document.xml");
                if (docEntry != null)
                {
                    using var stream = docEntry.Open();
                    using var reader = new StreamReader(stream);
                    var xmlContent = reader.ReadToEnd();

                    // Regex untuk mencari {{tag}}
                    var matches = Regex.Matches(xmlContent, @"\{\{([^}]+)\}\}");
                    foreach (Match m in matches)
                    {
                        var key = m.Groups[1].Value.Trim();
                        // Bersihkan tag formatting internal jika ada
                        key = Regex.Replace(key, "<.*?>", string.Empty).Trim();
                        if (!string.IsNullOrEmpty(key) && !key.StartsWith("/"))
                        {
                            detected.Add(key);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Gagal scan docx: {ex.Message}");
            }

            return detected.ToList();
        }

        // ====================================================
        // QR CODE GENERATOR UNTUK VALIDASI KEASLIAN SURAT
        // ====================================================

        public static byte[] GenerateQrCodePng(string verificationUrl)
        {
            using var qrGenerator = new QRCodeGenerator();
            using var qrCodeData = qrGenerator.CreateQrCode(verificationUrl, QRCodeGenerator.ECCLevel.Q);
            using var qrCode = new PngByteQRCode(qrCodeData);
            return qrCode.GetGraphic(5);
        }
    }
}
