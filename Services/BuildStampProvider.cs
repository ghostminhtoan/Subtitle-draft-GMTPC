using System;
using System.Globalization;
using System.IO;

namespace Subtitle_draft_GMTPC.Services
{
    internal static class BuildStampProvider
    {
        private const string BuildStampFileName = "BuildStamp.txt";

        public static DateTime GetBuildTimeUtc()
        {
            var embeddedStamp = BuildStampInfo.Utc;
            if (!string.IsNullOrWhiteSpace(embeddedStamp))
            {
                if (DateTimeOffset.TryParse(embeddedStamp, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var embeddedParsed))
                {
                    return embeddedParsed.UtcDateTime;
                }

                if (DateTime.TryParse(embeddedStamp, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var embeddedUtc))
                {
                    return DateTime.SpecifyKind(embeddedUtc, DateTimeKind.Utc);
                }
            }

            var stampPath = AppRuntimePaths.ResolvePath(BuildStampFileName);
            if (File.Exists(stampPath))
            {
                var raw = File.ReadAllText(stampPath).Trim();
                if (DateTimeOffset.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsed))
                {
                    return parsed.UtcDateTime;
                }

                if (DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var parsedUtc))
                {
                    return DateTime.SpecifyKind(parsedUtc, DateTimeKind.Utc);
                }
            }

            var exePath = Path.Combine(AppRuntimePaths.BaseDirectory, "Subtitle draft GMTPC.exe");
            if (File.Exists(exePath))
            {
                return File.GetLastWriteTimeUtc(exePath);
            }

            return DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc);
        }

        public static string GetBuildStampText()
        {
            return GetBuildTimeUtc().ToLocalTime().ToString("yyyy-MM-dd hh.mm.ss tt dddd", CultureInfo.InvariantCulture);
        }
    }
}
