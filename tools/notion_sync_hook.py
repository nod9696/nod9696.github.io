#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
PostToolUse hook: triggered after Write/Edit.
Reads tool input from stdin (JSON), checks if the file is a novel file,
and runs notion_sync.py if so.
"""
import json, sys, os, subprocess

WATCH_PATTERNS = [
    r'f:\Claude\KirieSaki\Assets\StreamingAssets\Scenarios',
    r'f:\Claude\KamiNoFuruMachi\My project\Assets\StreamingAssets\Scenarios',
    r'f:\Claude\Delight',
]

WATCH_EXTENSIONS = {'.json', '.txt'}

def is_novel_file(path):
    if not path:
        return False
    path = os.path.normpath(path)
    _, ext = os.path.splitext(path)
    if ext.lower() not in WATCH_EXTENSIONS:
        return False
    for pattern in WATCH_PATTERNS:
        if path.startswith(os.path.normpath(pattern)):
            return True
    return False

def main():
    try:
        data = json.load(sys.stdin)
    except Exception:
        return

    tool_input = data.get('tool_input', {})
    file_path = tool_input.get('file_path', '')

    if not is_novel_file(file_path):
        return

    print(f'[notion-sync] Novel file updated: {os.path.basename(file_path)}')
    result = subprocess.run(
        ['py', r'f:\Claude\notion_sync.py'],
        capture_output=True, text=True, encoding='utf-8'
    )
    if result.stdout.strip():
        print(result.stdout.strip())
    if result.returncode != 0 and result.stderr:
        print(f'[notion-sync] Error: {result.stderr[:200]}')

if __name__ == '__main__':
    main()
