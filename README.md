# Subtitle draft GMTPC

Desktop WPF tool for working with `.SRT` and `.ASS` subtitles, bilingual merge workflows, karaoke prep, timing cleanup, and ASS effect authoring.  
Công cụ desktop WPF để xử lý phụ đề `.SRT` và `.ASS`, workflow ghép song ngữ, chuẩn bị karaoke, chỉnh lại timing, và tạo ASS effect.

## Overview | Tổng quan

`Subtitle draft GMTPC` is a Windows desktop application built for practical subtitle and karaoke workflows. It combines multiple daily-use utilities into one UI so users can translate, merge, draft, retime, format, and prepare karaoke subtitles without switching across several tools.  
`Subtitle draft GMTPC` là ứng dụng desktop Windows được xây dựng cho workflow làm subtitle và karaoke thực tế. Ứng dụng gom nhiều công cụ thường dùng vào một giao diện duy nhất để người dùng có thể dịch, ghép, tạo bản nháp, canh lại timing, định dạng, và chuẩn bị karaoke mà không cần chuyển qua nhiều phần mềm.

## Core Capabilities | Chức năng chính

- Detect and parse `SRT` and `ASS` subtitle formats  
  Nhận diện và parse định dạng phụ đề `SRT` và `ASS`
- Sanitize pasted subtitle content before processing  
  Làm sạch nội dung phụ đề được dán vào trước khi xử lý
- Merge bilingual subtitles  
  Ghép phụ đề song ngữ
- Generate merged output with or without line breaks  
  Tạo kết quả merge có giữ dòng hoặc bỏ xuống dòng
- Translate subtitle content through a Qwen-based workflow  
  Dịch nội dung subtitle thông qua workflow dựa trên Qwen
- Save and reuse multiple translation prompts  
  Lưu và tái sử dụng nhiều prompt dịch
- Convert plain text into timed subtitle output  
  Chuyển văn bản thường thành phụ đề có mốc thời gian
- Prepare karaoke text for Vietnamese and English workflows  
  Chuẩn bị nội dung karaoke cho cả workflow tiếng Việt và tiếng Anh
- Apply and build ASS tags such as `\pos`, `\move`, `\fad`, `\k`, color, and alpha  
  Tạo và áp dụng các ASS tag như `\pos`, `\move`, `\fad`, `\k`, màu, và alpha
- Inspect local hardware information relevant to encoding workflows  
  Xem thông tin phần cứng liên quan đến workflow encode

## Main Tabs | Các tab chính

| Tab | English | Tiếng Việt |
| --- | --- | --- |
| `Hardware Info` | View GPU, CPU, RAM, mainboard, and hardware encoder hints | Xem GPU, CPU, RAM, mainboard, và gợi ý encoder phần cứng |
| `Dialogue` | Extract or isolate dialogue content for editing | Tách hoặc tách riêng phần hội thoại để chỉnh sửa |
| `Translate` | Translate subtitle content with reusable prompts | Dịch subtitle bằng các prompt có thể tái sử dụng |
| `Search Fonts` | Help search fonts used in subtitle and karaoke workflows | Hỗ trợ tìm font dùng trong workflow subtitle và karaoke |
| `ASS Font Adjust` | Adjust font-related settings in ASS subtitles | Điều chỉnh các thiết lập font trong ASS subtitle |
| `Subtitle Merge` | Merge two subtitle sources, commonly English and Vietnamese | Ghép hai nguồn subtitle, thường là tiếng Anh và tiếng Việt |
| `Subtitle Draft` | Build or refine subtitle draft output | Tạo hoặc tinh chỉnh bản nháp subtitle |
| `Text to Subtitle` | Convert plain text into timed subtitle content | Chuyển văn bản thường thành subtitle có timecode |
| `Karaoke` | General karaoke workflow entry point | Điểm bắt đầu tổng quan cho workflow karaoke |
| `Zero Time` | Reset subtitle timing to start from zero | Đưa mốc thời gian subtitle về 0 |
| `Karaoke Vietnamese` | Prepare Vietnamese karaoke text | Xử lý lời karaoke tiếng Việt |
| `Karaoke English` | Split English karaoke text using word rules | Tách từ hoặc âm tiết karaoke tiếng Anh bằng word rules |
| `Karaoke Merge` | Merge karaoke-related data from multiple sources | Ghép dữ liệu karaoke từ nhiều nguồn |
| `Karaoke Sync` | Synchronize karaoke timing | Đồng bộ timing karaoke |
| `Effect` | Build and edit ASS effects and tags | Tạo và chỉnh sửa ASS effect và tags |

