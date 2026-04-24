using System;
using System.Management;
using System.Text;

namespace Subtitle_draft_GMTPC.Services
{
    /// <summary>
    /// Service thu thap thong tin phan cung he thong
    /// </summary>
    public class HardwareInfoService
    {
        #region GPU Info

        public static string GetGpuInfo()
        {
            var sb = new StringBuilder();
            try
            {
                var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_VideoController");
                int gpuIndex = 0;

                foreach (ManagementObject obj in searcher.Get())
                {
                    gpuIndex++;
                    if (gpuIndex > 1) sb.AppendLine();
                    sb.AppendLine($"=== GPU {gpuIndex} ===");

                    var name = GetProperty(obj, "Name");
                    if (!string.IsNullOrEmpty(name)) sb.AppendLine($"  Ten: {name}");

                    var driverVersion = GetProperty(obj, "DriverVersion");
                    if (!string.IsNullOrEmpty(driverVersion)) sb.AppendLine($"  Driver Version: {driverVersion}");

                    var adapterRam = GetProperty(obj, "AdapterRAM");
                    if (!string.IsNullOrEmpty(adapterRam))
                    {
                        var ramGB = FormatGpuRam(adapterRam);
                        sb.AppendLine($"  VRAM: {ramGB}");
                    }

                    var videoProcessor = GetProperty(obj, "VideoProcessor");
                    if (!string.IsNullOrEmpty(videoProcessor)) sb.AppendLine($"  Video Processor: {videoProcessor}");

                    var currentRes = GetProperty(obj, "CurrentHorizontalResolution");
                    var currentResV = GetProperty(obj, "CurrentVerticalResolution");
                    if (!string.IsNullOrEmpty(currentRes) && !string.IsNullOrEmpty(currentResV))
                    {
                        sb.AppendLine($"  Do phan giai: {currentRes} x {currentResV}");
                    }

                    var refreshRate = GetProperty(obj, "CurrentRefreshRate");
                    if (!string.IsNullOrEmpty(refreshRate)) sb.AppendLine($"  Refresh Rate: {refreshRate} Hz");

                    var bitsPerPixel = GetProperty(obj, "CurrentBitsPerPixel");
                    if (!string.IsNullOrEmpty(bitsPerPixel)) sb.AppendLine($"  Bit Depth: {bitsPerPixel}-bit");

                    var status = GetProperty(obj, "Status");
                    if (!string.IsNullOrEmpty(status)) sb.AppendLine($"  Trang thai: {status}");

                    var encoderInfo = GetEncoderInfo(name);
                    if (!string.IsNullOrEmpty(encoderInfo))
                    {
                        sb.AppendLine($"  Hardware Encoder: {encoderInfo}");
                    }
                }

                if (gpuIndex == 0) sb.AppendLine("  Khong tim thay GPU!");
            }
            catch (Exception ex)
            {
                sb.AppendLine($"  Loi: {ex.Message}");
            }
            return sb.ToString().TrimEnd();
        }

        private static string FormatGpuRam(string ramStr)
        {
            if (long.TryParse(ramStr, out long ramBytes))
            {
                var ramMB = ramBytes / (1024 * 1024);
                if (ramMB >= 1024)
                {
                    return $"{ramMB / 1024.0:0.0} GB";
                }

                return $"{ramMB} MB";
            }

            return ramStr;
        }

