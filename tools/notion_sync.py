#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Notion sync script for novels.
Tracks file modification times and only re-uploads changed files.
Manifest: f:/Claude/notion_sync_manifest.json
"""
import json, os, time, urllib.request, urllib.error, sys

TOKEN    = '$(os.environ.get("NOTION_SYNC_TOKEN", ""))'
MANIFEST = r'f:\Claude\notion_sync_manifest.json'

HEADERS = {
    'Authorization': f'Bearer {TOKEN}',
    'Notion-Version': '2022-06-28',
    'Content-Type': 'application/json',
}

# Parent page IDs in Notion
PAGE_IDS = {
    'kirie':  '32b45ad0-1ef0-81be-bdc1-c48646692738',
    'delight': '32b45ad0-1ef0-8103-a677-f946122dff7d',
    'kami':   '32b45ad0-1ef0-81dd-a788-cbc255172d1d',
}

# Source definitions: (source_dir, parent_key, file_pattern, char_names_dict)
SOURCES = {
    'kirie': {
        'dir': r'f:\Claude\KirieSaki\Assets\StreamingAssets\Scenarios',
        'parent': 'kirie',
        'pattern': lambda f: f.startswith('chapter') and f.endswith('.json'),
        'chars': {'kirie':'逆真キリエ','saki':'入江サキ','suzumura':'鈴村','hakushoku':'白蝕'},
        'mode': 'scenario_json',
    },
    'kirie_docs': {
        'dir': r'f:\Claude\KirieSaki',
        'parent': 'kirie',
        'files': [
            ('chara_sheet.md', 'キャラクター設定書'),
        ],
        'mode': 'markdown_text',
    },
    'kami': {
        'dir': r'f:\Claude\KamiNoFuruMachi\My project\Assets\StreamingAssets\Scenarios',
        'parent': 'kami',
        'pattern': lambda f: f.startswith('chapter') and f.endswith('.json'),
        'chars': {'kanata':'カナタ・イグル','lilith':'リリス','eleonora':'エレオノーラ','lucius':'ルキウス','grief':'グリーフ'},
        'mode': 'scenario_json',
    },
    'delight_common': {
        'dir': r'f:\Claude\Delight',
        'parent': 'delight',
        'files': [
            ('novel_common_v2.txt',             '共通章'),
            ('novel_route01_criminal_mariage.txt','Route01: Criminal Mariage'),
            ('novel_route02_dominater.txt',       'Route02: Dominater'),
            ('novel_route03_magicatoxin.txt',     'Route03: Magica Toxin'),
            ('novel_route04_murder.txt',          'Route04: Murder'),
            ('novel_route05_pandemonium.txt',      'Route05: Pandemonium'),
            ('novel_route06_patagonist.txt',       'Route06: Patagonist'),
            ('character_settings.txt',              'キャラクター設定書'),
            ('character_settings_v2.txt',           'キャラクター設定書 v2（ラノベ版）'),
            ('content_Plot_main_Pandemonium_v2.txt','Plot: Pandemonium v2'),
        ],
        'mode': 'markdown_text',
    },
}

# ── Notion API ─────────────────────────────────────────────

def api(method, path, body=None):
    url = f'https://api.notion.com/v1{path}'
    data = json.dumps(body, ensure_ascii=False).encode('utf-8') if body else None
    req = urllib.request.Request(url, data=data, headers=HEADERS, method=method)
    try:
        with urllib.request.urlopen(req) as r:
            return json.loads(r.read().decode('utf-8'))
    except urllib.error.HTTPError as e:
        print(f'  API ERROR {e.code}: {e.read().decode()[:200]}')
        return None

def create_page(parent_id, title):
    r = api('POST', '/pages', {
        'parent': {'page_id': parent_id},
        'properties': {'title': {'title': [{'text': {'content': title}}]}}
    })
    return r['id'] if r else None

def archive_page(page_id):
    api('PATCH', f'/pages/{page_id}', {'archived': True})

def upload_blocks(page_id, blocks):
    i = 0
    while i < len(blocks):
        chunk = blocks[i:i+100]
        api('PATCH', f'/blocks/{page_id}/children', {'children': chunk})
        i += 100
        time.sleep(0.35)

def make_para(text):
    return {'type':'paragraph','paragraph':{'rich_text':[{'type':'text','text':{'content':text[:1990]}}]}}

def make_heading(text, level):
    t = f'heading_{level}'
    return {'type':t, t:{'rich_text':[{'type':'text','text':{'content':text}}]}}

# ── Block builders ─────────────────────────────────────────

def blocks_from_scenario(data, chars):
    blocks = []
    for cmd in data.get('commands', []):
        if cmd.get('cmd') == 'text':
            char = cmd.get('char','')
            body = cmd.get('body','')
            if char == 'narrator':
                blocks.append(make_para(body))
            else:
                name = chars.get(char, char)
                blocks.append(make_para(f'{name}「{body}」'))
    return blocks

def blocks_from_markdown(text):
    blocks = []
    for line in text.splitlines():
        line = line.rstrip()
        if not line:
            continue
        if line.startswith('### '):
            blocks.append(make_heading(line[4:], 3))
        elif line.startswith('## '):
            blocks.append(make_heading(line[3:], 2))
        elif line.startswith('# '):
            blocks.append(make_heading(line[2:], 1))
        else:
            while len(line) > 1990:
                blocks.append(make_para(line[:1990]))
                line = line[1990:]
            blocks.append(make_para(line))
    return blocks

# ── Manifest ───────────────────────────────────────────────

def load_manifest():
    if os.path.exists(MANIFEST):
        with open(MANIFEST, encoding='utf-8') as f:
            return json.load(f)
    return {}

def save_manifest(m):
    with open(MANIFEST, 'w', encoding='utf-8') as f:
        json.dump(m, f, ensure_ascii=False, indent=2)

# ── Sync logic ─────────────────────────────────────────────

def sync_file(key, parent_id, title, fpath, get_blocks_fn, manifest):
    mtime = os.path.getmtime(fpath)
    entry = manifest.get(key, {})

    if entry.get('mtime') == mtime and entry.get('page_id'):
        return False  # no change

    # Archive old page if exists
    if entry.get('page_id'):
        archive_page(entry['page_id'])
        time.sleep(0.2)

    page_id = create_page(parent_id, title)
    if not page_id:
        return False

    blocks = get_blocks_fn()
    if blocks:
        upload_blocks(page_id, blocks)

    manifest[key] = {'mtime': mtime, 'page_id': page_id, 'title': title}
    return True

def run_sync(force=False):
    manifest = load_manifest()
    updated = 0

    for src_key, cfg in SOURCES.items():
        mode = cfg['mode']
        parent_id = PAGE_IDS[cfg['parent']]

        if mode == 'scenario_json':
            d = cfg['dir']
            chars = cfg['chars']
            if not os.path.isdir(d):
                continue
            files = sorted([f for f in os.listdir(d) if cfg['pattern'](f)])
            for fname in files:
                fpath = os.path.join(d, fname)
                mk = f'{src_key}/{fname}'
                def get_blocks(fp=fpath, ch=chars):
                    with open(fp, encoding='utf-8') as f:
                        data = json.load(f)
                    return blocks_from_scenario(data, ch)
                def get_title(fp=fpath):
                    with open(fp, encoding='utf-8') as f:
                        data = json.load(f)
                    return data.get('title', fname)
                if force:
                    manifest.pop(mk, None)
                changed = sync_file(mk, parent_id, get_title(), fpath, get_blocks, manifest)
                if changed:
                    print(f'  Updated: {get_title()}')
                    updated += 1
                    save_manifest(manifest)
                    time.sleep(0.3)

        elif mode == 'markdown_text':
            d = cfg['dir']
            for fname, title in cfg['files']:
                fpath = os.path.join(d, fname)
                if not os.path.exists(fpath):
                    continue
                mk = f'{src_key}/{fname}'
                def get_blocks(fp=fpath):
                    with open(fp, encoding='utf-8') as f:
                        return blocks_from_markdown(f.read())
                if force:
                    manifest.pop(mk, None)
                changed = sync_file(mk, parent_id, title, fpath, get_blocks, manifest)
                if changed:
                    print(f'  Updated: {title}')
                    updated += 1
                    save_manifest(manifest)
                    time.sleep(0.3)

    save_manifest(manifest)
    return updated

# ── Entry point ────────────────────────────────────────────

if __name__ == '__main__':
    force = '--force' in sys.argv
    target = next((a for a in sys.argv[1:] if not a.startswith('--')), None)

    if force:
        print('Force re-upload all files...')

    # Filter sources if target specified
    if target:
        filtered = {k: v for k, v in SOURCES.items() if target in k}
        if filtered:
            orig = SOURCES.copy()
            SOURCES.clear()
            SOURCES.update(filtered)
            n = run_sync(force)
            SOURCES.clear()
            SOURCES.update(orig)
        else:
            print(f'Unknown target: {target}')
            n = 0
    else:
        n = run_sync(force)

    if n == 0:
        print('All files up to date.')
    else:
        print(f'\nSync complete: {n} file(s) updated.')