## Recommended Learning Order | Thứ tự nên tìm hiểu

For a new contributor or user, this is the easiest order to understand the product.  
Nếu là người mới, đây là thứ tự để hiểu sản phẩm dễ nhất.

1. `Translate`
2. `Subtitle Merge`
3. `Text to Subtitle`
4. `ASS Font Adjust`
5. `Effect`
6. `Karaoke English`
7. `Karaoke Vietnamese`
8. `Karaoke Sync`
9. `Karaoke Merge`
10. `Zero Time`
11. `Dialogue`
12. `Search Fonts`
13. `Hardware Info`

## Typical User Flows | Luồng sử dụng điển hình

### Translate existing subtitles | Dịch phụ đề có sẵn

1. Paste source subtitle content into `Translate`  
   Dán nội dung phụ đề gốc vào `Translate`
2. Load or edit a prompt  
   Tải prompt có sẵn hoặc sửa prompt
3. Run the translation workflow  
   Chạy workflow dịch
4. Copy the translated result back into the rest of the pipeline  
   Copy kết quả đã dịch để đưa vào các bước tiếp theo

### Build bilingual subtitles | Tạo phụ đề song ngữ

1. Paste subtitle source A into `Subtitle Merge`  
   Dán subtitle nguồn A vào `Subtitle Merge`
2. Paste subtitle source B into `Subtitle Merge`  
   Dán subtitle nguồn B vào `Subtitle Merge`
3. Copy either merged output or the unbroken-line output  
   Copy bản đã ghép hoặc bản bỏ xuống dòng

### Turn raw text into subtitles | Chuyển text thô thành subtitle

1. Paste transcript or lyrics into `Text to Subtitle`  
   Dán transcript hoặc lời bài hát vào `Text to Subtitle`
2. Tune max characters, CPS, and gap settings  
   Điều chỉnh số ký tự tối đa, CPS, và khoảng gap
3. Copy the generated subtitle output  
   Copy kết quả subtitle đã tạo

### Karaoke preparation | Chuẩn bị karaoke

1. Prepare lyrics in `Karaoke Vietnamese` or `Karaoke English`  
   Chuẩn bị lời bài hát trong `Karaoke Vietnamese` hoặc `Karaoke English`
2. Adjust split rules when needed  
   Điều chỉnh split rules khi cần
3. Use `Karaoke Sync` and `Karaoke Merge` for timing and consolidation  
   Dùng `Karaoke Sync` và `Karaoke Merge` để đồng bộ và ghép kết quả
4. Finish styling in `Effect`  
   Hoàn thiện phần hiệu ứng ở `Effect`

## Technical Stack | Công nghệ

- C#
- WPF
- .NET Framework 4.8
- Windows Forms interoperability  
  Tích hợp Windows Forms
- WMI for hardware inspection  
  WMI để đọc thông tin phần cứng
- `HttpClient` for translation workflow calls  
  `HttpClient` để gọi workflow dịch

## Project Structure | Cấu trúc project

