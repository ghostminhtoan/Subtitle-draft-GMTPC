#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Script xóa các file A.txt đến Z.txt sau khi đã gộp thành công
"""

import os

rules_dir = r'r:\HDD R\ZC SYMLINK\USERS\source\repos\ghostminhtoan\Subtitle draft GMTPC CS\english word rules karaoke'

def main():
    print("🗑️  Xóa các file A.txt đến Z.txt...\n")
    
    deleted_count = 0
    for i in range(ord('A'), ord('Z') + 1):
        filename = f"{chr(i)}.txt"
        filepath = os.path.join(rules_dir, filename)
        
        if os.path.exists(filepath):
            os.remove(filepath)
            print(f"  ✅ Đã xóa: {filename}")
            deleted_count += 1
        else:
            print(f"  ⚠️  Không tìm thấy: {filename}")
    
    print(f"\n✅ Đã xóa {deleted_count} file!")

if __name__ == '__main__':
    main()
