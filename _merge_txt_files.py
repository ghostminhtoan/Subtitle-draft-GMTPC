#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Script gộp các file A-Z.txt thành 3 file:
- A-G.txt: A.txt đến G.txt
- H-P.txt: H.txt đến P.txt
- Q-Z.txt: Q.txt đến Z.txt
"""

import os
import glob

rules_dir = r'r:\HDD R\ZC SYMLINK\USERS\source\repos\ghostminhtoan\Subtitle draft GMTPC CS\english word rules karaoke'

def merge_files(file_list, output_file):
    """Gộp danh sách file thành 1 file"""
    with open(output_file, 'w', encoding='utf-8') as outfile:
        for filepath in file_list:
            if os.path.exists(filepath):
                with open(filepath, 'r', encoding='utf-8') as infile:
                    content = infile.read()
                    outfile.write(content)
                    # Thêm newline nếu chưa có
                    if not content.endswith('\n'):
                        outfile.write('\n')
                print(f"  ✅ Đã thêm: {os.path.basename(filepath)}")
            else:
                print(f"  ⚠️  Không tìm thấy: {os.path.basename(filepath)}")
    
    # Đếm số dòng
    with open(output_file, 'r', encoding='utf-8') as f:
        lines = f.readlines()
        print(f"  📊 Tổng: {len(lines)} dòng")

def main():
    print("🔄 Gộp các file A-Z.txt...\n")
    
    # A-G.txt
    print("📁 Tạo file A-G.txt:")
    a_to_g = [os.path.join(rules_dir, f"{chr(i)}.txt") for i in range(ord('A'), ord('G') + 1)]
    merge_files(a_to_g, os.path.join(rules_dir, "A-G.txt"))
    
    print()
    
    # H-P.txt
    print("📁 Tạo file H-P.txt:")
    h_to_p = [os.path.join(rules_dir, f"{chr(i)}.txt") for i in range(ord('H'), ord('P') + 1)]
    merge_files(h_to_p, os.path.join(rules_dir, "H-P.txt"))
    
    print()
    
    # Q-Z.txt
    print("📁 Tạo file Q-Z.txt:")
    q_to_z = [os.path.join(rules_dir, f"{chr(i)}.txt") for i in range(ord('Q'), ord('Z') + 1)]
    merge_files(q_to_z, os.path.join(rules_dir, "Q-Z.txt"))
    
    print("\n✅ Hoàn tất!")

if __name__ == '__main__':
    main()
