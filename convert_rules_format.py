"""
Script convert file A-Z.txt sang format mới:
- Cũ: mỗi dòng 1 rule (aback:a/back)  
- Mới: (aback:a/back), (abacus:a/ba/cus) - 50 rules/dòng
"""

import os

def convert_file(filepath):
    with open(filepath, 'r', encoding='utf-8') as f:
        lines = [line.strip() for line in f if line.strip()]
    
    if not lines:
        return 0
    
    result = []
    for i in range(0, len(lines), 50):
        chunk = lines[i:i+50]
        formatted = ', '.join(f'({line})' for line in chunk)
        result.append(formatted)
    
    with open(filepath, 'w', encoding='utf-8') as f:
        f.write('\n'.join(result))
    
    return len(lines)

def main():
    rules_dir = r'R:\HDD R\ZC SYMLINK\USERS\source\repos\ghostminhtoan\Subtitle draft GMTPC CS\english word rules karaoke'
    
    if not os.path.exists(rules_dir):
        print(f"Không tìm thấy: {rules_dir}")
        return
    
    print("🔄 Convert word split rules sang format mới...\n")
    
    total_rules = 0
    for letter in 'ABCDEFGHIJKLMNOPQRSTUVWXYZ':
        filepath = os.path.join(rules_dir, f'{letter}.txt')
        if os.path.exists(filepath):
            count = convert_file(filepath)
            total_rules += count
            print(f"  {letter}.txt: {count} rules")
    
    print(f"\n✅ Hoàn thành! Tổng: {total_rules} rules")

if __name__ == '__main__':
    main()
