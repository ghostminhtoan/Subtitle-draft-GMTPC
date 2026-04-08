using System;
using System.Collections.Generic;
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
    /// </summary>
    public class KaraokeVietnameseService
    {
        /// <summary>
        /// Xử lý toàn bộ lời bài hát thành format karaoke
        /// </summary>
        public static string ProcessLyrics(string lyrics)
        {
            if (string.IsNullOrWhiteSpace(lyrics)) return string.Empty;

            var sb = new StringBuilder();
            var lines = lyrics.Split(new[] { Environment.NewLine, "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i].Trim();
                if (string.IsNullOrEmpty(line)) continue;

                var processedLine = ProcessSingleLine(line);
                sb.AppendLine(processedLine);
            }

            return sb.ToString().TrimEnd();
        }

        /// <summary>
        /// Xử lý một dòng lời bài hát
        /// Mỗi từ/âm tiết xuống dòng riêng biệt
        /// Đầu mỗi dòng luôn có ∞
        /// </summary>
        private static string ProcessSingleLine(string line)
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
                    // Tiếng Anh: chia theo âm tiết
                    var syllables = SplitEnglishSyllables(word);

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
    }
}
