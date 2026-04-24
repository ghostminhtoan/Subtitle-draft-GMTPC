using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Management;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

namespace Subtitle_draft_GMTPC.Services
{
    /// <summary>
    /// Service thu thập thông tin phần cứng hệ thống.
    /// </summary>
    public class HardwareInfoService
    {
        private static readonly Dictionary<string, DxDiagDisplayInfo> _dxDiagDisplayCache =
            new Dictionary<string, DxDiagDisplayInfo>(StringComparer.OrdinalIgnoreCase);

        private static readonly Dictionary<string, CpuClockInfo> _cpuClockCache =
            new Dictionary<string, CpuClockInfo>(StringComparer.OrdinalIgnoreCase);

        #region GPU Info

        public static string GetGpuInfo()
        {
            var sb = new StringBuilder();
            try
            {
                var dxDiagMap = GetDxDiagDisplayInfoMap();
                var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_VideoController");
                int gpuIndex = 0;

                foreach (ManagementObject obj in searcher.Get())
                {
                    gpuIndex++;
                    if (gpuIndex > 1) sb.AppendLine();
                    sb.AppendLine($"=== GPU {gpuIndex} ===");

                    var name = GetProperty(obj, "Name");
                    if (!string.IsNullOrEmpty(name)) sb.AppendLine($"  Tên: {name}");

                    var driverVersion = GetProperty(obj, "DriverVersion");
                    if (!string.IsNullOrEmpty(driverVersion)) sb.AppendLine($"  Phiên bản driver: {driverVersion}");

                    var vramText = GetGpuVramText(obj, dxDiagMap);
                    if (!string.IsNullOrEmpty(vramText)) sb.AppendLine($"  VRAM: {vramText}");

                    var videoProcessor = GetProperty(obj, "VideoProcessor");
                    if (!string.IsNullOrEmpty(videoProcessor)) sb.AppendLine($"  Bộ xử lý đồ họa: {videoProcessor}");

                    var currentRes = GetProperty(obj, "CurrentHorizontalResolution");
                    var currentResV = GetProperty(obj, "CurrentVerticalResolution");
                    if (!string.IsNullOrEmpty(currentRes) && !string.IsNullOrEmpty(currentResV))
                    {
                        sb.AppendLine($"  Độ phân giải: {currentRes} x {currentResV}");
                    }

                    var refreshRate = GetProperty(obj, "CurrentRefreshRate");
                    if (!string.IsNullOrEmpty(refreshRate)) sb.AppendLine($"  Tần số quét: {refreshRate} Hz");

                    var bitsPerPixel = GetProperty(obj, "CurrentBitsPerPixel");
                    if (!string.IsNullOrEmpty(bitsPerPixel)) sb.AppendLine($"  Độ sâu màu: {bitsPerPixel}-bit");

                    var status = GetProperty(obj, "Status");
                    if (!string.IsNullOrEmpty(status)) sb.AppendLine($"  Trạng thái: {status}");

                    var encoderInfo = GetEncoderInfo(name);
                    if (!string.IsNullOrEmpty(encoderInfo))
                    {
                        sb.AppendLine($"  Hardware Encoder: {encoderInfo}");
                    }
                }

                if (gpuIndex == 0) sb.AppendLine("  Không tìm thấy GPU.");
            }
            catch (Exception ex)
            {
                sb.AppendLine($"  Lỗi: {ex.Message}");
            }

            return sb.ToString().TrimEnd();
        }

        private static string GetGpuVramText(ManagementObject obj, Dictionary<string, DxDiagDisplayInfo> dxDiagMap)
        {
            var name = GetProperty(obj, "Name");
            if (!string.IsNullOrWhiteSpace(name) && dxDiagMap.TryGetValue(name.Trim(), out DxDiagDisplayInfo info))
            {
                if (info.DedicatedMemoryMb > 0)
                {
                    return FormatMemoryFromMb(info.DedicatedMemoryMb);
                }

                if (info.DisplayMemoryMb > 0)
                {
                    return FormatMemoryFromMb(info.DisplayMemoryMb);
                }
            }

            var adapterRam = GetProperty(obj, "AdapterRAM");
            if (!string.IsNullOrEmpty(adapterRam))
            {
                return FormatGpuRam(adapterRam);
            }

            return string.Empty;
        }

