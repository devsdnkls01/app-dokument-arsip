using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace ClientApp.Models
{
    public static class SpellCheckService
    {
        private static readonly Dictionary<string, string> StandardCorrections = new(StringComparer.OrdinalIgnoreCase)
        {
            { "sekedar", "sekadar" },
            { "merubah", "mengubah" },
            { "di karenakan", "dikarenakan" },
            { "di tandatangani", "ditandatangani" },
            { "di terbitkan", "diterbitkan" },
            { "analisa", "analisis" },
            { "kwitansi", "kuitansi" },
            { "silahkan", "silakan" },
            { "hakekat", "hakikat" },
            { "antri", "antre" },
            { "antrian", "antrean" },
            { "apotik", "apotek" },
            { "jadwal", "jadwal" },
            { "resiko", "risiko" },
            { "subyektif", "subjektif" },
            { "obyek", "objek" },
            { "praktek", "praktik" },
            { "atlit", "atlet" },
            { "standarisasi", "standardisasi" },
            { "pertanggung jawaban", "pertanggungjawaban" },
            { "tandatangan", "tanda tangan" },
            { "terimakasih", "terima kasih" }
        };

        public static (string CorrectedText, int ChangesCount) CorrectText(string input)
        {
            if (string.IsNullOrEmpty(input)) return (input, 0);

            string current = input;
            int count = 0;

            foreach (var kvp in StandardCorrections)
            {
                var pattern = $@"\b{Regex.Escape(kvp.Key)}\b";
                if (Regex.IsMatch(current, pattern, RegexOptions.IgnoreCase))
                {
                    current = Regex.Replace(current, pattern, kvp.Value, RegexOptions.IgnoreCase);
                    count++;
                }
            }

            return (current, count);
        }

        public static string CheckAndCorrect(string input, out int count)
        {
            var res = CorrectText(input);
            count = res.ChangesCount;
            return res.CorrectedText;
        }
    }
}
