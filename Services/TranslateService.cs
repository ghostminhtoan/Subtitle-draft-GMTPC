using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Subtitle_draft_GMTPC.Services
{
    /// <summary>
    /// Service dịch thuật Google Translate đơn giản, ổn định
    /// </summary>
    public class TranslateService
    {
        private static readonly string UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36";

        /// <summary>
        /// Dịch text đơn giản
        /// </summary>
        public static async Task<string> TranslateTextAsync(string text, string sourceLang, string targetLang)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;

            try
            {
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("User-Agent", UserAgent);
                    client.Timeout = TimeSpan.FromSeconds(30);

                    var encodedText = Uri.EscapeDataString(text);
                    var url = $"https://translate.googleapis.com/translate_a/single?client=gtx&sl={sourceLang}&tl={targetLang}&dt=t&q={encodedText}";

                    var response = await client.GetAsync(url);
                    if (response.IsSuccessStatusCode)
                    {
                        var json = await response.Content.ReadAsStringAsync();
                        return ExtractTranslations(json);
                    }
                    else
                    {
                        return $"[HTTP {response.StatusCode}]";
                    }
                }
            }
            catch (TaskCanceledException)
            {
                return "[Timeout]";
            }
            catch (Exception ex)
            {
                return $"[Lỗi: {ex.Message}]";
            }
        }

        /// <summary>
        /// Extract bản dịch từ JSON Google Translate
        /// Response: [[["dịch1","gốc1",...],["dịch2","gốc2",...]],null,"en"]
        /// Regex match: match 0=dịch1, match 1=gốc1, match 2=dịch2, match 3=gốc2...
        /// Lấy match chẵn: 0, 2, 4... (translation)
        /// </summary>
        private static string ExtractTranslations(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return string.Empty;

            try
            {
                var sb = new StringBuilder();
                bool found = false;

                // Pattern tìm string đầu tiên sau [" trong JSON
                var pattern = @"\[""((?:[^""\\]|\\.)*)""";
                var matches = Regex.Matches(json, pattern);

                // Lấy match ở vị trí chẵn: 0, 2, 4... (translation)
                // Bỏ match lẻ: 1, 3, 5... (original text)
                int idx = 0;
                while (idx < matches.Count)
                {
                    var text = matches[idx].Groups[1].Value;
                    text = text.Replace("\\n", "\n").Replace("\\r", "\r").Replace("\\t", "\t").Replace("\\\"", "\"").Replace("\\\\", "\\");

                    if (found) sb.Append("\n");
                    sb.Append(text);
                    found = true;

                    idx += 2; // Nhảy 2: bỏ original, lấy translation tiếp theo
                }

                if (found) return sb.ToString();
                return json.Substring(0, Math.Min(200, json.Length));

            }
            catch
            {
                return json;
            }
        }
    }
}
