#!/usr/bin/env python3
# -*- coding: utf-8 -*-
import json, os, time, urllib.request, urllib.error

TOKEN = '$(os.environ.get("NOTION_TOKEN", ""))'
PARENT_ID = '2c745ad01ef080e5a71ad0b7f9dedf07'
SCENARIO_DIR = r'f:\Claude\KamiNoFuruMachi\My project\Assets\StreamingAssets\Scenarios'

CHAR_NAMES = {
    'kanata':   'カナタ・イグル',
    'lilith':   'リリス',
    'eleonora': 'エレオノーラ',
    'lucius':   'ルキウス',
    'grief':    'グリーフ',
    'narrator': None,
}

HEADERS = {
    'Authorization': f'Bearer {TOKEN}',
    'Notion-Version': '2022-06-28',
    'Content-Type': 'application/json',
}

def api(method, path, body=None):
    url = f'https://api.notion.com/v1{path}'
    data = json.dumps(body, ensure_ascii=False).encode('utf-8') if body else None
    req = urllib.request.Request(url, data=data, headers=HEADERS, method=method)
    try:
        with urllib.request.urlopen(req) as r:
            return json.loads(r.read().decode('utf-8'))
    except urllib.error.HTTPError as e:
        print(f'  ERROR {e.code}: {e.read().decode()}')
        return None

def make_para(text, block_type='paragraph'):
    text = text[:1990]
    return {
        'type': block_type,
        block_type: {'rich_text': [{'type': 'text', 'text': {'content': text}}]}
    }

def make_heading(text, level=2):
    t = f'heading_{level}'
    return {'type': t, t: {'rich_text': [{'type': 'text', 'text': {'content': text}}]}}

def upload_blocks(page_id, blocks):
    i = 0
    while i < len(blocks):
        chunk = blocks[i:i+100]
        api('PATCH', f'/blocks/{page_id}/children', {'children': chunk})
        i += 100
        time.sleep(0.4)

def create_page(parent_id, title):
    r = api('POST', '/pages', {
        'parent': {'page_id': parent_id},
        'properties': {'title': {'title': [{'text': {'content': title}}]}}
    })
    return r['id'] if r else None

# Create 神の降る街 parent page
kami_page_id = create_page(PARENT_ID, '神の降る街')
print(f'Created: 神の降る街 ({kami_page_id})')

files = sorted([f for f in os.listdir(SCENARIO_DIR) if f.startswith('chapter') and f.endswith('.json')])
for fname in files:
    path = os.path.join(SCENARIO_DIR, fname)
    with open(path, encoding='utf-8') as f:
        data = json.load(f)

    title = data.get('title', fname)
    sub_id = create_page(kami_page_id, title)
    if not sub_id:
        print(f'  SKIP {title}')
        continue

    blocks = []
    for cmd in data.get('commands', []):
        if cmd.get('cmd') == 'text':
            char = cmd.get('char', '')
            body = cmd.get('body', '')
            if char == 'narrator':
                blocks.append(make_para(body))
            else:
                name = CHAR_NAMES.get(char, char)
                blocks.append(make_para(f'{name}「{body}」'))

    if blocks:
        upload_blocks(sub_id, blocks)
    print(f'  Done: {title} ({len(blocks)} blocks)')
    time.sleep(0.3)

print('神の降る街 upload complete')
