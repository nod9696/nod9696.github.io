#!/usr/bin/env python3
# -*- coding: utf-8 -*-
import json, os, time, urllib.request, urllib.error

TOKEN = os.environ.get('NOTION_TOKEN', '')
PARENT_ID = '2c745ad01ef080e5a71ad0b7f9dedf07'
KIRIE_PAGE_ID = '32b45ad0-1ef0-81be-bdc1-c48646692738'
DELIGHT_PAGE_ID = '32b45ad0-1ef0-8103-a677-f946122dff7d'
SCENARIO_DIR = r'f:\Claude\KirieSaki\Assets\StreamingAssets\Scenarios'
DELIGHT_DIR = r'f:\Claude\Delight'

CHAR_NAMES = {
    'kirie': '逆真キリエ',
    'saki': '入江サキ',
    'suzumura': '鈴村',
    'hakushoku': '白蝕',
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

# ── KirieSaki ──────────────────────────────────────────────
print('=== キリエとサキ ===')
files = sorted([f for f in os.listdir(SCENARIO_DIR) if f.startswith('chapter') and f.endswith('.json')])
for fname in files:
    path = os.path.join(SCENARIO_DIR, fname)
    with open(path, encoding='utf-8') as f:
        data = json.load(f)

    title = data.get('title', fname)
    sub_id = create_page(KIRIE_PAGE_ID, title)
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

print('KirieSaki complete\n')

# ── Delight ────────────────────────────────────────────────
print('=== Delight ===')
delight_files = [
    ('novel_common_v2.txt', '共通章'),
    ('novel_route01_criminal_mariage.txt', 'Route01: Criminal Mariage'),
    ('novel_route02_dominater.txt', 'Route02: Dominater'),
    ('novel_route03_magicatoxin.txt', 'Route03: Magica Toxin'),
    ('novel_route04_murder.txt', 'Route04: Murder'),
    ('novel_route05_pandemonium.txt', 'Route05: Pandemonium'),
    ('novel_route06_patagonist.txt', 'Route06: Patagonist'),
]

for fname, title in delight_files:
    fpath = os.path.join(DELIGHT_DIR, fname)
    if not os.path.exists(fpath):
        print(f'  NOT FOUND: {fname}')
        continue

    with open(fpath, encoding='utf-8') as f:
        text = f.read()

    sub_id = create_page(DELIGHT_PAGE_ID, title)
    if not sub_id:
        print(f'  SKIP {title}')
        continue

    # Split text into blocks by paragraph
    blocks = []
    for para in text.split('\n'):
        para = para.rstrip()
        if not para:
            continue
        if para.startswith('# '):
            blocks.append(make_heading(para[2:], 1))
        elif para.startswith('## '):
            blocks.append(make_heading(para[3:], 2))
        elif para.startswith('### '):
            blocks.append(make_heading(para[4:], 3))
        else:
            # Split long paragraphs
            while len(para) > 1990:
                blocks.append(make_para(para[:1990]))
                para = para[1990:]
            if para:
                blocks.append(make_para(para))

    if blocks:
        upload_blocks(sub_id, blocks)
    print(f'  Done: {title} ({len(blocks)} blocks)')
    time.sleep(0.3)

print('Delight complete')
print('\n=== All uploads finished ===')
