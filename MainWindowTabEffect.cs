using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Subtitle_draft_GMTPC.Models;
using Subtitle_draft_GMTPC.Services;

namespace Subtitle_draft_GMTPC
{
    public partial class MainWindow : Window
    {

        #region Effect - Fields

        private bool _isEffectUpdating = false;
        private DispatcherTimer _effectDebounceTimer = new DispatcherTimer();
        private bool _effectPendingUpdate = false;
        private string _currentEffectTag = "";
        private Models.AssTagType _currentEffectType = Models.AssTagType.Unknown;
        private string _currentEffectName = "";

        #endregion

        #region Effect - Initialization

        private void InitializeEffectDebounce()
        {
            _effectDebounceTimer.Interval = TimeSpan.FromMilliseconds(150);
            _effectDebounceTimer.Tick += (sender, e) =>
            {
                _effectDebounceTimer.Stop();
                if (_effectPendingUpdate)
                {
                    _effectPendingUpdate = false;
                    ApplyEffectsToOutput();
                }
            };
        }

        #endregion

        #region Effect - Event Handlers

        private void TxtEffectInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isEffectUpdating) return;
            try
            {
                _isEffectUpdating = true;
                var content = SubtitleParser.SanitizeContent(TxtEffectInput.Text);
                if (string.IsNullOrWhiteSpace(content))
                {
                    TxtEffectLineCount.Text = "";
                    TxtEffectOutput.Text = "";
                    return;
                }

                var lines = content.Split(new[] { Environment.NewLine, "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                TxtEffectLineCount.Text = string.Format("({0} dong)", lines.Length);

                _effectPendingUpdate = true;
                _effectDebounceTimer.Stop();
                _effectDebounceTimer.Start();
            }
            catch (Exception ex)
            {
                TxtEffectLineCount.Text = string.Format("(Loi: {0})", ex.Message);
            }
            finally
            {
                _isEffectUpdating = false;
            }
        }

        private void BtnColorPicker_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dlg = new ColorDialog();
                dlg.FullOpen = true;
                if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    var hex = dlg.Color.B.ToString("X2") + dlg.Color.G.ToString("X2") + dlg.Color.R.ToString("X2");
                    var hexBox = EffectConfigFields.Children.OfType<StackPanel>().SelectMany(sp => sp.Children.OfType<System.Windows.Controls.TextBox>()).FirstOrDefault(t => t.Tag != null && t.Tag.ToString().ToLower() == "hex");
                    if (hexBox != null)
                    {
                        hexBox.Text = hex;
                        ShowToastEffect("Da dien hex: " + hex);
                    }
                    else
                    {
                        ShowToastEffect("Mo 1 effect mau truoc (vi du: \\1c)");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Loi: " + ex.Message, "Loi", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private void BtnCopyEffect_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtEffectOutput.Text)) return;
            try
            {
                System.Windows.Clipboard.SetText(TxtEffectOutput.Text);
                ShowToastEffect("Da copy output!");
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Loi: " + ex.Message, "Loi", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private void BtnClearEffect_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var content = TxtEffectInput.Text;
                if (string.IsNullOrWhiteSpace(content)) return;
                TxtEffectInput.Text = AssEffectBuilder.RemoveAllTagsFromAllLines(content);
                TxtEffectOutput.Text = TxtEffectInput.Text;
                HideEffectConfig();
                ShowToastEffect("Da clear toan bo effects!");
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Loi: " + ex.Message, "Loi", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private void BtnResetEffect_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var content = TxtEffectInput.Text;
                if (string.IsNullOrWhiteSpace(content)) return;
                TxtEffectOutput.Text = AssEffectBuilder.RemoveAllTagsFromAllLines(content);
                ShowToastEffect("Da reset tags!");
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Loi: " + ex.Message, "Loi", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        #endregion

        #region Effect - Apply Methods

        private void ApplyEffectsToOutput()
        {
            var content = TxtEffectInput.Text;
            if (string.IsNullOrWhiteSpace(content))
            {
                TxtEffectOutput.Text = "";
                return;
            }
            TxtEffectOutput.Text = content;
        }

        private void ApplyTagToSelectedLine(string tag)
        {
            var outputContent = TxtEffectOutput.Text;
            if (string.IsNullOrWhiteSpace(outputContent)) outputContent = TxtEffectInput.Text;
            if (string.IsNullOrWhiteSpace(outputContent)) return;

            var selStart = TxtEffectInput.SelectionStart;
            var lineIndex = 0;
            var pos = 0;
            var lines = outputContent.Split(new[] { Environment.NewLine }, StringSplitOptions.None);

            for (int i = 0; i < lines.Length; i++)
            {
                var lineStart = pos;
                var lineEnd = pos + lines[i].Length;
                if (selStart >= lineStart && selStart < lineEnd)
                {
                    lineIndex = i;
                    break;
                }
                pos = lineEnd + Environment.NewLine.Length;
            }

            lines[lineIndex] = AssEffectBuilder.ApplyTagToLine(lines[lineIndex], tag);
            TxtEffectOutput.Text = string.Join(Environment.NewLine, lines);
            HideEffectConfig();
            ShowToastEffect("Da apply vao dong (Output)!");
        }

        private void ApplyTagToAllLines(string tag)
        {
            var outputContent = TxtEffectOutput.Text;
            if (string.IsNullOrWhiteSpace(outputContent)) outputContent = TxtEffectInput.Text;
            if (string.IsNullOrWhiteSpace(outputContent)) return;

            TxtEffectOutput.Text = AssEffectBuilder.ApplyTagsToAllLines(outputContent, new[] { tag });
            HideEffectConfig();
            ShowToastEffect("Da apply vao TAT CA dong (Output)!");
        }

        #endregion

        #region Effect - Config Panel Management

        private void ShowEffectConfig(string title, Models.AssTagType effectType, Tuple<string, string, string>[] fields)
        {
            _currentEffectType = effectType;
            _currentEffectName = title;
            EffectConfigTitle.Text = title;
            EffectConfigFields.Children.Clear();
            foreach (var f in fields)
            {
                var sp = new StackPanel() { Orientation = System.Windows.Controls.Orientation.Horizontal, Margin = new System.Windows.Thickness(0, 2, 0, 2) };
                var lbl = new TextBlock() { Text = f.Item1, Foreground = Brushes.White, Width = 80, VerticalAlignment = System.Windows.VerticalAlignment.Center };
                var txt = new System.Windows.Controls.TextBox()
                {
                    Text = f.Item2,
                    Tag = f.Item3,
                    Width = 150,
                    Style = TryFindResource("DarkTextBoxStyle") as System.Windows.Style
                };
                sp.Children.Add(lbl);
                sp.Children.Add(txt);
                EffectConfigFields.Children.Add(sp);
            }
            EffectConfigBorder.Visibility = Visibility.Visible;
        }

        private void HideEffectConfig()
        {
            EffectConfigBorder.Visibility = Visibility.Collapsed;
            EffectConfigFields.Children.Clear();
            _currentEffectType = Models.AssTagType.Unknown;
            _currentEffectName = "";
        }

        private string GetConfigValue(int index)
        {
            if (index < EffectConfigFields.Children.Count)
            {
                var sp = EffectConfigFields.Children[index] as StackPanel;
                if (sp != null && sp.Children.Count > 1)
                {
                    var txt = sp.Children[1] as System.Windows.Controls.TextBox;
                    if (txt != null) return txt.Text;
                }
            }
            return "";
        }

        #endregion

        #region Effect - Position & Movement Handlers

        private void BtnEffectPosition_Click(object sender, RoutedEventArgs e)
        {
            ShowEffectConfig("\\pos - Vi tri", Models.AssTagType.Position,
                new[] { Tuple.Create("X:", "640", "x"), Tuple.Create("Y:", "360", "y") });
        }

        private void BtnEffectMove_Click(object sender, RoutedEventArgs e)
        {
            ShowEffectConfig("\\move - Di chuyen", Models.AssTagType.Move,
                new[] { Tuple.Create("X1:", "0", "x1"), Tuple.Create("Y1:", "0", "y1"),
                        Tuple.Create("X2:", "640", "x2"), Tuple.Create("Y2:", "360", "y2") });
        }

        private void BtnEffectAlign_Click(object sender, RoutedEventArgs e)
        {
            ShowEffectConfig("\\an - Can le", Models.AssTagType.Alignment,
                new[] { Tuple.Create("Can le (1-9):", "5", "n") });
        }

        private void BtnEffectOrigin_Click(object sender, RoutedEventArgs e)
        {
            ShowEffectConfig("\\org - Tam xoay", Models.AssTagType.Origin,
                new[] { Tuple.Create("X:", "640", "x"), Tuple.Create("Y:", "360", "y") });
        }

        #endregion

        #region Effect - Transform & Rotation Handlers

        private void BtnEffectRotZ_Click(object sender, RoutedEventArgs e)
        {
            ShowEffectConfig("\\frz - Xoay Z", Models.AssTagType.RotateZ,
                new[] { Tuple.Create("Do:", "0", "degrees") });
        }

        private void BtnEffectRotX_Click(object sender, RoutedEventArgs e)
        {
            ShowEffectConfig("\\frx - Lat X", Models.AssTagType.RotateX,
                new[] { Tuple.Create("Do:", "0", "degrees") });
        }

        private void BtnEffectRotY_Click(object sender, RoutedEventArgs e)
        {
            ShowEffectConfig("\\fry - Lat Y", Models.AssTagType.RotateY,
                new[] { Tuple.Create("Do:", "0", "degrees") });
        }

        private void BtnEffectScaleX_Click(object sender, RoutedEventArgs e)
        {
            ShowEffectConfig("\\fscx - Co gian X", Models.AssTagType.ScaleX,
                new[] { Tuple.Create("Phan tram:", "100", "percent") });
        }

        private void BtnEffectScaleY_Click(object sender, RoutedEventArgs e)
        {
            ShowEffectConfig("\\fscy - Co gian Y", Models.AssTagType.ScaleY,
                new[] { Tuple.Create("Phan tram:", "100", "percent") });
        }

        private void BtnEffectShearX_Click(object sender, RoutedEventArgs e)
        {
            ShowEffectConfig("\\fax - Nghien X", Models.AssTagType.ShearX,
                new[] { Tuple.Create("Gia tri:", "0", "value") });
        }

        private void BtnEffectShearY_Click(object sender, RoutedEventArgs e)
        {
            ShowEffectConfig("\\fay - Nghien Y", Models.AssTagType.ShearY,
                new[] { Tuple.Create("Gia tri:", "0", "value") });
        }

        #endregion

        #region Effect - Font & Style Handlers

        private void BtnEffectFontName_Click(object sender, RoutedEventArgs e)
        {
            ShowEffectConfig("\\fn - Font chu", Models.AssTagType.FontName,
                new[] { Tuple.Create("Ten font:", "Arial", "fontName") });
        }

        private void BtnEffectFontSize_Click(object sender, RoutedEventArgs e)
        {
            ShowEffectConfig("\\fs - Co chu", Models.AssTagType.FontSize,
                new[] { Tuple.Create("Size:", "24", "size") });
        }

        private void BtnEffectBold_Click(object sender, RoutedEventArgs e)
        {
            ShowEffectConfig("\\b - In dam", Models.AssTagType.Bold,
                new[] { Tuple.Create("Dam (0/1):", "1", "weight") });
        }

        private void BtnEffectItalic_Click(object sender, RoutedEventArgs e)
        {
            ShowEffectConfig("\\i - In nghien", Models.AssTagType.Italic,
                new[] { Tuple.Create("Nghien (0/1):", "1", "on") });
        }

        private void BtnEffectUnderline_Click(object sender, RoutedEventArgs e)
        {
            ShowEffectConfig("\\u - Gach chan", Models.AssTagType.Underline,
                new[] { Tuple.Create("Gach chan (0/1):", "1", "on") });
        }

        private void BtnEffectStrikeout_Click(object sender, RoutedEventArgs e)
        {
            ShowEffectConfig("\\s - Gach ngang", Models.AssTagType.Strikeout,
                new[] { Tuple.Create("Gach ngang (0/1):", "1", "on") });
        }

        #endregion

        #region Effect - Border & Shadow Handlers

        private void BtnEffectBorder_Click(object sender, RoutedEventArgs e)
        {
            ShowEffectConfig("\\bord - Vien chu", Models.AssTagType.Border,
                new[] { Tuple.Create("Do day:", "2", "size") });
        }

        private void BtnEffectShadow_Click(object sender, RoutedEventArgs e)
        {
            ShowEffectConfig("\\shad - Do bong", Models.AssTagType.Shadow,
                new[] { Tuple.Create("Do dai:", "2", "size") });
        }

        private void BtnEffectBlur_Click(object sender, RoutedEventArgs e)
        {
            ShowEffectConfig("\\blur - Mo vien", Models.AssTagType.Blur,
                new[] { Tuple.Create("Muc do:", "1", "level") });
        }

        private void BtnEffectEdgeBlur_Click(object sender, RoutedEventArgs e)
        {
            ShowEffectConfig("\\be - Mo canh", Models.AssTagType.EdgeBlur,
                new[] { Tuple.Create("Muc do (0/1):", "1", "level") });
        }

        #endregion

        #region Effect - Fade & Karaoke Handlers

        private void BtnEffectFade_Click(object sender, RoutedEventArgs e)
        {
            ShowEffectConfig("\\fad - Mo dan", Models.AssTagType.Fade,
                new[] { Tuple.Create("Vao (ms):", "300", "inTime"), Tuple.Create("Ra (ms):", "500", "outTime") });
        }

        private void BtnEffectKaraokeK_Click(object sender, RoutedEventArgs e)
        {
            ShowEffectConfig("\\k - Karaoke", Models.AssTagType.KaraokeK,
                new[] { Tuple.Create("Thoi gian (cs):", "50", "duration") });
        }

        private void BtnEffectKaraokeKF_Click(object sender, RoutedEventArgs e)
        {
            ShowEffectConfig("\\kf - Karaoke quet", Models.AssTagType.KaraokeKF,
                new[] { Tuple.Create("Thoi gian (cs):", "50", "duration") });
        }

        private void BtnEffectKaraokeKO_Click(object sender, RoutedEventArgs e)
        {
            ShowEffectConfig("\\ko - Karaoke xoa vien", Models.AssTagType.KaraokeKO,
                new[] { Tuple.Create("Thoi gian (cs):", "50", "duration") });
        }

        #endregion

        #region Effect - Apply & Cancel Handlers

        private void BtnEffectApply_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var tag = BuildCurrentTag();
                if (string.IsNullOrEmpty(tag)) return;
                ApplyTagToSelectedLine(tag);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Loi: " + ex.Message, "Loi", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private void BtnEffectApplyAll_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var tag = BuildCurrentTag();
                if (string.IsNullOrEmpty(tag)) return;
                ApplyTagToAllLines(tag);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Loi: " + ex.Message, "Loi", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private void BtnEffectCancel_Click(object sender, RoutedEventArgs e)
        {
            HideEffectConfig();
        }

        private string BuildCurrentTag()
        {
            try
            {
                switch (_currentEffectType)
                {
                    case Models.AssTagType.Position:
                        return AssEffectBuilder.BuildPosition(double.Parse(GetConfigValue(0)), double.Parse(GetConfigValue(1)));
                    case Models.AssTagType.Move:
                        return AssEffectBuilder.BuildMove(double.Parse(GetConfigValue(0)), double.Parse(GetConfigValue(1)),
                                                          double.Parse(GetConfigValue(2)), double.Parse(GetConfigValue(3)));
                    case Models.AssTagType.Alignment:
                        return AssEffectBuilder.BuildAlignment(int.Parse(GetConfigValue(0)));
                    case Models.AssTagType.Origin:
                        return AssEffectBuilder.BuildOrigin(double.Parse(GetConfigValue(0)), double.Parse(GetConfigValue(1)));
                    case Models.AssTagType.RotateZ:
                        return AssEffectBuilder.BuildRotation("z", double.Parse(GetConfigValue(0)));
                    case Models.AssTagType.RotateX:
                        return AssEffectBuilder.BuildRotation("x", double.Parse(GetConfigValue(0)));
                    case Models.AssTagType.RotateY:
                        return AssEffectBuilder.BuildRotation("y", double.Parse(GetConfigValue(0)));
                    case Models.AssTagType.ScaleX:
                        return AssEffectBuilder.BuildScale("x", double.Parse(GetConfigValue(0)));
                    case Models.AssTagType.ScaleY:
                        return AssEffectBuilder.BuildScale("y", double.Parse(GetConfigValue(0)));
                    case Models.AssTagType.ShearX:
                        return AssEffectBuilder.BuildShear("x", double.Parse(GetConfigValue(0)));
                    case Models.AssTagType.ShearY:
                        return AssEffectBuilder.BuildShear("y", double.Parse(GetConfigValue(0)));
                    case Models.AssTagType.FontName:
                        return AssEffectBuilder.BuildFontName(GetConfigValue(0));
                    case Models.AssTagType.FontSize:
                        return AssEffectBuilder.BuildFontSize(int.Parse(GetConfigValue(0)));
                    case Models.AssTagType.Bold:
                        return AssEffectBuilder.BuildBold(int.Parse(GetConfigValue(0)));
                    case Models.AssTagType.Italic:
                        return AssEffectBuilder.BuildItalic(GetConfigValue(0) == "1");
                    case Models.AssTagType.Underline:
                        return AssEffectBuilder.BuildUnderline(GetConfigValue(0) == "1");
                    case Models.AssTagType.Strikeout:
                        return AssEffectBuilder.BuildStrikeout(GetConfigValue(0) == "1");
                    case Models.AssTagType.Border:
                        return AssEffectBuilder.BuildBorder(double.Parse(GetConfigValue(0)));
                    case Models.AssTagType.Shadow:
                        return AssEffectBuilder.BuildShadow(double.Parse(GetConfigValue(0)));
                    case Models.AssTagType.Blur:
                        return AssEffectBuilder.BuildBlur(double.Parse(GetConfigValue(0)));
                    case Models.AssTagType.EdgeBlur:
                        return AssEffectBuilder.BuildEdgeBlur(int.Parse(GetConfigValue(0)));
                    case Models.AssTagType.PrimaryColor:
                        return AssEffectBuilder.BuildColor("1c", GetConfigValue(0));
                    case Models.AssTagType.BorderColor:
                        return AssEffectBuilder.BuildColor("3c", GetConfigValue(0));
                    case Models.AssTagType.ShadowColor:
                        return AssEffectBuilder.BuildColor("4c", GetConfigValue(0));
                    case Models.AssTagType.Alpha:
                        return AssEffectBuilder.BuildAlpha("alpha", GetConfigValue(0));
                    case Models.AssTagType.Fade:
                        return AssEffectBuilder.BuildFade(int.Parse(GetConfigValue(0)), int.Parse(GetConfigValue(1)));
                    case Models.AssTagType.KaraokeK:
                        return AssEffectBuilder.BuildKaraoke("k", int.Parse(GetConfigValue(0)));
                    case Models.AssTagType.KaraokeKF:
                        return AssEffectBuilder.BuildKaraoke("kf", int.Parse(GetConfigValue(0)));
                    case Models.AssTagType.KaraokeKO:
                        return AssEffectBuilder.BuildKaraoke("ko", int.Parse(GetConfigValue(0)));
                    default:
                        return "";
                }
            }
            catch
            {
                return "";
            }
        }

        #endregion

        #region Effect - Preset Handlers

        private void BtnPresetFadeIn_Click(object sender, RoutedEventArgs e)
        {
            ApplyTagToAllLines(AssEffectBuilder.BuildFade(300, 200));
        }

        private void BtnPresetFadeOut_Click(object sender, RoutedEventArgs e)
        {
            ApplyTagToAllLines(AssEffectBuilder.BuildFade(200, 500));
        }

        private void BtnPresetGlow_Click(object sender, RoutedEventArgs e)
        {
            ApplyTagToAllLines(AssEffectBuilder.BuildBorder(2));
            ApplyTagToAllLines(AssEffectBuilder.BuildBlur(1));
        }

        private void BtnPresetBigText_Click(object sender, RoutedEventArgs e)
        {
            ApplyTagToAllLines(AssEffectBuilder.BuildFontSize(48));
            ApplyTagToAllLines(AssEffectBuilder.BuildBold(1));
        }

        private void BtnPresetCenterTop_Click(object sender, RoutedEventArgs e)
        {
            ApplyTagToAllLines(AssEffectBuilder.BuildAlignment(8));
        }

        private void BtnPresetCenterScreen_Click(object sender, RoutedEventArgs e)
        {
            ApplyTagToAllLines(AssEffectBuilder.BuildAlignment(5));
        }

        #endregion

        #region Effect - Toast

        private async void ShowToastEffect(string message)
        {
            ToastTextEffect.Text = message;
            ToastBorderEffect.Visibility = Visibility.Visible;
            await Task.Delay(2000);
            ToastBorderEffect.Visibility = Visibility.Collapsed;
        }

        #endregion

    }
}
