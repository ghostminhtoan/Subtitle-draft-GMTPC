using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Subtitle_draft_GMTPC.Services
{
    /// <summary>
    /// Quản lý cookies cho WebView2
    /// Lưu/Load cookies từ JSON array hoặc Cookie header string
    /// </summary>
    public class CookieManager
    {
        /// <summary>
        /// Parse cookies từ JSON array hoặc Cookie header string thành list
        /// </summary>
        public static Dictionary<string, string> ParseCookiesToDictionary(string rawInput)
        {
            var result = new Dictionary<string, string>();
            if (string.IsNullOrWhiteSpace(rawInput)) return result;

            var trimmed = rawInput.Trim();

            if (trimmed.StartsWith("[") && trimmed.EndsWith("]"))
            {
                return ParseJsonCookiesArray(trimmed);
            }

            return ParseCookieHeader(trimmed);
        }

        /// <summary>
        /// Parse JSON cookies array
        /// </summary>
        private static Dictionary<string, string> ParseJsonCookiesArray(string jsonArray)
        {
            var result = new Dictionary<string, string>();
            try
            {
                var objPattern = @"\{[^{}]*""name""\s*:\s*""([^""]+)""[^{}]*""value""\s*:\s*""([^""]+)""[^{}]*\}";
                var objMatches = Regex.Matches(jsonArray, objPattern, RegexOptions.Singleline);

                foreach (Match objMatch in objMatches)
                {
                    var name = objMatch.Groups[1].Value;
                    var value = objMatch.Groups[2].Value;

                    // Chỉ lấy cookies quan trọng cho chat.qwen.ai
                    if (name == "qwen-theme" || name == "qwen-locale" || name == "x-ap" ||
                        name == "_gcl_au" || name == "_bl_uid" || name == "cnaui" ||
                        name == "ssxmod_itna" || name == "ssxmod_itna2" || name == "atpsida" ||
                        name == "sca")
                    {
                        continue;
                    }

                    result[name] = value;
                }
            }
            catch
            {
            }

            return result;
        }

        /// <summary>
        /// Parse Cookie header string
        /// </summary>
        private static Dictionary<string, string> ParseCookieHeader(string cookieHeader)
        {
            var result = new Dictionary<string, string>();
            if (string.IsNullOrWhiteSpace(cookieHeader)) return result;

            var cleaned = cookieHeader.Replace("\r\n", "").Replace("\n", "").Replace("\r", "");
            var pairs = cleaned.Split(';');

            foreach (var pair in pairs)
            {
                var trimmedPair = pair.Trim();
                var eqIndex = trimmedPair.IndexOf('=');
                if (eqIndex > 0)
                {
                    var name = trimmedPair.Substring(0, eqIndex).Trim();
                    var value = trimmedPair.Substring(eqIndex + 1).Trim();
                    result[name] = value;
                }
            }

            return result;
        }

        /// <summary>
        /// Chuyển dictionary cookies thành Cookie header string
        /// </summary>
        public static string ToCookieHeader(Dictionary<string, string> cookies)
        {
            var sb = new StringBuilder();
            bool found = false;

            // Token trước
            if (cookies.ContainsKey("token"))
            {
                sb.Append($"token={cookies["token"]}");
                found = true;
            }

            foreach (var kvp in cookies)
            {
                if (kvp.Key == "token") continue;
                if (found) sb.Append("; ");
                sb.Append($"{kvp.Key}={kvp.Value}");
                found = true;
            }

            return sb.ToString();
        }

        /// <summary>
        /// Tạo JavaScript để inject cookies vào trang
        /// </summary>
        public static string GenerateCookieInjectScript(Dictionary<string, string> cookies)
        {
            var sb = new StringBuilder();
            sb.AppendLine("(function() {");
            sb.AppendLine("  var cookies = {");

            bool first = true;
            foreach (var kvp in cookies)
            {
                if (!first) sb.AppendLine(",");
                sb.Append($"    \"{EscapeJs(kvp.Key)}\": \"{EscapeJs(kvp.Value)}\"");
                first = false;
            }

            sb.AppendLine();
            sb.AppendLine("  };");
            sb.AppendLine("  for (var key in cookies) {");
            sb.AppendLine("    document.cookie = key + '=' + cookies[key] + '; path=/; domain=.qwen.ai';");
            sb.AppendLine("    document.cookie = key + '=' + cookies[key] + '; path=/; domain=chat.qwen.ai';");
            sb.AppendLine("  }");
            sb.AppendLine("  return 'Đã inject ' + Object.keys(cookies).length + ' cookies';");
            sb.AppendLine("})();");

            return sb.ToString();
        }

        /// <summary>
        /// Tạo JavaScript để điền prompt và subtitle vào ô input
        /// </summary>
        public static string GenerateInputScript(string prompt, string subtitleText)
        {
            var combinedText = prompt + Environment.NewLine + Environment.NewLine + subtitleText;
            var escapedText = EscapeJs(combinedText);

            var sb = new StringBuilder();
            sb.AppendLine("(function() {");
            sb.AppendLine("  var textarea = document.querySelector('textarea');");
            sb.AppendLine("  if (!textarea) return 'Không tìm thấy ô nhập liệu';");
            sb.AppendLine($"  textarea.value = '{escapedText}';");
            sb.AppendLine("  textarea.dispatchEvent(new Event('input', { bubbles: true }));");
            sb.AppendLine("  textarea.dispatchEvent(new Event('change', { bubbles: true }));");
            sb.AppendLine($"  return 'Đã điền {combinedText.Length} ký tự vào ô nhập liệu';");
            sb.AppendLine("})();");

            return sb.ToString();
        }

        private static string EscapeJs(string input)
        {
            return input.Replace("\\", "\\\\").Replace("'", "\\'").Replace("\r", "\\n").Replace("\n", "\\n").Replace("\t", "    ");
        }
    }
}