        private static string GetEncoderInfo(string gpuName)
        {
            if (string.IsNullOrWhiteSpace(gpuName)) return string.Empty;

            var nameLower = gpuName.ToLower();

            if (nameLower.Contains("nvidia") || nameLower.Contains("geforce") || nameLower.Contains("quadro") || nameLower.Contains("rtx") || nameLower.Contains("gtx") || nameLower.Contains("tesla"))
            {
                if (nameLower.Contains("rtx 50") || nameLower.Contains("5090") || nameLower.Contains("5080") || nameLower.Contains("5070"))
                {
                    return "NVENC (8th Gen - AV1 + HEVC)";
                }
                if (nameLower.Contains("rtx 40") || nameLower.Contains("4090") || nameLower.Contains("4080") || nameLower.Contains("4070") || nameLower.Contains("4060"))
                {
                    return "NVENC (8th Gen - AV1 + HEVC)";
                }
                if (nameLower.Contains("rtx 30") || nameLower.Contains("3090") || nameLower.Contains("3080") || nameLower.Contains("3070") || nameLower.Contains("3060"))
                {
                    return "NVENC (7th Gen - HEVC)";
                }
                if (nameLower.Contains("rtx 20") || nameLower.Contains("2080") || nameLower.Contains("2070") || nameLower.Contains("2060"))
                {
                    return "NVENC (Turing - HEVC)";
                }
                if (nameLower.Contains("gtx 16") || nameLower.Contains("1660") || nameLower.Contains("1650"))
                {
                    return "NVENC (Turing - HEVC)";
                }
                if (nameLower.Contains("gtx 10") || nameLower.Contains("1080") || nameLower.Contains("1070") || nameLower.Contains("1060") || nameLower.Contains("1050"))
                {
                    return "NVENC (Pascal - HEVC 8-bit)";
                }
                if (nameLower.Contains("gtx 9") || nameLower.Contains("980") || nameLower.Contains("970") || nameLower.Contains("960"))
                {
                    return "NVENC (Maxwell)";
                }
                if (nameLower.Contains("gtx 7") || nameLower.Contains("780") || nameLower.Contains("770") || nameLower.Contains("760"))
                {
                    return "NVENC (Kepler)";
                }
                return "NVENC (NVIDIA)";
            }

            if (nameLower.Contains("amd") || nameLower.Contains("radeon") || nameLower.Contains("rx ") || nameLower.Contains("rx vega") || nameLower.Contains("wrx") || nameLower.Contains("firepro"))
            {
                if (nameLower.Contains("rx 9000") || nameLower.Contains("9070") || nameLower.Contains("9060"))
                {
                    return "AMF (RDNA 4 - AV1 + HEVC + H.264)";
                }
                if (nameLower.Contains("rx 7") || nameLower.Contains("7900") || nameLower.Contains("7800") || nameLower.Contains("7700") || nameLower.Contains("7600"))
                {
                    return "AMF (RDNA 3 - AV1 + HEVC)";
                }
                if (nameLower.Contains("rx 6") || nameLower.Contains("6900") || nameLower.Contains("6800") || nameLower.Contains("6700") || nameLower.Contains("6600") || nameLower.Contains("6500"))
                {
                    return "AMF (RDNA 2 - HEVC)";
                }
                if (nameLower.Contains("rx 5") || nameLower.Contains("5700") || nameLower.Contains("5600") || nameLower.Contains("5500"))
                {
                    return "AMF (RDNA - HEVC)";
                }
                if (nameLower.Contains("rx vega") || nameLower.Contains("vega "))
                {
                    return "AMF (Vega - HEVC)";
                }
                if (nameLower.Contains("rx 4") || nameLower.Contains("480") || nameLower.Contains("470") || nameLower.Contains("460"))
                {
                    return "AMF (Polaris - HEVC)";
                }
                return "AMF (AMD)";
            }

            if (nameLower.Contains("intel") || nameLower.Contains("uhd") || nameLower.Contains("hd graphics") || nameLower.Contains("iris") || nameLower.Contains("arc ") || nameLower.Contains("battlemage"))
            {
                if (nameLower.Contains("battlemage") || nameLower.Contains("arc b"))
                {
                    return "Intel Quick Sync + Xe Media (AV1 + HEVC + H.264)";
                }
                if (nameLower.Contains("arc a"))
                {
                    return "Intel Quick Sync + Xe Media (AV1 + HEVC)";
                }
                if (nameLower.Contains("ultra") || nameLower.Contains("meteor lake") || nameLower.Contains("lunar lake") || nameLower.Contains("arrow lake"))
                {
                    return "Intel Quick Sync (Xe-LPG - AV1 + HEVC)";
                }
                if (nameLower.Contains("13th") || nameLower.Contains("14th") || nameLower.Contains("raptor lake"))
                {
                    return "Intel Quick Sync (Gen12 - AV1 + HEVC)";
                }
                if (nameLower.Contains("11th") || nameLower.Contains("12th"))
                {
                    return "Intel Quick Sync (Gen12 - HEVC)";
                }
                if (nameLower.Contains("10th") || nameLower.Contains("9th"))
                {
                    return "Intel Quick Sync (Gen9.5 - HEVC)";
                }
                if (nameLower.Contains("8th") || nameLower.Contains("7th"))
                {
                    return "Intel Quick Sync (Gen9 - HEVC)";
                }
                if (nameLower.Contains("uhd") || nameLower.Contains("iris xe"))
                {
                    return "Intel Quick Sync (Gen11/12 - HEVC)";
                }
                if (nameLower.Contains("hd graphics"))
                {
                    return "Intel Quick Sync (Gen8/9 - HEVC)";
                }
                return "Intel Quick Sync (QSV)";
            }

            return string.Empty;
        }

