Imports System.Drawing
Imports System.Text
Imports System.Windows
Imports System.Windows.Media
Imports Microsoft.Win32
Imports Subtitle_draft_GMTPC.Services

Partial Class MainWindow

#Region "Search Fonts - Fields"

    ' Danh sách các font có sẵn trên Windows
    Private _windowsFonts As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
    ' Danh sách các font không có trên Windows
    Private _missingFonts As New List(Of String)()
    Private _isSearchFontsUpdating As Boolean = False

#End Region

#Region "Search Fonts - Event Handlers"

    ''' <summary>
    ''' Khi nhập styles vào Panel 1 → tự động parse và tìm font thiếu
    ''' </summary>
    Private Sub TxtSearchFontsInput_TextChanged(sender As Object, e As TextChangedEventArgs)
        If _isSearchFontsUpdating Then Return
        Try
            _isSearchFontsUpdating = True
            Dim inputText = SubtitleParser.SanitizeContent(TxtSearchFontsInput.Text)
            ParseStylesAndFindMissingFonts(inputText)
        Catch ex As Exception
            TxtStylesFontCount.Text = String.Format("(Lỗi: {0})", ex.Message)
        Finally
            _isSearchFontsUpdating = False
        End Try
    End Sub

    ''' <summary>
    ''' Button Search Fonts: Mở Google search cho các font được chọn (hoặc tất cả)
    ''' </summary>
    Private Async Sub BtnSearchFonts_Click(sender As Object, e As RoutedEventArgs)
        If _missingFonts.Count = 0 Then
            ShowToastSearchFonts("Không có font nào thiếu!")
            Return
        End If

        ' Lấy danh sách font được chọn, nếu không chọn thì lấy tất cả
        Dim fontsToSearch As New List(Of String)()
        If LstMissingFonts.SelectedItems.Count > 0 Then
            For Each item In LstMissingFonts.SelectedItems
                fontsToSearch.Add(item.ToString())
            Next
        Else
            fontsToSearch.AddRange(_missingFonts)
        End If

        If fontsToSearch.Count = 0 Then
            ShowToastSearchFonts("Vui lòng chọn ít nhất 1 font!")
            Return
        End If

        ' Mở Google search cho từng font
        For Each fontName In fontsToSearch
            Try
                Dim searchUrl = String.Format("https://www.google.com/search?q={0}+font+download", System.Net.WebUtility.UrlEncode(fontName))
                System.Diagnostics.Process.Start(New ProcessStartInfo With {
                    .FileName = searchUrl,
                    .UseShellExecute = True
                })
                Await Task.Delay(500) ' Delay nhỏ để tránh mở quá nhiều tab cùng lúc
            Catch ex As Exception
                ' Bỏ qua lỗi khi mở link
            End Try
        Next

        ShowToastSearchFonts(String.Format("Đã mở {0} tab search!", fontsToSearch.Count))
    End Sub

    ''' <summary>
    ''' ListBox PreviewMouseWheel: Cho phép scroll mà không cần focus
    ''' </summary>
    Private Sub ListBox_PreviewMouseWheel(sender As Object, e As MouseWheelEventArgs)
        Dim listBox = TryCast(sender, ListBox)
        If listBox Is Nothing Then Return

        Dim scrollViewer As ScrollViewer = Nothing
        Dim dependencyObject = DirectCast(listBox, DependencyObject)

        ' Tìm ScrollViewer trong ListBox
        For i As Integer = 0 To VisualTreeHelper.GetChildrenCount(dependencyObject) - 1
            Dim child = VisualTreeHelper.GetChild(dependencyObject, i)
            If TypeOf child Is ScrollViewer Then
                scrollViewer = DirectCast(child, ScrollViewer)
                Exit For
            End If
        Next

        If scrollViewer IsNot Nothing Then
            scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset - e.Delta / 3)
            e.Handled = True
        End If
    End Sub

#End Region

#Region "Search Fonts - Font Parsing & Detection"

    ''' <summary>
    ''' Lấy danh sách font có sẵn trên Windows
    ''' </summary>
    Private Sub LoadWindowsFonts()
        _windowsFonts.Clear()
        Try
            Using g = Graphics.FromHwnd(IntPtr.Zero)
                Dim families = System.Drawing.FontFamily.Families
                For Each family In families
                    _windowsFonts.Add(family.Name)
                Next
            End Using
        Catch ex As Exception
            ' Fallback: đọc từ registry
            Try
                Using key = Registry.LocalMachine.OpenSubKey("SOFTWARE\Microsoft\Windows NT\CurrentVersion\Fonts")
                    If key IsNot Nothing Then
                        For Each valueName In key.GetValueNames()
                            _windowsFonts.Add(valueName.Replace(" (TrueType)", "").Replace(" (OpenType)", "").Trim())
                        Next
                    End If
                End Using
            Catch
                ' Nếu vẫn lỗi, để trống danh sách
            End Try
        End Try
    End Sub

    ''' <summary>
    ''' Parse styles và tìm font không có trên Windows
    ''' </summary>
    Private Sub ParseStylesAndFindMissingFonts(inputText As String)
        If String.IsNullOrWhiteSpace(inputText) Then
            LstMissingFonts.Items.Clear()
            TxtStylesFontCount.Text = ""
            _missingFonts.Clear()
            Return
        End If

        ' Load Windows fonts nếu chưa load
        If _windowsFonts.Count = 0 Then
            LoadWindowsFonts()
        End If

        ' Parse từng dòng Style
        Dim lines = inputText.Split({Environment.NewLine, vbCr, vbLf}, StringSplitOptions.None)
        Dim fontNames As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
        Dim styleCount As Integer = 0

        For Each line In lines
            Dim trimmed = line.Trim()
            If trimmed.StartsWith("Style:") OrElse trimmed.StartsWith("Style :") Then
                styleCount += 1
                ' Format: Style: Name,Fontname,Fontsize,...
                Dim parts = trimmed.Split(","c)
                If parts.Length >= 3 Then
                    ' parts(0) = "Style: Name" hoặc "Style : Name"
                    ' parts(1) = Fontname
                    Dim fontName = parts(1).Trim()
                    If Not String.IsNullOrWhiteSpace(fontName) Then
                        fontNames.Add(fontName)
                    End If
                End If
            End If
        Next

        ' Tìm font không có trên Windows
        _missingFonts.Clear()
        LstMissingFonts.Items.Clear()

        ' Kiểm tra xem có font đặc biệt nào không
        Dim hasSpecialFonts = False
        Dim specialPrefixes = {"MTO", "SFU", "UTM", "UVN", "VNI"}
        Dim specialFontsFound As New List(Of String)()

        For Each fontName In fontNames
            ' Kiểm tra font đặc biệt
            For Each prefix In specialPrefixes
                If fontName.ToUpper().StartsWith(prefix) Then
                    hasSpecialFonts = True
                    If Not specialFontsFound.Contains(fontName, StringComparer.OrdinalIgnoreCase) Then
                        specialFontsFound.Add(fontName)
                    End If
                    Exit For
                End If
            Next

            ' Thêm vào danh sách font thiếu nếu không có trên Windows
            If Not _windowsFonts.Contains(fontName) Then
                _missingFonts.Add(fontName)
                LstMissingFonts.Items.Add(fontName)
            End If
        Next

        TxtStylesFontCount.Text = String.Format("({0} styles - {1} font thiếu)", styleCount, _missingFonts.Count)

        ' Nếu có font đặc biệt, hỏi người dùng có muốn cài đặt không
        If hasSpecialFonts Then
            Dim specialFontList = String.Join(", ", specialFontsFound)
            Dim result = MessageBox.Show(String.Format("Phát hiện font đặc biệt: {0}{1}Bạn có muốn tải và cài đặt Gouenji.Fansub.Fonts ngay không?", specialFontList, Environment.NewLine),
                                         "Cài đặt Font Pack", MessageBoxButton.YesNo, MessageBoxImage.Question)
            If result = MessageBoxResult.Yes Then
                ' Chạy async với fire-and-forget pattern an toàn
                Task.Run(Sub() DownloadAndInstallFontPackAsync())
            End If
        End If

        ' Nếu có font thiếu, hiển thị danh sách
        If _missingFonts.Count > 0 Then
            ShowToastSearchFonts(String.Format("Tìm thấy {0} font không có trên Windows!", _missingFonts.Count))
        End If
    End Sub

#End Region

#Region "Search Fonts - Font Installation"

    ''' <summary>
    ''' Tải và cài đặt font pack từ GitHub (async version)
    ''' </summary>
    Private Async Function DownloadAndInstallFontPack() As Task
        Dim tempDir = System.IO.Path.GetTempPath()
        Dim installerPath = System.IO.Path.Combine(tempDir, "Gouenji.Fansub.Fonts.exe")
        Dim downloadUrl = "https://github.com/ghostminhtoan/MMT/releases/download/v1.0/Gouenji.Fansub.Fonts.exe"

        Try
            ShowToastSearchFonts("Đang tải Gouenji.Fansub.Fonts.exe...")

            ' Tải file
            Using client = New System.Net.WebClient()
                Await client.DownloadFileTaskAsync(New Uri(downloadUrl), installerPath)
            End Using

            If Not System.IO.File.Exists(installerPath) Then
                ShowToastSearchFonts("Lỗi: Không tải được file!")
                Return
            End If

            ShowToastSearchFonts("Đang cài đặt font pack...")

            ' Chạy installer với /passive
            Dim psi = New ProcessStartInfo With {
                .FileName = installerPath,
                .Arguments = "/passive",
                .UseShellExecute = True
            }

            Dim installerProcess = Process.Start(psi)
            If installerProcess IsNot Nothing Then
                ' Chờ installer kết thúc
                Await Task.Run(Sub()
                                   installerProcess.WaitForExit()
                               End Sub)

                ' Xóa file installer sau khi cài xong
                If System.IO.File.Exists(installerPath) Then
                    Try
                        System.IO.File.Delete(installerPath)
                        ShowToastSearchFonts("✅ Cài đặt font pack thành công!")
                    Catch ex As Exception
                        ShowToastSearchFonts("Cài đặt xong nhưng không xóa được file tạm.")
                    End Try
                End If
            Else
                ShowToastSearchFonts("Lỗi: Không thể chạy installer!")
            End If

        Catch ex As Exception
            ShowToastSearchFonts(String.Format("Lỗi tải/cài đặt: {0}", ex.Message))
            ' Cleanup nếu có lỗi
            If System.IO.File.Exists(installerPath) Then
                Try
                    System.IO.File.Delete(installerPath)
                Catch
                End Try
            End If
        End Try
    End Function

    ''' <summary>
    ''' Tải và cài đặt font pack từ GitHub (synchronous version for Task.Run)
    ''' </summary>
    Private Sub DownloadAndInstallFontPackAsync()
        Dim tempDir = System.IO.Path.GetTempPath()
        Dim installerPath = System.IO.Path.Combine(tempDir, "Gouenji.Fansub.Fonts.exe")
        Dim downloadUrl = "https://github.com/ghostminhtoan/MMT/releases/download/v1.0/Gouenji.Fansub.Fonts.exe"

        Try
            ' Tải file
            Using client = New System.Net.WebClient()
                client.DownloadFile(New Uri(downloadUrl), installerPath)
            End Using

            If Not System.IO.File.Exists(installerPath) Then
                ' Không thể hiển thị toast từ background thread, bỏ qua
                Return
            End If

            ' Chạy installer với /passive
            Dim psi = New ProcessStartInfo With {
                .FileName = installerPath,
                .Arguments = "/passive",
                .UseShellExecute = True
            }

            Dim installerProcess = Process.Start(psi)
            If installerProcess IsNot Nothing Then
                ' Chờ installer kết thúc
                installerProcess.WaitForExit()

                ' Xóa file installer sau khi cài xong
                If System.IO.File.Exists(installerPath) Then
                    Try
                        System.IO.File.Delete(installerPath)
                    Catch
                    End Try
                End If
            End If

        Catch ex As Exception
            ' Bỏ qua lỗi trong background thread
            If System.IO.File.Exists(installerPath) Then
                Try
                    System.IO.File.Delete(installerPath)
                Catch
                End Try
            End If
        End Try
    End Sub

#End Region

#Region "Search Fonts - Toast"

    Private Async Sub ShowToastSearchFonts(message As String)
        ToastTextSearchFonts.Text = message
        ToastBorderSearchFonts.Visibility = Visibility.Visible
        Await Task.Delay(2500)
        ToastBorderSearchFonts.Visibility = Visibility.Collapsed
    End Sub

#End Region

End Class
