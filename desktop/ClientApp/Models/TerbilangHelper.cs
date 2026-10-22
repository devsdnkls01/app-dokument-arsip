using System;

namespace ClientApp.Models
{
    public static class TerbilangHelper
    {
        private static readonly string[] Satuan = { "", "Satu", "Dua", "Tiga", "Empat", "Lima", "Enam", "Tujuh", "Delapan", "Sembilan", "Sepuluh", "Sebelas" };

        public static string ToTerbilang(double number)
        {
            if (number < 0) return "Minus " + ToTerbilang(Math.Abs(number));
            long n = (long)Math.Floor(number);
            if (n == 0) return "Nol Rupiah";

            return ConvertNumber(n).Trim() + " Rupiah";
        }

        private static string ConvertNumber(long n)
        {
            if (n < 12)
                return Satuan[n];
            if (n < 20)
                return Satuan[n - 10] + " Belas ";
            if (n < 100)
                return Satuan[n / 10] + " Puluh " + Satuan[n % 10];
            if (n < 200)
                return "Seratus " + ConvertNumber(n - 100);
            if (n < 1000)
                return Satuan[n / 100] + " Ratus " + ConvertNumber(n % 100);
            if (n < 2000)
                return "Seribu " + ConvertNumber(n - 1000);
            if (n < 1000000)
                return ConvertNumber(n / 1000) + " Ribu " + ConvertNumber(n % 1000);
            if (n < 1000000000)
                return ConvertNumber(n / 1000000) + " Juta " + ConvertNumber(n % 1000000);
            if (n < 1000000000000L)
                return ConvertNumber(n / 1000000000) + " Miliar " + ConvertNumber(n % 1000000000);
            if (n < 1000000000000000L)
                return ConvertNumber(n / 1000000000000L) + " Triliun " + ConvertNumber(n % 1000000000000L);

            return n.ToString();
        }
    }
}
