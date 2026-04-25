using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;

namespace Subtitle_draft_GMTPC
{
    public partial class MainWindow : Window
    {
        #region Updater - Fields

        private const string LatestExeApiUrl = "https://api.github.com/repos/ghostminhtoan/Subtitle-draft-GMTPC/commits?path=Subtitle%20draft%20GMTPC.exe&page=1&per_page=1";
        private const string LatestExeDownloadUrl = "https://raw.githubusercontent.com/ghostminhtoan/Subtitle-draft-GMTPC/master/Subtitle%20draft%20GMTPC.exe";
        private static readonly HttpClient _updateHttpClient = CreateUpdateHttpClient();
        private bool _isUpdateChecking = false;

        #endregion

        #region Updater - Event Handlers

        private async void BtnUpdateLatestVersion_Click(object sender, RoutedEventArgs e)
        {
            await CheckAndUpdateLatestVersionAsync();
        }

        #endregion

        #region Updater - Workflow

        private async Task CheckAndUpdateLatestVersionAsync()
        {
            if (_isUpdateChecking)
            {
                return;
            }

            try
            {
                _isUpdateChecking = true;
                BtnUpdateLatestVersion.IsEnabled = false;
                BtnUpdateLatestVersion.Content = "⏳ Checking...";

                var currentExePath = Process.GetCurrentProcess().MainModule.FileName;
                var currentBuildTimeUtc = File.GetLastWriteTimeUtc(currentExePath);
                var latestServerTimeUtc = await GetLatestServerBuildTimeUtcAsync();

                if (latestServerTimeUtc <= currentBuildTimeUtc)
                {
                    MessageBox.Show(
                        string.Format(
                            "Đã là bản latest.{0}{0}Build local: {1}{0}Build server: {2}",
                            Environment.NewLine,
                            currentBuildTimeUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
                            latestServerTimeUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)),
                        "Update latest version",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    return;
                }

                var currentFolder = Path.GetDirectoryName(currentExePath) ?? AppDomain.CurrentDomain.BaseDirectory;
                var latestExePath = Path.Combine(currentFolder, "Subtitle draft GMTPC latest.exe");

                await DownloadLatestExeAsync(latestExePath);
                PrepareLatestVersionSwapAndRestart(currentExePath, latestExePath);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không thể cập nhật phiên bản mới: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                if (BtnUpdateLatestVersion != null)
                {
                    BtnUpdateLatestVersion.IsEnabled = true;
                    BtnUpdateLatestVersion.Content = "⬇️ Update latest version";
                }

                _isUpdateChecking = false;
            }
        }

        private static HttpClient CreateUpdateHttpClient()
        {
            var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(60);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("SubtitleDraftGMTPC/1.0");
            return client;
        }

        private async Task<DateTime> GetLatestServerBuildTimeUtcAsync()
        {
            using (var request = new HttpRequestMessage(HttpMethod.Get, LatestExeApiUrl))
            {
                request.Headers.CacheControl = new CacheControlHeaderValue
                {
                    NoCache = true,
                    NoStore = true,
                    MaxAge = TimeSpan.Zero
                };
                request.Headers.Pragma.ParseAdd("no-cache");

                using (var response = await _updateHttpClient.SendAsync(request))
                {
                    response.EnsureSuccessStatusCode();
                    var json = await response.Content.ReadAsStringAsync();

                    var dateMatch = Regex.Match(json, "\"date\"\\s*:\\s*\"(?<date>[^\"]+)\"", RegexOptions.IgnoreCase);
                    if (!dateMatch.Success)
                    {
                        throw new InvalidOperationException("Không đọc được build time từ server.");
                    }

                    if (!DateTime.TryParse(dateMatch.Groups["date"].Value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var serverTimeUtc))
                    {
                        throw new InvalidOperationException("Build time trên server không hợp lệ.");
                    }

                    return serverTimeUtc.ToUniversalTime();
                }
            }
        }

        private async Task DownloadLatestExeAsync(string destinationPath)
        {
            using (var request = new HttpRequestMessage(HttpMethod.Get, LatestExeDownloadUrl + "?t=" + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()))
            {
                request.Headers.CacheControl = new CacheControlHeaderValue
                {
                    NoCache = true,
                    NoStore = true,
                    MaxAge = TimeSpan.Zero
                };
                request.Headers.Pragma.ParseAdd("no-cache");

                using (var response = await _updateHttpClient.SendAsync(request))
                {
                    response.EnsureSuccessStatusCode();
                    var bytes = await response.Content.ReadAsByteArrayAsync();
                    File.WriteAllBytes(destinationPath, bytes);
                }
            }
        }

        private void PrepareLatestVersionSwapAndRestart(string currentExePath, string latestExePath)
        {
            if (!File.Exists(latestExePath))
            {
                throw new FileNotFoundException("Không tìm thấy file update vừa tải.", latestExePath);
            }

            var currentFolder = Path.GetDirectoryName(currentExePath) ?? AppDomain.CurrentDomain.BaseDirectory;
            var updaterScriptPath = Path.Combine(currentFolder, "update_latest_version.cmd");

            var script = "@echo off" + Environment.NewLine
                + "setlocal" + Environment.NewLine
                + "set \"TARGET=" + currentExePath + "\"" + Environment.NewLine
                + "set \"LATEST=" + latestExePath + "\"" + Environment.NewLine
                + "set \"SCRIPT=%~f0\"" + Environment.NewLine
                + ":wait_old" + Environment.NewLine
                + "tasklist /FI \"IMAGENAME eq Subtitle draft GMTPC.exe\" | find /I \"Subtitle draft GMTPC.exe\" >nul" + Environment.NewLine
                + "if not errorlevel 1 (" + Environment.NewLine
                + "  timeout /t 1 /nobreak >nul" + Environment.NewLine
                + "  goto wait_old" + Environment.NewLine
                + ")" + Environment.NewLine
                + ":replace_file" + Environment.NewLine
                + "if exist \"%TARGET%\" del /f /q \"%TARGET%\"" + Environment.NewLine
                + "if exist \"%TARGET%\" (" + Environment.NewLine
                + "  timeout /t 1 /nobreak >nul" + Environment.NewLine
                + "  goto replace_file" + Environment.NewLine
                + ")" + Environment.NewLine
                + "rename \"%LATEST%\" \"Subtitle draft GMTPC.exe\"" + Environment.NewLine
                + "start \"\" \"%TARGET%\"" + Environment.NewLine
                + "del \"%SCRIPT%\"" + Environment.NewLine;

            File.WriteAllText(updaterScriptPath, script);

            Process.Start(new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = "/c \"" + updaterScriptPath + "\"",
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                WorkingDirectory = currentFolder
            });

            Application.Current.Shutdown();
        }

        #endregion
    }
}
