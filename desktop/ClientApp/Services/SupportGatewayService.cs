using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ClientApp.Models;

namespace ClientApp.Services
{
    public enum SupportIntent
    {
        UpgradeTier,
        TechnicalAssistance,
        AccessReset,
        NewLicenseOrder,
        ExecutiveBulletin
    }

    /// <summary>
    /// Stealth Support & License Upgrade Gateway Service.
    /// Strictly maintains zero phone number visibility on UI and zero raw phone numbers in C# source.
    /// Dispatches users directly to official channels with pre-formatted institutional messages.
    /// </summary>
    public static class SupportGatewayService
    {
        public static void OpenSupportChannel(SupportIntent intent, string? customNote = null, ApiClientService? api = null)
        {
            try
            {
                var session = SessionService.GetSession();
                string tenantName = session?.Tenant?.InstitutionName ?? "Instansi";
                string tenantCode = session?.Tenant?.InstitutionCode ?? "-";
                string tierName = session?.Tier?.DisplayName ?? "Trial";
                string baseUrl = api?.BaseUrl ?? "http://127.0.0.1:8787";

                string intentKey = intent switch
                {
                    SupportIntent.UpgradeTier => "upgrade",
                    SupportIntent.NewLicenseOrder => "purchase",
                    SupportIntent.ExecutiveBulletin => "bulletin",
                    SupportIntent.AccessReset => "reset",
                    _ => "support"
                };

                // Server-Side Dynamic Gateway URL with Context
                string gatewayUrl = $"{baseUrl}/api/v1/sys-prov/contact-gateway?intent={intentKey}" +
                                   $"&tenant_name={Uri.EscapeDataString(tenantName)}" +
                                   $"&tenant_code={Uri.EscapeDataString(tenantCode)}" +
                                   $"&tier_name={Uri.EscapeDataString(tierName)}" +
                                   (!string.IsNullOrWhiteSpace(customNote) ? $"&note={Uri.EscapeDataString(customNote)}" : "");

                Process.Start(new ProcessStartInfo
                {
                    FileName = gatewayUrl,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SupportGatewayService] Failed to launch channel: {ex.Message}");
            }
        }
    }
}
