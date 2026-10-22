using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

namespace ClientApp.Services
{
    public static class ImageCompressionService
    {
        public static byte[] CompressForDocumentArchive(string sourceFilePath, int maxWidth = 1200, int quality = 75)
        {
            using var originalImage = Image.FromFile(sourceFilePath);
            int newWidth = originalImage.Width;
            int newHeight = originalImage.Height;

            if (originalImage.Width > maxWidth)
            {
                float ratio = (float)maxWidth / originalImage.Width;
                newWidth = maxWidth;
                newHeight = (int)(originalImage.Height * ratio);
            }

            using var resizedImage = new Bitmap(newWidth, newHeight);
            using (var graphics = Graphics.FromImage(resizedImage))
            {
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                graphics.DrawImage(originalImage, 0, 0, newWidth, newHeight);
            }

            using var ms = new MemoryStream();
            var jpegEncoder = GetEncoder(ImageFormat.Jpeg);
            if (jpegEncoder != null)
            {
                var encoderParams = new EncoderParameters(1);
                encoderParams.Param[0] = new EncoderParameter(Encoder.Quality, (long)quality);
                resizedImage.Save(ms, jpegEncoder, encoderParams);
                return ms.ToArray();
            }

            resizedImage.Save(ms, ImageFormat.Jpeg);
            return ms.ToArray();
        }

        public static string CompressScanImageLocal(string sourceFilePath, int targetMaxKb = 350)
        {
            var ext = Path.GetExtension(sourceFilePath).ToLowerInvariant();
            if (ext != ".jpg" && ext != ".jpeg" && ext != ".png")
            {
                return sourceFilePath;
            }

            try
            {
                var fileInfo = new FileInfo(sourceFilePath);
                if (fileInfo.Length <= targetMaxKb * 1024)
                {
                    return sourceFilePath;
                }

                var tempPath = Path.Combine(Path.GetTempPath(), $"compressed_scan_{Guid.NewGuid():N}.jpg");
                var bytes = CompressForDocumentArchive(sourceFilePath, 1600, 75);
                File.WriteAllBytes(tempPath, bytes);
                return tempPath;
            }
            catch
            {
                return sourceFilePath;
            }
        }

        private static ImageCodecInfo? GetEncoder(ImageFormat format)
        {
            var codecs = ImageCodecInfo.GetImageDecoders();
            foreach (var codec in codecs)
            {
                if (codec.FormatID == format.Guid)
                {
                    return codec;
                }
            }
            return null;
        }
    }
}
