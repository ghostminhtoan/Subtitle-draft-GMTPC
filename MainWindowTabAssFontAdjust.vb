Imports System.Text
Imports System.Windows
Imports Subtitle_draft_GMTPC.Services

Partial Class MainWindow

#Region "ASS Font Adjust - Fields"

    Private _isStylesUpdating As Boolean = False

#End Region

#Region "ASS Font Adjust - Event Handlers"

    ''' <summary>
    ''' Khi nhập styles vào Panel 1 → tự động cập nhật Panel 2
    ''' </summary>
    Private Sub TxtStylesInput_TextChanged(sender As Object, e As TextChangedEventArgs)
        If _isStylesUpdating Then Return
        Try
            _isStylesUpdating = True
            Dim inputText = SubtitleParser.SanitizeContent(TxtStylesInput.Text)
            UpdateStylesOutputWithText(inputText)
        Catch ex As Exception
            TxtStylesCount.Text = String.Format("(Lỗi: {0})", ex.Message)
        Finally
            _isStylesUpdating = False
        End Try
    End Sub

    ''' <summary>
    ''' Button - : Giảm font size
    ''' </summary>
    Private Sub BtnFontMinus_Click(sender As Object, e As RoutedEventArgs)
        Dim idx = CmbFontSize.SelectedIndex
        If idx > 0 Then
            CmbFontSize.SelectedIndex = idx - 1
        End If
    End Sub

    ''' <summary>
    ''' Button + : Tăng font size
    ''' </summary>
    Private Sub BtnFontPlus_Click(sender As Object, e As RoutedEventArgs)
        Dim idx = CmbFontSize.SelectedIndex
        If idx < CmbFontSize.Items.Count - 1 Then
            CmbFontSize.SelectedIndex = idx + 1
        End If
    End Sub

    ''' <summary>
    ''' ComboBox font size thay đổi → cập nhật output
    ''' </summary>
    Private Sub CmbFontSize_SelectionChanged(sender As Object, e As SelectionChangedEventArgs)
        UpdateStylesOutput()
    End Sub

    ''' <summary>
    ''' Button Copy Styles
    ''' </summary>
    Private Sub BtnCopyStyles_Click(sender As Object, e As RoutedEventArgs)
        If String.IsNullOrWhiteSpace(TxtStylesOutput.Text) Then Return
        Try
            Clipboard.SetText(TxtStylesOutput.Text)
            ShowToastStyles("📋 Đã copy styles!")
        Catch ex As Exception
            MessageBox.Show("Lỗi: " & ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error)
        End Try
    End Sub

#End Region

#Region "ASS Font Adjust - Update Methods"

    ''' <summary>
    ''' Cập nhật output styles với font size mới
    ''' </summary>
    Private Sub UpdateStylesOutput()
        UpdateStylesOutputWithText(TxtStylesInput.Text)
    End Sub

    Private Sub UpdateStylesOutputWithText(inputText As String)
        If String.IsNullOrWhiteSpace(inputText) Then
            TxtStylesOutput.Text = ""
            TxtStylesCount.Text = ""
            Return
        End If

        ' Lấy giá trị font size tăng thêm từ ComboBox
        Dim fontSizeDelta As Integer = 0
        If CmbFontSize.SelectedIndex >= 0 Then
            fontSizeDelta = CmbFontSize.SelectedIndex + 1 ' Index 0 = font size 1
        End If

        ' Parse và xử lý từng dòng Style
        Dim lines = inputText.Split({Environment.NewLine, vbCr, vbLf}, StringSplitOptions.None)
        Dim sb = New StringBuilder()
        Dim styleCount As Integer = 0

        For Each line In lines
            Dim trimmed = line.Trim()
            If trimmed.StartsWith("Style:") OrElse trimmed.StartsWith("Style :") Then
                ' Format: Style: Name,Fontname,Fontsize,...
                ' Tách bằng dấu phẩy, field 3 (index 2) là Fontsize
                Dim parts = trimmed.Split(","c)
                If parts.Length >= 3 Then
                    ' Tìm vị trí field font size (sau "Style:" hoặc "Style :")
                    ' parts(0) = "Style: Name" hoặc "Style : Name"
                    ' parts(1) = Fontname
                    ' parts(2) = Fontsize
                    Dim currentFontSize As Integer = 0
                    If Integer.TryParse(parts(2).Trim(), currentFontSize) Then
                        Dim newFontSize = currentFontSize + fontSizeDelta
                        If newFontSize < 1 Then newFontSize = 1
                        parts(2) = newFontSize.ToString()
                        styleCount += 1
                    End If
                End If
                sb.AppendLine(String.Join(",", parts))
            Else
                ' Dòng không phải Style → giữ nguyên
                If Not String.IsNullOrWhiteSpace(trimmed) Then
                    sb.AppendLine(trimmed)
                End If
            End If
        Next

        TxtStylesOutput.Text = sb.ToString().TrimEnd()
        TxtStylesCount.Text = String.Format("({0} styles, +{1}px)", styleCount, fontSizeDelta)
    End Sub

#End Region

#Region "ASS Font Adjust - Toast"

    Private Async Sub ShowToastStyles(message As String)
        ToastTextStyles.Text = message
        ToastBorderStyles.Visibility = Visibility.Visible
        Await Task.Delay(2000)
        ToastBorderStyles.Visibility = Visibility.Collapsed
    End Sub

#End Region

End Class
