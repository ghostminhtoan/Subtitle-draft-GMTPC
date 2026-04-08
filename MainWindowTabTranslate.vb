Imports Subtitle_draft_GMTPC.Models
Imports Subtitle_draft_GMTPC.Services

Partial Class MainWindow

#Region "Translate - Fields"

    Private _translateLines As New List(Of SubtitleLine)()
    Private _translateFormat As SubtitleFormat = SubtitleFormat.Unknown
    Private _isTranslateUpdating As Boolean = False

#End Region

#Region "Translate - Event Handlers"

    Private Sub TxtTranslateInput_TextChanged(sender As Object, e As TextChangedEventArgs)
        If _isTranslateUpdating Then Return
        Try
            _isTranslateUpdating = True
            Dim content = SubtitleParser.SanitizeContent(TxtTranslateInput.Text)
            If String.IsNullOrWhiteSpace(content) Then
                _translateLines.Clear()
                _translateFormat = SubtitleFormat.Unknown
                TxtTranslateInputFormat.Text = ""
                Return
            End If
            _translateFormat = SubtitleParser.DetectFormat(content)
            _translateLines = SubtitleParser.Parse(content)
            TxtTranslateInputFormat.Text = String.Format("({0} - {1} dòng)", _translateFormat.ToString().ToUpper(), _translateLines.Count)
        Catch ex As Exception
            TxtTranslateInputFormat.Text = String.Format("(Lỗi: {0})", ex.Message)
        Finally
            _isTranslateUpdating = False
        End Try
    End Sub

    Private Sub TxtPrompt_LostFocus(sender As Object, e As RoutedEventArgs)
        SaveSettings()
    End Sub

    Private Sub BtnOpenQwen_Click(sender As Object, e As RoutedEventArgs)
        Try
            System.Diagnostics.Process.Start("https://chat.qwen.ai/")
            TxtTranslateStatus.Text = "✅ Đã mở chat.qwen.ai trên trình duyệt!"
        Catch ex As Exception
            MessageBox.Show("Lỗi: " & ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error)
        End Try
    End Sub

    Private Sub BtnCopyForPaste_Click(sender As Object, e As RoutedEventArgs)
        Dim inputText = TxtTranslateInput.Text
        If String.IsNullOrWhiteSpace(inputText) Then
            TxtTranslateStatus.Text = "⚠️ Vui lòng nhập phụ đề vào Panel 1!"
            Return
        End If
        Dim prompt = TxtPrompt.Text.Trim()
        If String.IsNullOrWhiteSpace(prompt) Then
            TxtTranslateStatus.Text = "⚠️ Vui lòng nhập prompt!"
            Return
        End If
        SaveSettings()
        Clipboard.SetText(prompt & vbCrLf & vbCrLf & inputText)
        TxtTranslateStatus.Text = "📋 Đã copy Prompt + Subtitle vào clipboard!"
    End Sub

    ''' <summary>
    ''' Khi click vào nút prompt → dán nội dung prompt vào TxtPrompt
    ''' </summary>
    Private Sub BtnPrompt_Click(sender As Object, e As RoutedEventArgs)
        Dim btn = TryCast(sender, Button)
        If btn Is Nothing Then Return

        Dim promptId As Integer = Integer.Parse(btn.Tag.ToString())
        Dim prompt = GetPromptById(promptId)

        If Not String.IsNullOrWhiteSpace(prompt.Content) Then
            TxtPrompt.Text = prompt.Content
            TxtTranslateStatus.Text = String.Format("📋 Đã load prompt: {0}", prompt.DisplayName)
        Else
            TxtTranslateStatus.Text = String.Format("⚠️ Prompt {0} chưa có nội dung!", promptId)
        End If
    End Sub

    ''' <summary>
    ''' Nút Rename: Hỏi chọn prompt cần rename, sau đó hỏi tên mới
    ''' </summary>
    Private Sub BtnRenamePrompt_Click(sender As Object, e As RoutedEventArgs)
        ' Luôn hỏi chọn prompt cần rename trước
        Dim input = Microsoft.VisualBasic.InputBox("Nhập số thứ tự prompt cần rename (1-5):" & vbCrLf & vbCrLf &
                                                   "1. " & GetPromptNameById(1) & vbCrLf &
                                                   "2. " & GetPromptNameById(2) & vbCrLf &
                                                   "3. " & GetPromptNameById(3) & vbCrLf &
                                                   "4. " & GetPromptNameById(4) & vbCrLf &
                                                   "5. " & GetPromptNameById(5), "Rename Prompt", "1")

        Dim currentPromptId As Integer = 0
        If Integer.TryParse(input, currentPromptId) AndAlso currentPromptId >= 1 AndAlso currentPromptId <= 5 Then
            ' OK
        Else
            TxtTranslateStatus.Text = "⚠️ Số thứ tự không hợp lệ!"
            Return
        End If

        Dim currentName = GetPromptNameById(currentPromptId)
        Dim newName = Microsoft.VisualBasic.InputBox(String.Format("Đổi tên cho Prompt {0} (hiện tại: {1}):", currentPromptId, currentName), "Rename Prompt", currentName)

        If Not String.IsNullOrWhiteSpace(newName) Then
            SetPromptNameById(currentPromptId, newName.Trim())
            UpdatePromptButtonDisplay()
            SaveSettings()
            TxtTranslateStatus.Text = String.Format("✅ Đã rename Prompt {0} thành: {1}", currentPromptId, newName.Trim())
        End If
    End Sub

    ''' <summary>
    ''' Nút Save Prompts: Hỏi chọn prompt cần save, sau đó lưu nội dung hiện tại vào prompt đó
    ''' </summary>
    Private Sub BtnSavePrompts_Click(sender As Object, e As RoutedEventArgs)
        ' Hỏi chọn prompt cần save vào
        Dim input = Microsoft.VisualBasic.InputBox("Nhập số thứ tự prompt cần save (1-5):" & vbCrLf & vbCrLf &
                                                   "1. " & GetPromptNameById(1) & vbCrLf &
                                                   "2. " & GetPromptNameById(2) & vbCrLf &
                                                   "3. " & GetPromptNameById(3) & vbCrLf &
                                                   "4. " & GetPromptNameById(4) & vbCrLf &
                                                   "5. " & GetPromptNameById(5), "Save Prompt", "1")

        Dim promptId As Integer = 0
        If Integer.TryParse(input, promptId) AndAlso promptId >= 1 AndAlso promptId <= 5 Then
            ' Lưu nội dung hiện tại trong TxtPrompt vào prompt đã chọn
            Dim currentContent = TxtPrompt.Text.Trim()
            If String.IsNullOrWhiteSpace(currentContent) Then
                ' Prompt trống thì clear (xóa nội dung cũ)
                SetPromptContentById(promptId, "")
                TxtTranslateStatus.Text = String.Format("🧹 Đã clear Prompt {0}!", promptId)
            Else
                SetPromptContentById(promptId, currentContent)
                TxtTranslateStatus.Text = String.Format("💾 Đã save nội dung vào Prompt {0}: {1}", promptId, GetPromptNameById(promptId))
            End If
            My.Settings.Save()
        Else
            TxtTranslateStatus.Text = "⚠️ Số thứ tự không hợp lệ!"
            Return
        End If
    End Sub

    ''' <summary>
    ''' Nút Load Prompts: Hỏi chọn prompt cần load từ Settings
    ''' </summary>
    Private Sub BtnLoadPrompts_Click(sender As Object, e As RoutedEventArgs)
        ' Hỏi chọn prompt cần load
        Dim input = Microsoft.VisualBasic.InputBox("Nhập số thứ tự prompt cần load (1-5):" & vbCrLf & vbCrLf &
                                                   "1. " & GetPromptNameById(1) & vbCrLf &
                                                   "2. " & GetPromptNameById(2) & vbCrLf &
                                                   "3. " & GetPromptNameById(3) & vbCrLf &
                                                   "4. " & GetPromptNameById(4) & vbCrLf &
                                                   "5. " & GetPromptNameById(5), "Load Prompt", "1")

        Dim promptId As Integer = 0
        If Integer.TryParse(input, promptId) AndAlso promptId >= 1 AndAlso promptId <= 5 Then
            Dim content = GetPromptContentById(promptId)
            If Not String.IsNullOrWhiteSpace(content) Then
                TxtPrompt.Text = content
                TxtTranslateStatus.Text = String.Format("📋 Đã load Prompt {0}: {1}", promptId, GetPromptNameById(promptId))
            Else
                ' Prompt trống thì clear TxtPrompt
                TxtPrompt.Text = ""
                TxtTranslateStatus.Text = String.Format("🧹 Prompt {0} đang trống, đã clear ô nhập!", promptId)
            End If
            ' Cập nhật lại display buttons
            UpdatePromptButtonDisplay()
        Else
            TxtTranslateStatus.Text = "⚠️ Số thứ tự không hợp lệ!"
            Return
        End If
    End Sub

