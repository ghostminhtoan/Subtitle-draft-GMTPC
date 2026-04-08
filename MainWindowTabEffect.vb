Imports System
Imports System.Windows.Controls
Imports System.Windows.Media
Imports Subtitle_draft_GMTPC.Models
Imports Subtitle_draft_GMTPC.Services

Partial Class MainWindow

#Region "Effect - Fields"

    Private _isEffectUpdating As Boolean = False
    Private _effectDebounceTimer As New System.Windows.Threading.DispatcherTimer()
    Private _effectPendingUpdate As Boolean = False
    Private _currentEffectTag As String = ""
    Private _currentEffectType As Models.AssTagType = Models.AssTagType.Unknown
    Private _currentEffectName As String = ""

#End Region

#Region "Effect - Initialization"

    Private Sub InitializeEffectDebounce()
        _effectDebounceTimer.Interval = TimeSpan.FromMilliseconds(150)
        AddHandler _effectDebounceTimer.Tick, Sub(sender, e)
            _effectDebounceTimer.Stop()
            If _effectPendingUpdate Then
                _effectPendingUpdate = False
                ApplyEffectsToOutput()
            End If
        End Sub
    End Sub

#End Region

#Region "Effect - Event Handlers"

    Private Sub TxtEffectInput_TextChanged(sender As Object, e As TextChangedEventArgs)
        If _isEffectUpdating Then Return
        Try
            _isEffectUpdating = True
            Dim content = SubtitleParser.SanitizeContent(TxtEffectInput.Text)
            If String.IsNullOrWhiteSpace(content) Then
                TxtEffectLineCount.Text = ""
                TxtEffectOutput.Text = ""
                Return
            End If

            Dim lines = content.Split({Environment.NewLine, vbCr, vbLf}, StringSplitOptions.RemoveEmptyEntries)
            TxtEffectLineCount.Text = String.Format("({0} dong)", lines.Length)

            _effectPendingUpdate = True
            _effectDebounceTimer.Stop()
            _effectDebounceTimer.Start()
        Catch ex As Exception
            TxtEffectLineCount.Text = String.Format("(Loi: {0})", ex.Message)
        Finally
            _isEffectUpdating = False
        End Try
    End Sub

    Private Sub BtnColorPicker_Click(sender As Object, e As RoutedEventArgs)
        Try
            Dim dlg As New System.Windows.Forms.ColorDialog()
            dlg.FullOpen = True
            If dlg.ShowDialog() = System.Windows.Forms.DialogResult.OK Then
                Dim hex = dlg.Color.B.ToString("X2") & dlg.Color.G.ToString("X2") & dlg.Color.R.ToString("X2")
                Dim hexBox = EffectConfigFields.Children.OfType(Of StackPanel)().SelectMany(Function(sp) sp.Children.OfType(Of TextBox)()).FirstOrDefault(Function(t) t.Tag IsNot Nothing AndAlso t.Tag.ToString().ToLower() = "hex")
                If hexBox IsNot Nothing Then
                    hexBox.Text = hex
                    ShowToastEffect("Da dien hex: " & hex)
                Else
                    ShowToastEffect("Mo 1 effect mau truoc (vi du: \\1c)")
                End If
            End If
        Catch ex As Exception
            System.Windows.MessageBox.Show("Loi: " & ex.Message, "Loi", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error)
        End Try
    End Sub
    Private Sub BtnCopyEffect_Click(sender As Object, e As RoutedEventArgs)
        If String.IsNullOrWhiteSpace(TxtEffectOutput.Text) Then Return
        Try
            Clipboard.SetText(TxtEffectOutput.Text)
            ShowToastEffect("Da copy output!")
        Catch ex As Exception
            System.Windows.MessageBox.Show("Loi: " & ex.Message, "Loi", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error)
        End Try
    End Sub

    Private Sub BtnClearEffect_Click(sender As Object, e As RoutedEventArgs)
        Try
            Dim content = TxtEffectInput.Text
            If String.IsNullOrWhiteSpace(content) Then Return
            TxtEffectInput.Text = AssEffectBuilder.RemoveAllTagsFromAllLines(content)
            TxtEffectOutput.Text = TxtEffectInput.Text
            HideEffectConfig()
            ShowToastEffect("Da clear toan bo effects!")
        Catch ex As Exception
            System.Windows.MessageBox.Show("Loi: " & ex.Message, "Loi", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error)
        End Try
    End Sub

    Private Sub BtnResetEffect_Click(sender As Object, e As RoutedEventArgs)
        Try
            Dim content = TxtEffectInput.Text
            If String.IsNullOrWhiteSpace(content) Then Return
            TxtEffectOutput.Text = AssEffectBuilder.RemoveAllTagsFromAllLines(content)
            ShowToastEffect("Da reset tags!")
        Catch ex As Exception
            System.Windows.MessageBox.Show("Loi: " & ex.Message, "Loi", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error)
        End Try
    End Sub

