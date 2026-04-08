using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Subtitle_draft_GMTPC.Models;

namespace Subtitle_draft_GMTPC.Services
{
    public class AssEffectBuilder
    {
        #region Tag String Builders

        public static string BuildPosition(double x, double y)
        {
            return $"\\pos({x},{y})";
        }

        public static string BuildMove(double x1, double y1, double x2, double y2)
        {
            return $"\\move({x1},{y1},{x2},{y2})";
        }

        public static string BuildMoveTimed(double x1, double y1, double x2, double y2, int t1, int t2)
        {
            return $"\\move({x1},{y1},{x2},{y2},{t1},{t2})";
        }

        public static string BuildAlignment(int n)
        {
            if (n < 1 || n > 9) throw new ArgumentException("Alignment must be 1-9");
            return $"\\an{n}";
        }

        public static string BuildOrigin(double x, double y)
        {
            return $"\\org({x},{y})";
        }

        public static string BuildRotation(string axis, double degrees)
        {
            switch (axis.ToLower())
            {
                case "z": case "frz": case "fr":
                    return $"\\frz{degrees}";
                case "x": case "frx":
                    return $"\\frx{degrees}";
                case "y": case "fry":
                    return $"\\fry{degrees}";
                default:
                    throw new ArgumentException("Axis must be x, y, or z");
            }
        }

        public static string BuildScale(string axis, double percent)
        {
            switch (axis.ToLower())
            {
                case "x": case "fscx":
                    return $"\\fscx{percent}";
                case "y": case "fscy":
                    return $"\\fscy{percent}";
                default:
                    throw new ArgumentException("Axis must be x or y");
            }
        }

        public static string BuildShear(string axis, double value)
        {
            switch (axis.ToLower())
            {
                case "x": case "fax":
                    return $"\\fax{value}";
                case "y": case "fay":
                    return $"\\fay{value}";
                default:
                    throw new ArgumentException("Axis must be x or y");
            }
        }

        public static string BuildFontName(string fontName)
        {
            return $"\\fn{fontName}";
        }

        public static string BuildFontSize(int size)
        {
            if (size < 1) throw new ArgumentException("Font size must be positive");
            return $"\\fs{size}";
        }

        public static string BuildBold(int weight)
        {
            if (weight <= 0) return "\\b0";
            return $"\\b{weight}";
        }

        public static string BuildItalic(bool isEnabled)
        {
            return isEnabled ? "\\i1" : "\\i0";
        }

        public static string BuildUnderline(bool isEnabled)
        {
            return isEnabled ? "\\u1" : "\\u0";
        }

        public static string BuildStrikeout(bool isEnabled)
        {
            return isEnabled ? "\\s1" : "\\s0";
        }

        public static string BuildBorder(double size)
        {
            return $"\\bord{size}";
        }

        public static string BuildShadow(double size)
        {
            return $"\\shad{size}";
        }

        public static string BuildBlur(double level)
        {
            return $"\\blur{level}";
        }

        public static string BuildEdgeBlur(int level)
        {
            return $"\\be{level}";
        }

        public static string BuildColor(string component, string hexBgr)
        {
            string tagPrefix;
            switch (component.ToLower())
            {
                case "1": case "1c": case "primary": case "c":
                    tagPrefix = "\\1c&H";
                    break;
                case "2": case "2c": case "secondary":
                    tagPrefix = "\\2c&H";
                    break;
                case "3": case "3c": case "border":
                    tagPrefix = "\\3c&H";
                    break;
                case "4": case "4c": case "shadow":
                    tagPrefix = "\\4c&H";
                    break;
                default:
                    throw new ArgumentException("Invalid color component");
            }
            string cleanHex = hexBgr.Trim().Replace("&H", "").Replace("&", "");
            return $"{tagPrefix}{cleanHex}&";
        }

        public static string RgbToBgrHex(int r, int g, int b)
        {
            return b.ToString("X2") + g.ToString("X2") + r.ToString("X2");
        }

        public static string BuildAlpha(string component, string hexAlpha)
        {
            string tagPrefix;
            switch (component.ToLower())
            {
                case "alpha": case "all":
                    tagPrefix = "\\alpha&H";
                    break;
                case "1a": case "1": case "primary":
                    tagPrefix = "\\1a&H";
                    break;
                case "2a": case "2": case "secondary":
                    tagPrefix = "\\2a&H";
                    break;
                case "3a": case "3": case "border":
                    tagPrefix = "\\3a&H";
                    break;
                case "4a": case "4": case "shadow":
                    tagPrefix = "\\4a&H";
                    break;
                default:
                    throw new ArgumentException("Invalid alpha component");
            }
            string cleanHex = hexAlpha.Trim().Replace("&H", "").Replace("&", "");
            return $"{tagPrefix}{cleanHex}&";
        }

