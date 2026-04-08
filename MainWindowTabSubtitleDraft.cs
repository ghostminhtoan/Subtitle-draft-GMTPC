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

#region "Subtitle Draft - Fields"

    private List<SubtitleLine> _originalLines = new List<SubtitleLine>();
    private List<SubtitleLine> _timeCodeLines = new List<SubtitleLine>();
    private List<SubtitleLine> _connectGapLines = new List<SubtitleLine>();
    private SubtitleFormat _currentFormat = SubtitleFormat.Unknown;
    private bool _isUpdating = false;
    private int _totalShiftMs = 0;

#endregion

#region "Subtitle Draft - Event Handlers"

    private void TxtOriginal_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_isUpdating) return;
        try
        {
            _isUpdating = true;
            var content = SubtitleParser.SanitizeContent(TxtOriginal.Text);
            if (string.IsNullOrWhiteSpace(content))
            {
                _originalLines.Clear();
                _timeCodeLines.Clear();
                _connectGapLines.Clear();
                TxtTimeCode.Text = "";
                TxtConnectGap.Text = "";
                TxtResult.Text = "";
                TxtOriginalFormat.Text = "";
                _currentFormat = SubtitleFormat.Unknown;
                return;
            }
            _currentFormat = SubtitleParser.DetectFormat(content);
            _originalLines = SubtitleParser.Parse(content);
            TxtOriginalFormat.Text = string.Format("({0} - {1} dòng)", _currentFormat.ToString().ToUpper(), _originalLines.Count);
            _timeCodeLines = new List<SubtitleLine>();
            foreach (var line in _originalLines)
            {
                _timeCodeLines.Add(line.Clone());
            }
            UpdateTimeCodeDisplay();
            _connectGapLines = new List<SubtitleLine>();
            foreach (var line in _timeCodeLines)
            {
                _connectGapLines.Add(line.Clone());
            }
            UpdateConnectGapDisplay();
            UpdateResultDisplay();
        }
        catch (Exception ex)
        {
            TxtOriginalFormat.Text = string.Format("(Lỗi: {0})", ex.Message);
        }
        finally
        {
            _isUpdating = false;
        }
    }

    private void BtnTimePlus200_Click(object sender, RoutedEventArgs e)
    {
        if (_timeCodeLines.Count == 0) return;
        _totalShiftMs += 200;
        _timeCodeLines = TimeCodeService.ShiftTime(_timeCodeLines, 200);
        UpdateTimeCodeDisplay();
        _connectGapLines = new List<SubtitleLine>();
        foreach (var line in _timeCodeLines)
        {
            _connectGapLines.Add(line.Clone());
        }
        UpdateConnectGapDisplay();
        UpdateResultDisplay();
        var seconds = Math.Abs(_totalShiftMs) / 1000.0;
        var sign = (_totalShiftMs >= 0) ? "+" : "-";
        ShowToastDraft(string.Format("\u23f1\ufe0f Đã shift {0}{1}ms ({2}s)", sign, _totalShiftMs, seconds.ToString("0.0")));
    }

    private void BtnTimeMinus200_Click(object sender, RoutedEventArgs e)
    {
        if (_timeCodeLines.Count == 0) return;
        _totalShiftMs -= 200;
        _timeCodeLines = TimeCodeService.ShiftTime(_timeCodeLines, -200);
        UpdateTimeCodeDisplay();
        _connectGapLines = new List<SubtitleLine>();
        foreach (var line in _timeCodeLines)
        {
            _connectGapLines.Add(line.Clone());
        }
        UpdateConnectGapDisplay();
        UpdateResultDisplay();
        var seconds = Math.Abs(_totalShiftMs) / 1000.0;
        var sign = (_totalShiftMs >= 0) ? "+" : "-";
        ShowToastDraft(string.Format("\u23f1\ufe0f Đã shift {0}{1}ms ({2}s)", sign, _totalShiftMs, seconds.ToString("0.0")));
    }

    private void BtnCopyTimeCode_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TxtTimeCode.Text)) return;
        try
        {
            Clipboard.SetText(TxtTimeCode.Text);
            System.Windows.MessageBox.Show("Đã copy!", "Copy", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show("Lỗi: " + ex.Message, "Lỗi", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }

    private void BtnConnectGap_Click(object sender, RoutedEventArgs e)
    {
        if (_connectGapLines.Count == 0) return;
        _connectGapLines = TimeCodeService.ConnectGap(_connectGapLines);
        UpdateConnectGapDisplay();
        UpdateResultDisplay();
        ShowToastDraft("\ud83d\udd17 Đã connect gap!");
    }

    private void BtnCopyResult_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TxtResult.Text)) return;
        try
        {
            Clipboard.SetText(TxtResult.Text);
            ShowToastDraft("\ud83d\udccb Đã copy Result!");
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show("Lỗi: " + ex.Message, "Lỗi", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }

#endregion

#region "Subtitle Draft - Display Methods"

    private void UpdateTimeCodeDisplay()
    {
        if (_timeCodeLines.Count == 0)
        {
            TxtTimeCode.Text = "";
            return;
        }
        TxtTimeCode.Text = SubtitleParser.ToText(_timeCodeLines, _currentFormat);
    }

    private void UpdateConnectGapDisplay()
    {
        if (_connectGapLines.Count == 0)
        {
            TxtConnectGap.Text = "";
            return;
        }
        TxtConnectGap.Text = SubtitleParser.ToText(_connectGapLines, _currentFormat);
    }

    private void UpdateResultDisplay()
    {
        if (_connectGapLines.Count > 0)
        {
            TxtResult.Text = SubtitleParser.ToText(_connectGapLines, _currentFormat);
        }
        else if (_timeCodeLines.Count > 0)
        {
            TxtResult.Text = SubtitleParser.ToText(_timeCodeLines, _currentFormat);
        }
        else
        {
            TxtResult.Text = "";
        }
    }

#endregion

#region "Subtitle Draft - Toast"

    private async void ShowToastDraft(string message)
    {
        ToastText.Text = message;
        ToastBorder.Visibility = Visibility.Visible;
        await Task.Delay(2000);
        ToastBorder.Visibility = Visibility.Collapsed;
    }

#endregion

    }
}