#End Region

#Region "Effect - Apply Methods"

    Private Sub ApplyEffectsToOutput()
        Dim content = TxtEffectInput.Text
        If String.IsNullOrWhiteSpace(content) Then
            TxtEffectOutput.Text = ""
            Return
        End If
        TxtEffectOutput.Text = content
    End Sub

    Private Sub ApplyTagToSelectedLine(tag As String)
        Dim outputContent = TxtEffectOutput.Text
        If String.IsNullOrWhiteSpace(outputContent) Then outputContent = TxtEffectInput.Text
        If String.IsNullOrWhiteSpace(outputContent) Then Return

        Dim selStart = TxtEffectInput.SelectionStart
        Dim lineIndex = 0
        Dim pos = 0
        Dim lines = outputContent.Split({Environment.NewLine}, StringSplitOptions.None)

        For i As Integer = 0 To lines.Length - 1
            Dim lineStart = pos
            Dim lineEnd = pos + lines(i).Length
            If selStart >= lineStart AndAlso selStart < lineEnd Then
                lineIndex = i
                Exit For
            End If
            pos = lineEnd + Environment.NewLine.Length
        Next

        lines(lineIndex) = AssEffectBuilder.ApplyTagToLine(lines(lineIndex), tag)
        TxtEffectOutput.Text = String.Join(Environment.NewLine, lines)
        HideEffectConfig()
        ShowToastEffect("Da apply vao dong (Output)!")
    End Sub

    Private Sub ApplyTagToAllLines(tag As String)
        Dim outputContent = TxtEffectOutput.Text
        If String.IsNullOrWhiteSpace(outputContent) Then outputContent = TxtEffectInput.Text
        If String.IsNullOrWhiteSpace(outputContent) Then Return

        TxtEffectOutput.Text = AssEffectBuilder.ApplyTagsToAllLines(outputContent, {tag})
        HideEffectConfig()
        ShowToastEffect("Da apply vao TAT CA dong (Output)!")
    End Sub

#End Region

#Region "Effect - Config Panel Management"


    Private Sub ShowEffectConfig(title As String, effectType As Models.AssTagType, fields As Tuple(Of String, String, String)())
        _currentEffectType = effectType
        _currentEffectName = title
        EffectConfigTitle.Text = title
        EffectConfigFields.Children.Clear()
        For Each f In fields
            Dim sp = New StackPanel() With {.Orientation = Orientation.Horizontal, .Margin = New Thickness(0, 2, 0, 2)}
            Dim lbl = New TextBlock() With {.Text = f.Item1, .Foreground = Brushes.White, .Width = 80, .VerticalAlignment = VerticalAlignment.Center}
            Dim txt = New TextBox() With {
                .Text = f.Item2,
                .Tag = f.Item3,
                .Width = 150,
                .Style = TryFindResource("DarkTextBoxStyle")
            }
            sp.Children.Add(lbl)
            sp.Children.Add(txt)
            EffectConfigFields.Children.Add(sp)
        Next
        EffectConfigBorder.Visibility = Visibility.Visible
    End Sub

    Private Sub HideEffectConfig()
        EffectConfigBorder.Visibility = Visibility.Collapsed
        EffectConfigFields.Children.Clear()
        _currentEffectType = Models.AssTagType.Unknown
        _currentEffectName = ""
    End Sub

    Private Function GetConfigValue(index As Integer) As String
        If index < EffectConfigFields.Children.Count Then
            Dim sp = TryCast(EffectConfigFields.Children(index), StackPanel)
            If sp IsNot Nothing AndAlso sp.Children.Count > 1 Then
                Dim txt = TryCast(sp.Children(1), TextBox)
                If txt IsNot Nothing Then Return txt.Text
            End If
        End If
        Return ""
    End Function