        private static Dictionary<string, DxDiagDisplayInfo> GetDxDiagDisplayInfoMap()
        {
            if (_dxDiagDisplayCache.Count > 0)
            {
                return _dxDiagDisplayCache;
            }

            var result = new Dictionary<string, DxDiagDisplayInfo>(StringComparer.OrdinalIgnoreCase);
            string tempFile = Path.Combine(Path.GetTempPath(), "subtitle-draft-gmtpc-dxdiag.xml");

            try
            {
                var process = new System.Diagnostics.Process();
                process.StartInfo.FileName = "dxdiag";
                process.StartInfo.Arguments = "/x \"" + tempFile + "\"";
                process.StartInfo.CreateNoWindow = true;
                process.StartInfo.UseShellExecute = false;
                process.Start();
                process.WaitForExit(10000);

                if (!File.Exists(tempFile))
                {
                    return result;
                }

                var xml = new XmlDocument();
                xml.Load(tempFile);
                var nodes = xml.SelectNodes("//DisplayDevice");
                if (nodes == null)
                {
                    return result;
                }

                foreach (XmlNode node in nodes)
                {
                    if (node == null) continue;

                    string cardName = GetXmlNodeText(node, "CardName");
                    if (string.IsNullOrWhiteSpace(cardName)) continue;

                    var info = new DxDiagDisplayInfo
                    {
                        DedicatedMemoryMb = ParseDxDiagMemoryMb(GetXmlNodeText(node, "DedicatedMemory")),
                        DisplayMemoryMb = ParseDxDiagMemoryMb(GetXmlNodeText(node, "DisplayMemory"))
                    };

                    result[cardName.Trim()] = info;
                }
            }
            catch
            {
                return result;
            }
            finally
            {
                try
                {
                    if (File.Exists(tempFile))
                    {
                        File.Delete(tempFile);
                    }
                }
                catch
                {
                }
            }

            _dxDiagDisplayCache.Clear();
            foreach (var item in result)
            {
                _dxDiagDisplayCache[item.Key] = item.Value;
            }

            return _dxDiagDisplayCache;
        }

        private static string GetXmlNodeText(XmlNode parent, string childName)
        {
            var child = parent.SelectSingleNode(childName);
            return child == null ? string.Empty : child.InnerText.Trim();
        }

        private static int ParseDxDiagMemoryMb(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return 0;
            }

            var match = Regex.Match(text, @"\d+");
            if (!match.Success)
            {
                return 0;
            }

            int memoryMb;
            return int.TryParse(match.Value, out memoryMb) ? memoryMb : 0;
        }

        private static string FormatMemoryFromMb(int memoryMb)
        {
            if (memoryMb <= 0)
            {
                return string.Empty;
            }

            if (memoryMb >= 1024)
            {
                var memoryGb = memoryMb / 1024.0;
                var roundedGb = Math.Round(memoryGb);
                if (Math.Abs(memoryGb - roundedGb) <= 0.35)
                {
                    return roundedGb.ToString("0", CultureInfo.InvariantCulture) + " GB";
                }

                return memoryGb.ToString("0.#", CultureInfo.InvariantCulture) + " GB";
            }

            return memoryMb.ToString(CultureInfo.InvariantCulture) + " MB";
        }

        private static string FormatGpuRam(string ramStr)
        {
            long ramBytes;
            if (long.TryParse(ramStr, out ramBytes))
            {
                var ramMB = ramBytes / (1024 * 1024);
                if (ramMB >= 1024)
                {
                    return (ramMB / 1024.0).ToString("0.#", CultureInfo.InvariantCulture) + " GB";
                }

                return ramMB.ToString(CultureInfo.InvariantCulture) + " MB";
            }

            return ramStr;
        }

