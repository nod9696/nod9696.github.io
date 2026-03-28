# 神の降る街 イラスト生成セットアップガイド

## 必要なもの（すべて無料）

| ツール | 目的 | URL |
|--------|------|-----|
| Stable Diffusion WebUI (AUTOMATIC1111) | 生成エンジン | https://github.com/AUTOMATIC1111/stable-diffusion-webui |
| Illustrious XL v0.1 | モデル（耽美・ラノベ系） | https://civitai.com/models/795765 |
| Python 3.10+ | スクリプト実行 | 標準インストール |

---

## セットアップ手順

### 1. AUTOMATIC1111 WebUI のインストール

```bash
git clone https://github.com/AUTOMATIC1111/stable-diffusion-webui
cd stable-diffusion-webui
```

Windows の場合は `webui-user.bat` を開き、以下を追記：
```bat
set COMMANDLINE_ARGS=--api --xformers --medvram
```
※ VRAMが8GB以上あれば `--medvram` は不要

### 2. モデルのダウンロードと配置

**推奨モデル（耽美・ラノベ的に最強）**

| 優先度 | モデル名 | 特徴 |
|--------|----------|------|
| ★★★ | **Illustrious XL v0.1** | ラノベ・耽美系に最適、細部が美麗 |
| ★★☆ | animagine-xl-3.1 | アニメ全般、安定した品質 |
| ★★☆ | AnythingXL | 汎用アニメ |

1. Civitai から `.safetensors` ファイルをDL
2. `stable-diffusion-webui/models/Stable-diffusion/` に配置

**アップスケーラー（高解像度背景に必要）**
- `4x-UltraSharp.pth` → `models/ESRGAN/` に配置
- DL: https://upscale.wiki/wiki/Model_Database

### 3. WebUI 起動

```
webui-user.bat をダブルクリック
→ http://127.0.0.1:7860 が起動したら準備完了
```

### 4. 生成スクリプト実行

```bash
cd f:\Claude\KamiNoFuruMachi\Assets

# プロンプト確認（APIなし・最初に確認推奨）
python generate_assets.py --dump

# 全アセット生成
python generate_assets.py

# キャラクターのみ
python generate_assets.py --only characters

# 背景のみ
python generate_assets.py --only backgrounds

# イベントCGのみ
python generate_assets.py --only eventcg

# 既存ファイルも再生成したい場合
python generate_assets.py --overwrite
```

---

## 生成されるアセット一覧

### キャラクタースプライト（13枚）
```
Images/Characters/
├── Kanata/
│   ├── kanata_default.png      通常（無表情）
│   ├── kanata_arm.png          右腕を見る
│   ├── kanata_smile.png        わずかに笑う
│   ├── kanata_wounded.png      負傷・侵蝕進行
│   └── kanata_ritual.png       緘葬装束
├── Lilith/
│   ├── lilith_default.png      通常（冷静）
│   ├── lilith_surprised.png    驚き（耳が赤い）
│   └── lilith_smile.png        笑い（稀な表情）
├── Eleonora/
│   ├── eleonora_default.png    通常（研究者）
│   ├── eleonora_anxious.png    焦燥
│   └── eleonora_monologue.png  独り言
└── Grief/
    ├── grief_default.png       通常（無感情）
    └── grief_pursuit.png       追跡モード
```

### 背景（18枚）
```
Images/Backgrounds/
├── Outdoor/
│   ├── bg_out_001_night.png    ナインフォール市街・夜
│   ├── bg_out_001_day.png      ナインフォール市街・昼
│   ├── bg_out_002_night.png    降骸広場・夜
│   ├── bg_out_006_night.png    聖堂区石畳・夜
│   ├── bg_out_010_dusk.png     屋上給水塔・夕
│   └── bg_wildlands_night.png  荒野・夜（第8〜9話）
├── Indoor/
│   ├── bg_in_001.png           カナタの部屋
│   ├── bg_in_003.png           緘葬組合・儀式準備室
│   ├── bg_in_005.png           廃聖堂・身廊
│   ├── bg_in_005_corrupted.png 廃聖堂・遺骸汚染
│   ├── bg_in_006.png           遺骸研究所・実験室
│   └── bg_tunnel.png           地下道（第8話脱出）
└── Special/
    ├── bg_sp_001_black.png     遺骸内部空間・黒虚空
    ├── bg_sp_001_white.png     遺骸内部空間・白虚空
    ├── bg_sp_004.png           ナインフォール俯瞰（神視点）
    └── bg_campfire_night.png   荒野の焚き火（第9話）
```

### イベントCG（4枚）
```
Images/EventCG/
├── ev_cg_001_opening.png   EV_CG_001: 遺骸落下・カナタが見上げる
├── ev_cg_003_ritual.png    EV_CG_003: 緘葬の儀式・二人並んで
├── ev_cg_005_hand.png      EV_CG_005: リリスが手を差し伸べる
└── ev_cg_009_cloak.png     EV_CG_009: 外套を渡す（第9話）
```

---

## プロンプト調整のヒント

### キャラクターをより統一感よく生成したい場合

SD WebUI の **Textual Inversion** や **LoRA** で
キャラクターの外見を固定すると一貫性が上がります。

キャラごとの外見固定：
```
カナタ: 黒外套、右腕侵蝕紋様、銀灰色短髪
リリス: 白銀ロングヘア、白ドレス、首筋の痕
```

### 品質を上げたい場合

`generate_assets.py` 内の `QUALITY` 定数に追加：
```python
QUALITY = "..., ultra-detailed face, perfect eyes, ..."
```

ステップ数を増やす（`steps: int = 28` → 40）

### Unity で使う際のサイズ調整

| 用途 | 推奨サイズ | スクリプト内変数 |
|------|----------|----------------|
| キャラスプライト | 832×1216 | `w=832, h=1216` |
| 背景（FHD） | 1920×1080 | `w=1920, h=1080` |
| イベントCG | 1920×1080 | `w=1920, h=1080` |