        #endregion

        #region CPU Info

        public static string GetCpuInfo()
        {
            var sb = new StringBuilder();
            try
            {
                var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_Processor");
                int cpuIndex = 0;

                foreach (ManagementObject obj in searcher.Get())
                {
                    cpuIndex++;
                    if (cpuIndex > 1) sb.AppendLine();

                    var name = GetProperty(obj, "Name");
                    if (!string.IsNullOrEmpty(name)) sb.AppendLine($"  Ten: {name.Trim()}");

                    var manufacturer = GetProperty(obj, "Manufacturer");
                    if (!string.IsNullOrEmpty(manufacturer)) sb.AppendLine($"  Hang san xuat: {manufacturer}");

                    var cores = GetProperty(obj, "NumberOfCores");
                    if (!string.IsNullOrEmpty(cores)) sb.AppendLine($"  So nhan: {cores}");

                    var logicalProc = GetProperty(obj, "NumberOfLogicalProcessors");
                    if (!string.IsNullOrEmpty(logicalProc)) sb.AppendLine($"  So luong: {logicalProc}");

                    var maxClock = GetProperty(obj, "MaxClockSpeed");
                    if (!string.IsNullOrEmpty(maxClock)) sb.AppendLine($"  Xung nhip toi da: {maxClock} MHz");

                    var curClock = GetProperty(obj, "CurrentClockSpeed");
                    if (!string.IsNullOrEmpty(curClock)) sb.AppendLine($"  Xung nhip hien tai: {curClock} MHz");

                    var l2Cache = GetProperty(obj, "L2CacheSize");
                    if (!string.IsNullOrEmpty(l2Cache)) sb.AppendLine($"  Cache L2: {l2Cache} KB");

                    var l3Cache = GetProperty(obj, "L3CacheSize");
                    if (!string.IsNullOrEmpty(l3Cache)) sb.AppendLine($"  Cache L3: {l3Cache} KB");

                    var architecture = GetProperty(obj, "Architecture");
                    if (!string.IsNullOrEmpty(architecture))
                    {
                        var archName = GetArchitectureName(architecture);
                        sb.AppendLine($"  Kien truc: {archName}");
                    }

                    var caption = GetProperty(obj, "Caption");
                    if (!string.IsNullOrEmpty(caption) && string.IsNullOrEmpty(name))
                    {
                        sb.AppendLine($"  Ten: {caption}");
                    }
                }
            }
            catch (Exception ex)
            {
                sb.AppendLine($"  Loi: {ex.Message}");
            }
            return sb.ToString().TrimEnd();
        }

        private static string GetArchitectureName(string arch)
        {
            switch (arch)
            {
                case "0": return "x86";
                case "1": return "MIPS";
                case "2": return "Alpha";
                case "3": return "PowerPC";
                case "5": return "ARM";
                case "6": return "IA-64";
                case "9": return "x64";
                case "12": return "ARM64";
                default: return arch;
            }
        }