#End Region

#Region "Effect - Position & Movement Handlers"

    Private Sub BtnEffectPosition_Click(sender As Object, e As RoutedEventArgs)
        ShowEffectConfig("\pos - Vi tri", Models.AssTagType.Position,
            {Tuple.Create("X:", "640", "x"), Tuple.Create("Y:", "360", "y")})
    End Sub

    Private Sub BtnEffectMove_Click(sender As Object, e As RoutedEventArgs)
        ShowEffectConfig("\move - Di chuyen", Models.AssTagType.Move,
            {Tuple.Create("X1:", "0", "x1"), Tuple.Create("Y1:", "0", "y1"),
             Tuple.Create("X2:", "640", "x2"), Tuple.Create("Y2:", "360", "y2")})
    End Sub

    Private Sub BtnEffectAlign_Click(sender As Object, e As RoutedEventArgs)
        ShowEffectConfig("\an - Can le", Models.AssTagType.Alignment,
            {Tuple.Create("Can le (1-9):", "5", "n")})
    End Sub

    Private Sub BtnEffectOrigin_Click(sender As Object, e As RoutedEventArgs)
        ShowEffectConfig("\org - Tam xoay", Models.AssTagType.Origin,
            {Tuple.Create("X:", "640", "x"), Tuple.Create("Y:", "360", "y")})
    End Sub

#End Region

#Region "Effect - Transform & Rotation Handlers"

    Private Sub BtnEffectRotZ_Click(sender As Object, e As RoutedEventArgs)
        ShowEffectConfig("\frz - Xoay Z", Models.AssTagType.RotateZ,
            {Tuple.Create("Do:", "0", "degrees")})
    End Sub

    Private Sub BtnEffectRotX_Click(sender As Object, e As RoutedEventArgs)
        ShowEffectConfig("\frx - Lat X", Models.AssTagType.RotateX,
            {Tuple.Create("Do:", "0", "degrees")})
    End Sub

    Private Sub BtnEffectRotY_Click(sender As Object, e As RoutedEventArgs)
        ShowEffectConfig("\fry - Lat Y", Models.AssTagType.RotateY,
            {Tuple.Create("Do:", "0", "degrees")})
    End Sub

    Private Sub BtnEffectScaleX_Click(sender As Object, e As RoutedEventArgs)
        ShowEffectConfig("\fscx - Co gian X", Models.AssTagType.ScaleX,
            {Tuple.Create("Phan tram:", "100", "percent")})
    End Sub

    Private Sub BtnEffectScaleY_Click(sender As Object, e As RoutedEventArgs)
        ShowEffectConfig("\fscy - Co gian Y", Models.AssTagType.ScaleY,
            {Tuple.Create("Phan tram:", "100", "percent")})
    End Sub

    Private Sub BtnEffectShearX_Click(sender As Object, e As RoutedEventArgs)
        ShowEffectConfig("\fax - Nghien X", Models.AssTagType.ShearX,
            {Tuple.Create("Gia tri:", "0", "value")})
    End Sub

    Private Sub BtnEffectShearY_Click(sender As Object, e As RoutedEventArgs)
        ShowEffectConfig("\fay - Nghien Y", Models.AssTagType.ShearY,
            {Tuple.Create("Gia tri:", "0", "value")})
    End Sub

#End Region