        public static string BuildFade(int inTime, int outTime)
        {
            return $"\\fad({inTime},{outTime})";
        }

        public static string BuildKaraoke(string type, int durationCentiSec)
        {
            switch (type.ToLower())
            {
                case "k": return $"\\k{durationCentiSec}";
                case "kf": return $"\\kf{durationCentiSec}";
                case "ko": return $"\\ko{durationCentiSec}";
                default:
                    throw new ArgumentException("Karaoke type must be k, kf, or ko");
            }
        }

        public static string BuildClipRect(int x1, int y1, int x2, int y2)
        {
            return $"\\clip({x1},{y1},{x2},{y2})";
        }

        public static string BuildIClipRect(int x1, int y1, int x2, int y2)
        {
            return $"\\iclip({x1},{y1},{x2},{y2})";
        }

        #endregion

        #region Tag Parsing and Extraction

        public static List<string> ExtractTags(string text)
        {
            var result = new List<string>();
            if (string.IsNullOrWhiteSpace(text)) return result;
            var matches = Regex.Matches(text, @"\{([^}]+)\}");
            foreach (Match m in matches)
            {
                result.Add(m.Groups[1].Value.Trim());
            }
            return result;
        }

        public static AssTag ParseTag(string rawTag)
        {
            if (string.IsNullOrWhiteSpace(rawTag)) return new AssTag(AssTagType.Unknown, rawTag, "Unknown");
            string lower = rawTag.ToLower();
            if (lower.StartsWith("\\pos(")) return new AssTag(AssTagType.Position, rawTag, "Position");
            if (lower.StartsWith("\\move(")) return new AssTag(AssTagType.Move, rawTag, "Move");
            if (lower.StartsWith("\\an")) return new AssTag(AssTagType.Alignment, rawTag, "Alignment");
            if (lower.StartsWith("\\org(")) return new AssTag(AssTagType.Origin, rawTag, "Origin");
            if (lower.StartsWith("\\frz") || (lower.StartsWith("\\fr") && !lower.StartsWith("\\frx") && !lower.StartsWith("\\fry")))
                return new AssTag(AssTagType.RotateZ, rawTag, "Rotate Z");
            if (lower.StartsWith("\\frx")) return new AssTag(AssTagType.RotateX, rawTag, "Rotate X");
            if (lower.StartsWith("\\fry")) return new AssTag(AssTagType.RotateY, rawTag, "Rotate Y");
            if (lower.StartsWith("\\fscx")) return new AssTag(AssTagType.ScaleX, rawTag, "Scale X");
            if (lower.StartsWith("\\fscy")) return new AssTag(AssTagType.ScaleY, rawTag, "Scale Y");
            if (lower.StartsWith("\\fax")) return new AssTag(AssTagType.ShearX, rawTag, "Shear X");
            if (lower.StartsWith("\\fay")) return new AssTag(AssTagType.ShearY, rawTag, "Shear Y");
            if (lower.StartsWith("\\fn")) return new AssTag(AssTagType.FontName, rawTag, "Font Name");
            if (lower.StartsWith("\\fs")) return new AssTag(AssTagType.FontSize, rawTag, "Font Size");
            if (lower.StartsWith("\\b")) return new AssTag(AssTagType.Bold, rawTag, "Bold");
            if (lower.StartsWith("\\i")) return new AssTag(AssTagType.Italic, rawTag, "Italic");
            if (lower.StartsWith("\\u")) return new AssTag(AssTagType.Underline, rawTag, "Underline");
            if (lower.StartsWith("\\s")) return new AssTag(AssTagType.Strikeout, rawTag, "Strikeout");
            if (lower.StartsWith("\\xbord")) return new AssTag(AssTagType.XBorder, rawTag, "X Border");
            if (lower.StartsWith("\\ybord")) return new AssTag(AssTagType.YBorder, rawTag, "Y Border");
            if (lower.StartsWith("\\bord")) return new AssTag(AssTagType.Border, rawTag, "Border");
            if (lower.StartsWith("\\xshad")) return new AssTag(AssTagType.XShadow, rawTag, "X Shadow");
            if (lower.StartsWith("\\yshad")) return new AssTag(AssTagType.YShadow, rawTag, "Y Shadow");
            if (lower.StartsWith("\\shad")) return new AssTag(AssTagType.Shadow, rawTag, "Shadow");
            if (lower.StartsWith("\\blur")) return new AssTag(AssTagType.Blur, rawTag, "Blur");
            if (lower.StartsWith("\\be")) return new AssTag(AssTagType.EdgeBlur, rawTag, "Edge Blur");
            if (lower.StartsWith("\\1c") || lower == "\\c" || lower.StartsWith("\\c&"))
                return new AssTag(AssTagType.PrimaryColor, rawTag, "Primary Color");
            if (lower.StartsWith("\\2c")) return new AssTag(AssTagType.SecondaryColor, rawTag, "Secondary Color");
            if (lower.StartsWith("\\3c")) return new AssTag(AssTagType.BorderColor, rawTag, "Border Color");
            if (lower.StartsWith("\\4c")) return new AssTag(AssTagType.ShadowColor, rawTag, "Shadow Color");
            if (lower.StartsWith("\\alpha")) return new AssTag(AssTagType.Alpha, rawTag, "Alpha");
            if (lower.StartsWith("\\1a")) return new AssTag(AssTagType.PrimaryAlpha, rawTag, "Primary Alpha");
            if (lower.StartsWith("\\2a")) return new AssTag(AssTagType.SecondaryAlpha, rawTag, "Secondary Alpha");
            if (lower.StartsWith("\\3a")) return new AssTag(AssTagType.BorderAlpha, rawTag, "Border Alpha");
            if (lower.StartsWith("\\4a")) return new AssTag(AssTagType.ShadowAlpha, rawTag, "Shadow Alpha");
            if (lower.StartsWith("\\fad(")) return new AssTag(AssTagType.Fade, rawTag, "Fade");
            if (lower.StartsWith("\\fade(")) return new AssTag(AssTagType.ComplexFade, rawTag, "Complex Fade");
            if (lower.StartsWith("\\t(")) return new AssTag(AssTagType.Transform, rawTag, "Transform");
            if (lower.StartsWith("\\kf")) return new AssTag(AssTagType.KaraokeKF, rawTag, "Karaoke KF");
            if (lower.StartsWith("\\ko")) return new AssTag(AssTagType.KaraokeKO, rawTag, "Karaoke KO");
            if (lower.StartsWith("\\k")) return new AssTag(AssTagType.KaraokeK, rawTag, "Karaoke K");
            if (lower.StartsWith("\\clip(")) return new AssTag(AssTagType.ClipRect, rawTag, "Clip");
            if (lower.StartsWith("\\iclip(")) return new AssTag(AssTagType.IClipRect, rawTag, "Inverse Clip");
            if (lower.StartsWith("\\p")) return new AssTag(AssTagType.VectorDrawing, rawTag, "Vector Draw");
            return new AssTag(AssTagType.Unknown, rawTag, "Unknown");
        }

