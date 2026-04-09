"""
Script chia file english word rules karaoke thành 26 file nhỏ theo chữ cái A-Z
"""

import os

# Đường dẫn file gốc
input_file = r"english word rules karaoke\english word rules karaoke.txt"
output_dir = r"english word rules karaoke"

# Đọc file gốc
print("📖 Đang đọc file gốc...")
with open(input_file, 'r', encoding='utf-8') as f:
    lines = f.readlines()

print(f"✅ Đọc được {len(lines)} dòng")

# Tạo dictionary để chứa từ theo chữ cái
words_by_letter = {}

# Phân loại từ theo chữ cái đầu tiên
for line in lines:
    line = line.strip()
    if not line or ':' not in line:
        continue
    
    word = line.split(':')[0].lower()
    if not word:
        continue
    
    first_letter = word[0].upper()
    
    # Chỉ lấy A-Z
    if first_letter not in words_by_letter:
        words_by_letter[first_letter] = []
    
    words_by_letter[first_letter].append(line)

# Tạo file cho từng chữ cái
print(f"\n📝 Đang tạo {len(words_by_letter)} file nhỏ...")
for letter in sorted(words_by_letter.keys()):
    output_file = os.path.join(output_dir, f"{letter}.txt")
    
    with open(output_file, 'w', encoding='utf-8') as f:
        f.write('\n'.join(words_by_letter[letter]) + '\n')
    
    print(f"✅ {letter}.txt: {len(words_by_letter[letter])} từ")

print(f"\n🎉 Hoàn thành! Đã tạo {len(words_by_letter)} file trong thư mục: {output_dir}")
