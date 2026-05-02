using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Subtitle_draft_GMTPC.Models;
using Subtitle_draft_GMTPC.Services;

namespace Subtitle_draft_GMTPC
{
    public partial class MainWindow : Window
    {
        #region Translate - Fields

        private enum TranslateProfileGroup
        {
            Sentence,
            OneWord
        }

        private sealed class TranslateTabContext
        {
            public TranslateProfileGroup Group;
            public TextBox InputBox;
            public TextBlock InputFormatTextBlock;
            public TextBox PromptBox;
            public TextBlock StatusTextBlock;
            public Border ToastBorder;
            public TextBlock ToastText;
            public Button[] PromptButtons;
            public List<SubtitleLine> Lines = new List<SubtitleLine>();
            public SubtitleFormat Format = SubtitleFormat.Unknown;
            public bool IsUpdating;
            public bool IsPromptLoading;
            public int SelectedPromptId = 1;
        }

        private static readonly HttpClient _promptHttpClient = CreatePromptHttpClient();

        private TranslateTabContext _sentenceTranslateTab;
        private TranslateTabContext _oneWordTranslateTab;

        private const string SentencePrompt1DefaultUrl = "https://raw.githubusercontent.com/ghostminhtoan/Subtitle-draft-GMTPC/master/Prompt/1.%20Anime.md";
        private const string SentencePrompt2DefaultUrl = "https://raw.githubusercontent.com/ghostminhtoan/Subtitle-draft-GMTPC/master/Prompt/2.%20Film.md";
        private const string OneWordPrompt1DefaultUrl = "https://raw.githubusercontent.com/ghostminhtoan/Subtitle-draft-GMTPC/master/Prompt/3.%20Anime%20one%20word.md";

        #endregion

        #region Translate - Event Handlers

        private void TxtTranslateSentenceInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            HandleTranslateInputChanged(GetSentenceTranslateContext());
        }