```text
.
|-- Application.xaml
|-- MainWindow.xaml
|-- MainWindow.xaml.cs
|-- MainWindowSystemGlobalEvents.cs
|-- MainWindowTab*.cs
|-- AppSettings.cs
|-- Services/
|-- Models/
|-- Helpers/
|-- english word rules karaoke/
|-- build.cmd
`-- Subtitle draft GMTPC.csproj
```

### Important Folders | Thư mục quan trọng

- `Services/`: core business logic such as parsing, merge, translation, karaoke processing, timing, and ASS effects  
  Chứa logic nghiệp vụ chính như parse, merge, dịch, xử lý karaoke, timing, và ASS effects
- `Models/`: subtitle line models and related data types  
  Chứa model dòng subtitle và các kiểu dữ liệu liên quan
- `Helpers/`: shared helper utilities such as search support  
  Chứa các tiện ích dùng chung như tìm kiếm
- `english word rules karaoke/`: rule files used by the English karaoke workflow  
  Chứa các file rule được dùng bởi workflow karaoke tiếng Anh

### Important Files | File đáng chú ý

- [MainWindow.xaml](./MainWindow.xaml): main UI layout and tab definitions  
  Giao diện chính và định nghĩa các tab
- [MainWindowSystemGlobalEvents.cs](./MainWindowSystemGlobalEvents.cs): startup logic, global events, search, shared behaviors  
  Logic khởi động, sự kiện toàn cục, tìm kiếm, và hành vi dùng chung
- [MainWindowTabTranslate.cs](./MainWindowTabTranslate.cs): translation tab logic  
  Logic cho tab dịch
- [MainWindowTabSubtitleMerge.cs](./MainWindowTabSubtitleMerge.cs): subtitle merge logic  
  Logic ghép subtitle
- [MainWindowTabTextToSubtitle.cs](./MainWindowTabTextToSubtitle.cs): plain-text to subtitle logic  
  Logic chuyển text thường thành subtitle
- [MainWindowTabKaraokeEnglish.cs](./MainWindowTabKaraokeEnglish.cs): English karaoke rule-based splitting  
  Logic tách karaoke tiếng Anh dựa trên rules
- [Services/SubtitleParser.cs](./Services/SubtitleParser.cs): subtitle detection, parsing, and serialization  
  Nhận diện, parse, và xuất subtitle
- [Services/MergeService.cs](./Services/MergeService.cs): subtitle merge operations  
  Các thao tác merge subtitle
- [Services/QwenTranslateService.cs](./Services/QwenTranslateService.cs): Qwen translation workflow integration  
  Tích hợp workflow dịch Qwen
- [Services/AssEffectBuilder.cs](./Services/AssEffectBuilder.cs): ASS tag and effect helpers  
  Tiện ích tạo và xử lý ASS tag/effect

## Build | Cách build

### Quick Start | Cách nhanh

Run:  
Chạy lệnh:

```bat
build.cmd
```

The script attempts to:  
Script sẽ cố gắng:

- locate `MSBuild.exe`  
  tìm `MSBuild.exe`
- build the project in `Debug`  
  build project ở chế độ `Debug`
- copy the built executable to the repository root  
  copy file `.exe` đã build ra thư mục gốc
- launch the application  
  mở ứng dụng

### Manual Build | Build thủ công

Requirements:  
Yêu cầu:

- Windows
- Visual Studio 2019 or 2022, or Build Tools with MSBuild  
  Visual Studio 2019/2022 hoặc Build Tools có MSBuild
- .NET Framework 4.8 targeting pack

Build command:  
Lệnh build:

```bat
MSBuild "Subtitle draft GMTPC.csproj" /p:Configuration=Debug /t:Build
```

Expected output:  
File output thường nằm ở:

```text
bin\Debug\net48\Subtitle draft GMTPC.exe
```

## Implementation Notes | Ghi chú kỹ thuật

- The application targets `net48`  
  Ứng dụng target `net48`
- The main window is intentionally split into multiple partial classes by tab  
  Cửa sổ chính được tách thành nhiều partial class theo tab
- The repository contains custom dark-theme WPF styling in the main window  
  Repo có dark theme WPF tự tùy biến trong cửa sổ chính
- `TranslateWindow.xaml` is excluded from build inputs  
  `TranslateWindow.xaml` đang bị loại khỏi build
- Some source comments and strings currently show encoding issues and could be normalized later  
  Một số comment và chuỗi hiện đang có dấu hiệu lỗi encoding và nên được chuẩn hóa sau

## Suggested Next Improvements | Gợi ý cải thiện tiếp theo

- Add screenshots for each major tab  
  Thêm ảnh chụp cho mỗi tab chính
- Split user documentation and developer documentation into separate files  
  Tách tài liệu người dùng và tài liệu developer thành hai file riêng
- Add a `Known Issues` section  
  Thêm mục `Known Issues`
- Add release packaging instructions  
  Thêm hướng dẫn đóng gói bản release
- Document prompt management and cookie setup for translation in more detail  
  Tài liệu hóa kỹ hơn phần quản lý prompt và thiết lập cookies cho tab dịch
