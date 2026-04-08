f = r'r:\HDD R\ZC SYMLINK\USERS\source\repos\ghostminhtoan\Subtitle draft GMTPC\changelog.cursorrules'
c = open(f, 'r', encoding='utf-8').read()

new_entry = """
## [2026-04-08 02:00:00 PM Wednesday]

### Added - Tab Effect (ASS Override Tags Visual Builder)
- Tab Effect mới trong Karaoke, layout 3 panel: Input (trái) | Effect Groups (giữa) | Output (phải)
- 7 nhóm Effect với Expander: Position & Movement, Transform & Rotation, Font & Style, Border & Shadow, Color & Alpha, Fade & Animation, Presets
- Nút 🎨 Chọn màu cạnh label Effect Groups - mở Windows Color Dialog, tự điền hex BGR vào config
- Config panel động với label tiếng Việt, hiện khi chọn effect con
- Nút Apply (dòng tại con trỏ) và Apply All (tất cả dòng)
- Nút Clear Effects (🗑️) xóa toàn bộ tags khỏi input
- 6 Presets: Fade In, Fade Out, Glow, Big Text, Center Top, Center Screen

### Added - Services & Models
- Services/AssEffectBuilder.vb: 378 dòng
  - Build tất cả ASS tags: \\pos, \\move, \\an, \\org, \\frz, \\frx, \\fry, \\fscx, \\fscy, \\fax, \\fay, \\fn, \\fs, \\b, \\i, \\u, \\s, \\bord, \\shad, \\blur, \\be, \\1c, \\2c, \\3c, \\4c, \\alpha, \\1a-\\4a, \\fad, \\k, \\kf, \\ko, \\clip, \\iclip
  - Parse tags từ text, extract tags, split tags
  - ApplyTagToLine: chèn tag mới vào phần TEXT của Dialogue (sau dấu , thứ 9), KHÔNG đụng time code
  - Mỗi effect = 1 block {} riêng biệt, cách nhau bởi space. VD: {\\bord20} {\\1c&H0000FF&} Text
  - RemoveTagByType: xóa chính xác block {} của tag cần xóa, giữ lại tags khác
  - RemoveAllTagsFromAllLines: xóa sạch tags
  - GetPresetEffects: danh sách presets thông dụng
  - ValidateTag: kiểm tra tham số hợp lệ
- Models/AssEffectModels.vb: 119 dòng
  - AssTagType enum: 40+ loại tags
  - AssTag class: đại diện 1 tag với type, raw tag, display name
  - AssTagGroup class: nhóm tags theo chức năng
  - EffectPreset class: tổ hợp tags cấu hình sẵn
  - LineEffectInfo class: thông tin effect áp dụng cho line

### Added - Code-behind
- MainWindowTabEffect.vb: 520 dòng
  - Input/Output sync với debounce 150ms
  - Handlers cho 30+ effect buttons
  - ShowEffectConfig/HideEffectConfig: quản lý config panel động
  - BuildCurrentTag: xây dựng tag từ config values
  - ApplyTagToSelectedLine: apply tag vào dòng tại con trỏ trong Output
  - ApplyTagToAllLines: apply tag vào tất cả dòng trong Output
  - BtnColorPicker_Click: mở Windows Color Dialog, điền hex BGR vào config
  - Preset handlers: Fade In/Out, Glow, Big Text, Center Top/Screen

### Changed - UI/UX
- Input luôn giữ nguyên bản gốc, KHÔNG bị thay đổi bởi Apply
- Output là nơi áp dụng hiệu ứng, tích lũy nhiều lần Apply
- Apply chọn dòng dựa trên vị trí con trỏ trong Input
- Config panel có label tiếng Việt cho mọi effect
- Toast notification thông báo kết quả mỗi thao tác

### Changed - Project Structure
- Subtitle draft GMTPC.vbproj: thêm 3 files mới + reference System.Windows.Forms
- MainWindow.xaml: thêm sub-tab Effect trong KaraokeTabControl
- MainWindowSystemGlobalEvents.vb: thêm InitializeEffectDebounce() vào MainWindow_Loaded
- MainWindow.xaml.vb: thêm Imports System

### Fixed
- Fix VB keyword conflict: đổi parameter "on" → "isEnabled" (on là keyword VB.NET)
- Fix namespace conflict: fully qualify System.Windows.MessageBox, System.Windows.Media
- Fix InsertTag: chỉ chèn vào phần text của Dialogue, không chèn vào block {} có sẵn
- Fix Regex Dialogue: \\s*\\d+ để bắt tùy biến số space sau "Dialogue:"
- Fix Grid RowDefinitions: ScrollViewer có MaxHeight=350 để config panel luôn hiện
- Fix WrapTag: mỗi tag có block {} riêng, không chồng vào nhau
- Fix color picker: chỉ điền hex, không auto-apply

---

"""

# Insert new entry after header
c = c.replace('---\n\n\n## [2026-04-08 01:00:00 PM Wednesday]', '---' + new_entry + '## [2026-04-08 01:00:00 PM Wednesday]')

open(f, 'w', encoding='utf-8').write(c)
print('Changelog updated with full trace')
