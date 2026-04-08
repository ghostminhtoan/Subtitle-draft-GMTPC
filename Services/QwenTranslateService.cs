using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Subtitle_draft_GMTPC.Services
{
    /// <summary>
    /// Service dịch thuật qua chat.qwen.ai sử dụng browser automation
    /// </summary>
    public class QwenTranslateService
    {
        private static readonly string UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36";

        /// <summary>
        /// Dịch subtitle qua chat.qwen.ai
        /// </summary>
        public static async Task<string> TranslateAsync(string subtitleText, string prompt, string cookies)
        {
            if (string.IsNullOrWhiteSpace(subtitleText)) return "Không có nội dung để dịch.";
            if (string.IsNullOrWhiteSpace(cookies)) return "Vui lòng nhập cookies trình duyệt.";

            try
            {
                var cleanCookies = ParseCookies(cookies);
                if (cleanCookies.StartsWith("["))
                {
                    return cleanCookies; // Parse error
                }

                var fullPrompt = prompt + Environment.NewLine + Environment.NewLine + subtitleText;
                var escapedPrompt = EscapeJson(fullPrompt);
                var jsonBody = $"{{\"model\":\"qwen3.6-plus\",\"messages\":[{{\"role\":\"user\",\"content\":\"{escapedPrompt}\"}}],\"temperature\":0.3,\"max_tokens\":8000,\"stream\":false}}";

                var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Clear();
                    client.DefaultRequestHeaders.Add("User-Agent", UserAgent);
                    client.DefaultRequestHeaders.Add("Accept", "*/*");
                    client.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.9");
                    client.DefaultRequestHeaders.Add("Origin", "https://chat.qwen.ai");
                    client.DefaultRequestHeaders.Add("Referer", "https://chat.qwen.ai/");
                    client.DefaultRequestHeaders.Add("Sec-Fetch-Dest", "empty");
                    client.DefaultRequestHeaders.Add("Sec-Fetch-Mode", "cors");
                    client.DefaultRequestHeaders.Add("Sec-Fetch-Site", "same-origin");
                    client.DefaultRequestHeaders.Add("Cookie", cleanCookies);
                    client.Timeout = TimeSpan.FromMinutes(3);

                    // Thử nhiều endpoint
                    var endpoints = new[] {
                        "https://chat.qwen.ai/api/chat/completions",
                        "https://chat.qwen.ai/v1/chat/completions",
                        "https://chat.qwen.ai/api/generate"
                    };

                    foreach (var endpoint in endpoints)
                    {
                        try
                        {
                            var clonedContent = new StringContent(content.ReadAsStringAsync().Result, Encoding.UTF8, "application/json");
                            var response = await client.PostAsync(endpoint, clonedContent);
                            var responseJson = await response.Content.ReadAsStringAsync();

                            if (response.IsSuccessStatusCode)
                            {
                                var result = ExtractResponseContent(responseJson);
                                if (!string.IsNullOrEmpty(result) && !result.StartsWith("["))
                                {
                                    return result;
                                }
                            }
                        }
                        catch
                        {
                            // Thử endpoint tiếp theo
                        }
                    }

                    return "[Lỗi] Không thể kết nối đến chat.qwen.ai. Vui lòng kiểm tra cookies hoặc thử lại sau.";
                }

            }
            catch (TaskCanceledException)
            {
                return "[Timeout] Yêu cầu dịch thuật hết thời gian chờ (3 phút).";
            }
            catch (Exception ex)
            {
                return $"[Lỗi: {ex.Message}]";
            }
        }

        /// <summary>
        /// Parse cookies từ JSON array hoặc Cookie header string
        /// </summary>
        private static string ParseCookies(string rawInput)
        {
            if (string.IsNullOrWhiteSpace(rawInput)) return "";

            var trimmed = rawInput.Trim();
            if (trimmed.StartsWith("[") && trimmed.EndsWith("]"))
            {
                return ParseJsonCookiesArray(trimmed);
            }

            return CleanCookieHeader(trimmed);
        }

        /// <summary>
        /// Parse JSON cookies array
        /// </summary>
        private static string ParseJsonCookiesArray(string jsonArray)
        {
            try
            {
                var cookieDict = new Dictionary<string, string>();
                var objPattern = @"\{[^{}]*""name""\s*:\s*""([^""]+)""[^{}]*""value""\s*:\s*""([^""]+)""[^{}]*\}";
                var objMatches = Regex.Matches(jsonArray, objPattern, RegexOptions.Singleline);

                foreach (Match objMatch in objMatches)
                {
                    var name = objMatch.Groups[1].Value;
                    var value = objMatch.Groups[2].Value;

                    // Chỉ lấy cookies quan trọng
                    if (name == "qwen-theme" || name == "qwen-locale" || name == "x-ap" ||
                        name == "_gcl_au" || name == "_bl_uid" || name == "cnaui" ||
                        name == "ssxmod_itna" || name == "ssxmod_itna2" || name == "atpsida" ||
                        name == "sca")
                    {
                        continue;
                    }

                    cookieDict[name] = value;
                }

                var sb = new StringBuilder();
                bool found = false;

                if (cookieDict.ContainsKey("token"))
                {
                    sb.Append($"token={cookieDict["token"]}");
                    found = true;
                }

                foreach (var kvp in cookieDict)
                {
                    if (kvp.Key == "token") continue;
                    if (found) sb.Append("; ");
                    sb.Append($"{kvp.Key}={kvp.Value}");
                    found = true;
                }

                return sb.ToString();

            }
            catch (Exception ex)
            {
                return $"[Parse error: {ex.Message}]";
            }
        }

        /// <summary>
        /// Làm sạch Cookie header
        /// </summary>
        private static string CleanCookieHeader(string rawCookies)
        {
            var cleaned = rawCookies;
            if (cleaned.StartsWith("Cookie:", StringComparison.OrdinalIgnoreCase))
            {
                cleaned = cleaned.Substring(7).Trim();
            }
            else if (cleaned.StartsWith("Cookie: ", StringComparison.OrdinalIgnoreCase))
            {
                cleaned = cleaned.Substring(8).Trim();
            }

            cleaned = cleaned.Replace("\r\n", "").Replace("\n", "").Replace("\r", "");
            cleaned = Regex.Replace(cleaned, @"\s+", " ").Trim();
            cleaned = Regex.Replace(cleaned, @"\s*;\s*", "; ").Trim();
            cleaned = Regex.Replace(cleaned, @";+\s*", "; ").Trim();

            if (cleaned.EndsWith(";"))
            {
                cleaned = cleaned.TrimEnd(';').Trim();
            }

            return cleaned;
        }

        /// <summary>
        /// Escape JSON string
        /// </summary>
        private static string EscapeJson(string input)
        {
            if (string.IsNullOrEmpty(input)) return "";
            return input.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "\\r").Replace("\n", "\\n").Replace("\t", "\\t");
        }

        /// <summary>
        /// Extract response content
        /// </summary>
        private static string ExtractResponseContent(string json)
        {
            try
            {
                var pattern = @"""content""\s*:\s*""((?:[^""\\]|\\.)*)""";
                var matches = Regex.Matches(json, pattern);

                if (matches.Count > 0)
                {
                    var lastMatch = matches[matches.Count - 1];
                    var content = lastMatch.Groups[1].Value;
                    content = content.Replace("\\n", "\n").Replace("\\r", "\r").Replace("\\t", "\t").Replace("\\\"", "\"").Replace("\\\\", "\\");
                    return content;
                }

                return "";
            }
            catch
            {
                return "";
            }
        }
    }
}
