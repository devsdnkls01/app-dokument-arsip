using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ClientApp.Models;

namespace ClientApp.Services
{
    public static class SessionService
    {
        private static readonly string StateDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SovereignSuite"
        );

        private static readonly string NodeStateFile = Path.Combine(StateDirectory, "node_state.dat");
        private static readonly byte[] EntropyBytes = Encoding.UTF8.GetBytes("SovereignExecutiveSuite.EnterpriseNodeState.v3");

        public static ClientSession? GetSession()
        {
            if (!File.Exists(NodeStateFile)) return null;

            try
            {
                var encryptedBytes = File.ReadAllBytes(NodeStateFile);
                var decryptedBytes = ProtectedData.Unprotect(encryptedBytes, EntropyBytes, DataProtectionScope.CurrentUser);
                var json = Encoding.UTF8.GetString(decryptedBytes);
                return JsonSerializer.Deserialize<ClientSession>(json);
            }
            catch
            {
                return null;
            }
        }

        public static void SaveSession(ClientSession session)
        {
            try
            {
                Directory.CreateDirectory(StateDirectory);
                var json = JsonSerializer.Serialize(session);
                var plainBytes = Encoding.UTF8.GetBytes(json);
                var encryptedBytes = ProtectedData.Protect(plainBytes, EntropyBytes, DataProtectionScope.CurrentUser);
                File.WriteAllBytes(NodeStateFile, encryptedBytes);
            }
            catch { }
        }

        public static void ClearSession()
        {
            if (File.Exists(NodeStateFile))
            {
                try
                {
                    File.Delete(NodeStateFile);
                }
                catch { }
            }
        }

        public static bool IsLoggedIn() => GetSession() != null && !string.IsNullOrEmpty(GetSession()?.Token);
    }
}
