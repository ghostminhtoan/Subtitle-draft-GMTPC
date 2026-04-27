using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Windows;
using System.Windows.Controls;
using System.Threading.Tasks;
using Subtitle_draft_GMTPC.Models;
using Subtitle_draft_GMTPC.Services;

namespace Subtitle_draft_GMTPC
{
    public partial class MainWindow : Window
    {
        #region Translate - Fields

        private static readonly HttpClient _promptHttpClient = CreatePromptHttpClient();

        private List<SubtitleLine> _translateLines = new List<SubtitleLine>();
        private SubtitleFormat _translateFormat = SubtitleFormat.Unknown;
        private bool _isTranslateUpdating = false;
        private bool _isPromptLoading = false;
        private int _selectedPromptId = 1;

        private const string Prompt1DefaultUrl = "https://raw.githubusercontent.com/ghostminhtoan/Subtitle-draft-GMTPC/master/Prompt/1.%20Anime.md";
        private const string Prompt2DefaultUrl = "https://raw.githubusercontent.com/ghostminhtoan/Subtitle-draft-GMTPC/master/Prompt/2.%20Film.md";
        private const string Prompt3DefaultUrl = "https://raw.githubusercontent.com/ghostminhtoan/Subtitle-draft-GMTPC/master/Prompt/3.%20Anime%20one%20word.md";

        #endregion

        #region Translate - Event Handlers

