using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Subtitle_draft_GMTPC.Services
{
    /// <summary>
    /// Service xử lý text karaoke tiếng Việt
    /// Quy tắc:
    /// 1. Đầu câu: chèn ∞ (dính liền với chữ)
    /// 2. Giữa các từ: chèn ♫
    /// 3. Cuối câu: không có ký tự đặc biệt
    /// 4. Tiếng Việt: full word
    /// 5. Tiếng Anh: chia theo âm tiết (không có ∞/♫ giữa các âm tiết)
    /// 6. Hỗ trợ quy tắc tách từ tùy chỉnh (vd: how/ling → how, ling)
    /// </summary>
    public class KaraokeVietnameseService
    {
        /// <summary>
        /// Xử lý toàn bộ lời bài hát thành format karaoke
        /// </summary>
        public static string ProcessLyrics(string lyrics)
        {
            return ProcessLyricsWithSplitRules(lyrics, null);
        }

        /// <summary>
        /// Xử lý toàn bộ lời bài hát với quy tắc tách từ tùy chỉnh
        /// </summary>
        /// <param name="lyrics">Lời bài hát</param>
        /// <param name="splitRules">Quy tắc tách từ mặc định/full</param>
        public static string ProcessLyricsWithSplitRules(string lyrics, string splitRules)
        {
            return ProcessLyricsWithSplitRules(lyrics, splitRules, null);
        }

        /// <summary>
        /// Xử lý toàn bộ lời bài hát với quy tắc tách từ full list kết hợp custom song list (ghi đè)
        /// </summary>
        /// <param name="lyrics">Lời bài hát</param>
        /// <param name="splitRules">Quy tắc tách từ mặc định/full (Word List)</param>
        /// <param name="customSplitRules">Quy tắc tách từ Custom Song List (sẽ ghi đè các từ tương ứng trong splitRules)</param>
        public static string ProcessLyricsWithSplitRules(string lyrics, string splitRules, string customSplitRules)
        {
            var mappingResult = ProcessLyricsWithMapping(lyrics, splitRules, customSplitRules);
            return mappingResult.FormattedOutput;
        }

        public class KaraokeWordMapping
        {
            public int InputStart { get; set; }
            public int InputLength { get; set; }
            public string InputWord { get; set; }
            public int OutputLineIndex { get; set; }
            public int OutputLineStart { get; set; }
            public int OutputLineLength { get; set; }
            public string SyllableText { get; set; }
            public int SyllableIndex { get; set; }
            public int TotalSyllablesInWord { get; set; }
        }

        public class KaraokeMappingResult
        {
            public string FormattedOutput { get; set; } = string.Empty;
            public List<KaraokeWordMapping> Mappings { get; set; } = new List<KaraokeWordMapping>();
        }

        /// <summary>
        /// Xử lý lời bài hát và xây dựng bản đồ ánh xạ 1-1 chính xác tuyệt đối giữa Panel 1 và Panel 2/3
        /// </summary>
        public static KaraokeMappingResult ProcessLyricsWithMapping(string lyrics, string splitRules, string customSplitRules, bool isJapaneseRomajiMode = false)
        {
            var result = new KaraokeMappingResult();
            if (string.IsNullOrWhiteSpace(lyrics)) return result;

            var customSyllableMap = ParseSplitRules(splitRules);
            if (!string.IsNullOrWhiteSpace(customSplitRules))
            {
                var customOverrides = ParseSplitRules(customSplitRules);
                foreach (var kvp in customOverrides)
                {
                    customSyllableMap[kvp.Key] = kvp.Value;
                }
            }

            var sb = new StringBuilder();
            int curOutputCharIndex = 0;
            int curOutputLineIndex = 0;

            // Dùng Regex để duyệt từng dòng mà giữ nguyên đúng vị trí Index trong lyrics gốc
            var lineMatches = Regex.Matches(lyrics, @"[^\r\n]+");

            foreach (Match lineMatch in lineMatches)
            {
                var rawLine = lineMatch.Value;
                if (string.IsNullOrWhiteSpace(rawLine))
                    continue;

                int rawLineStart = lineMatch.Index;

                // Tách các từ trong dòng bằng Regex \S+
                var wordMatches = Regex.Matches(rawLine, @"\S+");
                if (wordMatches.Count == 0)
                    continue;

                for (int w = 0; w < wordMatches.Count; w++)
                {
                    var wm = wordMatches[w];
                    var word = wm.Value;
                    var wordInputStart = rawLineStart + wm.Index;
                    var wordInputLen = wm.Length;

                    var isVietnamese = !isJapaneseRomajiMode && IsVietnameseWord(word);
                    var isFirstWordInLine = (w == 0);
                    var isLastWordInLine = (w == wordMatches.Count - 1);

                    if (isVietnamese)
                    {
                        var lineBuilder = new StringBuilder();
                        if (isFirstWordInLine) lineBuilder.Append($"∞{word}");
                        else lineBuilder.Append(word);

                        if (!isLastWordInLine) lineBuilder.Append("♫");

                        string outLineStr = lineBuilder.ToString();
                        sb.AppendLine(outLineStr);

                        result.Mappings.Add(new KaraokeWordMapping
                        {
                            InputStart = wordInputStart,
                            InputLength = wordInputLen,
                            InputWord = word,
                            OutputLineIndex = curOutputLineIndex,
                            OutputLineStart = curOutputCharIndex,
                            OutputLineLength = outLineStr.Length,
                            SyllableText = word,
                            SyllableIndex = 0,
                            TotalSyllablesInWord = 1
                        });

                        curOutputCharIndex += outLineStr.Length + Environment.NewLine.Length;
                        curOutputLineIndex++;
                    }
                    else
                    {
                        string[] syllables;
                        var wordLower = word.ToLowerInvariant();

                        if (ContainsApostrophe(word))
                        {
                            syllables = new[] { word };
                        }
                        else if (customSyllableMap != null && customSyllableMap.TryGetValue(wordLower, out var customSyllables))
                        {
                            syllables = AdjustSyllableCase(customSyllables, word);
                        }
                        else if (isJapaneseRomajiMode)
                        {
                            syllables = SplitJapaneseRomajiSyllables(word);
                        }
                        else
                        {
                            syllables = SplitEnglishSyllables(word);
                        }

                        for (int s = 0; s < syllables.Length; s++)
                        {
                            var lineBuilder = new StringBuilder();
                            if (s == 0 && isFirstWordInLine) lineBuilder.Append($"∞{syllables[s]}");
                            else lineBuilder.Append(syllables[s]);

                            bool isLastSyllable = (s == syllables.Length - 1);
                            if (isLastSyllable && !isLastWordInLine) lineBuilder.Append("♫");

                            string outLineStr = lineBuilder.ToString();
                            sb.AppendLine(outLineStr);

                            result.Mappings.Add(new KaraokeWordMapping
                            {
                                InputStart = wordInputStart,
                                InputLength = wordInputLen,
                                InputWord = word,
                                OutputLineIndex = curOutputLineIndex,
                                OutputLineStart = curOutputCharIndex,
                                OutputLineLength = outLineStr.Length,
                                SyllableText = syllables[s],
                                SyllableIndex = s,
                                TotalSyllablesInWord = syllables.Length
                            });

                            curOutputCharIndex += outLineStr.Length + Environment.NewLine.Length;
                            curOutputLineIndex++;
                        }
                    }
                }
            }

            result.FormattedOutput = sb.ToString().TrimEnd();
            return result;
        }

        /// <summary>
        /// Parse quy tắc tách từ từ input string
        /// Format mới: (word:part1/part2), (word2:part1/part2) - 50 rules/dòng
        /// Format cũ vẫn hỗ trợ: word:part1/part2 (1 rule/dòng)
        /// </summary>
        private static Dictionary<string, string[]> ParseSplitRules(string splitRules)
        {
            var result = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);

            if (string.IsNullOrWhiteSpace(splitRules)) return result;

            var lines = splitRules.Split(new[] { Environment.NewLine, "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (string.IsNullOrWhiteSpace(trimmed)) continue;

                // Tìm tất cả rules trong format: (word:part1/part2), (word2:part1/part2)
                var matches = System.Text.RegularExpressions.Regex.Matches(trimmed, @"\(([^)]+)\)");
                
                if (matches.Count > 0)
                {
                    // Format mới: có ngoặc
                    foreach (System.Text.RegularExpressions.Match match in matches)
                    {
                        var rule = match.Groups[1].Value; // "aback:a/back"
                        ParseSingleRule(rule, result);
                    }
                }
                else
                {
                    // Format cũ: không có ngoặc, 1 rule/dòng
                    ParseSingleRule(trimmed, result);
                }
            }

            return result;
        }

        /// <summary>
        /// Parse một rule đơn: "word:part1/part2"
        /// </summary>
        private static void ParseSingleRule(string rule, Dictionary<string, string[]> result)
        {
            var colonIndex = rule.IndexOf(':');
            if (colonIndex < 1) return;

            var originalWord = rule.Substring(0, colonIndex).Trim();
            var splitPart = rule.Substring(colonIndex + 1).Trim();

            if (string.IsNullOrEmpty(originalWord) || string.IsNullOrEmpty(splitPart)) return;

            var syllables = splitPart.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            if (syllables.Length < 1) return;

            result[originalWord.ToLowerInvariant()] = syllables;
        }

        /// <summary>
        /// Điều chỉnh case của syllables theo case của input word
        /// VD: input="HOWLING", rule="how/ling" → output="HOW/LING"
        /// VD: input="Howling", rule="how/ling" → output="How/ling"
        /// VD: input="howling", rule="HOW/LING" → output="how/ling"
        /// </summary>
        private static string[] AdjustSyllableCase(string[] syllables, string inputWord)
        {
            if (syllables == null || syllables.Length == 0) return syllables;

            var result = new string[syllables.Length];

            // Kiểm tra xem input word có phải ALL CAPS không
            bool isAllUpper = inputWord.All(char.IsUpper) && inputWord.Any(char.IsLetter);
            bool isAllLower = inputWord.All(char.IsLower) && inputWord.Any(char.IsLetter);
            bool isTitleCase = char.IsUpper(inputWord[0]) && inputWord.Substring(1).All(c => !char.IsLetter(c) || char.IsLower(c));

            for (int i = 0; i < syllables.Length; i++)
            {
                var syl = syllables[i];

                if (isAllUpper)
                {
                    // Input ALL CAPS → syllables cũng ALL CAPS
                    result[i] = syl.ToUpperInvariant();
                }
                else if (isAllLower)
                {
                    // Input lowercase → syllables cũng lowercase
                    result[i] = syl.ToLowerInvariant();
                }
                else if (isTitleCase && i == 0)
                {
                    // Title Case → syllable đầu viết hoa
                    if (syl.Length > 0)
                    {
                        result[i] = char.ToUpper(syl[0]) + syl.Substring(1).ToLowerInvariant();
                    }
                    else
                    {
                        result[i] = syl;
                    }
                }
                else
                {
                    // Mixed case hoặc syllable không phải đầu → giữ nguyên từ rule
                    result[i] = syl;
                }
            }

            return result;
        }

        /// <summary>
        /// Xử lý một dòng lời bài hát
        /// Mỗi từ/âm tiết xuống dòng riêng biệt
        /// Đầu mỗi dòng luôn có ∞
        /// </summary>
        private static string ProcessSingleLine(string line, Dictionary<string, string[]> customSyllableMap)
        {
            var sb = new StringBuilder();

            // Tách dòng thành các từ
            var words = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            for (int w = 0; w < words.Length; w++)
            {
                var word = words[w];
                var isVietnamese = IsVietnameseWord(word);
                var isFirstWordInLine = (w == 0);

                if (isVietnamese)
                {
                    // Tiếng Việt: full word
                    if (isFirstWordInLine)
                    {
                        // Đầu câu: chèn ∞
                        sb.Append($"∞{word}");
                    }
                    else
                    {
                        sb.Append(word);
                    }

                    // Nếu không phải từ cuối → thêm ♫
                    if (w < words.Length - 1)
                    {
                        sb.Append("♫");
                    }
                    // Xuống dòng
                    sb.AppendLine();
                }
                else
                {
                    // Tiếng Anh: kiểm tra quy tắc tùy chỉnh trước
                    string[] syllables;
                    var wordLower = word.ToLowerInvariant();

                    // QUY TẮC ĐẶC BIỆT: Từ có dấu nháy đơn (') - giữ nguyên, không tách
                    // Ví dụ: One's, I've, You're, Don't, Can't...
                    if (ContainsApostrophe(word))
                    {
                        syllables = new[] { word };
                    }
                    else if (customSyllableMap != null && customSyllableMap.TryGetValue(wordLower, out var customSyllables))
                    {
                        // Dùng quy tắc tùy chỉnh - ADJUST CASE theo input word
                        syllables = AdjustSyllableCase(customSyllables, word);
                    }
                    else
                    {
                        // Tách theo âm tiết tự động
                        syllables = SplitEnglishSyllables(word);
                    }

                    for (int s = 0; s < syllables.Length; s++)
                    {
                        if (s == 0 && isFirstWordInLine)
                        {
                            // Đầu câu tiếng Anh: ∞ dính với âm tiết đầu
                            sb.Append($"∞{syllables[s]}");
                        }
                        else
                        {
                            sb.Append(syllables[s]);
                        }

                        // Giữa các âm tiết trong cùng từ: không thêm gì
                        // Nhưng nếu là âm tiết cuối và không phải từ cuối → thêm ♫
                        var isLastSyllable = (s == syllables.Length - 1);
                        var isLastWord = (w == words.Length - 1);

                        if (isLastSyllable && !isLastWord)
                        {
                            sb.Append("♫");
                        }
                        // Xuống dòng
                        sb.AppendLine();
                    }
                }
            }

            return sb.ToString().TrimEnd();
        }

        /// <summary>
        /// Kiểm tra từ có phải tiếng Việt không
        /// Dựa vào: có dấu thanh, hoặc chứa ký tự tiếng Việt đặc trưng
        /// </summary>
        private static bool IsVietnameseWord(string word)
        {
            if (string.IsNullOrEmpty(word)) return false;

            // Kiểm tra ký tự tiếng Việt có dấu
            var vietnameseChars = "áàảãạăắằẳẵặâấầẩẫậéèẻẽẹêếềểễệíìỉĩịóòỏõọôốồổỗộơớờởỡợúùủũụưứừửữựýỳỷỹỵđÁÀẢÃẠĂẮẰẲẴẶÂẤẦẨẪẬÉÈẺẼẸÊẾỀỂỄỆÍÌỈĨỊÓÒỎÕỌÔỐỒỔỖỘƠỚỜỞỠỢÚÙỦŨỤƯỨỪỬỮỰÝỲỶỸỴĐ";

            foreach (var ch in word)
            {
                if (vietnameseChars.IndexOf(ch) >= 0)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Kiểm tra từ có chứa dấu nháy đơn (') - từ viết tắt/sở hữu
        /// Các từ này không nên tách, giữ nguyên làm 1 phần
        /// Ví dụ: One's, I've, You're, Don't, Can't...
        /// </summary>
        private static bool ContainsApostrophe(string word)
        {
            if (string.IsNullOrEmpty(word)) return false;
            return word.Contains("'");
        }

        /// <summary>
        /// Tách từ tiếng Anh thành các âm tiết
        /// Phương pháp: đếm vowel groups (nhóm nguyên âm liên tiếp)
        /// Mỗi vowel group = 1 âm tiết. Nếu ≤ 1 → không tách.
        /// </summary>
        private static string[] SplitEnglishSyllables(string word)
        {
            if (string.IsNullOrEmpty(word)) return new[] { word };
            if (word.Length <= 2) return new[] { word }; // Từ quá ngắn → không tách

            var lowerWord = word.ToLower();
            var vowels = "aeiou";
            var result = new List<string>();

            // Đếm số vowel groups
            var vowelGroups = CountVowelGroups(lowerWord);
            if (vowelGroups <= 1)
            {
                // Chỉ có 1 âm tiết → không tách
                return new[] { word };
            }

            // Tách tại boundary giữa các vowel groups
            result = SplitByVowelGroups(word, lowerWord, vowels);

            if (result.Count <= 1)
            {
                return new[] { word };
            }

            return result.ToArray();
        }

        /// <summary>
        /// Đếm số nhóm nguyên âm liên tiếp
        /// </summary>
        private static int CountVowelGroups(string lowerWord)
        {
            var vowels = "aeiou";
            var count = 0;
            var inVowelGroup = false;

            for (int i = 0; i < lowerWord.Length; i++)
            {
                var ch = lowerWord[i];
                var isVowel = (vowels.IndexOf(ch) >= 0);

                // Xử lý 'y' như nguyên âm nếu không phải ký tự đầu
                if (ch == 'y' && i > 0 && !inVowelGroup)
                {
                    isVowel = true;
                }

                if (isVowel && !inVowelGroup)
                {
                    count++;
                    inVowelGroup = true;
                }
                else if (!isVowel)
                {
                    inVowelGroup = false;
                }
            }

            return count;
        }

        /// <summary>
        /// Tách từ theo vowel group boundaries
        /// Mỗi syllable = (phụ âm trước) + vowel group
        /// Ví dụ: "baby" → "ba" + "by"
        /// every → "e" + "ve" + "ry"
        /// </summary>
        private static List<string> SplitByVowelGroups(string word, string lowerWord, string vowels)
        {
            var result = new List<string>();

            // Tìm tất cả vowel group: (start, end) positions
            var vowelGroups = new List<Tuple<int, int>>();
            var inVowelGroup = false;
            var vgStart = 0;

            for (int i = 0; i < lowerWord.Length; i++)
            {
                var ch = lowerWord[i];
                var isVowel = (vowels.IndexOf(ch) >= 0);

                if (ch == 'y' && i > 0 && !inVowelGroup)
                {
                    isVowel = true;
                }

                if (isVowel && !inVowelGroup)
                {
                    vgStart = i;
                    inVowelGroup = true;
                }
                else if (!isVowel && inVowelGroup)
                {
                    vowelGroups.Add(Tuple.Create(vgStart, i - 1));
                    inVowelGroup = false;
                }
            }
            if (inVowelGroup)
            {
                vowelGroups.Add(Tuple.Create(vgStart, lowerWord.Length - 1));
            }

            if (vowelGroups.Count <= 1)
            {
                result.Add(word);
                return result;
            }

            // Tách: mỗi syllable = phụ âm trước (nếu có) + vowel group
            var sylStart = 0;
            for (int i = 0; i < vowelGroups.Count; i++)
            {
                var vg = vowelGroups[i];
                var vgEnd = vg.Item2;
                // Syllable kết thúc tại cuối vowel group
                var sylEnd = vgEnd + 1;
                result.Add(word.Substring(sylStart, sylEnd - sylStart));
                // Syllable tiếp theo bắt đầu sau vowel group này
                sylStart = sylEnd;
            }

            // Nếu còn ký tự thừa → gộp vào syllable cuối
            if (sylStart < word.Length)
            {
                var lastIdx = result.Count - 1;
                result[lastIdx] = result[lastIdx] + word.Substring(sylStart);
            }

            return result;
        }

        /// <summary>
        /// Tách từ tiếng Nhật Romaji thành các âm tiết (Moras / Syllables)
        /// Phân tích tự động các dạng:
        /// - Âm ghép 3 ký tự (Yōon): kya, kyu, kyo, sha, shu, sho, cha, chu, cho, nya, hya, mya, rya, gya, ja, bya, pya...
        /// - Âm phụ âm đôi/ngắt (Sokuon): kk, tt, pp, ss, tch, cch...
        /// - Âm thường 2 ký tự (CV): ka, ki, ku, ke, ko, sa, shi, su, se, so, ta, chi, tsu, te, to...
        /// - Âm đơn 1 ký tự: a, i, u, e, o, n
        /// </summary>
        public static string[] SplitJapaneseRomajiSyllables(string word)
        {
            if (string.IsNullOrEmpty(word)) return new[] { word };
            if (word.Length <= 1) return new[] { word };

            var result = new List<string>();
            var lowerWord = word.ToLowerInvariant();
            int i = 0;
            int n = lowerWord.Length;

            // Pattern Regex khớp 1 âm tiết Romaji tiếng Nhật từ dài đến ngắn
            // 1. Phụ âm ngắt/kép: [kpsztcgdjb] đứng trước phụ âm khác (vd: tt trong matte -> mat, tch trong maccha -> mat)
            // 2. Âm ghép 3 ký tự: (ky|sh|ch|ny|hy|my|ry|gy|by|py|ts|dz)[aeiou]
            // 3. Âm phụ âm + nguyên âm đôi hoặc đơn: [b-df-hj-np-tv-z]?[aeiou]
            // 4. Âm mũi n cuối từ hoặc trước phụ âm: n
            var romajiPattern = new Regex(
                @"^(?:(?:sh|ch|ts|dz|ky|ny|hy|my|ry|gy|by|py|jy|[kpsztcgdjb])?(?:[aeiou]|ou|ei|ai|oi|ui)|" +
                @"(?:sh|ch|ts|dz|ky|ny|hy|my|ry|gy|by|py|jy|[k-np-z])[aeiou]|" +
                @"(?<sokuon>[kpsztcgdjb])(?=\k<sokuon>)|" +
                @"n(?=[^aeiouy]|$)|" +
                @"[aeiou]|" +
                @"[a-z])",
                RegexOptions.IgnoreCase);

            while (i < n)
            {
                var remaining = lowerWord.Substring(i);
                var match = romajiPattern.Match(remaining);

                if (match.Success && match.Length > 0)
                {
                    int matchLen = match.Length;
                    
                    // Kiểm tra trường hợp sokuon (phụ âm kép như 'tt' trong nemutte -> 'ne', 'mut', 'te')
                    // Nếu là phụ âm đầu của cặp phụ âm kép, lấy 1 ký tự phụ âm ghép vào âm tiết trước hoặc đứng riêng
                    if (i + 1 < n && lowerWord[i] == lowerWord[i + 1] && !"aeiou".Contains(lowerWord[i]))
                    {
                        // Lấy ký tự phụ âm kép ghép vào âm tiết trước nếu có, hoặc tạo âm ngắt
                        if (result.Count > 0)
                        {
                            result[result.Count - 1] += word.Substring(i, 1);
                        }
                        else
                        {
                            result.Add(word.Substring(i, 1));
                        }
                        i += 1;
                        continue;
                    }

                    result.Add(word.Substring(i, matchLen));
                    i += matchLen;
                }
                else
                {
                    // Fallback ký tự đơn
                    result.Add(word.Substring(i, 1));
                    i += 1;
                }
            }

            if (result.Count == 0)
                return new[] { word };

            return result.ToArray();
        }
    }
}
