
import os

for root, dirs, files in os.walk('wwwroot/turlar/inkoy'):
    for file in files:
        path = os.path.join(root, file)
        try:
            with open(path, 'r', encoding='utf-8', errors='replace') as f:
                content = f.read()
                if '\ufffd' in content:
                    print(f"Found REPLACEMENT CHARACTER in {path}")
        except Exception as e:
            print(f"Could not read {path}: {e}")
