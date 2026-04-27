# 🎬 Subtitle Edit

> Trình chỉnh sửa phụ đề video mã nguồn mở, hoàn toàn miễn phí – công cụ "gối đầu giường" của dân dịch phụ đề, làm video và phụ đề hóa nội dung.

## 📖 Mục lục
- [Giới thiệu](#-giới-thiệu)
- [Tải xuống & Yêu cầu hệ thống](#-tải-xuống--yêu-cầu-hệ-thống)
- [Tính năng nổi bật](#-tính-năng-nổi-bật)
- [Hỗ trợ đa ngôn ngữ](#-hỗ-trợ-đa-ngôn-ngữ)
- [Định dạng được hỗ trợ](#-định-dạng-được-hỗ-trợ)
- [Xử lý hàng loạt & Dòng lệnh](#-xử-lý-hàng-loạt--dòng-lệnh)
- [Giấy phép & Mã nguồn](#-giấy-phép--mã-nguồn)
- [Liên kết hữu ích](#-liên-kết-hữu-ích)

---

## 📝 Giới thiệu

Subtitle Edit (hay gọi tắt là **SE**) là một trình biên tập phụ đề video mã nguồn mở, nhẹ, nhưng cực kỳ mạnh mẽ. 

Với SE, bạn có thể:
✅ Dễ dàng căn chỉnh lại phụ đề khi bị lệch nhịp với video theo nhiều cách khác nhau.  
✅ Tạo phụ đề mới hoàn toàn từ đầu (hỗ trợ trực quan trục thời gian, dạng sóng âm thanh & phổ tần).  
✅ Dịch phụ đề nhanh chóng giữa các ngôn ngữ.  


---


---

## ✨ Tính năng nổi bật

### 🎛️ Chỉnh sửa & Đồng bộ
- Tạo, sửa, đồng bộ & dịch từng dòng phụ đề dễ dàng
- Đồng bộ trực quan: kéo-thả để chỉnh thời gian bắt đầu/kết thúc, điều chỉnh tốc độ
- Hiển thị phụ đề sớm/muộn hơn, đồng bộ theo điểm mốc, hoặc đồng bộ chéo dựa trên file phụ đề khác
- Gộp/tách file phụ đề, điều chỉnh thời gian hiển thị, đánh số lại tự động
- Trợ lý "Sửa lỗi nhanh": tự động phát hiện & khắc phục các lỗi phụ đề thường gặp
- Gộp dòng quá ngắn / tách dòng quá dài để phụ đề dễ đọc hơn trên mọi thiết bị

### 🤖 AI & Xử lý ngôn ngữ
- **Speech-to-Text**: Nhận diện giọng nói thành văn bản qua [Whisper](https://openai.com/whisper)
- **Text-to-Speech**: Chuyển văn bản thành giọng nói qua ElevenLabs, Azure, Google, hoặc engine TTS cài sẵn trên máy
- **Dịch tự động**: Hỗ trợ Google Translate, DeepL, các mô hình LLM chạy cục bộ (Ollama, LM Studio) và nhiều AI khác
- Kiểm tra chính tả qua từ điển LibreOffice (hỗ trợ đa ngôn ngữ)
- Tự động chỉnh viết hoa/thường theo từ điển tên riêng

### 🎥 Media & Tích hợp
- Bộ hiển thị âm thanh trực quan: waveform & spectrogram
- Trình phát video tích hợp: hỗ trợ `mpv`, `DirectShow` hoặc `VLC` (tùy chọn)
- "Rút" phụ đề từ đĩa DVD đã giải mã
- Nhập & OCR phụ đề nhị phân: VobSub (`.sub`/`.idx`), Blu-ray (`.sup`), XSub, DVB/Teletext (`.ts`/`.m2ts`)
- Đọc phụ đề nhúng sẵn trong: Matroska (`.mkv`), MP4/M4V, DivX/AVI, Transport Stream (`.ts`/`.m2ts`)
- Hỗ trợ đầy đủ UTF-8, Unicode & ANSI → tương thích mọi ngôn ngữ & bảng mã trên Windows

### 🛠️ Tiện ích nâng cao
- So sánh 2 file phụ đề để tìm điểm khác biệt
- Tìm kiếm & thay thế hàng loạt
- Loại bỏ ghi chú dành cho người khiếm thính (HI)
- Hiệu ứng thú vị: Typewriter & Karaoke
- Quản lý lịch sử thao tác: Hoàn tác (`Ctrl+Z`), Làm lại (`Ctrl+Y`)
- Xuất ra: PNG (+ BDN XML), Adobe Encore FAB, VobSub, Blu-ray sup, EBU STL, PAC, văn bản thuần
- Tạo video có phụ đề "khắc chìm" (burn-in) hoặc video nền trong suốt để ghép hậu kỳ

---

## 🌍 Hỗ trợ đa ngôn ngữ

Subtitle Edit hiện đã được dịch sang **hơn 32 ngôn ngữ**.  


---

## 📁 Định dạng được hỗ trợ

SE có thể đọc, ghi & chuyển đổi giữa **hơn 300 định dạng phụ đề**, bao gồm nhưng không giới hạn:

- `SubRip (*.srt)` • `Advanced Sub Station Alpha (*.ass)` • `Sub Station Alpha (*.ssa)`
- `MicroDVD (*.sub)` • `WebVTT (*.vtt)` • `YouTube Sbv (*.sbv)`
- `SAMI (*.smi)` • `EBU STL (*.stl)` • `PAC (*.pac)` • `D-Cinema`
- `Timed Text 1.0/TTML/DFXP (*.xml)` • `Flash XML` • `JSON` • `CSV`
- `VobSub (*.sub/*.idx)` • `Blu-ray sup (*.sup)` • `BDN XML`
- `Transport Stream (*.ts)` • `DVD Vob (*.vob)` • `QuickTime Text` • `RealTime Text`
- Và hàng trăm định dạng khác: `Avid`, `Scenarist`, `Sony DVD Architect`, `F4`, `TMPlayer`, `UTX`, `Zero G`, `Ultech (*.cap)`, `Cavena890`, `Cheetah Caption`, `Pinnacle Impression`, `TurboTitle`, `Ulead`, `Son (*.son)`, `OpenDvt`, `MPlayer2`, `Captionate`, `CapMaker Plus`, `AQTitle`, `Adobe Encore`, `ABC iView`, v.v.

*(Danh sách đầy đủ có thể thay đổi theo phiên bản. SE liên tục cập nhật trình phân tích cú pháp mới.)*

---

## 💻 Xử lý hàng loạt & Dòng lệnh

SE không chỉ là giao diện đồ họa. Bạn có thể:
- Chuyển đổi phụ đề hàng loạt qua **Batch-Convert UI**
- Tự động hóa quy trình qua **Command Line** (CLI)
→ Cực kỳ tiện khi cần xử lý số lượng lớn file hoặc tích hợp vào pipeline dựng video.

---