#Region "Effect - Font & Style Handlers"

    Private Sub BtnEffectFontName_Click(sender As Object, e As RoutedEventArgs)
        ShowEffectConfig("\fn - Font chu", Models.AssTagType.FontName,
            {Tuple.Create("Ten font:", "Arial", "fontName")})
    End Sub

    Private Sub BtnEffectFontSize_Click(sender As Object, e As RoutedEventArgs)
        ShowEffectConfig("\fs - Co chu", Models.AssTagType.FontSize,
            {Tuple.Create("Size:", "24", "size")})
    End Sub

    Private Sub BtnEffectBold_Click(sender As Object, e As RoutedEventArgs)
        ShowEffectConfig("\b - In dam", Models.AssTagType.Bold,
            {Tuple.Create("Dam (0/1):", "1", "weight")})
    End Sub

    Private Sub BtnEffectItalic_Click(sender As Object, e As RoutedEventArgs)
        ShowEffectConfig("\i - In nghien", Models.AssTagType.Italic,
            {Tuple.Create("Nghien (0/1):", "1", "on")})
    End Sub

    Private Sub BtnEffectUnderline_Click(sender As Object, e As RoutedEventArgs)
        ShowEffectConfig("\u - Gach chan", Models.AssTagType.Underline,
            {Tuple.Create("Gach chan (0/1):", "1", "on")})
    End Sub

    Private Sub BtnEffectStrikeout_Click(sender As Object, e As RoutedEventArgs)
        ShowEffectConfig("\s - Gach ngang", Models.AssTagType.Strikeout,
            {Tuple.Create("Gach ngang (0/1):", "1", "on")})
    End Sub

#End Region

#Region "Effect - Border & Shadow Handlers"

    Private Sub BtnEffectBorder_Click(sender As Object, e As RoutedEventArgs)
        ShowEffectConfig("\bord - Vien chu", Models.AssTagType.Border,
            {Tuple.Create("Do day:", "2", "size")})
    End Sub

    Private Sub BtnEffectShadow_Click(sender As Object, e As RoutedEventArgs)
        ShowEffectConfig("\shad - Do bong", Models.AssTagType.Shadow,
            {Tuple.Create("Do dai:", "2", "size")})
    End Sub

    Private Sub BtnEffectBlur_Click(sender As Object, e As RoutedEventArgs)
        ShowEffectConfig("\blur - Mo vien", Models.AssTagType.Blur,
            {Tuple.Create("Muc do:", "1", "level")})
    End Sub

    Private Sub BtnEffectEdgeBlur_Click(sender As Object, e As RoutedEventArgs)
        ShowEffectConfig("\be - Mo canh", Models.AssTagType.EdgeBlur,
            {Tuple.Create("Muc do (0/1):", "1", "level")})
    End Sub

#End Region

#Region "Effect - Color & Alpha Handlers"

    Private Sub BtnEffectPriColor_Click(sender As Object, e As RoutedEventArgs)
        ShowEffectConfig("\1c - Mau loi chinh", Models.AssTagType.PrimaryColor,
            {Tuple.Create("Hex BGR:", "0000FF", "hex")})
    End Sub

    Private Sub BtnEffectBordColor_Click(sender As Object, e As RoutedEventArgs)
        ShowEffectConfig("\3c - Mau vien", Models.AssTagType.BorderColor,
            {Tuple.Create("Hex BGR:", "00FF00", "hex")})
    End Sub

    Private Sub BtnEffectShadColor_Click(sender As Object, e As RoutedEventArgs)
        ShowEffectConfig("\4c - Mau bong", Models.AssTagType.ShadowColor,
            {Tuple.Create("Hex BGR:", "000000", "hex")})
    End Sub

    Private Sub BtnEffectAlpha_Click(sender As Object, e As RoutedEventArgs)
        ShowEffectConfig("\alpha - Do trong suot", Models.AssTagType.Alpha,
            {Tuple.Create("Alpha (00-FF):", "80", "hex")})
    End Sub

#End Region