        #endregion

        #region Tag Application

        public static string ApplyTagToLine(string line, string newRawTag)
        {
            AssTagType tagType = GetTagTypeFromString(newRawTag);
            line = RemoveTagByType(line, tagType);
            string wrapTag = "{" + newRawTag + "}";
            return InsertTag(line, wrapTag);
        }

        private static string InsertTag(string line, string wrapTag)
        {
            var dialogueMatch = Regex.Match(line, @"^Dialogue:\s*\d+,[^,]+,[^,]+,[^,]+,[^,]*,[^,]*,[^,]*,[^,]*,[^,]*,(.*)$", RegexOptions.IgnoreCase);
            if (dialogueMatch.Success)
            {
                int textStartIndex = dialogueMatch.Groups[1].Index;
                return line.Insert(textStartIndex, wrapTag + " ");
            }
            else
            {
                return wrapTag + " " + line;
            }
        }

        public static string ApplyTagsToAllLines(string content, IEnumerable<string> tags)
        {
            string[] lineArr = content.Split(new[] { Environment.NewLine, "\r", "\n" }, StringSplitOptions.None);
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < lineArr.Length; i++)
            {
                string line = lineArr[i];
                if (string.IsNullOrWhiteSpace(line))
                {
                    sb.AppendLine(line);
                    continue;
                }
                foreach (var tag in tags)
                {
                    line = ApplyTagToLine(line, tag);
                }
                sb.AppendLine(line);
            }
            return sb.ToString().TrimEnd();
        }

        public static string RemoveAllTags(string line)
        {
            return Regex.Replace(line, @"\{[^}]*\} *", "");
        }

