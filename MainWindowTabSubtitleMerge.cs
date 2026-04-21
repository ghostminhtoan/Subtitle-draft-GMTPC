using System;
using System.Windows;
using System.Windows.Controls;
using System.Threading.Tasks;
using System.Collections.Generic;
using Subtitle_draft_GMTPC.Models;
using Subtitle_draft_GMTPC.Services;

namespace Subtitle_draft_GMTPC
{
    public partial class MainWindow : Window
    {

#region "Subtitle Merge - Fields"

    private List<SubtitleLine> _engLines = new List<SubtitleLine>();
    private List<SubtitleLine> _vietLines = new List<SubtitleLine>();
    private SubtitleFormat _mergeFormat = SubtitleFormat.Unknown;
    private bool _isMergeUpdating = false;
    private bool _mergeNotesEnabled = false;
    private bool _isMergeNoteToggleSyncing = false;
    private const int MergeBlankNoteLineCount = 5;

#endregion

#region "Subtitle Merge - Event Handlers"

    private void TxtEngsub_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_isMergeUpdating) return;
        try
        {
            _isMergeUpdating = true;
            var content = SubtitleParser.SanitizeContent(TxtEngsub.Text);
            if (string.IsNullOrWhiteSpace(content))
            {
                _engLines.Clear();
                UpdateMergeDisplays();
                return;
            }
            var detectedFormat = SubtitleParser.DetectFormat(content);
            if (_mergeFormat == SubtitleFormat.Unknown && detectedFormat != SubtitleFormat.Unknown)
            {
                _mergeFormat = detectedFormat;
            }
            _engLines = SubtitleParser.Parse(content);
            AutoDetectMergeNotes();
            TxtEngsubFormat.Text = string.Format("({0} - {1} dòng)", detectedFormat.ToString().ToUpper(), _engLines.Count);
        }
        catch (Exception ex)
        {
            TxtEngsubFormat.Text = string.Format("(Lỗi: {0})", ex.Message);
            _engLines.Clear();
        }
        finally
        {
            UpdateMergeDisplays();
            _isMergeUpdating = false;
        }
    }

    private void TxtVietsub_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_isMergeUpdating) return;
        try
        {
            _isMergeUpdating = true;
            var content = SubtitleParser.SanitizeContent(TxtVietsub.Text);
            if (string.IsNullOrWhiteSpace(content))
            {
                _vietLines.Clear();
                UpdateMergeDisplays();
                return;
            }
            var detectedFormat = SubtitleParser.DetectFormat(content);
            if (_mergeFormat == SubtitleFormat.Unknown && detectedFormat != SubtitleFormat.Unknown)
            {
                _mergeFormat = detectedFormat;
            }
            _vietLines = SubtitleParser.Parse(content);
            AutoDetectMergeNotes();
            TxtVietsubFormat.Text = string.Format("({0} - {1} dòng)", detectedFormat.ToString().ToUpper(), _vietLines.Count);
        }
        catch (Exception ex)
        {
            TxtVietsubFormat.Text = string.Format("(Lỗi: {0})", ex.Message);
            _vietLines.Clear();
        }
        finally
        {
            UpdateMergeDisplays();
            _isMergeUpdating = false;
        }
    }

    private void BtnCopyMerge_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TxtMerge.Text)) return;
        try
        {
            Clipboard.SetText(TxtMerge.Text);
            ShowToastMerge("\ud83d\udccb Đã copy Merge!");
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show("Lỗi: " + ex.Message, "Lỗi", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }

    private void BtnCopyMergeUnbreak_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TxtMergeUnbreak.Text)) return;
        try
        {
            Clipboard.SetText(TxtMergeUnbreak.Text);
            ShowToastMerge("\ud83d\udccb Đã copy Unbreak!");
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show("Lỗi: " + ex.Message, "Lỗi", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }

    private void ToggleMergeNotes_CheckedChanged(object sender, RoutedEventArgs e)
    {
        if (_isMergeNoteToggleSyncing) return;

        _mergeNotesEnabled = ToggleMergeNotes.IsChecked == true;
        UpdateMergeDisplays();
    }

#endregion

#region "Subtitle Merge - Display Methods"

    private void UpdateMergeDisplays()
    {
        // Luôn gọi để cập nhật cả 2 panel
        try
        {
            if (_engLines == null) _engLines = new List<SubtitleLine>();
            if (_vietLines == null) _vietLines = new List<SubtitleLine>();

            var engLines = GetMergeLinesWithOptionalNote(_engLines, "Engsub");
            var vietLines = GetMergeLinesWithOptionalNote(_vietLines, "Vietsub");

            var mergedLines = MergeService.MergeSubtitles(engLines, vietLines, _mergeFormat);
            TxtMerge.Text = (mergedLines != null && mergedLines.Count > 0) ? SubtitleParser.ToText(mergedLines, _mergeFormat) : "";

            var unbreakLines = MergeService.MergeUnbreak(engLines, vietLines, _mergeFormat);
            TxtMergeUnbreak.Text = (unbreakLines != null && unbreakLines.Count > 0) ? SubtitleParser.ToText(unbreakLines, _mergeFormat) : "";
        }
        catch (Exception)
        {
            TxtMerge.Text = "";
            TxtMergeUnbreak.Text = "";
        }
    }

    private List<SubtitleLine> GetMergeLinesWithOptionalNote(List<SubtitleLine> lines, string noteText)
    {
        var result = new List<SubtitleLine>();
        if (lines == null || lines.Count == 0) return result;

        if (_mergeNotesEnabled && !HasCompleteMergeNoteBlock(lines, noteText))
        {
            for (int i = 0; i < MergeBlankNoteLineCount; i++)
            {
                result.Add(CreateMergeNoteLine(""));
            }

            if (!HasMergeNoteAtStart(lines, noteText))
            {
                result.Add(CreateMergeNoteLine(noteText));
            }
        }

        foreach (var line in lines)
        {
            result.Add(line);
        }

        return result;
    }

    private SubtitleLine CreateMergeNoteLine(string noteText)
    {
        if (_mergeFormat == SubtitleFormat.Ass)
        {
            return new AssSubtitleLine()
            {
                OriginalText = noteText,
                StartTime = TimeSpan.Zero,
                EndTime = TimeSpan.Zero,
                Content = noteText,
                Layer = "0",
                Style = "Default",
                Name = "",
                MarginL = "0000",
                MarginR = "0000",
                MarginV = "0000",
                Effect = "",
                DialogText = noteText,
                Header = "Dialogue"
            };
        }

        return new SrtSubtitleLine(0, TimeSpan.Zero, TimeSpan.Zero, noteText)
        {
            OriginalText = noteText,
            Content = noteText
        };
    }

    private void AutoDetectMergeNotes()
    {
        if (_mergeNotesEnabled) return;
        if (!HasMergeNote(_engLines, "Engsub") || !HasMergeNote(_vietLines, "Vietsub")) return;

        _mergeNotesEnabled = true;
        SetMergeNoteToggleChecked(true);
    }

    private void SetMergeNoteToggleChecked(bool isChecked)
    {
        if (ToggleMergeNotes == null) return;

        try
        {
            _isMergeNoteToggleSyncing = true;
            ToggleMergeNotes.IsChecked = isChecked;
        }
        finally
        {
            _isMergeNoteToggleSyncing = false;
        }
    }

    private bool HasMergeNote(List<SubtitleLine> lines, string noteText)
    {
        if (lines == null || lines.Count == 0) return false;

        return HasMergeNoteAtStart(lines, noteText) || HasCompleteMergeNoteBlock(lines, noteText);
    }

    private bool HasMergeNoteAtStart(List<SubtitleLine> lines, string noteText)
    {
        return lines != null && lines.Count > 0 && IsMergeNoteLine(lines[0], noteText);
    }

    private bool HasCompleteMergeNoteBlock(List<SubtitleLine> lines, string noteText)
    {
        if (lines == null || lines.Count == 0) return false;

        var noteIndex = FindMergeNoteIndex(lines, noteText);
        if (noteIndex != MergeBlankNoteLineCount) return false;

        for (int i = 0; i < noteIndex; i++)
        {
            if (!IsBlankMergeNoteLine(lines[i])) return false;
        }

        return true;
    }

    private int FindMergeNoteIndex(List<SubtitleLine> lines, string noteText)
    {
        var searchCount = Math.Min(lines.Count, MergeBlankNoteLineCount + 1);
        for (int i = 0; i < searchCount; i++)
        {
            if (IsMergeNoteLine(lines[i], noteText)) return i;
        }

        return -1;
    }

    private bool IsBlankMergeNoteLine(SubtitleLine line)
    {
        return IsMergeNoteLine(line, "");
    }

    private bool IsMergeNoteLine(SubtitleLine line, string noteText)
    {
        if (line == null || line.StartTime != TimeSpan.Zero || line.EndTime != TimeSpan.Zero) return false;

        var assLine = line as AssSubtitleLine;
        if (assLine != null)
        {
            return string.Equals((assLine.DialogText ?? "").Trim(), noteText, StringComparison.OrdinalIgnoreCase);
        }

        var srtLine = line as SrtSubtitleLine;
        if (srtLine != null)
        {
            return string.Equals((srtLine.Text ?? "").Trim(), noteText, StringComparison.OrdinalIgnoreCase);
        }

        return false;
    }

#endregion

#region "Subtitle Merge - Toast"

    private async void ShowToastMerge(string message)
    {
        ToastTextMerge.Text = message;
        ToastBorderMerge.Visibility = Visibility.Visible;
        await Task.Delay(2000);
        ToastBorderMerge.Visibility = Visibility.Collapsed;
    }

#endregion

    }
}