        #endregion

        #region RAM Info

        public static string GetRamInfo()
        {
            var sb = new StringBuilder();
            try
            {
                var totalSearcher = new ManagementObjectSearcher("SELECT * FROM Win32_ComputerSystem");
                foreach (ManagementObject obj in totalSearcher.Get())
                {
                    var totalRam = GetProperty(obj, "TotalPhysicalMemory");
                    if (!string.IsNullOrEmpty(totalRam))
                    {
                        var totalGB = double.Parse(totalRam) / Math.Pow(1024, 3);
                        sb.AppendLine($"  Tong RAM vat ly: {totalGB:0.00} GB");
                    }
                }

                var freeRamSearcher = new ManagementObjectSearcher("SELECT * FROM Win32_OperatingSystem");
                foreach (ManagementObject obj in freeRamSearcher.Get())
                {
                    var freeRam = GetProperty(obj, "FreePhysicalMemory");
                    if (!string.IsNullOrEmpty(freeRam))
                    {
                        var freeGB = double.Parse(freeRam) / Math.Pow(1024, 2);
                        sb.AppendLine($"  RAM trong: {freeGB:0.00} GB");
                    }
                }

                sb.AppendLine();
                sb.AppendLine("  Chi tiet cac slot RAM:");
                sb.AppendLine();

                var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_PhysicalMemory");
                int stickIndex = 0;

                foreach (ManagementObject obj in searcher.Get())
                {
                    stickIndex++;
                    if (stickIndex > 1) sb.AppendLine();

                    sb.AppendLine($"  --- Thanh RAM {stickIndex} ---");

                    var manufacturer = GetProperty(obj, "Manufacturer");
                    if (!string.IsNullOrEmpty(manufacturer)) sb.AppendLine($"    Hang: {manufacturer}");

                    var partNumber = GetProperty(obj, "PartNumber");
                    if (!string.IsNullOrEmpty(partNumber)) sb.AppendLine($"    Ma: {partNumber.Trim()}");

                    var capacity = GetProperty(obj, "Capacity");
                    if (!string.IsNullOrEmpty(capacity))
                    {
                        var capGB = double.Parse(capacity) / Math.Pow(1024, 3);
                        sb.AppendLine($"    Dung luong: {capGB:0.0} GB");
                    }

                    var speed = GetProperty(obj, "Speed");
                    if (!string.IsNullOrEmpty(speed)) sb.AppendLine($"    Toc do: {speed} MHz");

                    var configuredSpeed = GetProperty(obj, "ConfiguredClockSpeed");
                    if (!string.IsNullOrEmpty(configuredSpeed)) sb.AppendLine($"    Xung nhip: {configuredSpeed} MHz");

                    var memType = GetProperty(obj, "MemoryType");
                    if (!string.IsNullOrEmpty(memType))
                    {
                        var typeName = GetMemoryTypeName(memType);
                        sb.AppendLine($"    Loai: {typeName}");
                    }

                    var formFactor = GetProperty(obj, "FormFactor");
                    if (!string.IsNullOrEmpty(formFactor))
                    {
                        var ffName = GetFormFactorName(formFactor);
                        sb.AppendLine($"    Kieu: {ffName}");
                    }

                    var dataWidth = GetProperty(obj, "DataWidth");
                    if (!string.IsNullOrEmpty(dataWidth)) sb.AppendLine($"    Do rong bus: {dataWidth}-bit");

                    var voltage = GetProperty(obj, "ConfiguredVoltage");
                    if (!string.IsNullOrEmpty(voltage)) sb.AppendLine($"    Dien ap: {double.Parse(voltage):0.0} mV");
                }

                if (stickIndex == 0)
                {
                    sb.AppendLine("  Khong tim thay thong tin RAM!");
                }
            }
            catch (Exception ex)
            {
                sb.AppendLine($"  Loi: {ex.Message}");
            }
            return sb.ToString().TrimEnd();
        }