        private void TxtTranslateOneWordInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            HandleTranslateInputChanged(GetOneWordTranslateContext());
        }

        private void TxtTranslateSentencePrompt_LostFocus(object sender, RoutedEventArgs e)
        {
            SavePromptEditorContent(GetSentenceTranslateContext());
        }

        private void TxtTranslateOneWordPrompt_LostFocus(object sender, RoutedEventArgs e)
        {
            SavePromptEditorContent(GetOneWordTranslateContext());
        }

        private void BtnOpenQwenSentence_Click(object sender, RoutedEventArgs e)
        {
            OpenQwen(GetSentenceTranslateContext());
        }

        private void BtnOpenQwenOneWord_Click(object sender, RoutedEventArgs e)
        {
            OpenQwen(GetOneWordTranslateContext());
        }

        private void BtnCopyForPasteSentence_Click(object sender, RoutedEventArgs e)
        {
            CopyPromptAndInputToClipboard(GetSentenceTranslateContext());
        }

        private void BtnCopyForPasteOneWord_Click(object sender, RoutedEventArgs e)
        {
            CopyPromptAndInputToClipboard(GetOneWordTranslateContext());
        }

        /// <summary>
        /// Khi click vào nút prompt -> tải nội dung prompt vào ô editor của tab tương ứng.
        /// </summary>
        private async void BtnSentencePrompt_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            if (btn == null) return;

            int promptId;
            if (!int.TryParse(btn.Tag == null ? null : btn.Tag.ToString(), out promptId))
            {
                return;
            }

            await LoadPromptForSelectionAsync(GetSentenceTranslateContext(), promptId, promptId <= 2, true);
        }

        private async void BtnOneWordPrompt_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            if (btn == null) return;

            int promptId;
            if (!int.TryParse(btn.Tag == null ? null : btn.Tag.ToString(), out promptId))
            {
                return;
            }

            await LoadPromptForSelectionAsync(GetOneWordTranslateContext(), promptId, promptId == 1, true);
        }

        /// <summary>
        /// Load the default source for the currently selected prompt.
        /// </summary>
        private async void BtnLoadDefaultSentencePrompt_Click(object sender, RoutedEventArgs e)
        {
            var context = GetSentenceTranslateContext();
            await LoadPromptForSelectionAsync(context, context.SelectedPromptId, true, false);
        }

        private async void BtnLoadDefaultOneWordPrompt_Click(object sender, RoutedEventArgs e)
        {
            var context = GetOneWordTranslateContext();
            await LoadPromptForSelectionAsync(context, context.SelectedPromptId, true, false);
        }

        /// <summary>
        /// Rename the currently selected prompt.
        /// </summary>
        private void BtnRenameSentencePrompt_Click(object sender, RoutedEventArgs e)
        {
            RenameSelectedPrompt(GetSentenceTranslateContext());
        }

        private void BtnRenameOneWordPrompt_Click(object sender, RoutedEventArgs e)
        {
            RenameSelectedPrompt(GetOneWordTranslateContext());
        }

        /// <summary>
        /// Save the current editor content into the selected prompt slot.
        /// </summary>
        private void BtnSaveSentencePrompts_Click(object sender, RoutedEventArgs e)
        {
            SavePromptEditorContent(GetSentenceTranslateContext());
        }

        private void BtnSaveOneWordPrompts_Click(object sender, RoutedEventArgs e)
        {
            SavePromptEditorContent(GetOneWordTranslateContext());
        }

        /// <summary>
        /// Load the saved local content for the currently selected prompt.
        /// </summary>
        private void BtnLoadSentencePrompts_Click(object sender, RoutedEventArgs e)
        {
            LoadPromptEditorContent(GetSentenceTranslateContext());
        }

        private void BtnLoadOneWordPrompts_Click(object sender, RoutedEventArgs e)
        {
            LoadPromptEditorContent(GetOneWordTranslateContext());
        }

        #endregion

        #region Translate - Prompt Management

        private void InitializeDefaultPrompts()
        {
            AppSettings.MigrateLegacyTranslatePrompts();

            EnsureDefaultPrompt(GetSentenceTranslateContext(), 1, "Anime", GetDefaultAnimePrompt());
            EnsureDefaultPrompt(GetSentenceTranslateContext(), 2, "Film", GetDefaultFilmPrompt());
            EnsureDefaultPrompt(GetSentenceTranslateContext(), 3, null, null);
            EnsureDefaultPrompt(GetSentenceTranslateContext(), 4, null, null);
            EnsureDefaultPrompt(GetSentenceTranslateContext(), 5, null, null);

            EnsureDefaultPrompt(GetOneWordTranslateContext(), 1, "Anime One Word", GetDefaultAnimeOneWordPrompt());
            EnsureDefaultPrompt(GetOneWordTranslateContext(), 2, null, null);
            EnsureDefaultPrompt(GetOneWordTranslateContext(), 3, null, null);
            EnsureDefaultPrompt(GetOneWordTranslateContext(), 4, null, null);
            EnsureDefaultPrompt(GetOneWordTranslateContext(), 5, null, null);

            AppSettings.Save();
        }

        private void LoadAllPrompts()
        {
            _ = LoadAllPromptsAsync();
        }

        private async Task LoadAllPromptsAsync()
        {
            var sentenceContext = GetSentenceTranslateContext();
            var oneWordContext = GetOneWordTranslateContext();

            UpdatePromptButtonDisplay(sentenceContext);
            UpdatePromptButtonDisplay(oneWordContext);

            await LoadPromptForSelectionAsync(sentenceContext, sentenceContext.SelectedPromptId, sentenceContext.SelectedPromptId <= 2, false);
            await LoadPromptForSelectionAsync(oneWordContext, oneWordContext.SelectedPromptId, oneWordContext.SelectedPromptId == 1, false);
        }

        private void SaveAllPrompts()
        {
            SavePromptEditorContent(GetSentenceTranslateContext());
            SavePromptEditorContent(GetOneWordTranslateContext());
        }

        private TranslateTabContext GetSentenceTranslateContext()
        {
            EnsureTranslateTabContexts();
            return _sentenceTranslateTab;
        }

        private TranslateTabContext GetOneWordTranslateContext()
        {
            EnsureTranslateTabContexts();
            return _oneWordTranslateTab;
        }

        private void EnsureTranslateTabContexts()
        {
            if (_sentenceTranslateTab != null && _oneWordTranslateTab != null)
            {
                return;
            }

            _sentenceTranslateTab = new TranslateTabContext
            {
                Group = TranslateProfileGroup.Sentence,
                InputBox = TxtTranslateSentenceInput,
                InputFormatTextBlock = TxtTranslateSentenceInputFormat,
                PromptBox = TxtTranslateSentencePrompt,
                StatusTextBlock = TxtTranslateSentenceStatus,
                ToastBorder = ToastBorderTranslateSentence,
                ToastText = ToastTextTranslateSentence,
                PromptButtons = new[] { BtnSentencePrompt1, BtnSentencePrompt2, BtnSentencePrompt3, BtnSentencePrompt4, BtnSentencePrompt5 }
            };

            _oneWordTranslateTab = new TranslateTabContext
            {
                Group = TranslateProfileGroup.OneWord,
                InputBox = TxtTranslateOneWordInput,
                InputFormatTextBlock = TxtTranslateOneWordInputFormat,
                PromptBox = TxtTranslateOneWordPrompt,
                StatusTextBlock = TxtTranslateOneWordStatus,
                ToastBorder = ToastBorderTranslateOneWord,
                ToastText = ToastTextTranslateOneWord,
                PromptButtons = new[] { BtnOneWordPrompt1, BtnOneWordPrompt2, BtnOneWordPrompt3, BtnOneWordPrompt4, BtnOneWordPrompt5 }
            };
        }

        private void HandleTranslateInputChanged(TranslateTabContext context)
        {
            if (context == null || context.IsUpdating)
            {
                return;
            }

            try
            {
                context.IsUpdating = true;
                var content = SubtitleParser.SanitizeContent(context.InputBox.Text);
                if (string.IsNullOrWhiteSpace(content))
                {
                    context.Lines.Clear();
                    context.Format = SubtitleFormat.Unknown;
                    context.InputFormatTextBlock.Text = "";
                    return;
                }

                context.Format = SubtitleParser.DetectFormat(content);
                context.Lines = SubtitleParser.Parse(content);
                context.InputFormatTextBlock.Text = string.Format("({0} - {1} dòng)", context.Format.ToString().ToUpper(), context.Lines.Count);
            }
            catch (Exception ex)
            {
                context.InputFormatTextBlock.Text = string.Format("(Lỗi: {0})", ex.Message);
            }
            finally
            {
                context.IsUpdating = false;
            }
        }

        private void OpenQwen(TranslateTabContext context)
        {
            try
            {
                System.Diagnostics.Process.Start("https://chat.qwen.ai/");
                context.StatusTextBlock.Text = "✅ Đã mở chat.qwen.ai trên trình duyệt!";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CopyPromptAndInputToClipboard(TranslateTabContext context)
        {
            var inputText = context.InputBox.Text;
            if (string.IsNullOrWhiteSpace(inputText))
            {
                context.StatusTextBlock.Text = "⚠️ Vui lòng nhập phụ đề vào Panel 1!";
                return;
            }

            var prompt = context.PromptBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(prompt))
            {
                context.StatusTextBlock.Text = "⚠️ Vui lòng nhập prompt!";
                return;
            }

            SavePromptEditorContent(context);
            Clipboard.SetText(prompt + Environment.NewLine + Environment.NewLine + inputText);
            context.StatusTextBlock.Text = "📋 Đã copy Prompt + Subtitle vào clipboard!";
        }

        private void RenameSelectedPrompt(TranslateTabContext context)
        {
            var currentPromptId = context.SelectedPromptId;
            var currentName = GetPromptNameById(context.Group, currentPromptId);
            var newName = Microsoft.VisualBasic.Interaction.InputBox(
                string.Format("Đổi tên cho Prompt {0} (hiện tại: {1}):", currentPromptId, currentName),
                "Rename Prompt", currentName);

            if (!string.IsNullOrWhiteSpace(newName))
            {
                SetPromptNameById(context.Group, currentPromptId, newName.Trim());
                UpdatePromptButtonDisplay(context);
                AppSettings.Save();
                context.StatusTextBlock.Text = string.Format("✅ Đã rename Prompt {0} thành: {1}", currentPromptId, newName.Trim());
            }
        }

        private void SavePromptEditorContent(TranslateTabContext context)
        {
            var promptId = context.SelectedPromptId;
            var currentContent = context.PromptBox.Text.Trim();

            SetPromptContentById(context.Group, promptId, currentContent);

            if (string.IsNullOrWhiteSpace(currentContent))
            {
                context.StatusTextBlock.Text = string.Format("🧹 Đã clear Prompt {0}!", promptId);
            }
            else
            {
                context.StatusTextBlock.Text = string.Format("💾 Đã save nội dung vào Prompt {0}: {1}", promptId, GetPromptNameById(context.Group, promptId));
            }

            AppSettings.Save();
            UpdatePromptButtonDisplay(context);
        }

        private void LoadPromptEditorContent(TranslateTabContext context)
        {
            var promptId = context.SelectedPromptId;
            var content = GetPromptContentById(context.Group, promptId);
            context.PromptBox.Text = content ?? string.Empty;
            context.StatusTextBlock.Text = string.IsNullOrWhiteSpace(content)
                ? string.Format("🧹 Prompt {0} đang trống, đã clear ô nhập!", promptId)
                : string.Format("📂 Đã load Prompt {0}: {1}", promptId, GetPromptNameById(context.Group, promptId));
            UpdatePromptButtonDisplay(context);
        }

        private void UpdatePromptButtonDisplay(TranslateTabContext context)
        {
            for (int i = 1; i <= context.PromptButtons.Length; i++)
            {
                var button = context.PromptButtons[i - 1];
                if (button != null)
                {
                    button.Content = FormatPromptButtonContent(context, i);
                }
            }
        }

        private string FormatPromptButtonContent(TranslateTabContext context, int id)
        {
            var label = string.Format("{0}. {1}", id, GetPromptNameById(context.Group, id));
            return id == context.SelectedPromptId ? "▶ " + label : label;
        }

        private void EnsureDefaultPrompt(TranslateTabContext context, int id, string preferredName, string preferredContent)
        {
            var currentName = GetPromptNameById(context.Group, id);
            if (string.IsNullOrWhiteSpace(currentName) || currentName == DefaultPromptName(id))
            {
                if (!string.IsNullOrWhiteSpace(preferredName))
                {
                    SetPromptNameById(context.Group, id, preferredName);
                }
                else if (string.IsNullOrWhiteSpace(currentName))
                {
                    SetPromptNameById(context.Group, id, DefaultPromptName(id));
                }

                if (preferredContent != null)
                {
                    SetPromptContentById(context.Group, id, preferredContent);
                }
                else if (string.IsNullOrWhiteSpace(GetPromptContentById(context.Group, id)))
                {
                    SetPromptContentById(context.Group, id, string.Empty);
                }
            }
            else if (string.IsNullOrWhiteSpace(currentName))
            {
                SetPromptNameById(context.Group, id, DefaultPromptName(id));
            }
        }

        private string DefaultPromptName(int id)
        {
            return string.Format("Prompt {0}", id);
        }

        private string GetPromptSettingKey(TranslateProfileGroup group, bool isName, int id)
        {
            var prefix = group == TranslateProfileGroup.Sentence ? "SentencePrompt" : "OneWordPrompt";
            return prefix + (isName ? "Name" : "Content") + id;
        }

        private string GetPromptNameById(TranslateProfileGroup group, int id)
        {
            return AppSettings.GetString(GetPromptSettingKey(group, true, id), DefaultPromptName(id));
        }

        private string GetPromptContentById(TranslateProfileGroup group, int id)
        {
            return AppSettings.GetString(GetPromptSettingKey(group, false, id), string.Empty);
        }

        private void SetPromptNameById(TranslateProfileGroup group, int id, string name)
        {
            AppSettings.SetString(GetPromptSettingKey(group, true, id), name);
        }

        private void SetPromptContentById(TranslateProfileGroup group, int id, string content)
        {
            AppSettings.SetString(GetPromptSettingKey(group, false, id), content);
        }

        private string GetPromptDefaultUrlById(TranslateProfileGroup group, int id)
        {
            if (group == TranslateProfileGroup.Sentence)
            {
                if (id == 1) return SentencePrompt1DefaultUrl;
                if (id == 2) return SentencePrompt2DefaultUrl;
                return null;
            }

            if (group == TranslateProfileGroup.OneWord && id == 1)
            {
                return OneWordPrompt1DefaultUrl;
            }

            return null;
        }

        private static HttpClient CreatePromptHttpClient()
        {
            var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("SubtitleDraftGMTPC/1.0");
            return client;
        }

        private async Task<string> LoadPromptDefaultContentAsync(TranslateProfileGroup group, int id)
        {
            var url = GetPromptDefaultUrlById(group, id);
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

        private async Task LoadPromptForSelectionAsync(TranslateTabContext context, int promptId, bool useDefaultSource, bool updateSelection)
        {
            if (context.IsPromptLoading)
            {
                return;
            }

            try
            {
                context.IsPromptLoading = true;
                if (updateSelection)
                {
                    context.SelectedPromptId = promptId;
                }

                UpdatePromptButtonDisplay(context);

                string content = null;
                if (useDefaultSource)
                {
                    content = await LoadPromptDefaultContentAsync(context.Group, promptId);
                    if (!string.IsNullOrWhiteSpace(content))
                    {
                        context.PromptBox.Text = content;
                        context.StatusTextBlock.Text = string.Format("Loaded default Prompt {0}: {1}", promptId, GetPromptNameById(context.Group, promptId));
                        return;
                    }
                }

                content = GetPromptContentById(context.Group, promptId);
                context.PromptBox.Text = content ?? string.Empty;
                context.StatusTextBlock.Text = string.IsNullOrWhiteSpace(content)
                    ? string.Format("Prompt {0} is empty.", promptId)
                    : string.Format("Loaded Prompt {0}: {1}", promptId, GetPromptNameById(context.Group, promptId));
            }
            catch (Exception ex)
            {
                MessageBox.Show("Khong the tai prompt: " + ex.Message, "Loi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                context.IsPromptLoading = false;
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

        private async Task ShowToastTranslate(TranslateTabContext context, string message)
        {
            if (context == null || context.ToastBorder == null || context.ToastText == null)
            {
                return;
            }

            context.ToastText.Text = message;
            context.ToastBorder.Visibility = Visibility.Visible;
            await Task.Delay(2000);
            context.ToastBorder.Visibility = Visibility.Collapsed;
        }

        private async void ShowToastSentenceTranslate(string message)
        {
            await ShowToastTranslate(GetSentenceTranslateContext(), message);
        }

        private async void ShowToastOneWordTranslate(string message)
        {
            await ShowToastTranslate(GetOneWordTranslateContext(), message);
        }

        #endregion
    }
}