        private static string GetEncoderInfo(string gpuName)
        {
            if (string.IsNullOrWhiteSpace(gpuName)) return string.Empty;

            var nameLower = gpuName.ToLowerInvariant();

            if (nameLower.Contains("nvidia") || nameLower.Contains("geforce") || nameLower.Contains("quadro") || nameLower.Contains("rtx") || nameLower.Contains("gtx") || nameLower.Contains("tesla"))
            {
                if (nameLower.Contains("rtx 50") || nameLower.Contains("5090") || nameLower.Contains("5080") || nameLower.Contains("5070"))
                    return "NVENC (8th Gen - AV1 + HEVC)";
                if (nameLower.Contains("rtx 40") || nameLower.Contains("4090") || nameLower.Contains("4080") || nameLower.Contains("4070") || nameLower.Contains("4060"))
                    return "NVENC (8th Gen - AV1 + HEVC)";
                if (nameLower.Contains("rtx 30") || nameLower.Contains("3090") || nameLower.Contains("3080") || nameLower.Contains("3070") || nameLower.Contains("3060"))
                    return "NVENC (7th Gen - HEVC)";
                if (nameLower.Contains("rtx 20") || nameLower.Contains("2080") || nameLower.Contains("2070") || nameLower.Contains("2060"))
                    return "NVENC (Turing - HEVC)";
                if (nameLower.Contains("gtx 16") || nameLower.Contains("1660") || nameLower.Contains("1650"))
                    return "NVENC (Turing - HEVC)";
                if (nameLower.Contains("gtx 10") || nameLower.Contains("1080") || nameLower.Contains("1070") || nameLower.Contains("1060") || nameLower.Contains("1050"))
                    return "NVENC (Pascal - HEVC 8-bit)";
                if (nameLower.Contains("gtx 9") || nameLower.Contains("980") || nameLower.Contains("970") || nameLower.Contains("960"))
                    return "NVENC (Maxwell)";
                if (nameLower.Contains("gtx 7") || nameLower.Contains("780") || nameLower.Contains("770") || nameLower.Contains("760"))
                    return "NVENC (Kepler)";
                return "NVENC (NVIDIA)";
            }

            if (nameLower.Contains("amd") || nameLower.Contains("radeon") || nameLower.Contains("rx ") || nameLower.Contains("rx vega") || nameLower.Contains("wrx") || nameLower.Contains("firepro"))
            {
                if (nameLower.Contains("rx 9000") || nameLower.Contains("9070") || nameLower.Contains("9060"))
                    return "AMF (RDNA 4 - AV1 + HEVC + H.264)";
                if (nameLower.Contains("rx 7") || nameLower.Contains("7900") || nameLower.Contains("7800") || nameLower.Contains("7700") || nameLower.Contains("7600"))
                    return "AMF (RDNA 3 - AV1 + HEVC)";
                if (nameLower.Contains("rx 6") || nameLower.Contains("6900") || nameLower.Contains("6800") || nameLower.Contains("6700") || nameLower.Contains("6600") || nameLower.Contains("6500"))
                    return "AMF (RDNA 2 - HEVC)";
                if (nameLower.Contains("rx 5") || nameLower.Contains("5700") || nameLower.Contains("5600") || nameLower.Contains("5500"))
                    return "AMF (RDNA - HEVC)";
                if (nameLower.Contains("rx vega") || nameLower.Contains("vega "))
                    return "AMF (Vega - HEVC)";
                if (nameLower.Contains("rx 4") || nameLower.Contains("480") || nameLower.Contains("470") || nameLower.Contains("460"))
                    return "AMF (Polaris - HEVC)";
                return "AMF (AMD)";
            }

            if (nameLower.Contains("intel") || nameLower.Contains("uhd") || nameLower.Contains("hd graphics") || nameLower.Contains("iris") || nameLower.Contains("arc ") || nameLower.Contains("battlemage"))
            {
                if (nameLower.Contains("battlemage") || nameLower.Contains("arc b"))
                    return "Intel Quick Sync + Xe Media (AV1 + HEVC + H.264)";
                if (nameLower.Contains("arc a"))
                    return "Intel Quick Sync + Xe Media (AV1 + HEVC)";
                if (nameLower.Contains("ultra") || nameLower.Contains("meteor lake") || nameLower.Contains("lunar lake") || nameLower.Contains("arrow lake"))
                    return "Intel Quick Sync (Xe-LPG - AV1 + HEVC)";
                if (nameLower.Contains("13th") || nameLower.Contains("14th") || nameLower.Contains("raptor lake"))
                    return "Intel Quick Sync (Gen12 - AV1 + HEVC)";
                if (nameLower.Contains("11th") || nameLower.Contains("12th"))
                    return "Intel Quick Sync (Gen12 - HEVC)";
                if (nameLower.Contains("10th") || nameLower.Contains("9th"))
                    return "Intel Quick Sync (Gen9.5 - HEVC)";
                if (nameLower.Contains("8th") || nameLower.Contains("7th"))
                    return "Intel Quick Sync (Gen9 - HEVC)";
                if (nameLower.Contains("uhd") || nameLower.Contains("iris xe"))
                    return "Intel Quick Sync (Gen11/12 - HEVC)";
                if (nameLower.Contains("hd graphics"))
                    return "Intel Quick Sync (Gen8/9 - HEVC)";
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

                    var rawName = GetProperty(obj, "Name");
                    var displayName = NormalizeCpuDisplayName(rawName);
                    if (!string.IsNullOrEmpty(displayName))
                    {
                        sb.AppendLine($"  Tên: {displayName}");
                    }

                    var manufacturer = GetProperty(obj, "Manufacturer");
                    if (!string.IsNullOrEmpty(manufacturer)) sb.AppendLine($"  Hãng sản xuất: {manufacturer}");

                    var socketDesignation = GetProperty(obj, "SocketDesignation");
                    if (!string.IsNullOrEmpty(socketDesignation)) sb.AppendLine($"  Socket: {socketDesignation}");

                    var cores = GetProperty(obj, "NumberOfCores");
                    if (!string.IsNullOrEmpty(cores)) sb.AppendLine($"  Số nhân: {cores}");

                    var logicalProc = GetProperty(obj, "NumberOfLogicalProcessors");
                    if (!string.IsNullOrEmpty(logicalProc)) sb.AppendLine($"  Số luồng: {logicalProc}");

                    var clockInfo = GetCpuClockInfo(rawName);
                    if (!string.IsNullOrEmpty(clockInfo.BaseClock))
                    {
                        sb.AppendLine($"  Xung nhịp cơ bản: {clockInfo.BaseClock}");
                    }

                    if (!string.IsNullOrEmpty(clockInfo.TurboClock))
                    {
                        sb.AppendLine($"  Xung nhịp Turbo Boost: {clockInfo.TurboClock}");
                    }

                    var l2Cache = GetProperty(obj, "L2CacheSize");
                    if (!string.IsNullOrEmpty(l2Cache)) sb.AppendLine($"  Cache L2: {l2Cache} KB");

                    var l3Cache = GetProperty(obj, "L3CacheSize");
                    if (!string.IsNullOrEmpty(l3Cache)) sb.AppendLine($"  Cache L3: {l3Cache} KB");

                    var architecture = GetProperty(obj, "Architecture");
                    if (!string.IsNullOrEmpty(architecture))
                    {
                        sb.AppendLine($"  Kiến trúc: {GetArchitectureName(architecture)}");
                    }
                }

                if (cpuIndex == 0) sb.AppendLine("  Không tìm thấy CPU.");
            }
            catch (Exception ex)
            {
                sb.AppendLine($"  Lỗi: {ex.Message}");
            }

