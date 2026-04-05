Imports System.Management
Imports System.Text

Namespace Services
    ''' <summary>
    ''' Service thu thập thông tin phần cứng hệ thống
    ''' </summary>
    Public Class HardwareInfoService

#Region "GPU Info"

        Public Shared Function GetGpuInfo() As String
            Dim sb = New StringBuilder()
            Try
                Dim searcher = New ManagementObjectSearcher("SELECT * FROM Win32_VideoController")
                Dim gpuIndex As Integer = 0

                For Each obj As ManagementObject In searcher.Get()
                    gpuIndex += 1
                    If gpuIndex > 1 Then sb.AppendLine()
                    sb.AppendLine(String.Format("=== GPU {0} ===", gpuIndex))

                    Dim name = GetProperty(obj, "Name")
                    If Not String.IsNullOrEmpty(name) Then sb.AppendLine(String.Format("  Tên: {0}", name))

                    Dim driverVersion = GetProperty(obj, "DriverVersion")
                    If Not String.IsNullOrEmpty(driverVersion) Then sb.AppendLine(String.Format("  Driver Version: {0}", driverVersion))

                    Dim adapterRam = GetProperty(obj, "AdapterRAM")
                    If Not String.IsNullOrEmpty(adapterRam) Then
                        Dim ramGB = FormatGpuRam(adapterRam)
                        sb.AppendLine(String.Format("  VRAM: {0}", ramGB))
                    End If

                    Dim videoProcessor = GetProperty(obj, "VideoProcessor")
                    If Not String.IsNullOrEmpty(videoProcessor) Then sb.AppendLine(String.Format("  Video Processor: {0}", videoProcessor))

                    Dim currentRes = GetProperty(obj, "CurrentHorizontalResolution")
                    Dim currentResV = GetProperty(obj, "CurrentVerticalResolution")
                    If Not String.IsNullOrEmpty(currentRes) AndAlso Not String.IsNullOrEmpty(currentResV) Then
                        sb.AppendLine(String.Format("  Độ phân giải: {0} x {1}", currentRes, currentResV))
                    End If

                    Dim refreshRate = GetProperty(obj, "CurrentRefreshRate")
                    If Not String.IsNullOrEmpty(refreshRate) Then sb.AppendLine(String.Format("  Refresh Rate: {0} Hz", refreshRate))

                    Dim bitsPerPixel = GetProperty(obj, "CurrentBitsPerPixel")
                    If Not String.IsNullOrEmpty(bitsPerPixel) Then sb.AppendLine(String.Format("  Bit Depth: {0}-bit", bitsPerPixel))

                    Dim status = GetProperty(obj, "Status")
                    If Not String.IsNullOrEmpty(status) Then sb.AppendLine(String.Format("  Trạng thái: {0}", status))
                Next

                If gpuIndex = 0 Then sb.AppendLine("  Không tìm thấy GPU!")
            Catch ex As Exception
                sb.AppendLine(String.Format("  Lỗi: {0}", ex.Message))
            End Try
            Return sb.ToString().TrimEnd()
        End Function

        Private Shared Function FormatGpuRam(ramStr As String) As String
            Dim ramBytes As Long = 0
            If Long.TryParse(ramStr, ramBytes) Then
                Dim ramMB = ramBytes \ (1024 * 1024)
                If ramMB >= 1024 Then
                    Return String.Format("{0:0.0} GB", ramMB / 1024.0)
                Else
                    Return String.Format("{0} MB", ramMB)
                End If
            End If
            Return ramStr
        End Function

#End Region

#Region "CPU Info"

        Public Shared Function GetCpuInfo() As String
            Dim sb = New StringBuilder()
            Try
                Dim searcher = New ManagementObjectSearcher("SELECT * FROM Win32_Processor")

                For Each obj As ManagementObject In searcher.Get()
                    Dim name = GetProperty(obj, "Name")
                    If Not String.IsNullOrEmpty(name) Then sb.AppendLine(String.Format("  Tên: {0}", name.Trim()))

                    Dim manufacturer = GetProperty(obj, "Manufacturer")
                    If Not String.IsNullOrEmpty(manufacturer) Then sb.AppendLine(String.Format("  Hãng sản xuất: {0}", manufacturer))

                    Dim cores = GetProperty(obj, "NumberOfCores")
                    If Not String.IsNullOrEmpty(cores) Then sb.AppendLine(String.Format("  Số nhân: {0}", cores))

                    Dim logicalProc = GetProperty(obj, "NumberOfLogicalProcessors")
                    If Not String.IsNullOrEmpty(logicalProc) Then sb.AppendLine(String.Format("  Số luồng: {0}", logicalProc))

                    Dim maxClock = GetProperty(obj, "MaxClockSpeed")
                    If Not String.IsNullOrEmpty(maxClock) Then sb.AppendLine(String.Format("  Xung nhịp tối đa: {0} MHz", maxClock))

                    Dim curClock = GetProperty(obj, "CurrentClockSpeed")
                    If Not String.IsNullOrEmpty(curClock) Then sb.AppendLine(String.Format("  Xung nhịp hiện tại: {0} MHz", curClock))

                    Dim l2Cache = GetProperty(obj, "L2CacheSize")
                    If Not String.IsNullOrEmpty(l2Cache) Then sb.AppendLine(String.Format("  Cache L2: {0} KB", l2Cache))

                    Dim l3Cache = GetProperty(obj, "L3CacheSize")
                    If Not String.IsNullOrEmpty(l3Cache) Then sb.AppendLine(String.Format("  Cache L3: {0} KB", l3Cache))

                    Dim architecture = GetProperty(obj, "Architecture")
                    If Not String.IsNullOrEmpty(architecture) Then
                        Dim archName = GetArchitectureName(architecture)
                        sb.AppendLine(String.Format("  Kiến trúc: {0}", archName))
                    End If

                    Dim caption = GetProperty(obj, "Caption")
                    If Not String.IsNullOrEmpty(caption) AndAlso String.IsNullOrEmpty(name) Then
                        sb.AppendLine(String.Format("  Tên: {0}", caption))
                    End If
                Next
            Catch ex As Exception
                sb.AppendLine(String.Format("  Lỗi: {0}", ex.Message))
            End Try
            Return sb.ToString().TrimEnd()
        End Function

        Private Shared Function GetArchitectureName(arch As String) As String
            Select Case arch
                Case "0" : Return "x86"
                Case "1" : Return "MIPS"
                Case "2" : Return "Alpha"
                Case "3" : Return "PowerPC"
                Case "5" : Return "ARM"
                Case "6" : Return "IA-64"
                Case "9" : Return "x64"
                Case "12" : Return "ARM64"
                Case Else : Return arch
            End Select
        End Function

#End Region

#Region "RAM Info"

        Public Shared Function GetRamInfo() As String
            Dim sb = New StringBuilder()
            Try
                ' Tổng quan RAM hệ thống
                Dim totalSearcher = New ManagementObjectSearcher("SELECT * FROM Win32_ComputerSystem")
                For Each obj As ManagementObject In totalSearcher.Get()
                    Dim totalRam = GetProperty(obj, "TotalPhysicalMemory")
                    If Not String.IsNullOrEmpty(totalRam) Then
                        Dim totalGB = CDbl(totalRam) / (1024 ^ 3)
                        sb.AppendLine(String.Format("  Tổng RAM vật lý: {0:0.00} GB", totalGB))
                    End If

                    Dim freeRam = GetProperty(obj, "FreePhysicalMemory")
                    If Not String.IsNullOrEmpty(freeRam) Then
                        Dim freeGB = CDbl(freeRam) / (1024 ^ 2)
                        sb.AppendLine(String.Format("  RAM trống: {0:0.00} GB", freeGB))
                    End If
                Next

                sb.AppendLine()

                ' Chi tiết từng thanh RAM
                Dim searcher = New ManagementObjectSearcher("SELECT * FROM Win32_PhysicalMemory")
                Dim stickIndex As Integer = 0

                For Each obj As ManagementObject In searcher.Get()
                    stickIndex += 1
                    sb.AppendLine(String.Format("  --- Thanh RAM {0} ---", stickIndex))

                    Dim manufacturer = GetProperty(obj, "Manufacturer")
                    If Not String.IsNullOrEmpty(manufacturer) Then sb.AppendLine(String.Format("    Hãng: {0}", manufacturer))

                    Dim partNumber = GetProperty(obj, "PartNumber")
                    If Not String.IsNullOrEmpty(partNumber) Then sb.AppendLine(String.Format("    Mã: {0}", partNumber.Trim()))

                    Dim capacity = GetProperty(obj, "Capacity")
                    If Not String.IsNullOrEmpty(capacity) Then
                        Dim capGB = CDbl(capacity) / (1024 ^ 3)
                        sb.AppendLine(String.Format("    Dung lượng: {0:0.0} GB", capGB))
                    End If

                    Dim speed = GetProperty(obj, "Speed")
                    If Not String.IsNullOrEmpty(speed) Then sb.AppendLine(String.Format("    Tốc độ: {0} MHz", speed))

                    Dim configuredSpeed = GetProperty(obj, "ConfiguredClockSpeed")
                    If Not String.IsNullOrEmpty(configuredSpeed) Then sb.AppendLine(String.Format("    Xung nhịp: {0} MHz", configuredSpeed))

                    Dim memType = GetProperty(obj, "MemoryType")
                    If Not String.IsNullOrEmpty(memType) Then
                        Dim typeName = GetMemoryTypeName(memType)
                        sb.AppendLine(String.Format("    Loại: {0}", typeName))
                    End If

                    Dim formFactor = GetProperty(obj, "FormFactor")
                    If Not String.IsNullOrEmpty(formFactor) Then
                        Dim ffName = GetFormFactorName(formFactor)
                        sb.AppendLine(String.Format("    Kiểu: {0}", ffName))
                    End If

                    Dim dataWidth = GetProperty(obj, "DataWidth")
                    If Not String.IsNullOrEmpty(dataWidth) Then sb.AppendLine(String.Format("    Độ rộng bus: {0}-bit", dataWidth))

                    Dim voltage = GetProperty(obj, "ConfiguredVoltage")
                    If Not String.IsNullOrEmpty(voltage) Then sb.AppendLine(String.Format("    Điện áp: {0:0.0} mV", CDbl(voltage)))
                Next

                If stickIndex = 0 Then sb.AppendLine("  Không tìm thấy thông tin RAM!")
            Catch ex As Exception
                sb.AppendLine(String.Format("  Lỗi: {0}", ex.Message))
            End Try
            Return sb.ToString().TrimEnd()
        End Function

        Private Shared Function GetMemoryTypeName(memType As String) As String
            Select Case memType
                Case "0" : Return "Unknown"
                Case "20" : Return "DDR"
                Case "21" : Return "DDR2"
                Case "24" : Return "DDR3"
                Case "26" : Return "DDR4"
                Case "30" : Return "LPDDR4"
                Case "34" : Return "DDR5"
                Case "35" : Return "LPDDR5"
                Case Else : Return "DDR" & memType
            End Select
        End Function

        Private Shared Function GetFormFactorName(formFactor As String) As String
            Select Case formFactor
                Case "8" : Return "DIMM"
                Case "12" : Return "SODIMM"
                Case "13" : Return "SRDIMM"
                Case Else : Return formFactor
            End Select
        End Function

#End Region

#Region "Mainboard Info"

        Public Shared Function GetMainboardInfo() As String
            Dim sb = New StringBuilder()
            Try
                Dim searcher = New ManagementObjectSearcher("SELECT * FROM Win32_BaseBoard")

                For Each obj As ManagementObject In searcher.Get()
                    Dim manufacturer = GetProperty(obj, "Manufacturer")
                    If Not String.IsNullOrEmpty(manufacturer) Then sb.AppendLine(String.Format("  Hãng sản xuất: {0}", manufacturer))

                    Dim product = GetProperty(obj, "Product")
                    If Not String.IsNullOrEmpty(product) Then sb.AppendLine(String.Format("  Model: {0}", product))

                    Dim serial = GetProperty(obj, "SerialNumber")
                    If Not String.IsNullOrEmpty(serial) Then sb.AppendLine(String.Format("  Serial: {0}", serial.Trim()))

                    Dim version = GetProperty(obj, "Version")
                    If Not String.IsNullOrEmpty(version) Then sb.AppendLine(String.Format("  Version: {0}", version))

                    Dim status = GetProperty(obj, "Status")
                    If Not String.IsNullOrEmpty(status) Then sb.AppendLine(String.Format("  Trạng thái: {0}", status))
                Next

                ' Thêm thông tin BIOS
                sb.AppendLine()
                Dim biosSearcher = New ManagementObjectSearcher("SELECT * FROM Win32_BIOS")
                For Each obj As ManagementObject In biosSearcher.Get()
                    Dim biosManufacturer = GetProperty(obj, "Manufacturer")
                    If Not String.IsNullOrEmpty(biosManufacturer) Then sb.AppendLine(String.Format("  BIOS: {0}", biosManufacturer))

                    Dim biosVersion = GetProperty(obj, "SMBIOSBIOSVersion")
                    If Not String.IsNullOrEmpty(biosVersion) Then sb.AppendLine(String.Format("  BIOS Version: {0}", biosVersion))

                    Dim releaseDate = GetProperty(obj, "ReleaseDate")
                    If Not String.IsNullOrEmpty(releaseDate) Then
                        Dim formattedDate = FormatWmiDate(releaseDate)
                        sb.AppendLine(String.Format("  Ngày phát hành: {0}", formattedDate))
                    End If
                Next

                ' Thêm thông tin hệ thống
                sb.AppendLine()
                Dim sysSearcher = New ManagementObjectSearcher("SELECT * FROM Win32_ComputerSystem")
                For Each obj As ManagementObject In sysSearcher.Get()
                    Dim sysManufacturer = GetProperty(obj, "Manufacturer")
                    If Not String.IsNullOrEmpty(sysManufacturer) Then sb.AppendLine(String.Format("  System: {0}", sysManufacturer))

                    Dim model = GetProperty(obj, "Model")
                    If Not String.IsNullOrEmpty(model) Then sb.AppendLine(String.Format("  Model hệ thống: {0}", model))
                Next
            Catch ex As Exception
                sb.AppendLine(String.Format("  Lỗi: {0}", ex.Message))
            End Try
            Return sb.ToString().TrimEnd()
        End Function

        Private Shared Function FormatWmiDate(dateStr As String) As String
            If dateStr.Length >= 8 Then
                Dim year = dateStr.Substring(0, 4)
                Dim month = dateStr.Substring(4, 2)
                Dim day = dateStr.Substring(6, 2)
                Return String.Format("{0}/{1}/{2}", day, month, year)
            End If
            Return dateStr
        End Function

#End Region

#Region "Helper"

        Private Shared Function GetProperty(obj As ManagementObject, propertyName As String) As String
            Dim value = obj(propertyName)
            If value IsNot Nothing Then
                Return value.ToString().Trim()
            End If
            Return String.Empty
        End Function

#End Region

    End Class
End Namespace
