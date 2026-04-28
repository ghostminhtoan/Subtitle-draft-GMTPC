using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Web.WebView2.Core;

namespace Subtitle_draft_GMTPC.Services
{
    internal static class WebView2RuntimeManager
    {
        private const string BootstrapperUrl = "https://go.microsoft.com/fwlink/p/?LinkId=2124703";
        private static readonly HttpClient _httpClient = CreateHttpClient();

        public static async Task EnsureInstalledAsync()
        {
            if (IsRuntimeAvailable())
            {
                return;
            }

            var installerPath = Path.Combine(
                Path.GetTempPath(),
                "GMTPC-WebView2",
                "MicrosoftEdgeWebview2Setup.exe");

            Directory.CreateDirectory(Path.GetDirectoryName(installerPath));
            await DownloadBootstrapperAsync(installerPath);
            await RunBootstrapperAsync(installerPath);

            if (!IsRuntimeAvailable())
            {
                throw new InvalidOperationException("WebView2 Runtime vẫn chưa sẵn sàng sau khi cài đặt.");
            }
        }

        private static bool IsRuntimeAvailable()
        {
            try
            {
                return !string.IsNullOrWhiteSpace(CoreWebView2Environment.GetAvailableBrowserVersionString());
            }
            catch
            {
                return false;
            }
        }

        private static async Task DownloadBootstrapperAsync(string installerPath)
        {
            using (var response = await _httpClient.GetAsync(BootstrapperUrl))
            {
                response.EnsureSuccessStatusCode();
                using (var stream = await response.Content.ReadAsStreamAsync())
                using (var fileStream = new FileStream(installerPath, FileMode.Create, FileAccess.Write, FileShare.Read))
                {
                    await stream.CopyToAsync(fileStream);
                }
            }
        }

        private static async Task RunBootstrapperAsync(string installerPath)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = installerPath,
                Arguments = "/silent /install",
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            };

            using (var process = Process.Start(startInfo))
            {
                if (process == null)
                {
                    throw new InvalidOperationException("Không thể khởi chạy WebView2 bootstrapper.");
                }

                await Task.Run(() => process.WaitForExit());

                if (process.ExitCode != 0)
                {
                    throw new InvalidOperationException("WebView2 bootstrapper trả về mã lỗi: " + process.ExitCode);
                }
            }
        }

        private static HttpClient CreateHttpClient()
        {
            var client = new HttpClient();
            client.Timeout = TimeSpan.FromMinutes(5);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("SubtitleDraftGMTPC/1.0");
            return client;
        }
    }
}
