#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Script cập nhật rules từ file word list rules fix.txt vào các file A-Z.txt
"""

import os
import re
import glob

# Đường dẫn
rules_dir = r'r:\HDD R\ZC SYMLINK\USERS\source\repos\ghostminhtoan\Subtitle draft GMTPC CS\english word rules karaoke'
fix_file = r'c:\Users\Admin\Desktop\word list rules fix.txt'

def parse_fix_file(filepath):
    """Parse file fix.txt thành dictionary {word: rule}"""
    rules = {}
    with open(filepath, 'r', encoding='utf-8') as f:
        for line in f:
            line = line.strip()
            if not line or ':' not in line:
                continue
            parts = line.split(':', 1)
            if len(parts) == 2:
                word = parts[0].strip()
                rule = parts[1].strip()
                rules[word.lower()] = (word, rule)
    return rules

def update_a_to_z_files(fix_rules):
    """Cập nhật tất cả các file A-Z.txt"""
    txt_files = sorted(glob.glob(os.path.join(rules_dir, '*.txt')))
    
    total_updated = 0
    updated_words = []
    
    for filepath in txt_files:
        with open(filepath, 'r', encoding='utf-8') as f:
            content = f.read()
        
        original_content = content
        
        # Pattern để tìm các rule trong file: (word:rule)
        pattern = re.compile(r'\((\w+\'?\w*):([^)]+)\)')
        
        def replace_match(match):
            nonlocal total_updated
            word = match.group(1)
            current_rule = match.group(2)
            
            word_lower = word.lower()
            
            # Kiểm tra xem từ này có trong file fix không
            if word_lower in fix_rules:
                fixed_word, fixed_rule = fix_rules[word_lower]
                
                # Nếu rule khác nhau → cập nhật
                if current_rule != fixed_rule:
                    total_updated += 1
                    updated_words.append(f"{word}: {current_rule} → {fixed_rule}")
                    return f'({fixed_word}:{fixed_rule})'
            
            return match.group(0)
        
        content = pattern.sub(replace_match, content)
        
        if content != original_content:
            with open(filepath, 'w', encoding='utf-8') as f:
                f.write(content)
    
    return total_updated, updated_words

def main():
    print("📖 Đọc file fix.txt...")
    fix_rules = parse_fix_file(fix_file)
    print(f"✅ Đã đọc {len(fix_rules)} rules từ file fix")
    
    print("\n🔄 Cập nhật các file A-Z.txt...")
    total_updated, updated_words = update_a_to_z_files(fix_rules)
    
    print(f"\n✅ Đã cập nhật {total_updated} từ")
    
    # Hiển thị một số ví dụ
    if updated_words:
        print("\n📋 Một số ví dụ đã cập nhật:")
        for word in updated_words[:50]:
            print(f"  {word}")
        if len(updated_words) > 50:
            print(f"  ... và {len(updated_words) - 50} từ khác")

if __name__ == '__main__':
    main()
    print("\n🎉 Hoàn tất! Bây giờ chạy embed_word_rules.py để cập nhật WordListRules.cs")
