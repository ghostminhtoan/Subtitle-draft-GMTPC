using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Web.WebView2.Core;

namespace Subtitle_draft_GMTPC.Services
{
    internal static class WebView2EnvironmentProvider
    {
        private static readonly object _sync = new object();
        private static Task<CoreWebView2Environment> _environmentTask;
        private static string _userDataFolder;

        public static Task<CoreWebView2Environment> GetEnvironmentAsync()
        {
            lock (_sync)
            {
                if (_environmentTask == null)
                {
                    _environmentTask = CreateEnvironmentAsync();
                }

                return _environmentTask;
            }
        }

        public static string GetUserDataFolder()
        {
            lock (_sync)
            {
                if (!string.IsNullOrWhiteSpace(_userDataFolder))
                {
                    return _userDataFolder;
                }

                var profileKey = BuildProfileKey();
                _userDataFolder = Path.Combine(
                    Path.GetTempPath(),
                    "GMTPC",
                    "Subtitle Draft GMTPC",
                    "WebView2",
                    profileKey);
                return _userDataFolder;
            }
        }

        private static async Task<CoreWebView2Environment> CreateEnvironmentAsync()
        {
            var userDataFolder = GetUserDataFolder();
            Directory.CreateDirectory(userDataFolder);
            return await CoreWebView2Environment.CreateAsync(null, userDataFolder);
        }

        private static string BuildProfileKey()
        {
            var baseDirectory = AppRuntimePaths.BaseDirectory ?? AppDomain.CurrentDomain.BaseDirectory;
            var normalized = Path.GetFullPath(baseDirectory).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).ToLowerInvariant();
            using (var sha = SHA256.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(normalized);
                var hash = sha.ComputeHash(bytes);
                var builder = new StringBuilder(16);
                for (var i = 0; i < 8 && i < hash.Length; i++)
                {
                    builder.Append(hash[i].ToString("x2"));
                }

                return builder.ToString();
            }
        }
    }
}