#Region "Effect - Fade & Karaoke Handlers"

    Private Sub BtnEffectFade_Click(sender As Object, e As RoutedEventArgs)
        ShowEffectConfig("\fad - Mo dan", Models.AssTagType.Fade,
            {Tuple.Create("Vao (ms):", "300", "inTime"), Tuple.Create("Ra (ms):", "500", "outTime")})
    End Sub

    Private Sub BtnEffectKaraokeK_Click(sender As Object, e As RoutedEventArgs)
        ShowEffectConfig("\k - Karaoke", Models.AssTagType.KaraokeK,
            {Tuple.Create("Thoi gian (cs):", "50", "duration")})
    End Sub

    Private Sub BtnEffectKaraokeKF_Click(sender As Object, e As RoutedEventArgs)
        ShowEffectConfig("\kf - Karaoke quet", Models.AssTagType.KaraokeKF,
            {Tuple.Create("Thoi gian (cs):", "50", "duration")})
    End Sub

    Private Sub BtnEffectKaraokeKO_Click(sender As Object, e As RoutedEventArgs)
        ShowEffectConfig("\ko - Karaoke xoa vien", Models.AssTagType.KaraokeKO,
            {Tuple.Create("Thoi gian (cs):", "50", "duration")})
    End Sub

#End Region

#Region "Effect - Apply & Cancel Handlers"

    Private Sub BtnEffectApply_Click(sender As Object, e As RoutedEventArgs)
        Try
            Dim tag = BuildCurrentTag()
            If String.IsNullOrEmpty(tag) Then Return
            ApplyTagToSelectedLine(tag)
        Catch ex As Exception
            System.Windows.MessageBox.Show("Loi: " & ex.Message, "Loi", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error)
        End Try
    End Sub

    Private Sub BtnEffectApplyAll_Click(sender As Object, e As RoutedEventArgs)
        Try
            Dim tag = BuildCurrentTag()
            If String.IsNullOrEmpty(tag) Then Return
            ApplyTagToAllLines(tag)
        Catch ex As Exception
            System.Windows.MessageBox.Show("Loi: " & ex.Message, "Loi", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error)
        End Try
    End Sub

    Private Sub BtnEffectCancel_Click(sender As Object, e As RoutedEventArgs)
        HideEffectConfig()
    End Sub

    Private Function BuildCurrentTag() As String
        Try
            Select Case _currentEffectType
                Case Models.AssTagType.Position
                    Return AssEffectBuilder.BuildPosition(CDbl(GetConfigValue(0)), CDbl(GetConfigValue(1)))
                Case Models.AssTagType.Move
                    Return AssEffectBuilder.BuildMove(CDbl(GetConfigValue(0)), CDbl(GetConfigValue(1)),
                                                      CDbl(GetConfigValue(2)), CDbl(GetConfigValue(3)))
                Case Models.AssTagType.Alignment
                    Return AssEffectBuilder.BuildAlignment(CInt(GetConfigValue(0)))
                Case Models.AssTagType.Origin
                    Return AssEffectBuilder.BuildOrigin(CDbl(GetConfigValue(0)), CDbl(GetConfigValue(1)))
                Case Models.AssTagType.RotateZ
                    Return AssEffectBuilder.BuildRotation("z", CDbl(GetConfigValue(0)))
                Case Models.AssTagType.RotateX
                    Return AssEffectBuilder.BuildRotation("x", CDbl(GetConfigValue(0)))
                Case Models.AssTagType.RotateY
                    Return AssEffectBuilder.BuildRotation("y", CDbl(GetConfigValue(0)))
                Case Models.AssTagType.ScaleX
                    Return AssEffectBuilder.BuildScale("x", CDbl(GetConfigValue(0)))
                Case Models.AssTagType.ScaleY
                    Return AssEffectBuilder.BuildScale("y", CDbl(GetConfigValue(0)))
                Case Models.AssTagType.ShearX
                    Return AssEffectBuilder.BuildShear("x", CDbl(GetConfigValue(0)))
                Case Models.AssTagType.ShearY
                    Return AssEffectBuilder.BuildShear("y", CDbl(GetConfigValue(0)))
                Case Models.AssTagType.FontName
                    Return AssEffectBuilder.BuildFontName(GetConfigValue(0))
                Case Models.AssTagType.FontSize
                    Return AssEffectBuilder.BuildFontSize(CInt(GetConfigValue(0)))
                Case Models.AssTagType.Bold
                    Return AssEffectBuilder.BuildBold(CInt(GetConfigValue(0)))
                Case Models.AssTagType.Italic
                    Return AssEffectBuilder.BuildItalic(GetConfigValue(0) = "1")
                Case Models.AssTagType.Underline
                    Return AssEffectBuilder.BuildUnderline(GetConfigValue(0) = "1")
                Case Models.AssTagType.Strikeout
                    Return AssEffectBuilder.BuildStrikeout(GetConfigValue(0) = "1")
                Case Models.AssTagType.Border
                    Return AssEffectBuilder.BuildBorder(CDbl(GetConfigValue(0)))
                Case Models.AssTagType.Shadow
                    Return AssEffectBuilder.BuildShadow(CDbl(GetConfigValue(0)))
                Case Models.AssTagType.Blur
                    Return AssEffectBuilder.BuildBlur(CDbl(GetConfigValue(0)))
                Case Models.AssTagType.EdgeBlur
                    Return AssEffectBuilder.BuildEdgeBlur(CInt(GetConfigValue(0)))
                Case Models.AssTagType.PrimaryColor
                    Return AssEffectBuilder.BuildColor("1c", GetConfigValue(0))
                Case Models.AssTagType.BorderColor
                    Return AssEffectBuilder.BuildColor("3c", GetConfigValue(0))
                Case Models.AssTagType.ShadowColor
                    Return AssEffectBuilder.BuildColor("4c", GetConfigValue(0))
                Case Models.AssTagType.Alpha
                    Return AssEffectBuilder.BuildAlpha("alpha", GetConfigValue(0))
                Case Models.AssTagType.Fade
                    Return AssEffectBuilder.BuildFade(CInt(GetConfigValue(0)), CInt(GetConfigValue(1)))
                Case Models.AssTagType.KaraokeK
                    Return AssEffectBuilder.BuildKaraoke("k", CInt(GetConfigValue(0)))
                Case Models.AssTagType.KaraokeKF
                    Return AssEffectBuilder.BuildKaraoke("kf", CInt(GetConfigValue(0)))
                Case Models.AssTagType.KaraokeKO
                    Return AssEffectBuilder.BuildKaraoke("ko", CInt(GetConfigValue(0)))
                Case Else
                    Return ""
            End Select
        Catch
            Return ""
        End Try
    End Function

