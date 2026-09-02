using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using Subtitle_draft_GMTPC.Services;

namespace Subtitle_draft_GMTPC
{
    /// <summary>
    /// Interaction logic for AddWordListRulesWindow.xaml
    /// </summary>
    public partial class AddWordListRulesWindow : Window
    {
        private readonly string _wordListFilePath;
        private readonly Action<string> _onRulesUpdated;

        public AddWordListRulesWindow(string wordListFilePath, Action<string> onRulesUpdated)
        {
            InitializeComponent();
            _wordListFilePath = wordListFilePath;
            _onRulesUpdated = onRulesUpdated;
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            var input = TxtRulesInput.Text;
            if (string.IsNullOrWhiteSpace(input))
            {
                TxtStatus.Text = "⚠️ Vui lòng nhập ít nhất một quy tắc.";
                TxtStatus.Foreground = System.Windows.Media.Brushes.Orange;
                return;
            }

            try
            {
                // Parse các rules mới từ input
                var newRules = ParseInputRules(input);
                if (newRules.Count == 0)
                {
                    TxtStatus.Text = "⚠️ Không tìm thấy quy tắc hợp lệ (đúng format word:part1/part2).";
                    TxtStatus.Foreground = System.Windows.Media.Brushes.Orange;
                    return;
                }

                // Đọc file word list hiện tại hoặc default rules
                string currentContent = "";
                if (File.Exists(_wordListFilePath))
                {
                    currentContent = File.ReadAllText(_wordListFilePath);
                }
                else
                {
                    currentContent = WordListRules.DefaultRules;
                }

                // Merge rules mới vào full word list
                var allRules = ParseFullWordList(currentContent);
                int addedCount = 0;
                int updatedCount = 0;

                foreach (var kvp in newRules)
                {
                    if (allRules.ContainsKey(kvp.Key))
                    {
                        allRules[kvp.Key] = kvp.Value;
                        updatedCount++;
                    }
                    else
                    {
                        allRules[kvp.Key] = kvp.Value;
                        addedCount++;
                    }
                }

                // Ghi lại vào file theo thứ tự alphabet
                var sb = new StringBuilder();
                foreach (var kvp in allRules.OrderBy(k => k.Key, StringComparer.OrdinalIgnoreCase))
                {
                    sb.AppendLine(string.Format("{0}:{1}", kvp.Key, kvp.Value));
                }

                var updatedText = sb.ToString();
                File.WriteAllText(_wordListFilePath, updatedText);

                _onRulesUpdated?.Invoke(updatedText);

                TxtStatus.Text = string.Format("✅ Đã cập nhật thành công! (Thêm mới: {0}, Cập nhật: {1})", addedCount, updatedCount);
                TxtStatus.Foreground = System.Windows.Media.Brushes.LightGreen;
                TxtRulesInput.Text = "";
            }
            catch (Exception ex)
            {
                TxtStatus.Text = "❌ Lỗi: " + ex.Message;
                TxtStatus.Foreground = System.Windows.Media.Brushes.Red;
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private static Dictionary<string, string> ParseInputRules(string text)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrWhiteSpace(text)) return result;

            // Hỗ trợ cả nhiều format:
            // 1. (word:part1/part2)
            // 2. word:part1/part2 (xuống dòng hoặc phân cách bởi dấu phẩy, chấm phẩy)
            var lines = text.Split(new[] { "\r\n", "\r", "\n", ",", ";" }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var rawItem in lines)
            {
                var trimmed = rawItem.Trim();
                if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith("#")) continue;

                // Kiểm tra nếu có ngoặc (word:part1/part2)
                var matches = Regex.Matches(trimmed, @"\(([^)]+)\)");
                if (matches.Count > 0)
                {
                    foreach (Match m in matches)
                    {
                        AddSingleRule(m.Groups[1].Value, result);
                    }
                }
                else
                {
                    AddSingleRule(trimmed, result);
                }
            }

            return result;
        }

        private static void AddSingleRule(string item, Dictionary<string, string> dict)
        {
            var colonIndex = item.IndexOf(':');
            if (colonIndex <= 0) return;

            var word = item.Substring(0, colonIndex).Trim().ToLowerInvariant();
            var split = item.Substring(colonIndex + 1).Trim();

            if (!string.IsNullOrEmpty(word) && !string.IsNullOrEmpty(split))
            {
                dict[word] = split;
            }
        }

        private static Dictionary<string, string> ParseFullWordList(string content)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrWhiteSpace(content)) return result;

            var lines = content.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith("#")) continue;

                var colonIndex = trimmed.IndexOf(':');
                if (colonIndex > 0)
                {
                    var word = trimmed.Substring(0, colonIndex).Trim().ToLowerInvariant();
                    var split = trimmed.Substring(colonIndex + 1).Trim();
                    if (!string.IsNullOrEmpty(word) && !string.IsNullOrEmpty(split))
                    {
                        result[word] = split;
                    }
                }
            }

            return result;
        }
    }
}
