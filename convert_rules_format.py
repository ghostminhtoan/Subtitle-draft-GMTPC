"""
Script convert file A-Z.txt sang format mới:
- Cũ: mỗi dòng 1 rule (aback:a/back)
- Mới: 50 rules/dòng, format (aback:a/back), (abacus:a/ba/cus)
"""

import os
import sys

def convert_file(filepath):
    """Convert một file sang format mới"""
    with open(filepath, 'r', encoding='utf-8') as f:
        lines = [line.strip() for line in f if line.strip()]
    
    if not lines:
        print(f"  {os.path.basename(filepath)}: rỗng, bỏ qua")
        return 0
    
    result = []
    for i in range(0, len(lines), 50):
        chunk = lines[i:i+50]
        formatted = ', '.join(f'({line})' for line in chunk)
        result.append(formatted)
    
    with open(filepath, 'w', encoding='utf-8') as f:
        f.write('\n'.join(result))
    
    print(f"  {os.path.basename(filepath)}: {len(lines)} rules → {len(result)} dòng")
    return len(lines)

def main():
    # Đường dẫn tuyệt đối đến thư mục rules
    rules_dir = r'R:\HDD R\ZC SYMLINK\USERS\source\repos\ghostminhtoan\Subtitle draft GMTPC CS\english word rules karaoke'
    
    if not os.path.exists(rules_dir):
        print(f"❌ Không tìm thấy thư mục: {rules_dir}")
        return
    
    print("🔄 Convert word split rules sang format mới...\n")
    
    total_rules = 0
    for letter in 'ABCDEFGHIJKLMNOPQRSTUVWXYZ':
        filepath = os.path.join(rules_dir, f'{letter}.txt')
        if os.path.exists(filepath):
            total_rules += convert_file(filepath)
        else:
            print(f"  {letter}.txt: không tồn tại")
    
    print(f"\n✅ Hoàn thành! Tổng: {total_rules} rules")
    print("\nFormat mới: (word:part1/part2), (word2:part1/part2) - 50 rules/dòng")
    input("\nNhấn Enter để thoát...")

if __name__ == '__main__':
    main()
