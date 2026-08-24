using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Subtitle_draft_GMTPC
{
    public partial class MainWindow : Window
    {
#region "Dialogue Normalize - Fields"

        private bool _isDialogueNormUpdating = false;
        // Lưu trữ danh sách mục parsed từ Panel 1: STT và Text
        private List<DialogueNormItem> _dialogueNormInputItems = new List<DialogueNormItem>();

        private class DialogueNormItem
        {
            public string Index { get; set; }
            public string OriginalText { get; set; }
        }

#endregion

#region "Dialogue Normalize - Event Handlers"

        private void TxtDialogueNormInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isDialogueNormUpdating) return;
            try
            {
                _isDialogueNormUpdating = true;
                ParsePanel1AndGeneratePanel2();
                GeneratePanel4();
            }
            catch (Exception ex)
            {
                TxtDialogueNormSentence.Text = "Lỗi: " + ex.Message;
            }
            finally
            {
                _isDialogueNormUpdating = false;
            }
        }

        private void TxtDialogueNormEdited_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isDialogueNormUpdating) return;
            try
            {
                _isDialogueNormUpdating = true;
                GeneratePanel4();
            }
            catch (Exception ex)
            {
                TxtDialogueNormOutput.Text = "Lỗi: " + ex.Message;
            }
            finally
            {
                _isDialogueNormUpdating = false;
            }
        }

        private void BtnCopyDialogueNormSentence_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtDialogueNormSentence.Text)) return;
            try
            {
                Clipboard.SetText(TxtDialogueNormSentence.Text);
                ShowToastDialogueNorm("📋 Đã copy Sentence Dialogue!");
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Lỗi: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnCopyDialogueNormOutput_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtDialogueNormOutput.Text)) return;
            try
            {
                Clipboard.SetText(TxtDialogueNormOutput.Text);
                ShowToastDialogueNorm("📋 Đã copy Output Dialogue!");
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Lỗi: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

#endregion

#region "Dialogue Normalize - Processing Logic"

        private void ParsePanel1AndGeneratePanel2()
        {
            _dialogueNormInputItems.Clear();
            string raw = TxtDialogueNormInput.Text;
            if (string.IsNullOrWhiteSpace(raw))
            {
                TxtDialogueNormInputCount.Text = "";
                TxtDialogueNormSentence.Text = "";
                TxtDialogueNormEdited.Text = "";
                TxtDialogueNormOutput.Text = "";
                return;
            }

            var lines = raw.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            var sbPanel2 = new StringBuilder();
            int validCount = 0;

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (string.IsNullOrEmpty(line)) continue;

                // Tách số thứ tự ở đầu dòng (hỗ trợ tab, space, dấu chấm hoặc bất kỳ khoảng trắng nào)
                var match = Regex.Match(line, @"^(\d+)[.\s\t]+(.*)$");
                string indexStr = "";
                string content = "";

                if (match.Success)
                {
                    indexStr = match.Groups[1].Value;
                    content = match.Groups[2].Value.Trim();
                }
                else
                {
                    // Trường hợp chỉ có số hoặc không có số
                    if (Regex.IsMatch(line, @"^\d+$"))
                    {
                        indexStr = line;
                        content = "";
                    }
                    else
                    {
                        validCount++;
                        indexStr = validCount.ToString();
                        content = line;
                    }
                }

                _dialogueNormInputItems.Add(new DialogueNormItem
                {
                    Index = indexStr,
                    OriginalText = content
                });

                if (!string.IsNullOrEmpty(content))
                {
                    if (sbPanel2.Length == 0)
                    {
                        // Bắt đầu dòng đầu tiên với ∞
                        sbPanel2.Append("∞");
                    }
                    else
                    {
                        // Kiểm tra từ/ký tự trước đó kết thúc bằng dấu ngắt câu
                        string currentText = sbPanel2.ToString();
                        if (EndsWithSentenceBoundary(currentText))
                        {
                            sbPanel2.Append("\r\n∞");
                        }
                        else
                        {
                            sbPanel2.Append("♫");
                        }
                    }

                    sbPanel2.Append(content);
                }
            }

            TxtDialogueNormInputCount.Text = string.Format("({0} mục)", _dialogueNormInputItems.Count);
            string sentenceResult = sbPanel2.ToString();
            TxtDialogueNormSentence.Text = sentenceResult;
            // Tự động đồng bộ sang Panel 3 nếu Panel 3 đang trống hoặc người dùng vừa nhập mới Panel 1
            TxtDialogueNormEdited.Text = sentenceResult;
        }

        private static bool EndsWithSentenceBoundary(string text)
        {
            if (string.IsNullOrEmpty(text)) return false;
            text = text.TrimEnd();
            return text.EndsWith(".") || text.EndsWith("!") || text.EndsWith("?") ||
                   text.EndsWith(":") || text.EndsWith("...") || text.EndsWith("…") ||
                   text.EndsWith(";") || text.EndsWith("。") || text.EndsWith("！") ||
                   text.EndsWith("？") || text.EndsWith("：");
        }

        private void GeneratePanel4()
        {
            string edited = TxtDialogueNormEdited.Text;
            if (string.IsNullOrWhiteSpace(edited))
            {
                TxtDialogueNormOutput.Text = "";
                return;
            }

            // Tách tokens dựa trên dấu phân cách ∞ và ♫
            var rawTokens = edited.Split(new[] { '∞', '♫' }, StringSplitOptions.None);
            var words = new List<string>();
            foreach (var t in rawTokens)
            {
                string clean = t.Trim();
                if (!string.IsNullOrEmpty(clean))
                {
                    words.Add(clean);
                }
            }

            var sbOutput = new StringBuilder();
            int total = Math.Max(_dialogueNormInputItems.Count, words.Count);

            for (int i = 0; i < total; i++)
            {
                string stt = (i < _dialogueNormInputItems.Count && !string.IsNullOrEmpty(_dialogueNormInputItems[i].Index))
                    ? _dialogueNormInputItems[i].Index
                    : (i + 1).ToString();

                string word = (i < words.Count) ? words[i] : "";

                sbOutput.AppendLine(string.Format("{0}\t{1}", stt, word));
            }

            TxtDialogueNormOutput.Text = sbOutput.ToString().TrimEnd();
        }

        private async void ShowToastDialogueNorm(string message)
        {
            try
            {
                if (ToastBorderDialogueNorm == null || ToastTextDialogueNorm == null) return;
                ToastTextDialogueNorm.Text = message;
                ToastBorderDialogueNorm.Visibility = Visibility.Visible;
                await Task.Delay(2000);
                ToastBorderDialogueNorm.Visibility = Visibility.Collapsed;
            }
            catch { }
        }

#endregion
    }
}
