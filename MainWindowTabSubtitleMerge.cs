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

#endregion

#region "Subtitle Merge - Display Methods"

    private void UpdateMergeDisplays()
    {
        // Luôn gọi để cập nhật cả 2 panel
        try
        {
            if (_engLines == null) _engLines = new List<SubtitleLine>();
            if (_vietLines == null) _vietLines = new List<SubtitleLine>();

            var mergedLines = MergeService.MergeSubtitles(_engLines, _vietLines, _mergeFormat);
            TxtMerge.Text = (mergedLines != null && mergedLines.Count > 0) ? SubtitleParser.ToText(mergedLines, _mergeFormat) : "";

            var unbreakLines = MergeService.MergeUnbreak(_engLines, _vietLines, _mergeFormat);
            TxtMergeUnbreak.Text = (unbreakLines != null && unbreakLines.Count > 0) ? SubtitleParser.ToText(unbreakLines, _mergeFormat) : "";
        }
        catch (Exception)
        {
            TxtMerge.Text = "";
            TxtMergeUnbreak.Text = "";
        }
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