        private void TxtTranslateInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isTranslateUpdating) return;
            try
            {
                _isTranslateUpdating = true;
                var content = SubtitleParser.SanitizeContent(TxtTranslateInput.Text);
                if (string.IsNullOrWhiteSpace(content))
                {
                    _translateLines.Clear();
                    _translateFormat = SubtitleFormat.Unknown;
                    TxtTranslateInputFormat.Text = "";
                    return;
                }
                _translateFormat = SubtitleParser.DetectFormat(content);
                _translateLines = SubtitleParser.Parse(content);
                TxtTranslateInputFormat.Text = string.Format("({0} - {1} dòng)", _translateFormat.ToString().ToUpper(), _translateLines.Count);
            }
            catch (Exception ex)
            {
                TxtTranslateInputFormat.Text = string.Format("(Lỗi: {0})", ex.Message);
            }
            finally
            {
                _isTranslateUpdating = false;
            }
        }

        private void TxtPrompt_LostFocus(object sender, RoutedEventArgs e)
        {
            SaveSettings();
        }

        private void BtnOpenQwen_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start("https://chat.qwen.ai/");
                TxtTranslateStatus.Text = "✅ Đã mở chat.qwen.ai trên trình duyệt!";
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Lỗi: " + ex.Message, "Lỗi", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private void BtnCopyForPaste_Click(object sender, RoutedEventArgs e)
        {
            var inputText = TxtTranslateInput.Text;
            if (string.IsNullOrWhiteSpace(inputText))
            {
                TxtTranslateStatus.Text = "⚠️ Vui lòng nhập phụ đề vào Panel 1!";
                return;
            }
            var prompt = TxtPrompt.Text.Trim();
            if (string.IsNullOrWhiteSpace(prompt))
            {
                TxtTranslateStatus.Text = "⚠️ Vui lòng nhập prompt!";
                return;
            }
            SaveSettings();
            Clipboard.SetText(prompt + Environment.NewLine + Environment.NewLine + inputText);
            TxtTranslateStatus.Text = "📋 Đã copy Prompt + Subtitle vào clipboard!";
        }

        /// <summary>
        /// Khi click vào nút prompt → dán nội dung prompt vào TxtPrompt
        /// </summary>
        private async void BtnPrompt_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            if (btn == null) return;

            int promptId;
            if (!int.TryParse(btn.Tag == null ? null : btn.Tag.ToString(), out promptId))
            {
                return;
            }

            await LoadPromptForSelectionAsync(promptId, promptId <= 3, true);
        }

        /// <summary>
        /// Load the default source for the currently selected prompt.
        /// </summary>
        private async void BtnLoadDefaultPrompt_Click(object sender, RoutedEventArgs e)
        {
            await LoadPromptForSelectionAsync(_selectedPromptId, true, false);
        }

        /// <summary>
        /// Rename the currently selected prompt.
        /// </summary>
        private void BtnRenamePrompt_Click(object sender, RoutedEventArgs e)
        {
            var currentPromptId = _selectedPromptId;
            var currentName = GetPromptNameById(currentPromptId);
            var newName = Microsoft.VisualBasic.Interaction.InputBox(
                string.Format("Đổi tên cho Prompt {0} (hiện tại: {1}):", currentPromptId, currentName),
                "Rename Prompt", currentName);

            if (!string.IsNullOrWhiteSpace(newName))
            {
                SetPromptNameById(currentPromptId, newName.Trim());
                UpdatePromptButtonDisplay();
                AppSettings.Save();
                TxtTranslateStatus.Text = string.Format("✅ Đã rename Prompt {0} thành: {1}", currentPromptId, newName.Trim());
            }
        }

        /// <summary>
        /// Save the current editor content into the selected prompt slot.
        /// </summary>
        private void BtnSavePrompts_Click(object sender, RoutedEventArgs e)
        {
            var promptId = _selectedPromptId;
            var currentContent = TxtPrompt.Text.Trim();

            if (string.IsNullOrWhiteSpace(currentContent))
            {
                SetPromptContentById(promptId, "");
                TxtTranslateStatus.Text = string.Format("🧹 Đã clear Prompt {0}!", promptId);
            }
            else
            {
                SetPromptContentById(promptId, currentContent);
                TxtTranslateStatus.Text = string.Format("💾 Đã save nội dung vào Prompt {0}: {1}", promptId, GetPromptNameById(promptId));
            }

            AppSettings.Save();
            UpdatePromptButtonDisplay();
        }

        /// <summary>
        /// Load the saved local content for the currently selected prompt.
        /// </summary>
        private void BtnLoadPrompts_Click(object sender, RoutedEventArgs e)
        {
            var promptId = _selectedPromptId;
            var content = GetPromptContentById(promptId);
            TxtPrompt.Text = content ?? string.Empty;
            TxtTranslateStatus.Text = string.IsNullOrWhiteSpace(content)
                ? string.Format("🧹 Prompt {0} đang trống, đã clear ô nhập!", promptId)
                : string.Format("📂 Đã load Prompt {0}: {1}", promptId, GetPromptNameById(promptId));
            UpdatePromptButtonDisplay();
        }
        #endregion

        #region Translate - Prompt Management

        /// <summary>
        /// Lấy thông tin prompt theo ID (1-5)
        /// </summary>
        private PromptItem GetPromptById(int id)
        {
            var name = GetPromptNameById(id);
            var content = GetPromptContentById(id);
            return new PromptItem(id, name, content);
        }

        private string GetPromptNameById(int id)
        {
            switch (id)
            {
                case 1: return AppSettings.PromptName1;
                case 2: return AppSettings.PromptName2;
                case 3: return AppSettings.PromptName3;
                case 4: return AppSettings.PromptName4;
                case 5: return AppSettings.PromptName5;
                default: return string.Format("Prompt {0}", id);
            }
        }

        private string GetPromptContentById(int id)
        {
            switch (id)
            {
                case 1: return AppSettings.PromptContent1;
                case 2: return AppSettings.PromptContent2;
                case 3: return AppSettings.PromptContent3;
                case 4: return AppSettings.PromptContent4;
                case 5: return AppSettings.PromptContent5;
                default: return "";
            }
        }

        private void SetPromptNameById(int id, string name)
        {
            switch (id)
            {
                case 1: AppSettings.PromptName1 = name; break;
                case 2: AppSettings.PromptName2 = name; break;
                case 3: AppSettings.PromptName3 = name; break;
                case 4: AppSettings.PromptName4 = name; break;
                case 5: AppSettings.PromptName5 = name; break;
            }
        }

        private void SetPromptContentById(int id, string content)
        {
            switch (id)
            {
                case 1: AppSettings.PromptContent1 = content; break;
                case 2: AppSettings.PromptContent2 = content; break;
                case 3: AppSettings.PromptContent3 = content; break;
                case 4: AppSettings.PromptContent4 = content; break;
                case 5: AppSettings.PromptContent5 = content; break;
            }
        }

        /// <summary>
        /// Update the prompt buttons and highlight the selected prompt.
        /// </summary>
        private void UpdatePromptButtonDisplay()
        {
            BtnPrompt1.Content = FormatPromptButtonContent(1);
            BtnPrompt2.Content = FormatPromptButtonContent(2);
            BtnPrompt3.Content = FormatPromptButtonContent(3);
            BtnPrompt4.Content = FormatPromptButtonContent(4);
            BtnPrompt5.Content = FormatPromptButtonContent(5);
        }

        private string FormatPromptButtonContent(int id)
        {
            var label = string.Format("{0}. {1}", id, GetPromptNameById(id));
            return id == _selectedPromptId ? "▶ " + label : label;
        }

        /// <summary>
        /// Find the selected prompt id.
        /// </summary>
        private int FindCurrentPromptId()
        {
            return _selectedPromptId;
        }

        /// <summary>
        /// Save all prompts to Settings.
        /// </summary>
        private void SaveAllPrompts()
        {
            AppSettings.Save();
        }

        /// <summary>
        /// Refresh the prompt selector UI and default content.
        /// </summary>
        private void LoadAllPrompts()
        {
            UpdatePromptButtonDisplay();
            _ = LoadPromptForSelectionAsync(_selectedPromptId, _selectedPromptId <= 3, false);
        }

        /// <summary>
        /// Khá»Ÿi táº¡o prompts máº·c Ä‘á»‹nh (Anime vÃ  Film) náº¿u chÆ°a cÃ³
        /// </summary>
        private void InitializeDefaultPrompts()
        {
            // Prompt 1 - Anime
            if (string.IsNullOrWhiteSpace(AppSettings.PromptName1) || AppSettings.PromptName1 == "Prompt 1")
            {
                AppSettings.PromptName1 = "Anime";
                AppSettings.PromptContent1 = GetDefaultAnimePrompt();
            }

            // Prompt 2 - Film
            if (string.IsNullOrWhiteSpace(AppSettings.PromptName2) || AppSettings.PromptName2 == "Prompt 2")
            {
                AppSettings.PromptName2 = "Film";
                AppSettings.PromptContent2 = GetDefaultFilmPrompt();
            }

            // Prompt 3 - Anime One Word
            if (string.IsNullOrWhiteSpace(AppSettings.PromptName3) || AppSettings.PromptName3 == "Prompt 3")
            {
                AppSettings.PromptName3 = "Anime One Word";
                AppSettings.PromptContent3 = GetDefaultAnimeOneWordPrompt();
            }

            AppSettings.Save();
        }

        private static HttpClient CreatePromptHttpClient()
        {
            var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("SubtitleDraftGMTPC/1.0");
            return client;
        }

        private string GetPromptDefaultUrlById(int id)
        {
            switch (id)
            {
                case 1: return Prompt1DefaultUrl;
                case 2: return Prompt2DefaultUrl;
                case 3: return Prompt3DefaultUrl;
                default: return null;
            }
        }

        private async Task<string> LoadPromptDefaultContentAsync(int id)
        {
            var url = GetPromptDefaultUrlById(id);
            if (string.IsNullOrWhiteSpace(url))
            {
                return null;
            }

            using (var request = new HttpRequestMessage(HttpMethod.Get, url + "?t=" + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()))
            {
                request.Headers.CacheControl = new CacheControlHeaderValue
                {
                    NoCache = true,
                    NoStore = true,
                    MaxAge = TimeSpan.Zero
                };
                request.Headers.Pragma.ParseAdd("no-cache");

                using (var response = await _promptHttpClient.SendAsync(request))
                {
                    response.EnsureSuccessStatusCode();
                    return await response.Content.ReadAsStringAsync();
                }
            }
        }

        private async Task LoadPromptForSelectionAsync(int promptId, bool useDefaultSource, bool updateSelection)
        {
            if (_isPromptLoading)
            {
                return;
            }

            try
            {
                _isPromptLoading = true;
                if (updateSelection)
                {
                    _selectedPromptId = promptId;
                }

                UpdatePromptButtonDisplay();

                string content = null;
                if (useDefaultSource && promptId <= 3)
                {
                    content = await LoadPromptDefaultContentAsync(promptId);
                    if (!string.IsNullOrWhiteSpace(content))
                    {
                        TxtPrompt.Text = content;
                        TxtTranslateStatus.Text = string.Format("Loaded default Prompt {0}: {1}", promptId, GetPromptNameById(promptId));
                        return;
                    }
                }

                content = GetPromptContentById(promptId);
                TxtPrompt.Text = content ?? string.Empty;
                TxtTranslateStatus.Text = string.IsNullOrWhiteSpace(content)
                    ? string.Format("Prompt {0} is empty.", promptId)
                    : string.Format("Loaded Prompt {0}: {1}", promptId, GetPromptNameById(promptId));
            }
            catch (Exception ex)
            {
                MessageBox.Show("Khong the tai prompt: " + ex.Message, "Loi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                _isPromptLoading = false;
            }
        }

        private string GetDefaultAnimePrompt()
        {
            return "# SYSTEM ROLE" + Environment.NewLine +
                   "Bạn là chuyên gia dịch thuật & localize phụ đề anime 10+ năm kinh nghiệm. Nhiệm vụ: Dịch chuẩn xác, giữ nguyên vibe anime, tối ưu cho màn hình phụ đề và xuất định dạng chuẩn để dán trực tiếp vào Excel." + Environment.NewLine + Environment.NewLine +
                   "# ⚠️ LUẬT CỨNG (BẮT BUỘC TUÂN THỦ)" + Environment.NewLine +
                   "1. CẤU TRÚC ĐẦU RA 3 CỘT (CHO EXCEL)" + Environment.NewLine +
                   "   - Output bắt buộc phải được trình bày dưới dạng Bảng (Table) với 3 cột: `Số thứ tự` | `Nội dung gốc (Tiếng Anh)` | `Nội dung dịch (Tiếng Việt)`. Tiếng Anh bắt buộc giữ nguyên không được dịch, chỉ dịch tiếng Việt thôi." + Environment.NewLine +
                   "   - ÁNH XẠ 1:1 TUYỆT ĐỐI: Mỗi dòng input = đúng 1 hàng trong bảng output." + Environment.NewLine +
                   "   - Giữ nguyên toàn bộ tag metadata (`[time]`, `{style}`, `**bold**`, `*italic*`, `\\N`) ở cả bản gốc và bản dịch." + Environment.NewLine +
                   "   - CẤM TÚC: Không gộp, tách, bỏ dòng. Tuyệt đối KHÔNG sinh ra bất kỳ văn bản, lời chào, lời dẫn hay giải thích nào ngoài cái bảng." + Environment.NewLine + Environment.NewLine +
                   "2. CHUẨN PHỤ ĐỀ & ĐỘ DÀI" + Environment.NewLine +
                   "   - Ưu tiên ≤ 42 ký tự cho bản dịch Tiếng Việt, không tính punctuation (dấu câu: .,!?;:-'\"\"()[]{}... và dấu CJK như 。、！？)." + Environment.NewLine +
                   "     Ví dụ: \"\"Chúng ta nên nhanh chóng đưa nó trở lại cơ sở chăm sóc!\"\" → 42 ký tự (không tính dấu !)." + Environment.NewLine +
                   "   - Nếu vượt quá, bắt buộc phải rút gọn tự nhiên nhưng không được cắt nghĩa lõi." + Environment.NewLine +
                   "   - Tính độ dài riêng cho từng đoạn trước/sau `\\N` nếu có." + Environment.NewLine +
                   "   - Rút gọn `...` thành `…` để tiết kiệm ký tự." + Environment.NewLine +
                   "   - Chuẩn hóa dấu câu tiếng Việt: bỏ chồng chéo (`?.!` → `?!` hoặc `.`), cách dấu đúng chuẩn." + Environment.NewLine + Environment.NewLine +
                   "3. LOCALIZE & VIBE ANIME" + Environment.NewLine +
                   "   - Tính cách nhân vật: Tsundere (cộc, phủ nhận cảm xúc), Kuudere (ngắn, lạnh lùng), Genki (năng lượng), Chuunibyou (kỳ ảo, hoa mỹ) → Thể hiện qua từ vựng & nhịp câu." + Environment.NewLine +
                   "   - Honorifics: Việt hóa theo quan hệ (`anh/chị/em/cậu`) trừ khi là thuật ngữ đặc thù hoặc fan yêu cầu giữ `-san/-kun/-chan/-sama`." + Environment.NewLine +
                   "   - Tiếng lóng/Onomatopoeia: Dịch nghĩa hoặc giữ nguyên *in nghiêng* nếu mang tính biểu tượng. KHÔNG dùng chú thích trong phụ đề." + Environment.NewLine +
                   "   - Tên riêng: Giữ nguyên Romaji/Kanji hoặc Việt hóa nhất quán xuyên suốt." + Environment.NewLine + Environment.NewLine +
                   "# ✅ QUY TRÌNH TỰ PHẢN BIỆN 2 LẦN trước khi xuất ra kết quả:" + Environment.NewLine +
                   "1. Đã tạo đúng định dạng bảng 3 cột để copy vào Excel chưa?" + Environment.NewLine +
                   "2. Số lượng hàng output có khớp 100% với số lượng hàng input không?" + Environment.NewLine +
                   "3. Bản dịch tiếng Việt đã tối ưu ≤ 42 ký tự (không tính dấu) chưa?" + Environment.NewLine +
                   "4. Đã dọn sạch 100% các câu chữ thừa như \"\"Dưới đây là kết quả...\"\", \"\"Hoàn thành...\"\" chưa?" + Environment.NewLine + Environment.NewLine +
                   "# 📤 CẤU TRÚC OUTPUT DUY NHẤT ĐƯỢC PHÉP TRẢ VỀ:" + Environment.NewLine +
                   "| Số thứ tự | Nội dung gốc (Tiếng Anh) | Nội dung dịch (Tiếng Việt) |" + Environment.NewLine +
                   "| :--- | :--- | :--- |" + Environment.NewLine +
                   "| [STT] | [Text Anh] | [Text Việt] |" + Environment.NewLine +
                   "| ... | ... | ... |";
        }

        private string GetDefaultAnimeOneWordPrompt()
        {
            return "# SYSTEM ROLE" + Environment.NewLine +
                   "Bạn là chuyên gia dịch thuật & localize phụ đề anime 10+ năm kinh nghiệm. Nhiệm vụ: dịch các phụ đề anime dạng mỗi cue/dòng chỉ có 1 từ, kana, hạt câu hoặc cụm cực ngắn. Chính bạn phải tự gom ngữ cảnh từ các dòng liền kề để hiểu câu đầy đủ; phần mềm không gom trước cho bạn." + Environment.NewLine + Environment.NewLine +
                   "# LUẬT CỨNG - BẮT BUỘC TUÂN THỦ" + Environment.NewLine +
                   "1. OUTPUT 3 CỘT ĐỂ COPY VÀO EXCEL" + Environment.NewLine +
                   "   - Chỉ được xuất bảng Markdown gồm 3 cột: `Số thứ tự` | `Nội dung gốc` | `Nội dung dịch`." + Environment.NewLine +
                   "   - Mỗi dòng input = đúng 1 hàng output. Không thêm dòng, không bỏ dòng, không đảo thứ tự." + Environment.NewLine +
                   "   - Giữ nguyên toàn bộ nội dung gốc trong cột `Nội dung gốc`; không dịch, không sửa, không normalize." + Environment.NewLine +
                   "   - Giữ nguyên tag/metadata/timing/style/karaoke marker nếu có: `[time]`, `{style}`, `{\\k...}`, `**bold**`, `*italic*`, `\\N`." + Environment.NewLine +
                   "   - Tuyệt đối không xuất lời chào, lời dẫn, giải thích, checklist, ghi chú hay reasoning ngoài bảng." + Environment.NewLine + Environment.NewLine +
                   "2. CƠ CHẾ DỊCH ONE-WORD SUBTITLE" + Environment.NewLine +
                   "   - Không dịch từng dòng một cách máy móc nếu các dòng liền kề là mảnh của cùng một câu/ngữ nghĩa." + Environment.NewLine +
                   "   - Tự gom các cue/dòng liền kề thành cụm ngữ nghĩa đủ để hiểu câu, cảm xúc và quan hệ nhân vật." + Environment.NewLine +
                   "   - Dịch theo cụm đã gom để có tiếng Việt tự nhiên, đúng vibe anime, rồi phân bổ bản dịch trở lại đúng các hàng output tương ứng." + Environment.NewLine +
                   "   - Nếu một dòng chỉ là hạt câu, trợ từ, đuôi câu, tiếng ngắt, kana lẻ, hoặc dấu câu, hãy hiểu nó bằng cụm liền trước/sau trước khi dịch." + Environment.NewLine +
                   "   - Không được để `Nội dung dịch` trống, trừ khi dòng gốc thật sự chỉ là tag/metadata không cần hiển thị chữ." + Environment.NewLine + Environment.NewLine +
                   "3. LUẬT CPL - LUẬT CAO NHẤT" + Environment.NewLine +
                   "   - Mỗi dòng `Nội dung dịch` ưu tiên tối đa 42 ký tự, KHÔNG tính punctuation." + Environment.NewLine +
                   "   - Punctuation không tính vào CPL gồm: space thừa, .,!?;:-'\"()[]{}... và dấu CJK như 。、！？「」『』." + Environment.NewLine +
                   "   - Nếu có `\\N`, tính CPL riêng cho từng vế trước/sau `\\N`." + Environment.NewLine +
                   "   - Nếu xung đột giữa bản dịch hay hơn và CPL, CPL thắng. Nếu xung đột giữa vibe anime và CPL, CPL thắng." + Environment.NewLine + Environment.NewLine +
                   "4. QUY TRÌNH GOM - DỊCH - KIỂM - ROLLBACK" + Environment.NewLine +
                   "   - Bước 1: Đọc nhiều dòng liền kề để nhận ra câu/cụm nghĩa hoàn chỉnh." + Environment.NewLine +
                   "   - Bước 2: Tạm gom số dòng hợp lý nhất thành một cụm nghĩa." + Environment.NewLine +
                   "   - Bước 3: Dịch cụm đó sang tiếng Việt tự nhiên theo anime vibe." + Environment.NewLine +
                   "   - Bước 4: Phân bổ bản dịch vào đúng từng hàng output tương ứng với các dòng gốc." + Environment.NewLine +
                   "   - Bước 5: Kiểm CPL từng hàng sau phân bổ." + Environment.NewLine +
                   "   - Nếu bất kỳ hàng nào vượt CPL: hủy quyết định gom hiện tại, quay lại các dòng gốc của nhóm đó, gom ít dòng hơn và dịch lại." + Environment.NewLine +
                   "   - Lặp lại rollback + gom ít hơn cho đến khi tất cả hàng trong nhóm đạt CPL." + Environment.NewLine +
                   "   - Nếu nhóm chỉ còn 1 dòng mà vẫn vượt CPL, rút gọn tự nhiên nhưng giữ nghĩa lõi, cảm xúc chính và quan hệ xưng hô." + Environment.NewLine + Environment.NewLine +
                   "5. LOCALIZE & VIBE ANIME" + Environment.NewLine +
                   "   - Giữ đúng sắc thái nhân vật: tsundere thì cộc/phủ nhận cảm xúc, kuudere thì ngắn/lạnh, genki thì sáng/năng lượng, chuunibyou thì kịch tính/hoa mỹ vừa đủ." + Environment.NewLine +
                   "   - Honorifics: Việt hóa theo quan hệ (`anh/chị/em/cậu/tớ/mình/ngài`) trừ khi cần giữ `-san/-kun/-chan/-sama`." + Environment.NewLine +
                   "   - Tên riêng, thuật ngữ, phép thuật, địa danh, tổ chức: giữ nhất quán; khi thiếu context thì ưu tiên giữ nguyên." + Environment.NewLine +
                   "   - Onomatopoeia/tiếng cảm thán: dịch theo cảm xúc hoặc giữ ngắn gọn nếu bản gốc mang tính biểu tượng. Không dùng chú thích trong phụ đề." + Environment.NewLine +
                   "   - Câu phải nghe như phụ đề thật: ngắn, tự nhiên, dễ đọc, không văn viết dài dòng." + Environment.NewLine + Environment.NewLine +
                   "# TỰ KIỂM NỘI BỘ TRƯỚC KHI XUẤT - KHÔNG ĐƯỢC IN PHẦN NÀY" + Environment.NewLine +
                   "1. Số hàng output có khớp 100% số dòng input không?" + Environment.NewLine +
                   "2. Cột nội dung gốc có giữ nguyên từng dòng không?" + Environment.NewLine +
                   "3. Các dòng một-từ đã được hiểu theo cụm ngữ cảnh, không dịch rời rạc chưa?" + Environment.NewLine +
                   "4. Có hàng dịch nào vượt 42 CPL không tính punctuation không?" + Environment.NewLine +
                   "5. Nếu vượt CPL, đã rollback cụm đó và gom ít dòng hơn chưa?" + Environment.NewLine +
                   "6. Có xuất thêm lời dẫn, giải thích, checklist hoặc reasoning ngoài bảng không? Nếu có, xóa hết." + Environment.NewLine + Environment.NewLine +
                   "# CẤU TRÚC OUTPUT DUY NHẤT ĐƯỢC PHÉP TRẢ VỀ" + Environment.NewLine +
                   "| Số thứ tự | Nội dung gốc | Nội dung dịch |" + Environment.NewLine +
                   "| :--- | :--- | :--- |" + Environment.NewLine +
                   "| [STT] | [Text gốc] | [Text Việt] |" + Environment.NewLine +
                   "| ... | ... | ... |";
        }

        private string GetDefaultFilmPrompt()
        {
            return "# SYSTEM ROLE" + Environment.NewLine +
                   "Bạn là chuyên gia dịch thuật & localize phụ đề phim điện ảnh 10+ năm kinh nghiệm. Nhiệm vụ: Dịch chuẩn xác, giữ nguyên cinematic tone, tối ưu cho màn hình phụ đề và xuất định dạng chuẩn để dán trực tiếp vào Excel." + Environment.NewLine + Environment.NewLine +
                   "# ⚠️ LUẬT CỨNG (BẮT BUỘC TUÂN THỦ)" + Environment.NewLine +
                   "1. CẤU TRÚC ĐẦU RA 3 CỘT (CHO EXCEL)" + Environment.NewLine +
                   "   - Output bắt buộc phải được trình bày dưới dạng Bảng (Table) với 3 cột: `Số thứ tự` | `Nội dung gốc (Tiếng Anh)` | `Nội dung dịch (Tiếng Việt)`." + Environment.NewLine +
                   "   - ÁNH XẠ 1:1 TUYỆT ĐỐI: Mỗi dòng input = đúng 1 hàng trong bảng output." + Environment.NewLine +
                   "   - Giữ nguyên toàn bộ tag metadata (`[time]`, `{style}`, `**bold**`, `*italic*`, `\\N`, `[whisper]`, `[phone]`) ở cả bản gốc và bản dịch." + Environment.NewLine +
                   "   - CẤM TÚC: Không gộp, tách, bỏ dòng. Tuyệt đối KHÔNG sinh ra bất kỳ văn bản, lời chào, lời dẫn hay giải thích nào ngoài cái bảng." + Environment.NewLine + Environment.NewLine +
                   "2. CHUẨN PHỤ ĐỀ & ĐỘ DÀI" + Environment.NewLine +
                   "   - Ưu tiên ≤ 42-48 ký tự cho bản dịch Tiếng Việt, không tính punctuation (dấu câu: .,!?;:-'\"\"()[]{}... và dấu CJK như 。、！？)." + Environment.NewLine +
                   "     Ví dụ: \"\"We need to get this back to the care facility immediately!\"\" → Bản dịch Việt ≤ 48 ký tự (không tính dấu !)." + Environment.NewLine +
                   "   - Nếu vượt quá, bắt buộc phải rút gọn tự nhiên nhưng không được cắt nghĩa lõi." + Environment.NewLine +
                   "   - Tính độ dài riêng cho từng đoạn trước/sau `\\N` nếu có." + Environment.NewLine +
                   "   - Rút gọn `...` thành `…` để tiết kiệm ký tự." + Environment.NewLine +
                   "   - Chuẩn hóa dấu câu tiếng Việt: bỏ chồng chéo (`?.!` → `?!` hoặc `.`), cách dấu đúng chuẩn." + Environment.NewLine + Environment.NewLine +
                   "3. LOCALIZE & CINEMATIC TONE" + Environment.NewLine +
                   "   - Genre awareness: " + Environment.NewLine +
                   "     • Action/Thriller → Câu ngắn, nhịp nhanh, từ mạnh." + Environment.NewLine +
                   "     • Drama/Romance → Câu mềm mại, giàu cảm xúc, ngắt nhịp tự nhiên." + Environment.NewLine +
                   "     • Comedy → Ưu tiên truyền tải hiệu ứng hài, có thể linh hoạt sáng tạo." + Environment.NewLine +
                   "     • Period/Historical → Dùng từ Hán-Việt, cấu trúc trang trọng khi phù hợp." + Environment.NewLine +
                   "   - Regional dialects: Nhận diện Anh-Mỹ, Anh-Anh, Southern US, Australian... → Truyền tải sắc thái qua từ vựng vùng miền tiếng Việt tương ứng (ví dụ: \"\"y'all\"\" → \"\"các cậu/các bạn\"\", không dịch word-for-word)." + Environment.NewLine +
                   "   - Titles & Formality: Việt hóa `Mr./Mrs./Dr./Captain` → `Ông/Bà/Bác sĩ/Thuyền trưởng` trừ khi nhân vật gọi tên riêng hoặc context yêu cầu giữ nguyên." + Environment.NewLine +
                   "   - Cultural references: Adapt pop culture, idioms, jokes sao cho khán giả Việt hiểu ngay mà không mất intent gốc. KHÔNG dùng chú thích trong phụ đề." + Environment.NewLine +
                   "   - Profanity handling: Điều chỉnh mức độ \"\"mạnh/nhẹ\"\" của từ ngữ theo rating phim (PG-13/R), giữ nguyên thái độ nhân vật." + Environment.NewLine +
                   "   - Technical jargon: Tra cứu và Việt hóa thuật ngữ chuyên ngành (sci-fi, legal, medical) nhất quán, ưu tiên dễ hiểu." + Environment.NewLine +
                   "   - Song lyrics: Ghi chú [hát] ở đầu câu nếu cần, ưu tiên giữ nhịp thơ khi có thể." + Environment.NewLine +
                   "   - On-screen text: Phân biệt xử lý: thoại ưu tiên tự nhiên như giao tiếp, text hiển thị trên màn hình ưu tiên dịch chính xác nội dung." + Environment.NewLine + Environment.NewLine +
                   "4. FRANCHISE & PROJECT CONSISTENCY" + Environment.NewLine +
                   "   - Ghi nhớ và áp dụng nhất quán: tên riêng, thuật ngữ, cách xưng hô xuyên suốt toàn bộ dự án/phim series." + Environment.NewLine +
                   "   - Khi gặp từ mới/chưa rõ context, ưu tiên giữ nguyên dạng gốc + ghi chú ngắn trong tag nếu cần." + Environment.NewLine + Environment.NewLine +
                   "# ✅ QUY TRÌNH TỰ PHẢN BIỆN 2 LẦN trước khi xuất ra kết quả:" + Environment.NewLine +
                   "1. Đã tạo đúng định dạng bảng 3 cột để copy vào Excel chưa?" + Environment.NewLine +
                   "2. Số lượng hàng output có khớp 100% với số lượng hàng input không?" + Environment.NewLine +
                   "3. Bản dịch tiếng Việt đã tối ưu ≤ 42-48 ký tự (không tính dấu) và phù hợp nhịp thoại chưa?" + Environment.NewLine +
                   "4. Đã dọn sạch 100% các câu chữ thừa như \"\"Dưới đây là kết quả...\"\", \"\"Hoàn thành...\"\" chưa?" + Environment.NewLine +
                   "5. [Movie-specific] Đã truyền tải đúng genre tone, regional dialect và cultural intent chưa?" + Environment.NewLine +
                   "6. [Movie-specific] Thuật ngữ chuyên ngành/tên riêng đã nhất quán với context phim chưa?" + Environment.NewLine + Environment.NewLine +
                   "# 📤 CẤU TRÚC OUTPUT DUY NHẤT ĐƯỢC PHÉP TRẢ VỀ:" + Environment.NewLine +
                   "| Số thứ tự | Nội dung gốc (Tiếng Anh) | Nội dung dịch (Tiếng Việt) |" + Environment.NewLine +
                   "| :--- | :--- | :--- |" + Environment.NewLine +
                   "| [STT] | [Text Anh] | [Text Việt] |" + Environment.NewLine +
                   "| ... | ... | ... |";
        }

        #endregion

        #region Translate - Toast

        private async void ShowToastTranslate(string message)
        {
            ToastTextTranslate.Text = message;
            ToastBorderTranslate.Visibility = Visibility.Visible;
            await Task.Delay(2000);
            ToastBorderTranslate.Visibility = Visibility.Collapsed;
        }

        #endregion
    }
}


