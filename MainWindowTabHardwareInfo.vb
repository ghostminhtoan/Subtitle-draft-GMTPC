Imports Subtitle_draft_GMTPC.Services

Partial Class MainWindow

#Region "Hardware Info - Fields"

    Private _isHardwareLoading As Boolean = False

#End Region

#Region "Hardware Info - Methods"

    ''' <summary>
    ''' Load toàn bộ thông tin phần cứng khi mở app
    ''' </summary>
    Private Sub LoadHardwareInfo()
        If _isHardwareLoading Then Return
        Try
            _isHardwareLoading = True

            TxtGpuInfo.Text = HardwareInfoService.GetGpuInfo()
            TxtCpuInfo.Text = HardwareInfoService.GetCpuInfo()
            TxtRamInfo.Text = HardwareInfoService.GetRamInfo()
            TxtMainboardInfo.Text = HardwareInfoService.GetMainboardInfo()
        Catch ex As Exception
            TxtGpuInfo.Text = String.Format("Lỗi: {0}", ex.Message)
            TxtCpuInfo.Text = String.Format("Lỗi: {0}", ex.Message)
            TxtRamInfo.Text = String.Format("Lỗi: {0}", ex.Message)
            TxtMainboardInfo.Text = String.Format("Lỗi: {0}", ex.Message)
        Finally
            _isHardwareLoading = False
        End Try
    End Sub

#End Region

#Region "Hardware Info - Toast"

    Private Async Sub ShowToastHardware(message As String)
        ToastTextHardware.Text = message
        ToastBorderHardware.Visibility = Visibility.Visible
        Await Task.Delay(2000)
        ToastBorderHardware.Visibility = Visibility.Collapsed
    End Sub

#End Region

#Region "Hardware Info - Copy Buttons"

    Private Sub BtnCopyGpu_Click(sender As Object, e As RoutedEventArgs)
        If String.IsNullOrWhiteSpace(TxtGpuInfo.Text) Then Return
        Try
            Clipboard.SetText(TxtGpuInfo.Text)
            ShowToastHardware("📋 Đã copy GPU info!")
        Catch ex As Exception
            MessageBox.Show("Lỗi: " & ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error)
        End Try
    End Sub

    Private Sub BtnCopyCpu_Click(sender As Object, e As RoutedEventArgs)
        If String.IsNullOrWhiteSpace(TxtCpuInfo.Text) Then Return
        Try
            Clipboard.SetText(TxtCpuInfo.Text)
            ShowToastHardware("📋 Đã copy CPU info!")
        Catch ex As Exception
            MessageBox.Show("Lỗi: " & ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error)
        End Try
    End Sub

    Private Sub BtnCopyRam_Click(sender As Object, e As RoutedEventArgs)
        If String.IsNullOrWhiteSpace(TxtRamInfo.Text) Then Return
        Try
            Clipboard.SetText(TxtRamInfo.Text)
            ShowToastHardware("📋 Đã copy RAM info!")
        Catch ex As Exception
            MessageBox.Show("Lỗi: " & ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error)
        End Try
    End Sub

    Private Sub BtnCopyMainboard_Click(sender As Object, e As RoutedEventArgs)
        If String.IsNullOrWhiteSpace(TxtMainboardInfo.Text) Then Return
        Try
            Clipboard.SetText(TxtMainboardInfo.Text)
            ShowToastHardware("📋 Đã copy Mainboard info!")
        Catch ex As Exception
            MessageBox.Show("Lỗi: " & ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error)
        End Try
    End Sub

#End Region

End Class
