using System;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using System.Text;

namespace ClientApp.Services
{
    /// <summary>
    /// Menegakkan ZERO-DOWNLOAD POLICY:
    /// Seluruh pemrosesan teks dokumen dinas, surat keluar, dan spreadsheet RAB diproses
    /// secara murni di dalam memori RAM (MemoryStream) tanpa pernah menulis berkas bebas di hard disk.
    /// </summary>
    public static class ZeroDownloadMemoryService
    {
        public static MemoryStream CreateMemoryDocumentStream(string contentJson)
        {
            var bytes = Encoding.UTF8.GetBytes(contentJson);
            return new MemoryStream(bytes);
        }

        public static string ReadFromMemoryStream(MemoryStream stream)
        {
            stream.Position = 0;
            using var reader = new StreamReader(stream, Encoding.UTF8);
            return reader.ReadToEnd();
        }

        public static void PrintDirectToHardware(string textContent, string printerName, int copies = 1)
        {
            var printDoc = new PrintDocument();
            printDoc.PrinterSettings.PrinterName = printerName;
            printDoc.PrinterSettings.Copies = (short)Math.Max(1, copies);

            var font = new Font("Segoe UI", 10);
            string remainingText = textContent;

            printDoc.PrintPage += (s, e) =>
            {
                if (e.Graphics == null) return;
                var format = new StringFormat();
                var layoutArea = new RectangleF(e.MarginBounds.Left, e.MarginBounds.Top, e.MarginBounds.Width, e.MarginBounds.Height);
                e.Graphics.MeasureString(remainingText, font, layoutArea.Size, format, out int charsFitted, out int linesFilled);
                e.Graphics.DrawString(remainingText.Substring(0, charsFitted), font, Brushes.Black, layoutArea, format);
                remainingText = remainingText.Substring(charsFitted);
                e.HasMorePages = remainingText.Length > 0;
            };

            printDoc.Print();
        }
    }
}
