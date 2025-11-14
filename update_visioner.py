#!/usr/bin/env python3
import os
import re
from pathlib import Path

# Dortage veritabanı klasörü
base_dir = r'C:\Users\mami\Desktop\claude-code-project\dortageDB'

# Exclude directories
exclude_dirs = {'Migrations', '.git', 'bin', 'obj', 'node_modules'}

# File extensions to process
extensions = {'.cs', '.cshtml', '.json'}

def process_file(file_path):
    """Dosyayı oku, visioner -> vekarer dönüştür, kaydet"""
    try:
        # Try UTF-8 first, then fallback to latin-1
        try:
            with open(file_path, 'r', encoding='utf-8') as f:
                content = f.read()
        except UnicodeDecodeError:
            with open(file_path, 'r', encoding='latin-1') as f:
                content = f.read()

        original_content = content

        # Replacements (order matters)
        replacements = [
            # Exact case replacements
            (r'\bVisioner\b', 'Vekarer'),
            (r'\bvisioner\b', 'vekarer'),
            (r'\bVISIONER\b', 'VEKARER'),
            # In strings and comments
            (r'"visioner"', '"vekarer"'),
            (r"'visioner'", "'vekarer'"),
            (r'visioner-', 'vekarer-'),
        ]

        for pattern, replacement in replacements:
            content = re.sub(pattern, replacement, content)

        # Eğer değişiklik olduysa, dosyayı kaydet
        if content != original_content:
            with open(file_path, 'w', encoding='utf-8') as f:
                f.write(content)
            print(f"Updated: {file_path}")
            return True
        else:
            return False
    except Exception as e:
        print(f"Error in {file_path}: {str(e)[:50]}")
        return False

def main():
    updated_count = 0

    # Recursively process files
    for root, dirs, files in os.walk(base_dir):
        # Exclude certain directories
        dirs[:] = [d for d in dirs if d not in exclude_dirs]

        for file in files:
            file_path = os.path.join(root, file)
            _, ext = os.path.splitext(file)

            if ext in extensions:
                if process_file(file_path):
                    updated_count += 1

    print(f"\nTotal files updated: {updated_count}")

if __name__ == '__main__':
    main()
