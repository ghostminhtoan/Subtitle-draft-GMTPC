#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Script cập nhật các từ có dấu nháy đơn (') trong word rules
Thay thế các rule chia sai như "i've:i'/ve" thành "i've:i've"
"""

import os
import re
import glob

# Đường dẫn đến thư mục chứa các file A-Z.txt
rules_dir = r'r:\HDD R\ZC SYMLINK\USERS\source\repos\ghostminhtoan\Subtitle draft GMTPC CS\english word rules karaoke'

# Danh sách các từ có dấu ' cần giữ nguyên (không tách)
contractions_and_possessives = [
    # I
    "i'm", "i've", "i'll", "i'd", "i'm",
    # You
    "you're", "you've", "you'll", "you'd",
    # He/She/It
    "he's", "he'll", "he'd",
    "she's", "she'll", "she'd",
    "it's", "it'll", "it'd",
    # We
    "we're", "we've", "we'll", "we'd",
    # They
    "they're", "they've", "they'll", "they'd",
    # Negations
    "don't", "doesn't", "didn't", "won't", "wouldn't",
    "can't", "cannot", "couldn't",
    "shan't", "shouldn't",
    "mustn't", "needn't",
    "ain't", "isn't", "aren't", "wasn't", "weren't",
    "hasn't", "haven't", "hadn't",
    # Possessives với danh từ
    "one's",
    # Từ sở hữu thông dụng
    "who's", "whose", "what's", "where's", "when's", "why's", "how's",
    "there's", "here's",
    "that's", "this's", "these's", "those's",
    # Let's
    "let's",
]

def update_word_rules():
    """Cập nhật tất cả các file A-Z.txt"""
    pattern = re.compile(r'\((\w+\'?\w*):([^)]+)\)')
    
    txt_files = sorted(glob.glob(os.path.join(rules_dir, '*.txt')))
    
    updated_count = 0
    total_updated = 0
    
    for filepath in txt_files:
        with open(filepath, 'r', encoding='utf-8') as f:
            content = f.read()
        
        original_content = content
        
        # Tìm và thay thế các từ có dấu '
        def replace_match(match):
            word = match.group(1)
            rule = match.group(2)
            
            word_lower = word.lower()
            
            # Nếu từ chứa dấu ' và là contraction/possessive
            if "'" in word and word_lower in contractions_and_possessives:
                # Giữ nguyên từ, không tách
                return f'({word}:{word})'
            
            return match.group(0)
        
        content = pattern.sub(replace_match, content)
        
        if content != original_content:
            with open(filepath, 'w', encoding='utf-8') as f:
                f.write(content)
            updated_count += 1
            # Đếm số lượng đã cập nhật
            total_updated += len(re.findall(r'\((\w+\'\w*):\1\)', content))
    
    print(f"Đã cập nhật {updated_count} file")
    print(f"Tổng số từ đã cập nhật: {total_updated}")

if __name__ == '__main__':
    update_word_rules()
    print("\nHoàn tất! Bây giờ chạy embed_word_rules.py để cập nhật WordListRules.cs")