#End Region

#Region "Translate - Prompt Management"

    ''' <summary>
    ''' Lấy thông tin prompt theo ID (1-5)
    ''' </summary>
    Private Function GetPromptById(id As Integer) As PromptItem
        Dim name = GetPromptNameById(id)
        Dim content = GetPromptContentById(id)
        Return New PromptItem(id, name, content)
    End Function

    Private Function GetPromptNameById(id As Integer) As String
        Select Case id
            Case 1 : Return My.Settings.PromptName1
            Case 2 : Return My.Settings.PromptName2
            Case 3 : Return My.Settings.PromptName3
            Case 4 : Return My.Settings.PromptName4
            Case 5 : Return My.Settings.PromptName5
            Case Else : Return String.Format("Prompt {0}", id)
        End Select
    End Function

    Private Function GetPromptContentById(id As Integer) As String
        Select Case id
            Case 1 : Return My.Settings.PromptContent1
            Case 2 : Return My.Settings.PromptContent2
            Case 3 : Return My.Settings.PromptContent3
            Case 4 : Return My.Settings.PromptContent4
            Case 5 : Return My.Settings.PromptContent5
            Case Else : Return ""
        End Select
    End Function

    Private Sub SetPromptNameById(id As Integer, name As String)
        Select Case id
            Case 1 : My.Settings.PromptName1 = name
            Case 2 : My.Settings.PromptName2 = name
            Case 3 : My.Settings.PromptName3 = name
            Case 4 : My.Settings.PromptName4 = name
            Case 5 : My.Settings.PromptName5 = name
        End Select
    End Sub

    Private Sub SetPromptContentById(id As Integer, content As String)
        Select Case id
            Case 1 : My.Settings.PromptContent1 = content
            Case 2 : My.Settings.PromptContent2 = content
            Case 3 : My.Settings.PromptContent3 = content
            Case 4 : My.Settings.PromptContent4 = content
            Case 5 : My.Settings.PromptContent5 = content
        End Select
    End Sub

    ''' <summary>
    ''' Cập nhật hiển thị tên trên 5 nút prompt
    ''' </summary>
    Private Sub UpdatePromptButtonDisplay()
        BtnPrompt1.Content = String.Format("{0}. {1}", 1, GetPromptNameById(1))
        BtnPrompt2.Content = String.Format("{0}. {1}", 2, GetPromptNameById(2))
        BtnPrompt3.Content = String.Format("{0}. {1}", 3, GetPromptNameById(3))
        BtnPrompt4.Content = String.Format("{0}. {1}", 4, GetPromptNameById(4))
        BtnPrompt5.Content = String.Format("{0}. {1}", 5, GetPromptNameById(5))
    End Sub

    ''' <summary>
    ''' Tìm ID của prompt đang được hiển thị trong TxtPrompt (nếu khớp content)
    ''' </summary>
    Private Function FindCurrentPromptId() As Integer
        Dim currentContent = TxtPrompt.Text.Trim()
        If String.IsNullOrWhiteSpace(currentContent) Then Return 0

        For id As Integer = 1 To 5
            Dim promptContent = GetPromptContentById(id).Trim()
            If promptContent = currentContent Then
                Return id
            End If
        Next

        Return 0
    End Function

    ''' <summary>
    ''' Lưu toàn bộ 5 prompts vào Settings
    ''' </summary>
    Private Sub SaveAllPrompts()
        ' Lưu content hiện tại trong TxtPrompt vào prompt đang chọn (nếu có)
        Dim currentId = FindCurrentPromptId()
        If currentId > 0 Then
            SetPromptContentById(currentId, TxtPrompt.Text.Trim())
        End If

        My.Settings.Save()
    End Sub

    ''' <summary>
    ''' Load toàn bộ 5 prompts từ Settings và cập nhật UI
    ''' </summary>
    Private Sub LoadAllPrompts()
        UpdatePromptButtonDisplay()

        ' Nếu TxtPrompt đang trống, load prompt đầu tiên có content
        If String.IsNullOrWhiteSpace(TxtPrompt.Text) Then
            For id As Integer = 1 To 5
                Dim content = GetPromptContentById(id)
                If Not String.IsNullOrWhiteSpace(content) Then
                    TxtPrompt.Text = content
                    TxtTranslateStatus.Text = String.Format("📋 Đã load prompt: {0}. {1}", id, GetPromptNameById(id))
                    Exit For
                End If
            Next
        End If
    End Sub

    ''' <summary>
    ''' Khởi tạo prompts mặc định (Anime và Film) nếu chưa có
    ''' </summary>
    Private Sub InitializeDefaultPrompts()
        ' Prompt 1 - Anime
        If String.IsNullOrWhiteSpace(My.Settings.PromptName1) OrElse My.Settings.PromptName1 = "Prompt 1" Then
            My.Settings.PromptName1 = "Anime"
            My.Settings.PromptContent1 = GetDefaultAnimePrompt()
        End If

        ' Prompt 2 - Film
        If String.IsNullOrWhiteSpace(My.Settings.PromptName2) OrElse My.Settings.PromptName2 = "Prompt 2" Then
            My.Settings.PromptName2 = "Film"
            My.Settings.PromptContent2 = GetDefaultFilmPrompt()
        End If

        My.Settings.Save()
    End Sub

    Private Function GetDefaultAnimePrompt() As String
        Return "# SYSTEM ROLE" & vbCrLf & _
               "Bạn là chuyên gia dịch thuật & localize phụ đề anime 10+ năm kinh nghiệm. Nhiệm vụ: Dịch chuẩn xác, giữ nguyên vibe anime, tối ưu cho màn hình phụ đề và xuất định dạng chuẩn để dán trực tiếp vào Excel." & vbCrLf & vbCrLf & _
               "# ⚠️ LUẬT CỨNG (BẮT BUỘC TUÂN THỦ)" & vbCrLf & _
               "1. CẤU TRÚC ĐẦU RA 3 CỘT (CHO EXCEL)" & vbCrLf & _
               "   - Output bắt buộc phải được trình bày dưới dạng Bảng (Table) với 3 cột: `Số thứ tự` | `Nội dung gốc (Tiếng Anh)` | `Nội dung dịch (Tiếng Việt)`. Tiếng Anh bắt buộc giữ nguyên không được dịch, chỉ dịch tiếng Việt thôi." & vbCrLf & _
               "   - ÁNH XẠ 1:1 TUYỆT ĐỐI: Mỗi dòng input = đúng 1 hàng trong bảng output." & vbCrLf & _
               "   - Giữ nguyên toàn bộ tag metadata (`[time]`, `{style}`, `**bold**`, `*italic*`, `\N`) ở cả bản gốc và bản dịch." & vbCrLf & _
               "   - CẤM TÚC: Không gộp, tách, bỏ dòng. Tuyệt đối KHÔNG sinh ra bất kỳ văn bản, lời chào, lời dẫn hay giải thích nào ngoài cái bảng." & vbCrLf & vbCrLf & _
               "2. CHUẨN PHỤ ĐỀ & ĐỘ DÀI" & vbCrLf & _
               "   - Ưu tiên ≤ 42 ký tự cho bản dịch Tiếng Việt, không tính punctuation (dấu câu: .,!?;:-'""()[]{}... và dấu CJK như 。、！？)." & vbCrLf & _
               "     Ví dụ: ""Chúng ta nên nhanh chóng đưa nó trở lại cơ sở chăm sóc!"" → 42 ký tự (không tính dấu !)." & vbCrLf & _
               "   - Nếu vượt quá, bắt buộc phải rút gọn tự nhiên nhưng không được cắt nghĩa lõi." & vbCrLf & _
               "   - Tính độ dài riêng cho từng đoạn trước/sau `\N` nếu có." & vbCrLf & _
               "   - Rút gọn `...` thành `…` để tiết kiệm ký tự." & vbCrLf & _
               "   - Chuẩn hóa dấu câu tiếng Việt: bỏ chồng chéo (`?.!` → `?!` hoặc `.`), cách dấu đúng chuẩn." & vbCrLf & vbCrLf & _
               "3. LOCALIZE & VIBE ANIME" & vbCrLf & _
               "   - Tính cách nhân vật: Tsundere (cộc, phủ nhận cảm xúc), Kuudere (ngắn, lạnh lùng), Genki (năng lượng), Chuunibyou (kỳ ảo, hoa mỹ) → Thể hiện qua từ vựng & nhịp câu." & vbCrLf & _
               "   - Honorifics: Việt hóa theo quan hệ (`anh/chị/em/cậu`) trừ khi là thuật ngữ đặc thù hoặc fan yêu cầu giữ `-san/-kun/-chan/-sama`." & vbCrLf & _
               "   - Tiếng lóng/Onomatopoeia: Dịch nghĩa hoặc giữ nguyên *in nghiêng* nếu mang tính biểu tượng. KHÔNG dùng chú thích trong phụ đề." & vbCrLf & _
               "   - Tên riêng: Giữ nguyên Romaji/Kanji hoặc Việt hóa nhất quán xuyên suốt." & vbCrLf & vbCrLf & _
               "# ✅ QUY TRÌNH TỰ PHẢN BIỆN 2 LẦN trước khi xuất ra kết quả:" & vbCrLf & _
               "1. Đã tạo đúng định dạng bảng 3 cột để copy vào Excel chưa?" & vbCrLf & _
               "2. Số lượng hàng output có khớp 100% với số lượng hàng input không?" & vbCrLf & _
               "3. Bản dịch tiếng Việt đã tối ưu ≤ 42 ký tự (không tính dấu) chưa?" & vbCrLf & _
               "4. Đã dọn sạch 100% các câu chữ thừa như ""Dưới đây là kết quả..."", ""Hoàn thành..."" chưa?" & vbCrLf & vbCrLf & _
               "# 📤 CẤU TRÚC OUTPUT DUY NHẤT ĐƯỢC PHÉP TRẢ VỀ:" & vbCrLf & _
               "| Số thứ tự | Nội dung gốc (Tiếng Anh) | Nội dung dịch (Tiếng Việt) |" & vbCrLf & _
               "| :--- | :--- | :--- |" & vbCrLf & _
               "| [STT] | [Text Anh] | [Text Việt] |" & vbCrLf & _
               "| ... | ... | ... |"
    End Function

    Private Function GetDefaultFilmPrompt() As String
        Return "# SYSTEM ROLE" & vbCrLf & _
               "Bạn là chuyên gia dịch thuật & localize phụ đề phim điện ảnh 10+ năm kinh nghiệm. Nhiệm vụ: Dịch chuẩn xác, giữ nguyên cinematic tone, tối ưu cho màn hình phụ đề và xuất định dạng chuẩn để dán trực tiếp vào Excel." & vbCrLf & vbCrLf & _
               "# ⚠️ LUẬT CỨNG (BẮT BUỘC TUÂN THỦ)" & vbCrLf & _
               "1. CẤU TRÚC ĐẦU RA 3 CỘT (CHO EXCEL)" & vbCrLf & _
               "   - Output bắt buộc phải được trình bày dưới dạng Bảng (Table) với 3 cột: `Số thứ tự` | `Nội dung gốc (Tiếng Anh)` | `Nội dung dịch (Tiếng Việt)`." & vbCrLf & _
               "   - ÁNH XẠ 1:1 TUYỆT ĐỐI: Mỗi dòng input = đúng 1 hàng trong bảng output." & vbCrLf & _
               "   - Giữ nguyên toàn bộ tag metadata (`[time]`, `{style}`, `**bold**`, `*italic*`, `\N`, `[whisper]`, `[phone]`) ở cả bản gốc và bản dịch." & vbCrLf & _
               "   - CẤM TÚC: Không gộp, tách, bỏ dòng. Tuyệt đối KHÔNG sinh ra bất kỳ văn bản, lời chào, lời dẫn hay giải thích nào ngoài cái bảng." & vbCrLf & vbCrLf & _
               "2. CHUẨN PHỤ ĐỀ & ĐỘ DÀI" & vbCrLf & _
               "   - Ưu tiên ≤ 42-48 ký tự cho bản dịch Tiếng Việt, không tính punctuation (dấu câu: .,!?;:-'""()[]{}... và dấu CJK như 。、！？)." & vbCrLf & _
               "     Ví dụ: ""We need to get this back to the care facility immediately!"" → Bản dịch Việt ≤ 48 ký tự (không tính dấu !)." & vbCrLf & _
               "   - Nếu vượt quá, bắt buộc phải rút gọn tự nhiên nhưng không được cắt nghĩa lõi." & vbCrLf & _
               "   - Tính độ dài riêng cho từng đoạn trước/sau `\N` nếu có." & vbCrLf & _
               "   - Rút gọn `...` thành `…` để tiết kiệm ký tự." & vbCrLf & _
               "   - Chuẩn hóa dấu câu tiếng Việt: bỏ chồng chéo (`?.!` → `?!` hoặc `.`), cách dấu đúng chuẩn." & vbCrLf & vbCrLf & _
               "3. LOCALIZE & CINEMATIC TONE" & vbCrLf & _
               "   - Genre awareness: " & vbCrLf & _
               "     • Action/Thriller → Câu ngắn, nhịp nhanh, từ mạnh." & vbCrLf & _
               "     • Drama/Romance → Câu mềm mại, giàu cảm xúc, ngắt nhịp tự nhiên." & vbCrLf & _
               "     • Comedy → Ưu tiên truyền tải hiệu ứng hài, có thể linh hoạt sáng tạo." & vbCrLf & _
               "     • Period/Historical → Dùng từ Hán-Việt, cấu trúc trang trọng khi phù hợp." & vbCrLf & _
               "   - Regional dialects: Nhận diện Anh-Mỹ, Anh-Anh, Southern US, Australian... → Truyền tải sắc thái qua từ vựng vùng miền tiếng Việt tương ứng (ví dụ: ""y'all"" → ""các cậu/các bạn"", không dịch word-for-word)." & vbCrLf & _
               "   - Titles & Formality: Việt hóa `Mr./Mrs./Dr./Captain` → `Ông/Bà/Bác sĩ/Thuyền trưởng` trừ khi nhân vật gọi tên riêng hoặc context yêu cầu giữ nguyên." & vbCrLf & _
               "   - Cultural references: Adapt pop culture, idioms, jokes sao cho khán giả Việt hiểu ngay mà không mất intent gốc. KHÔNG dùng chú thích trong phụ đề." & vbCrLf & _
               "   - Profanity handling: Điều chỉnh mức độ ""mạnh/nhẹ"" của từ ngữ theo rating phim (PG-13/R), giữ nguyên thái độ nhân vật." & vbCrLf & _
               "   - Technical jargon: Tra cứu và Việt hóa thuật ngữ chuyên ngành (sci-fi, legal, medical) nhất quán, ưu tiên dễ hiểu." & vbCrLf & _
               "   - Song lyrics: Ghi chú [hát] ở đầu câu nếu cần, ưu tiên giữ nhịp thơ khi có thể." & vbCrLf & _
               "   - On-screen text: Phân biệt xử lý: thoại ưu tiên tự nhiên như giao tiếp, text hiển thị trên màn hình ưu tiên dịch chính xác nội dung." & vbCrLf & vbCrLf & _
               "4. FRANCHISE & PROJECT CONSISTENCY" & vbCrLf & _
               "   - Ghi nhớ và áp dụng nhất quán: tên riêng, thuật ngữ, cách xưng hô xuyên suốt toàn bộ dự án/phim series." & vbCrLf & _
               "   - Khi gặp từ mới/chưa rõ context, ưu tiên giữ nguyên dạng gốc + ghi chú ngắn trong tag nếu cần." & vbCrLf & vbCrLf & _
               "# ✅ QUY TRÌNH TỰ PHẢN BIỆN 2 LẦN trước khi xuất ra kết quả:" & vbCrLf & _
               "1. Đã tạo đúng định dạng bảng 3 cột để copy vào Excel chưa?" & vbCrLf & _
               "2. Số lượng hàng output có khớp 100% với số lượng hàng input không?" & vbCrLf & _
               "3. Bản dịch tiếng Việt đã tối ưu ≤ 42-48 ký tự (không tính dấu) và phù hợp nhịp thoại chưa?" & vbCrLf & _
               "4. Đã dọn sạch 100% các câu chữ thừa như ""Dưới đây là kết quả..."", ""Hoàn thành..."" chưa?" & vbCrLf & _
               "5. [Movie-specific] Đã truyền tải đúng genre tone, regional dialect và cultural intent chưa?" & vbCrLf & _
               "6. [Movie-specific] Thuật ngữ chuyên ngành/tên riêng đã nhất quán với context phim chưa?" & vbCrLf & vbCrLf & _
               "# 📤 CẤU TRÚC OUTPUT DUY NHẤT ĐƯỢC PHÉP TRẢ VỀ:" & vbCrLf & _
               "| Số thứ tự | Nội dung gốc (Tiếng Anh) | Nội dung dịch (Tiếng Việt) |" & vbCrLf & _
               "| :--- | :--- | :--- |" & vbCrLf & _
               "| [STT] | [Text Anh] | [Text Việt] |" & vbCrLf & _
               "| ... | ... | ... |"
    End Function

#End Region

#Region "Translate - Toast"

    Private Async Sub ShowToastTranslate(message As String)
        ToastTextTranslate.Text = message
        ToastBorderTranslate.Visibility = Visibility.Visible
        Await Task.Delay(2000)
        ToastBorderTranslate.Visibility = Visibility.Collapsed
    End Sub

#End Region

End Class