#End Region

#Region "Effect - Preset Handlers"

    Private Sub BtnPresetFadeIn_Click(sender As Object, e As RoutedEventArgs)
        ApplyTagToAllLines(AssEffectBuilder.BuildFade(300, 200))
    End Sub

    Private Sub BtnPresetFadeOut_Click(sender As Object, e As RoutedEventArgs)
        ApplyTagToAllLines(AssEffectBuilder.BuildFade(200, 500))
    End Sub

    Private Sub BtnPresetGlow_Click(sender As Object, e As RoutedEventArgs)
        ApplyTagToAllLines(AssEffectBuilder.BuildBorder(2))
        ApplyTagToAllLines(AssEffectBuilder.BuildBlur(1))
    End Sub

    Private Sub BtnPresetBigText_Click(sender As Object, e As RoutedEventArgs)
        ApplyTagToAllLines(AssEffectBuilder.BuildFontSize(48))
        ApplyTagToAllLines(AssEffectBuilder.BuildBold(1))
    End Sub

    Private Sub BtnPresetCenterTop_Click(sender As Object, e As RoutedEventArgs)
        ApplyTagToAllLines(AssEffectBuilder.BuildAlignment(8))
    End Sub

    Private Sub BtnPresetCenterScreen_Click(sender As Object, e As RoutedEventArgs)
        ApplyTagToAllLines(AssEffectBuilder.BuildAlignment(5))
    End Sub

#End Region

#Region "Effect - Toast"

    Private Async Sub ShowToastEffect(message As String)
        ToastTextEffect.Text = message
        ToastBorderEffect.Visibility = Visibility.Visible
        Await Task.Delay(2000)
        ToastBorderEffect.Visibility = Visibility.Collapsed
    End Sub

#End Region

End Class


