# SUBTITLE EDIT - PROFILES TỔNG HỢP
## Bộ 4 Profile cho các workflow phụ đề khác nhau

> **Nguồn:** File `shortcut MMT.xml` (~380 shortcuts)
> **Mục đích:** Tối ưu hóa phím tắt cho từng giai đoạn trong workflow làm phụ đề

---

## 📊 BẢNG SO SÁNH NHANH 4 PROFILE

| Tiêu chí | 01 - New Subtitle | 02 - Translate Existing | 03 - Edit Translated | 04 - Quick Checker |
|----------|-------------------|------------------------|---------------------|-------------------|
| **Mục đích** | Tạo sub từ đầu | Dịch sub có sẵn | Sửa sub đã dịch | Kiểm tra trước xuất bản |
| **Số shortcuts** | ~150 | ~80 | ~100 | ~50 |
| **Timing** | ⭐⭐⭐⭐⭐ Đầy đủ | ⭐⭐ Hạn chế | ⭐⭐⭐ Vừa phải | ⭐ Tối thiểu |
| **Translation** | ⭐⭐ Cơ bản | ⭐⭐⭐⭐⭐ Đầy đủ | ⭐⭐ Tối thiểu | ❌ Không |
| **Spell Check** | ⭐⭐ Có | ⭐⭐ Có | ⭐⭐⭐ Đầy đủ | ⭐⭐ Có |
| **Fix Errors** | ⭐⭐ Có | ⭐⭐ Có | ⭐⭐⭐⭐ Đầy đủ | ⭐⭐⭐ Có |
| **Waveform** | ⭐⭐⭐⭐ Đầy đủ | ⭐ Tối thiểu | ⭐⭐⭐ Có | ❌ Không |
| **Sync Tools** | ❌ Không | ❌ Không | ⭐⭐⭐ Có | ❌ Không |

---

## 📁 DANH SÁCH FILE

### Profile 01: Translate - New Subtitle
- **File XML:** `profile-01-translate-new.xml`
- **File MD:** `profile-01-translate-new.md`
- **Dành cho:** Người làm sub từ đầu, chưa có file phụ đề nào
- **Workflow:** Nghe video → Tạo timing → Nhập văn bản → Dịch

### Profile 02: Translate - Existing Subtitle
- **File XML:** `profile-02-translate-existing.xml`
- **File MD:** `profile-02-translate-existing.md`
- **Dành cho:** File sub đã có timing chuẩn, chỉ cần nhập bản dịch
- **Workflow:** Mở file sub có sẵn → Bật Translation Mode → Dịch → Save

### Profile 03: Edit Translated Subtitle
- **File XML:** `profile-03-edit-translated.xml`
- **File MD:** `profile-03-edit-translated.md`
- **Dành cho:** Editor kiểm tra & chỉnh sửa sub đã được người khác dịch
- **Workflow:** Mở file đã dịch → Sửa lỗi chính tả, format, timing → Save

### Profile 04: Quick Checker
- **File XML:** `profile-04-quick-checker.xml`
- **File MD:** `profile-04-quick-checker.md`
- **Dành cho:** Người review sub cuối cùng trước khi xuất bản
- **Workflow:** Mở file sub → Phát kiểm tra → Tìm lỗi → Sửa nhỏ nếu cần → Export

---

## 🎯 HƯỚNG DẪN IMPORT VÀO SUBTITLE EDIT

### Cách 1: Import qua giao diện
1. Mở Subtitle Edit
2. Vào menu **Options** → **Settings** → **Shortcuts**
3. Click **Import** (hoặc **Load**)
4. Chọn file `.xml` tương ứng với workflow của bạn
5. Click **OK** và khởi động lại Subtitle Edit

### Cách 2: Thay thế trực tiếp
1. Đóng Subtitle Edit
2. Tìm file shortcuts của Subtitle Edit trong thư mục cài đặt hoặc AppData
3. Backup file cũ
4. Copy file `.xml` từ profile vào, đổi tên thành tên file gốc
5. Mở Subtitle Edit

---

## 🔄 WORKFLOW KHUYÊN DÙNG CHO TEAM