        private static string GetMemoryTypeName(string memType)
        {
            switch (memType)
            {
                case "0": return "Unknown";
                case "20": return "DDR";
                case "21": return "DDR2";
                case "24": return "DDR3";
                case "26": return "DDR4";
                case "30": return "LPDDR4";
                case "34": return "DDR5";
                case "35": return "LPDDR5";
                default: return "DDR" + memType;
            }
        }

        private static string GetFormFactorName(string formFactor)
        {
            switch (formFactor)
            {
                case "8": return "DIMM";
                case "12": return "SODIMM";
                case "13": return "SRDIMM";
                default: return formFactor;
            }
        }

        #endregion

        #region Mainboard Info

        public static string GetMainboardInfo()
        {
            var sb = new StringBuilder();
            try
            {
                var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_BaseBoard");

                foreach (ManagementObject obj in searcher.Get())
                {
                    var manufacturer = GetProperty(obj, "Manufacturer");
                    if (!string.IsNullOrEmpty(manufacturer)) sb.AppendLine($"  Hang san xuat: {manufacturer}");

                    var product = GetProperty(obj, "Product");
                    if (!string.IsNullOrEmpty(product)) sb.AppendLine($"  Model: {product}");

                    var serial = GetProperty(obj, "SerialNumber");
                    if (!string.IsNullOrEmpty(serial)) sb.AppendLine($"  Serial: {serial.Trim()}");

                    var version = GetProperty(obj, "Version");
                    if (!string.IsNullOrEmpty(version)) sb.AppendLine($"  Version: {version}");

                    var status = GetProperty(obj, "Status");
                    if (!string.IsNullOrEmpty(status)) sb.AppendLine($"  Trang thai: {status}");
                }

                sb.AppendLine();
                var biosSearcher = new ManagementObjectSearcher("SELECT * FROM Win32_BIOS");
                foreach (ManagementObject obj in biosSearcher.Get())
                {
                    var biosManufacturer = GetProperty(obj, "Manufacturer");
                    if (!string.IsNullOrEmpty(biosManufacturer)) sb.AppendLine($"  BIOS: {biosManufacturer}");

                    var biosVersion = GetProperty(obj, "SMBIOSBIOSVersion");
                    if (!string.IsNullOrEmpty(biosVersion)) sb.AppendLine($"  BIOS Version: {biosVersion}");

                    var releaseDate = GetProperty(obj, "ReleaseDate");
                    if (!string.IsNullOrEmpty(releaseDate))
                    {
                        var formattedDate = FormatWmiDate(releaseDate);
                        sb.AppendLine($"  Ngay phat hanh: {formattedDate}");
                    }
                }

                sb.AppendLine();
                var sysSearcher = new ManagementObjectSearcher("SELECT * FROM Win32_ComputerSystem");
                foreach (ManagementObject obj in sysSearcher.Get())
                {
                    var sysManufacturer = GetProperty(obj, "Manufacturer");
                    if (!string.IsNullOrEmpty(sysManufacturer)) sb.AppendLine($"  System: {sysManufacturer}");

                    var model = GetProperty(obj, "Model");
                    if (!string.IsNullOrEmpty(model)) sb.AppendLine($"  Model he thong: {model}");
                }
            }
            catch (Exception ex)
            {
                sb.AppendLine($"  Loi: {ex.Message}");
            }
            return sb.ToString().TrimEnd();
        }

        private static string FormatWmiDate(string dateStr)
        {
            if (dateStr.Length >= 8)
            {
                var year = dateStr.Substring(0, 4);
                var month = dateStr.Substring(4, 2);
                var day = dateStr.Substring(6, 2);
                return $"{day}/{month}/{year}";
            }
            return dateStr;
        }

        #endregion

        #region Helper

        private static string GetProperty(ManagementObject obj, string propertyName)
        {
            try
            {
                var value = obj[propertyName];
                if (value != null)
                {
                    return value.ToString().Trim();
                }
            }
            catch
            {
            }

            return string.Empty;
        }

        #endregion
    }
}
