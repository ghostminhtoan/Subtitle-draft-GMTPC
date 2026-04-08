using System;
using System.Windows;
using System.Windows.Controls;
using System.Threading.Tasks;
using System.Text;
using System.Collections.Generic;
using Subtitle_draft_GMTPC.Models;
using Subtitle_draft_GMTPC.Services;

namespace Subtitle_draft_GMTPC
{
    public partial class MainWindow : Window
    {

#region "Dialogue Only - Fields"

    private List<SubtitleLine> _dialogueLines = new List<SubtitleLine>();
    private SubtitleFormat _dialogueFormat = SubtitleFormat.Unknown;
    private bool _isDialogueUpdating = false;
    private Dictionary<int, string> _dialogueManualTexts = new Dictionary<int, string>();

#endregion

#region "Dialogue Only - Event Handlers"

    private void TxtDialogueInput_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_isDialogueUpdating) return;
        try
        {
            _isDialogueUpdating = true;
            var content = SubtitleParser.SanitizeContent(TxtDialogueInput.Text);
            if (string.IsNullOrWhiteSpace(content))
            {
                _dialogueLines.Clear();
                _dialogueFormat = SubtitleFormat.Unknown;
                TxtDialogueFormat.Text = "";
                TxtDialogueOutput.Text = "";
                TxtDialogueMerged.Text = "";
                return;
            }
            _dialogueFormat = SubtitleParser.DetectFormat(content);
            _dialogueLines = SubtitleParser.Parse(content);
            TxtDialogueFormat.Text = string.Format("({0} - {1} dòng)", _dialogueFormat.ToString().ToUpper(), _dialogueLines.Count);
            UpdateDialogueOutput();
            UpdateDialogueMerged();
        }
        catch (Exception ex)
        {
            TxtDialogueFormat.Text = string.Format("(Lỗi: {0})", ex.Message);
        }
        finally
        {
            _isDialogueUpdating = false;
        }
    }

    /// <summary>
    /// Khi nhập text manual vào Panel 3 → cập nhật Panel 4
    /// </summary>
    private void TxtDialogueManual_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_isDialogueUpdating) return;
        try
        {
            _isDialogueUpdating = true;
            ParseManualText();
            UpdateDialogueMerged();
        }
        catch (Exception ex)
        {
            TxtDialogueMerged.Text = string.Format("(Lỗi: {0})", ex.Message);
        }
        finally
        {
            _isDialogueUpdating = false;
        }
    }

    private void BtnCopyDialogue_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TxtDialogueOutput.Text)) return;
        try
        {
            Clipboard.SetText(TxtDialogueOutput.Text);
            ShowToastDialogue("\ud83d\udccb Đã copy Dialogue!");
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show("Lỗi: " + ex.Message, "Lỗi", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }

    private void BtnCopyMerged_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TxtDialogueMerged.Text)) return;
        try
        {
            Clipboard.SetText(TxtDialogueMerged.Text);
            ShowToastDialogue("\ud83d\udccb Đã copy Merged!");
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show("Lỗi: " + ex.Message, "Lỗi", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }

#endregion

#region "Dialogue Only - Display Methods"

    private void UpdateDialogueOutput()
    {
        if (_dialogueLines.Count == 0)
        {
            TxtDialogueOutput.Text = "";
            return;
        }
        var sb = new StringBuilder();
        int lineNum = 1;
        foreach (var line in _dialogueLines)
        {
            var dialogueText = GetDialogueText(line);
            if (!string.IsNullOrWhiteSpace(dialogueText))
            {
                var cleanText = dialogueText.Replace(Environment.NewLine, " ").Replace("\r", " ").Replace("\n", " ");
                sb.AppendLine(string.Format("{0}\t{1}", lineNum, cleanText));
                lineNum += 1;
            }
        }
        TxtDialogueOutput.Text = sb.ToString().TrimEnd();
    }

    private string GetDialogueText(SubtitleLine line)
    {
        var assLine = line as AssSubtitleLine;
        if (assLine != null) return assLine.DialogText;
        var srtLine = line as SrtSubtitleLine;
        if (srtLine != null) return srtLine.Text;
        return line.OriginalText;
    }

    /// <summary>
    /// Parse text từ Panel 3: Hỗ trợ 2 format
    /// Format 1 - 3 cột (ngang): STT&lt;TAB&gt;Text gốc&lt;TAB&gt;Text tiếng Việt → Lấy cột 2 (Text gốc)
    /// Format 2 - 3 hàng (dọc): Mỗi nhóm 3 dòng (STT / Text tiếng Việt / Text gốc) → Lấy hàng 3 (Text gốc)
    /// Luôn luôn lấy Text gốc để merge với time code từ Panel 1
    /// </summary>
    private void ParseManualText()
    {
        _dialogueManualTexts.Clear();
        var content = TxtDialogueManual.Text;
        if (string.IsNullOrWhiteSpace(content)) return;

        var lines = content.Split(new[] { Environment.NewLine, "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length == 0) return;

        // Bước 1: Xác định format (3 cột hay 3 hàng)
        var is3ColumnFormat = false;

        // Kiểm tra dòng đầu tiên xem có chứa tab và có >= 2 phần tử không
        if (lines.Length > 0)
        {
            var firstLine = lines[0].Trim();
            if (firstLine.Contains("\t"))
            {
                var parts = firstLine.Split(new[] { "\t" }, StringSplitOptions.None);
                if (parts.Length >= 2)
                {
                    // Kiểm tra phần tử đầu có phải là số không
                    int stt = 0;
                    if (int.TryParse(parts[0].Trim(), out stt))
                    {
                        is3ColumnFormat = true;
                    }
                }
            }
        }

        // Bước 2: Parse theo format đã xác định
        if (is3ColumnFormat)
        {
            // Format 3 cột: Mỗi dòng là STT<TAB>Text gốc<TAB>Text tiếng Việt
            // Lấy cột 2 (index 1) làm text để merge
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (string.IsNullOrWhiteSpace(trimmed)) continue;

                if (trimmed.Contains("\t"))
                {
                    var parts = trimmed.Split(new[] { "\t" }, StringSplitOptions.None);
                    if (parts.Length >= 2)
                    {
                        int stt = 0;
                        if (int.TryParse(parts[0].Trim(), out stt))
                        {
                            // Lấy cột 2 (index 1) làm text - Text gốc
                            _dialogueManualTexts[stt] = parts[1].Trim();
                        }
                    }
                }
            }
        }
        else
        {
            // Format 3 hàng: Mỗi nhóm 3 dòng (STT / Text tiếng Việt / Text gốc)
            // Lấy dòng 3 (Text gốc - index 2) làm text để merge
            int groupIndex = 0;
            int currentStt = 0;
            bool hasValidStt = false;

            for (int i = 0; i < lines.Length; i++)
            {
                var trimmed = lines[i].Trim();
                if (string.IsNullOrWhiteSpace(trimmed)) continue;

                var positionInGroup = groupIndex % 3;

                if (positionInGroup == 0)
                {
                    // Dòng 1: STT (số thứ tự)
                    int stt = 0;
                    if (int.TryParse(trimmed, out stt))
                    {
                        currentStt = stt;
                        hasValidStt = true;
                    }
                    else
                    {
                        hasValidStt = false;
                    }
                }
                else if (positionInGroup == 1)
                {
                    // Dòng 2: Text tiếng Việt (bỏ qua)
                    // Không cần làm gì
                }
                else if (positionInGroup == 2)
                {
                    // Dòng 3: Text gốc (lưu lại làm text để merge)
                    if (hasValidStt && currentStt > 0)
                    {
                        _dialogueManualTexts[currentStt] = trimmed;
                    }
                }

                groupIndex += 1;
            }
        }
    }

    /// <summary>
    /// Cập nhật Panel 4: Time Code từ Panel 1 + Text từ Panel 3
    /// Format SRT: STT + Time Code + Text từ Panel 3
    /// </summary>
    private void UpdateDialogueMerged()
    {
        if (_dialogueLines.Count == 0)
        {
            TxtDialogueMerged.Text = "";
            return;
        }

        var sb = new StringBuilder();
        int entryNum = 0;

        foreach (var line in _dialogueLines)
        {
            var dialogueText = GetDialogueText(line);
            if (string.IsNullOrWhiteSpace(dialogueText)) continue;

            entryNum += 1;

            // Kiểm tra xem có text manual cho STT này không
            string manualText;
            if (_dialogueManualTexts.TryGetValue(entryNum, out manualText) && !string.IsNullOrWhiteSpace(manualText))
            {
                // Dùng text từ Panel 3
                if (_dialogueFormat == SubtitleFormat.Srt)
                {
                    sb.AppendLine(entryNum.ToString());
                    sb.AppendLine(string.Format("{0} --> {1}",
                        SubtitleLine.FormatSrtTime(line.StartTime),
                        SubtitleLine.FormatSrtTime(line.EndTime)));
                    sb.AppendLine(manualText);
                    sb.AppendLine();
                }
                else if (_dialogueFormat == SubtitleFormat.Ass)
                {
                    var assLine = line as AssSubtitleLine;
                    if (assLine != null)
                    {
                        // Giữ nguyên time code, thay thế dialogue text
                        // Format đúng: Layer,Start,End,Style,Name,MarginL,MarginR,MarginV,Effect,Text
                        sb.AppendLine(string.Format("Dialogue: {0},{1},{2},{3},{4},{5},{6},{7},{8},{9}",
                            assLine.Layer, SubtitleLine.FormatAssTime(line.StartTime),
                            SubtitleLine.FormatAssTime(line.EndTime), assLine.Style,
                            assLine.Name, assLine.MarginL, assLine.MarginR, assLine.MarginV,
                            assLine.Effect, manualText));
                    }
                }
                else
                {
                    // Format khác: ghi ra dạng time code + text
                    sb.AppendLine(string.Format("{0}", manualText));
                }
            }
        }

        TxtDialogueMerged.Text = sb.ToString().TrimEnd();
    }

#endregion

#region "Dialogue Only - Toast"

    private async void ShowToastDialogue(string message)
    {
        ToastTextDialogue.Text = message;
        ToastBorderDialogue.Visibility = Visibility.Visible;
        await Task.Delay(2000);
        ToastBorderDialogue.Visibility = Visibility.Collapsed;
    }

#endregion

    }
}