        public static string RemoveTagByType(string line, AssTagType tagType)
        {
            var tags = ExtractTags(line);
            if (tags.Count == 0) return line;
            foreach (var tag in tags)
            {
                var parsed = ParseTag(tag);
                if (parsed.TagType == tagType)
                {
                    string wrapTag = "{" + tag + "}";
                    string escaped = Regex.Escape(wrapTag);
                    line = Regex.Replace(line, escaped, "");
                    line = Regex.Replace(line, escaped + " ", "");
                }
            }
            line = Regex.Replace(line, @"\{\s*\} *", "");
            return line;
        }

        public static string RemoveAllTagsFromAllLines(string content)
        {
            return Regex.Replace(content, @"\{[^}]*\} *", "");
        }

        #endregion

        #region Internal Helpers

        private static AssTagType GetTagTypeFromString(string rawTag)
        {
            return ParseTag(rawTag.Trim()).TagType;
        }

        #endregion

        #region Preset Effects

        public static List<EffectPreset> GetPresetEffects()
        {
            var presets = new List<EffectPreset>();

            var fadeIn = new EffectPreset("Fade In", "Mờ dần xuất hiện");
            fadeIn.Tags.Add(new AssTag(AssTagType.Fade, "\\fad(300,200)", "Fade"));
            presets.Add(fadeIn);

            var fadeOut = new EffectPreset("Fade Out", "Mờ dần biến mất");
            fadeOut.Tags.Add(new AssTag(AssTagType.Fade, "\\fad(200,500)", "Fade"));
            presets.Add(fadeOut);

            var glow = new EffectPreset("Glow", "Viền sáng bao quanh chữ");
            glow.Tags.Add(new AssTag(AssTagType.Border, "\\bord2", "Border"));
            glow.Tags.Add(new AssTag(AssTagType.Blur, "\\blur1", "Blur"));
            presets.Add(glow);

            var bigText = new EffectPreset("Big Text", "Chữ to đậm");
            bigText.Tags.Add(new AssTag(AssTagType.FontSize, "\\fs48", "Font Size"));
            bigText.Tags.Add(new AssTag(AssTagType.Bold, "\\b1", "Bold"));
            presets.Add(bigText);

            var centerTop = new EffectPreset("Center Top", "Can giữa trên cùng");
            centerTop.Tags.Add(new AssTag(AssTagType.Alignment, "\\an8", "Alignment"));
            presets.Add(centerTop);

            var centerScreen = new EffectPreset("Center Screen", "Can chỉnh giữa màn hình");
            centerScreen.Tags.Add(new AssTag(AssTagType.Alignment, "\\an5", "Alignment"));
            presets.Add(centerScreen);

            var karaokeSweep = new EffectPreset("Karaoke Sweep", "Quét màu karaoke");
            karaokeSweep.Tags.Add(new AssTag(AssTagType.PrimaryColor, "\\1c&H0000FF&", "Primary Color"));
            karaokeSweep.Tags.Add(new AssTag(AssTagType.SecondaryColor, "\\2c&HFFFF00&", "Secondary Color"));
            presets.Add(karaokeSweep);

            return presets;
        }

        #endregion

        #region Validation

        public static Tuple<bool, string> ValidateTag(AssTagType tagType, Dictionary<string, object> @params)
        {
            switch (tagType)
            {
                case AssTagType.Position:
                    if (!@params.ContainsKey("x") || !@params.ContainsKey("y"))
                        return Tuple.Create(false, "Missing x,y parameters");
                    return Tuple.Create(true, "");
                case AssTagType.Alignment:
                    if (!@params.ContainsKey("n"))
                        return Tuple.Create(false, "Missing alignment value (1-9)");
                    int n = Convert.ToInt32(@params["n"]);
                    if (n < 1 || n > 9)
                        return Tuple.Create(false, "Alignment must be 1-9");
                    return Tuple.Create(true, "");
                case AssTagType.FontSize:
                    if (!@params.ContainsKey("size"))
                        return Tuple.Create(false, "Missing font size");
                    int size = Convert.ToInt32(@params["size"]);
                    if (size < 1)
                        return Tuple.Create(false, "Font size must be positive");
                    return Tuple.Create(true, "");
                case AssTagType.PrimaryColor:
                case AssTagType.SecondaryColor:
                case AssTagType.BorderColor:
                case AssTagType.ShadowColor:
                    if (!@params.ContainsKey("hex"))
                        return Tuple.Create(false, "Missing hex color value");
                    string hex = @params["hex"].ToString();
                    if (!Regex.IsMatch(hex, @"^[0-9A-Fa-f]{6}$"))
                        return Tuple.Create(false, "Invalid hex color (need 6 hex digits: BBGGRR)");
                    return Tuple.Create(true, "");
                default:
                    return Tuple.Create(true, "");
            }
        }

        #endregion
    }
}