            return sb.ToString().TrimEnd();
        }

        private static CpuClockInfo GetCpuClockInfo(string rawCpuName)
        {
            var normalizedCpuName = NormalizeCpuDisplayName(rawCpuName);
            if (string.IsNullOrWhiteSpace(normalizedCpuName))
            {
                return new CpuClockInfo();
            }

            CpuClockInfo cachedInfo;
            if (_cpuClockCache.TryGetValue(normalizedCpuName, out cachedInfo))
            {
                return cachedInfo;
            }

            var info = new CpuClockInfo
            {
                BaseClock = GetCpuBaseClockFromName(rawCpuName)
            };

            var webInfo = GetCpuClockInfoFromTechPowerUp(normalizedCpuName);
            if (!string.IsNullOrEmpty(webInfo.BaseClock))
            {
                info.BaseClock = webInfo.BaseClock;
            }

            if (!string.IsNullOrEmpty(webInfo.TurboClock))
            {
                info.TurboClock = webInfo.TurboClock;
            }

            _cpuClockCache[normalizedCpuName] = info;
            return info;
        }

        private static string NormalizeCpuDisplayName(string rawCpuName)
        {
            if (string.IsNullOrWhiteSpace(rawCpuName))
            {
                return string.Empty;
            }

            var name = rawCpuName.Replace("(R)", string.Empty)
                                 .Replace("(TM)", string.Empty)
                                 .Replace("CPU", string.Empty)
                                 .Replace("Intel", string.Empty)
                                 .Replace("AMD", string.Empty)
                                 .Trim();

            name = Regex.Replace(name, @"\s*@\s*[\d.,]+\s*GHz", string.Empty, RegexOptions.IgnoreCase);
            name = Regex.Replace(name, @"\s+", " ").Trim();
            return name;
        }

        private static string GetCpuBaseClockFromName(string rawCpuName)
        {
            if (string.IsNullOrWhiteSpace(rawCpuName))
            {
                return string.Empty;
            }

            var match = Regex.Match(rawCpuName, @"@\s*(?<ghz>\d+(?:[.,]\d+)?)\s*GHz", RegexOptions.IgnoreCase);
            if (!match.Success)
            {
                return string.Empty;
            }

            double ghzValue;
            var ghz = match.Groups["ghz"].Value.Replace(',', '.');
            if (!double.TryParse(ghz, NumberStyles.Float, CultureInfo.InvariantCulture, out ghzValue))
            {
                return string.Empty;
            }

            return ghzValue.ToString("0.##", CultureInfo.InvariantCulture) + " GHz";
        }

        private static CpuClockInfo GetCpuClockInfoFromTechPowerUp(string normalizedCpuName)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(5);
                    var query = Uri.EscapeDataString(normalizedCpuName);
                    var json = client.GetStringAsync("https://www.techpowerup.com/cpu-specs/?q=" + query + "&ajax")
                                     .GetAwaiter()
                                     .GetResult();
                    var htmlList = ExtractTechPowerUpListHtml(json);
                    if (string.IsNullOrWhiteSpace(htmlList))
                    {
                        return new CpuClockInfo();
                    }

                    var rowMatch = Regex.Match(
                        htmlList,
                        "<a href=\"(?<href>/cpu-specs/[^\"]+)\">(?<name>[^<]+)</a>\\s*</td>\\s*<td>(?<codename>[^<]*)</td>\\s*<td>(?<cores>[^<]*)</td>\\s*<td>(?<clock>[^<]*)</td>",
                        RegexOptions.IgnoreCase);

                    if (!rowMatch.Success)
                    {
                        return new CpuClockInfo();
                    }

                    var matchedName = NormalizeCpuDisplayName(rowMatch.Groups["name"].Value.Trim());
                    if (!string.Equals(matchedName, normalizedCpuName, StringComparison.OrdinalIgnoreCase))
                    {
                        return new CpuClockInfo();
                    }

                    return ParseCpuClockText(rowMatch.Groups["clock"].Value.Trim());
                }
            }
            catch
            {
                return new CpuClockInfo();
            }
        }

        private static CpuClockInfo ParseCpuClockText(string clockText)
        {
            if (string.IsNullOrWhiteSpace(clockText))
            {
                return new CpuClockInfo();
            }

            var normalized = Regex.Replace(clockText, @"\s+", " ").Trim();
            var parts = normalized.Split(new[] { " to " }, StringSplitOptions.None);

            if (parts.Length >= 2)
            {
                return new CpuClockInfo
                {
                    BaseClock = NormalizeClockValue(parts[0], parts[1]),
                    TurboClock = NormalizeClockValue(parts[1], parts[0])
                };
            }

            return new CpuClockInfo
            {
                BaseClock = NormalizeClockValue(normalized, string.Empty)
            };
        }

        private static string ExtractTechPowerUpListHtml(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return string.Empty;
            }

            var match = Regex.Match(json, "\"list\":\"(?<html>.*?)\",\"dropdown\":", RegexOptions.Singleline);
            if (!match.Success)
            {
                return string.Empty;
            }

            var html = match.Groups["html"].Value;
            html = html.Replace("\\/", "/");
            html = Regex.Unescape(html);
            return html;
        }

        private static string NormalizeClockValue(string value, string fallbackUnitSource)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            var text = value.Trim();
            if (Regex.IsMatch(text, @"GHz|MHz", RegexOptions.IgnoreCase))
            {
                return text;
            }

            if (!string.IsNullOrWhiteSpace(fallbackUnitSource))
            {
                if (fallbackUnitSource.IndexOf("GHz", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return text + " GHz";
                }

                if (fallbackUnitSource.IndexOf("MHz", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return text + " MHz";
                }
            }

            return text;
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
                    double totalRamBytes;
                    if (!string.IsNullOrEmpty(totalRam) &&
                        double.TryParse(totalRam, NumberStyles.Float, CultureInfo.InvariantCulture, out totalRamBytes))
                    {
                        var totalGB = totalRamBytes / Math.Pow(1024, 3);
                        sb.AppendLine($"  Tổng RAM vật lý: {totalGB:0.00} GB");
                    }
                }

                var freeRamSearcher = new ManagementObjectSearcher("SELECT * FROM Win32_OperatingSystem");
                foreach (ManagementObject obj in freeRamSearcher.Get())
                {
                    var freeRam = GetProperty(obj, "FreePhysicalMemory");
                    double freeRamKb;
                    if (!string.IsNullOrEmpty(freeRam) &&
                        double.TryParse(freeRam, NumberStyles.Float, CultureInfo.InvariantCulture, out freeRamKb))
                    {
                        var freeGB = freeRamKb / Math.Pow(1024, 2);
                        sb.AppendLine($"  RAM trống: {freeGB:0.00} GB");
                    }
                }

                sb.AppendLine();
                sb.AppendLine("  Chi tiết các slot RAM:");
                sb.AppendLine();

                var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_PhysicalMemory");
                int stickIndex = 0;

                foreach (ManagementObject obj in searcher.Get())
                {
                    stickIndex++;
                    if (stickIndex > 1) sb.AppendLine();

                    sb.AppendLine($"  --- Thanh RAM {stickIndex} ---");

                    var manufacturer = GetProperty(obj, "Manufacturer");
                    if (!string.IsNullOrEmpty(manufacturer)) sb.AppendLine($"    Hãng: {manufacturer}");

                    var partNumber = GetProperty(obj, "PartNumber");
                    if (!string.IsNullOrEmpty(partNumber)) sb.AppendLine($"    Mã: {partNumber.Trim()}");

                    var capacity = GetProperty(obj, "Capacity");
                    double capacityBytes;
                    if (!string.IsNullOrEmpty(capacity) &&
                        double.TryParse(capacity, NumberStyles.Float, CultureInfo.InvariantCulture, out capacityBytes))
                    {
                        var capGB = capacityBytes / Math.Pow(1024, 3);
                        sb.AppendLine($"    Dung lượng: {capGB:0.0} GB");
                    }

                    var speed = GetProperty(obj, "Speed");
                    if (!string.IsNullOrEmpty(speed)) sb.AppendLine($"    Tốc độ: {speed} MHz");

                    var configuredSpeed = GetProperty(obj, "ConfiguredClockSpeed");
                    if (!string.IsNullOrEmpty(configuredSpeed)) sb.AppendLine($"    Xung nhịp: {configuredSpeed} MHz");

                    var memType = GetProperty(obj, "SMBIOSMemoryType");
                    if (string.IsNullOrEmpty(memType))
                    {
                        memType = GetProperty(obj, "MemoryType");
                    }
                    if (!string.IsNullOrEmpty(memType))
                    {
                        sb.AppendLine($"    Loại: {GetMemoryTypeName(memType)}");
                    }

                    var formFactor = GetProperty(obj, "FormFactor");
                    if (!string.IsNullOrEmpty(formFactor))
                    {
                        sb.AppendLine($"    Kiểu: {GetFormFactorName(formFactor)}");
                    }

                    var dataWidth = GetProperty(obj, "DataWidth");
                    if (!string.IsNullOrEmpty(dataWidth)) sb.AppendLine($"    Độ rộng bus: {dataWidth}-bit");

                    var voltage = GetProperty(obj, "ConfiguredVoltage");
                    int milliVolt;
                    if (int.TryParse(voltage, out milliVolt) && milliVolt > 0)
                    {
                        sb.AppendLine($"    Điện áp: {milliVolt} mV");
                    }
                }

                if (stickIndex == 0)
                {
                    sb.AppendLine("  Không tìm thấy thông tin RAM.");
                }
            }
            catch (Exception ex)
            {
                sb.AppendLine($"  Lỗi: {ex.Message}");
            }

            return sb.ToString().TrimEnd();
        }

        private static string GetMemoryTypeName(string memType)
        {
            switch (memType)
            {
                case "0": return "Không rõ";
                case "20": return "DDR";
                case "21": return "DDR2";
                case "24": return "DDR3";
                case "26": return "DDR4";
                case "30": return "LPDDR4";
                case "34": return "DDR5";
                case "35": return "LPDDR5";
                default: return memType;
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
                    if (!string.IsNullOrEmpty(manufacturer)) sb.AppendLine($"  Hãng sản xuất: {manufacturer}");

                    var product = GetProperty(obj, "Product");
                    if (!string.IsNullOrEmpty(product)) sb.AppendLine($"  Model: {product}");

                    var serial = GetProperty(obj, "SerialNumber");
                    if (!string.IsNullOrEmpty(serial)) sb.AppendLine($"  Serial: {serial.Trim()}");

                    var version = GetProperty(obj, "Version");
                    if (!string.IsNullOrEmpty(version)) sb.AppendLine($"  Phiên bản: {version}");

                    var status = GetProperty(obj, "Status");
                    if (!string.IsNullOrEmpty(status)) sb.AppendLine($"  Trạng thái: {status}");
                }

                sb.AppendLine();

                var biosSearcher = new ManagementObjectSearcher("SELECT * FROM Win32_BIOS");
                foreach (ManagementObject obj in biosSearcher.Get())
                {
                    var biosManufacturer = GetProperty(obj, "Manufacturer");
                    if (!string.IsNullOrEmpty(biosManufacturer)) sb.AppendLine($"  BIOS: {biosManufacturer}");

                    var biosVersion = GetProperty(obj, "SMBIOSBIOSVersion");
                    if (!string.IsNullOrEmpty(biosVersion)) sb.AppendLine($"  Phiên bản BIOS: {biosVersion}");

                    var releaseDate = GetProperty(obj, "ReleaseDate");
                    if (!string.IsNullOrEmpty(releaseDate))
                    {
                        sb.AppendLine($"  Ngày phát hành: {FormatWmiDate(releaseDate)}");
                    }
                }

                sb.AppendLine();

                var sysSearcher = new ManagementObjectSearcher("SELECT * FROM Win32_ComputerSystem");
                foreach (ManagementObject obj in sysSearcher.Get())
                {
                    var sysManufacturer = GetProperty(obj, "Manufacturer");
                    if (!string.IsNullOrEmpty(sysManufacturer)) sb.AppendLine($"  Hệ thống: {sysManufacturer}");

                    var model = GetProperty(obj, "Model");
                    if (!string.IsNullOrEmpty(model)) sb.AppendLine($"  Model hệ thống: {model}");
                }
            }
            catch (Exception ex)
            {
                sb.AppendLine($"  Lỗi: {ex.Message}");
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

        private sealed class DxDiagDisplayInfo
        {
            public int DedicatedMemoryMb { get; set; }
            public int DisplayMemoryMb { get; set; }
        }

        private sealed class CpuClockInfo
        {
            public string BaseClock { get; set; }
            public string TurboClock { get; set; }
        }

        #endregion
    }
}
