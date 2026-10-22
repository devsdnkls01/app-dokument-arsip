using System;
using System.Collections.Generic;
using System.Drawing.Printing;
using System.Linq;

namespace ClientApp.Services
{
    /// <summary>
    /// Hardware Spooler Filter:
    /// Memverifikasi dan memfilter spooler perangkat keras fisik guna memastikan
    /// integritas dan keaslian naskah kedinasan sesuai standar tata naskah resmi.
    /// </summary>
    public static class HardwareSpoolerFilter
    {
        private static readonly string[] NonPhysicalDrivers =
        {
            "pdf",
            "xps",
            "onenote",
            "fax",
            "document writer",
            "virtual",
            "print to",
            "cutepdf",
            "foxit",
            "bullzip",
            "primopdf",
            "dopdf",
            "nitro"
        };

        public static List<string> GetPhysicalSpoolers()
        {
            var results = new List<string>();

            foreach (string printer in PrinterSettings.InstalledPrinters)
            {
                var lower = printer.ToLowerInvariant();
                bool isNonPhysical = NonPhysicalDrivers.Any(k => lower.Contains(k));

                if (!isNonPhysical)
                {
                    results.Add(printer);
                }
            }

            return results;
        }

        public static bool VerifyPhysicalDevice(string printerName)
        {
            var lower = printerName.ToLowerInvariant();
            return !NonPhysicalDrivers.Any(k => lower.Contains(k));
        }
    }

    public static class PrinterFilterService
    {
        public static List<string> GetPhysicalPrinters() => HardwareSpoolerFilter.GetPhysicalSpoolers();
        public static bool IsPhysicalPrinter(string printerName) => HardwareSpoolerFilter.VerifyPhysicalDevice(printerName);
    }
}