```
┌─────────────────────────────────────────────────────────────┐
│                    WORKFLOW TEAM SUBTITLE                    │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│  Person A              Person B              Person C       │
│  ┌──────────┐         ┌──────────┐         ┌──────────┐    │
│  │ Profile  │  FILE   │ Profile  │  FILE   │ Profile  │    │
│  │   01     │ ──────► │   02     │ ──────► │   03     │    │
│  │ New Sub  │  .srt   │ Translate│  .srt   │  Edit    │    │
│  └──────────┘         └──────────┘         └──────────┘    │
│                                             │               │
│                                             ▼               │
│                                      ┌──────────┐           │
│                                      │ Profile  │           │
│                                      │   04     │           │
│                                      │ Quick QC │           │
│                                      └──────────┘           │
│                                             │               │
│                                             ▼               │
│                                      ┌──────────┐           │
│                                      │ EXPORT   │           │
│                                      │ PUBLISH  │           │
│                                      └──────────┘           │
│                                                              │
└─────────────────────────────────────────────────────────────┘
```

### Phân công công việc:
1. **Person A (Timing):** Dùng Profile 01 → Tạo timing từ video, nhập text gốc
2. **Person B (Translation):** Dùng Profile 02 → Dịch từ text gốc sang ngôn ngữ đích
3. **Person C (Editing):** Dùng Profile 03 → Sửa lỗi chính tả, format, timing
4. **Person D (QC):** Dùng Profile 04 → Kiểm tra lần cuối trước khi xuất bản

---

## 🔑 PHÍM TẮT DÙNG CHUNG CHO TẤT cả PROFILE

| Phím tắt | Chức năng | Mô tả |
|----------|-----------|-------|
| `Ctrl+S` | Save | Lưu file |
| `Ctrl+Z` | Undo | Hoàn tác |
| `Ctrl+Y` | Redo | Làm lại |
| `Ctrl+F` | Find | Tìm kiếm |
| `F3` | Find Next | Tìm tiếp |
| `Ctrl+H` | Replace | Thay thế |
| `Ctrl+Space` | Play/Pause | Phát/Tạm dừng |
| `Ctrl+Down` | Next Sub + Play | Tới sub sau & phát |
| `Ctrl+Up` | Prev Sub + Play | Lùi sub trước & phát |
| `Alt+F7` | Spell Check | Kiểm tra chính tả |
| `Ctrl+Shift+F` | Fix Common Errors | Sửa lỗi thường gặp |
| `F1` | Help | Trợ giúp |

---

## 📝 CHI TIẾT TỪNG PROFILE

Xem file `.md` tương ứng để biết:
- Top 15 phím tắt quan trọng nhất
- Bảng đầy đủ theo nhóm chức năng
- Tips sử dụng cho từng profile

---

## 📌 LƯU Ý

1. **File XML:** Dùng để import vào Subtitle Edit
2. **File MD:** Dùng để đọc & tra cứu phím tắt
3. **Shortcut trống:** Các mục không có phím tắt được gán (để trống `/>`) vẫn được giữ lại trong file XML để tránh lỗi import
4. **Tùy chỉnh:** Bạn có thể chỉnh sửa thêm phím tắt sau khi import qua giao diện Subtitle Edit
5. **Backup:** Luôn backup file shortcuts hiện tại trước khi import profile mới

---

## 📅 THÔNG TIN

- **Ngày tạo:** 13/04/2026
- **Nguồn:** `shortcut MMT.xml`
- **Tổng số shortcuts gốc:** ~380
- **Số profile:** 4
- **Tổng số file tạo ra:** 9 (4 XML + 4 MD + 1 README)

---

## ⌨️ BẢNG ĐỐI CHIẾU PHÍM OEM

Các phím Oem được chuẩn hóa theo định dạng: `Tên+OemName (ký tự)`

| Tên Oem | Ký tự | Ví dụ | Vị trí trên bàn phím |
|---------|-------|-------|---------------------|
| **OemMinus** | `-` | `Alt+OemMinus (-)` | Phím trừ, cạnh số 0 |
| **Oemplus** | `=` | `Ctrl+Oemplus (=)` | Phím bằng, cạnh phím trừ |
| **Oem7** | `"` | `Ctrl+Oem7 (")` | Phím nháy kép, cạnh Enter |
| **Oemtilde** | `` ` `` | `Ctrl+Oemtilde (`)` | Phím backtick, trên phím Tab |
| **OemOpenBrackets** | `[` | `Ctrl+OemOpenBrackets ([)` | Ngoặc vuông mở, cạnh phím P |
| **Oem6** | `]` | `Alt+Oem6 (])` | Ngoặc vuông đóng, cạnh phím ngoặc mở |

> **Lưu:** Tên Oem giữ nguyên để tương thích với file XML gốc của Subtitle Edit. Ký tự trong ngoặc là phím thực tế trên bàn phím.

---

*Tài liệu được tạo tự động từ file shortcut MMT.xml của Subtitle Edit*
