"""
神の降る街 - Asset Generation Script
=====================================
仕様書 gamespec_v1.md / gamespec_基本システム設計_20260322.md に基づく
背景・キャラクターイラスト自動生成スクリプト

【使用ツール】Stable Diffusion WebUI (AUTOMATIC1111) - 完全無料・ローカル
【推奨モデル】Illustrious XL v0.1（ラノベ・耽美系最強）
              animagine-xl-3.1（アニメ全般）
              どちらもCivitaiから無料DL可能

【セットアップ手順】
  1. https://github.com/AUTOMATIC1111/stable-diffusion-webui をgit clone
  2. webui-user.bat の COMMANDLINE_ARGS に --api を追加
  3. webui-user.bat を起動 → http://127.0.0.1:7860 が立ち上がる
  4. Civitai から Illustrious XL をDL → models/Stable-diffusion/ に配置
  5. python generate_assets.py を実行

【実行例】
  python generate_assets.py                   # 全アセット生成
  python generate_assets.py --only characters # キャラのみ
  python generate_assets.py --only backgrounds
  python generate_assets.py --only eventcg
  python generate_assets.py --dump            # APIなしでプロンプト一覧確認
"""

import argparse
import json
import time
import base64
import sys
from pathlib import Path
from dataclasses import dataclass
from typing import Optional
import urllib.request

# ============================================================
# 出力先
# ============================================================
BASE_DIR = Path(__file__).parent / "Images"

# ============================================================
# スタイル定数（耽美・ラノベ的）
# 仕様書 4-5 カラーパレット準拠
# ============================================================

QUALITY = (
    "masterpiece, best quality, ultra detailed, highly detailed, "
    "absurdres, highres, intricate details"
)

STYLE = (
    "light novel illustration, visual novel art, anime style, "
    "dark fantasy, steampunk, gothic aesthetic, "
    "aestheticism, elegant, dramatic lighting, atmospheric, "
    "cinematic composition, deep shadows, moody atmosphere"
)

COLOR_PALETTE = (
    "dark color palette, deep black, dark charcoal, rusty iron, old bronze tones, "
    "parchment white text highlights, deep crimson accents, "
    "teal blue green accents, divine purple, seal gold"
)

NEG = (
    "lowres, bad anatomy, bad hands, text, error, missing fingers, extra digit, "
    "fewer digits, cropped, worst quality, low quality, jpeg artifacts, "
    "signature, watermark, username, blurry, deformed, ugly, duplicate, "
    "mutated, out of frame, bright neon colors, pure white background, "
    "saturated colors, pink, yellow, cyan, magenta, lime green"
)

CHAR_BASE = "full body portrait, standing pose, white background, character sheet"
BG_BASE = "background art, scenery, no characters, no people, wide shot, environment"


@dataclass
class Spec:
    id: str
    filename: str
    out_dir: Path
    positive: str
    negative: str = NEG
    w: int = 832
    h: int = 1216
    steps: int = 28
    cfg: float = 7.0


def char(cid: str, fname: str, subdir: str, desc: str, costume: str, expression: str, extra: str = "") -> Spec:
    parts = [QUALITY, STYLE, COLOR_PALETTE, CHAR_BASE, desc, costume, expression]
    if extra:
        parts.append(extra)
    return Spec(
        id=cid, filename=fname,
        out_dir=BASE_DIR / "Characters" / subdir,
        positive=", ".join(parts),
        w=768, h=1152,   # SDXL 8GB VRAM向けに縮小
    )


def bg(bid: str, fname: str, subdir: str, scene: str, lighting: str, mood: str) -> Spec:
    return Spec(
        id=bid, filename=fname,
        out_dir=BASE_DIR / "Backgrounds" / subdir,
        positive=", ".join([QUALITY, STYLE, COLOR_PALETTE, BG_BASE, scene, lighting, mood]),
        w=1920, h=1080,
        steps=32, cfg=7.5,
    )


def cg(cid: str, fname: str, scene: str) -> Spec:
    return Spec(
        id=cid, filename=fname,
        out_dir=BASE_DIR / "EventCG",
        positive=", ".join([QUALITY, STYLE, COLOR_PALETTE,
                            "event CG illustration, cinematic composition, detailed", scene]),
        w=1920, h=1080,
        steps=35, cfg=8.0,
    )


# ============================================================
# キャラクター（スプライト）
# ============================================================

def get_characters() -> list[Spec]:
    # --- カナタ・イグル（主人公）---
    # 黒外套、右腕に遺骸の侵蝕紋様、寡黙・無表情
    kanata_base = "young man, short dark silver hair, pale skin, sharp cold eyes, lean athletic build"
    kanata_outfit = (
        "black worn long coat with metal buckles and straps, steampunk traveler outfit, "
        "dark gloves, right arm wrapped in cloth with crimson glowing erosion runes beneath"
    )

    # リリス（ヒロイン）
    # 白い肌、首筋に侵蝕の痕、音を立てずに歩く
    lilith_base = "young woman, long silver white hair, very pale almost translucent skin, ethereal fragile beauty, slender"
    lilith_outfit = (
        "worn white tattered dress, thin faded fabric, barefoot, "
        "barely visible dark scar marking at neck throat (erosion mark), "
        "graceful silent presence"
    )

    # エレオノーラ（帝国科学者）
    # 白衣、眼鏡
    eleonora_base = "young woman, short dark brown hair, round glasses, sharp intelligent eyes"
    eleonora_outfit = (
        "white lab coat, steampunk research tools at belt, "
        "mechanical pen and instruments, research notes tucked away"
    )

    # グリーフ（帝国騎士）
    grief_base = "tall imposing man, mid-30s, stern angular face, dark eyes, military bearing, scar on face"
    grief_outfit = (
        "dark steel imperial knight armor, deep crimson military cape, "
        "empire crest insignia, battle-worn armor plates, intimidating silhouette"
    )

    return [
        # カナタ
        char("Kanata", "kanata_default.png", "Kanata",
             kanata_base, kanata_outfit,
             "completely neutral expressionless face, cold distant gaze, mouth closed, stoic"),
        char("Kanata_arm", "kanata_arm.png", "Kanata",
             kanata_base, kanata_outfit,
             "looking down at right arm, slight concern in eyes, examining erosion rune glow",
             "right arm raised slightly, runes glowing brighter"),
        char("Kanata_smile", "kanata_smile.png", "Kanata",
             kanata_base, kanata_outfit,
             "barely perceptible faint smile, warmth in eyes, rare gentle expression, mouth very slightly curved"),
        char("Kanata_wounded", "kanata_wounded.png", "Kanata",
             kanata_base,
             "tattered damaged coat, visible chest, dark crimson erosion veins spreading across chest",
             "pained expression, exhausted but resolute, heavy breathing"),
        char("Kanata_ritual", "kanata_ritual.png", "Kanata",
             kanata_base,
             "dark ceremonial ritual robes, kansou funeral garment, "
             "ornate sealing symbols embroidered in gold, sacred attire",
             "solemn concentrated expression, performing ritual"),

        # リリス
        char("Lilith", "lilith_default.png", "Lilith",
             lilith_base, lilith_outfit,
             "calm composed expression, cool distant gaze, slight melancholy, serene guarded look"),
        char("Lilith_surprised", "lilith_surprised.png", "Lilith",
             lilith_base, lilith_outfit,
             "surprised wide eyes, ears flushed blushing pink-red, startled but controlled, "
             "slight parted lips"),
        char("Lilith_smile", "lilith_smile.png", "Lilith",
             lilith_base, lilith_outfit,
             "rare genuine warm smile, soft shining eyes, precious fleeting happiness, "
             "fragile beauty like porcelain"),

        # エレオノーラ
        char("Eleonora", "eleonora_default.png", "Eleonora",
             eleonora_base, eleonora_outfit,
             "composed intelligent expression, analytical gaze, professional confident demeanor"),
        char("Eleonora_anxious", "eleonora_anxious.png", "Eleonora",
             eleonora_base, eleonora_outfit,
             "anxious worried expression, frustrated desperation, slight disheveled, "
             "clutching papers, bags under eyes"),
        char("Eleonora_monologue", "eleonora_monologue.png", "Eleonora",
             eleonora_base, eleonora_outfit,
             "talking to herself, unfocused distant gaze, lost in thought, "
             "finger touching chin, absorbed in calculations"),

        # グリーフ
        char("Grief", "grief_default.png", "Grief",
             grief_base, grief_outfit,
             "emotionless factual expression, cold calculating stare, "
             "neither mocking nor pitying, absolute certainty"),
        char("Grief_pursuit", "grief_pursuit.png", "Grief",
             grief_base, grief_outfit,
             "focused predatory hunter's gaze, hand resting on sword hilt, "
             "ready to strike, relentless"),
    ]


# ============================================================
# 背景
# ============================================================

def get_backgrounds() -> list[Spec]:
    return [
        # ===== 屋外 =====
        # BG_OUT_001 ナインフォール市街・大通り
        bg("BG_OUT_001_night", "bg_out_001_night.png", "Outdoor",
           "steampunk labyrinth city street, cobblestone road, tall gothic industrial buildings, "
           "steam pipes and gears on walls, gas lamp streetlights, "
           "steam vents billowing fog, narrow winding streets, distant cathedral spires",
           "night time, moonlight, blue-violet sky, deep shadows, warm orange gas lamp glow",
           "oppressive eerie quiet city, gothic industrial atmosphere, dark fantasy urban"),

        bg("BG_OUT_001_day", "bg_out_001_day.png", "Outdoor",
           "steampunk labyrinth city street, cobblestone road, tall gothic industrial buildings, "
           "steam pipes on walls, gas lamps off, industrial workers distant figures",
           "overcast grey sky, cold diffuse daylight, industrial haze, smoke and smog",
           "grim oppressive daytime, dark fantasy city, cold and unwelcoming"),

        # BG_OUT_002 降骸広場（中央祭壇跡）
        bg("BG_OUT_002", "bg_out_002_night.png", "Outdoor",
           "ruined central city plaza, destroyed stone altar at center, "
           "impact craters in cobblestones, scattered divine bone fragments faintly glowing, "
           "surrounding gothic facades, wide open square",
           "dusk transitioning to night, remnant crimson sunset, stars appearing, "
           "ghostly pale glow from divine relic fragments on ground",
           "ominous sacred fallen site, aftermath of divine descent, "
           "supernatural eerie atmosphere"),

        # BG_OUT_006 聖堂区・石畳広場
        bg("BG_OUT_006", "bg_out_006_night.png", "Outdoor",
           "cathedral district stone plaza, old church facades, stone gargoyles, "
           "tall dark stained glass windows, candles in stone alcoves, religious district",
           "night, moonlight casting deep blue shadows, candle warmth in alcoves",
           "sacred yet decayed atmosphere, ritualistic space, funeral rite setting"),

        # BG_OUT_010 屋上・給水塔前（夕）
        bg("BG_OUT_010", "bg_out_010_dusk.png", "Outdoor",
           "rooftop view, large water storage tower, panoramic city skyline, "
           "industrial chimneys smoking, distant cathedral spires, "
           "steam rising from city below, mechanical pipes and cables",
           "dramatic dusk sunset, crimson orange sky, silhouette city view, "
           "fog rolling in, sun just setting",
           "melancholic city overview, isolation on rooftop, "
           "contemplative vantage point"),

        # 荒野（第8〜9話）
        bg("BG_WILDLANDS_night", "bg_wildlands_night.png", "Outdoor",
           "desolate wasteland outside city walls, barren rocky terrain, "
           "sparse dead vegetation, distant city walls visible on horizon, "
           "vast open sky",
           "starry night sky, moonlight on rocks, cold blue-silver light",
           "freedom outside oppressive city, vast emptiness, "
           "melancholic new beginning"),

        # ===== 屋内 =====
        # BG_IN_001 カナタの部屋
        bg("BG_IN_001", "bg_in_001.png", "Indoor",
           "sparse austere room, worn wooden floorboards, single grimy window, "
           "simple bed with dark covers, black coat hanging on hook, "
           "old wooden desk, small relic containment box, dim oil lantern",
           "dim amber lantern light, night visible through small window, "
           "single moody light source",
           "isolated solitary dwelling, character's sparse private space, "
           "minimalist austere poverty"),

        # BG_IN_003 緘葬組合・儀式準備室
        bg("BG_IN_003", "bg_in_003.png", "Indoor",
           "ritual preparation chamber, stone walls with carved glowing runes, "
           "ceremonial tools arranged on shelves, tall candles on iron holders, "
           "sealing chains and ritual circle on floor, ancient tomes, "
           "incense smoke rising",
           "candlelight only, warm gold against deep shadows, "
           "flickering dramatic flame light",
           "sacred mystical ritual space, funeral guild preparation, "
           "solemn ceremonial atmosphere"),

        # BG_IN_005 廃聖堂・身廊
        bg("BG_IN_005", "bg_in_005.png", "Indoor",
           "abandoned cathedral nave interior, tall gothic pointed arches, "
           "broken stained glass windows with moonlight, overgrown with vines and moss, "
           "collapsed broken pews, rubble and debris on floor",
           "cold moonlight beams cutting through dust and darkness, "
           "dramatic shaft of pale light, deep shadows",
           "haunted abandoned sacred space, gothic decay, "
           "eerie yet beautiful ruin"),

        # BG_IN_005 遺骸汚染版
        bg("BG_IN_005_corrupt", "bg_in_005_corrupted.png", "Indoor",
           "abandoned cathedral nave corrupted by divine relic, "
           "black twisted organic vines spreading across walls and floor, "
           "pulsing crimson glowing veins in stone walls, "
           "supernatural contamination growing",
           "ominous dark purple red glow from veins, eldritch crimson illumination",
           "divine relic contamination spreading, supernatural horror, "
           "dangerous forbidden energy"),

        # BG_IN_006 遺骸研究所・実験室
        bg("BG_IN_006", "bg_in_006.png", "Indoor",
           "steampunk imperial research laboratory, "
           "glass containment vessels with glowing divine specimens, "
           "brass and copper mechanical apparatus, books and notes everywhere, "
           "specimen jars with preserved relics, measurement instruments, "
           "imperial research facility interior",
           "cold blue-white laboratory light, warm desk lamp spots, "
           "glowing specimens provide secondary light",
           "scientific cold clinical atmosphere, imperial science ambition, "
           "fascinating and unsettling"),

        # 地下道（第8話脱出）
        bg("BG_TUNNEL", "bg_tunnel.png", "Indoor",
           "underground escape tunnel, rough hewn stone walls, "
           "narrow stone passage, ancient drainage channel, "
           "iron torch sconces on walls, dripping water, "
           "darkness stretching ahead and behind",
           "dim orange torch light, warm glow against cold dark stone, "
           "deep dramatic shadows",
           "urgent tense escape atmosphere, underground labyrinth, "
           "claustrophobic danger"),

        # ===== 特殊・幻想 =====
        # BG_SP_001 遺骸内部（黒虚空）
        bg("BG_SP_001_black", "bg_sp_001_black.png", "Special",
           "infinite abstract black void, divine relic inner consciousness space, "
           "floating fragments of bone-white divine matter, "
           "crimson glowing veins in absolute darkness, "
           "supernatural divine consciousness, fragments drifting",
           "absolute darkness, only crimson and seal gold light sources, "
           "divine mysterious radiance, no other colors",
           "inside divine relic mind, the god's dying dream, "
           "otherworldly existential space"),

        # BG_SP_001 白虚空
        bg("BG_SP_001_white", "bg_sp_001_white.png", "Special",
           "infinite pure white void, overwhelming divine light space, "
           "faint silhouettes barely visible at distance, "
           "abstract divine transcendent realm, pure emptiness",
           "pure white overwhelming radiance, soft gradient transitions, "
           "blinding sacred light",
           "divine overwhelming presence, transcendent sacred realm, "
           "dream-like dissolution"),

        # BG_SP_004 ナインフォール俯瞰（神視点）
        bg("BG_SP_004", "bg_sp_004.png", "Special",
           "aerial god's eye view of entire labyrinth city, "
           "full city visible from high above, circular concentric layout, "
           "cathedral spires, industrial smoke, city walls surrounding all, "
           "tiny buildings below",
           "otherworldly divine lighting from above, dawn light, "
           "god's perspective illumination",
           "divine omniscient perspective over city, "
           "city as cage and labyrinth from above"),

        # 焚き火（第9話）
        bg("BG_CAMPFIRE", "bg_campfire_night.png", "Special",
           "wilderness campfire at night, rocks arranged around fire, "
           "three empty spots around flames, sparse dead scrub, "
           "vast starry sky, distant silhouette of city walls on horizon",
           "campfire as sole warm light source, "
           "orange warm center against cold deep blue night sky",
           "intimate gathering place, peace after long escape, "
           "melancholic quiet hope, first rest"),
    ]


# ============================================================
# イベントCG
# ============================================================

def get_eventcg() -> list[Spec]:
    return [
        # EV_CG_001: オープニング
        cg("EV_CG_001", "ev_cg_001_opening.png",
           "divine relic falling from sky, massive glowing bone-white divine fragment descending, "
           "young man in dark cloak standing below looking up in awe, "
           "dramatic scale contrast tiny figure vs enormous divine relic, "
           "city silhouette in background, crimson and gold light radiating from falling relic, "
           "opening title illustration"),

        # EV_CG_003: 緘葬の儀式
        cg("EV_CG_003", "ev_cg_003_ritual.png",
           "two figures side by side in sacred ritual ceremony, "
           "young man in dark ritual robes and young woman in white, "
           "surrounding candlelight, glowing ritual circle on stone floor, "
           "solemn sacred funeral sealing ceremony, intimate yet ceremonial moment, "
           "wax dripping candles, smoke rising"),

        # EV_CG_005: リリスが手を差し伸べる
        cg("EV_CG_005", "ev_cg_005_hand.png",
           "young woman with long silver white hair extending pale hand toward viewer, "
           "fragile beautiful face with soft expression, "
           "young man in black coat hesitating in background, "
           "emotional turning point scene, backlit with divine soft light, "
           "she reaches forward, viewer's choice moment"),

        # EV_CG_009: 外套を渡す（第9話）
        cg("EV_CG_009", "ev_cg_009_cloak.png",
           "campfire scene in wilderness at night, "
           "young man silently offering dark cloak to young woman, "
           "she receives it, both silent without words, "
           "pure quiet understated emotion, "
           "warm orange firelight, vast starry sky above, "
           "intimate wordless moment"),
    ]


# ============================================================
# SD WebUI API
# ============================================================

class SDWebUI:
    def __init__(self, url: str = "http://127.0.0.1:7860"):
        self.url = url.rstrip("/")

    def alive(self) -> bool:
        try:
            urllib.request.urlopen(f"{self.url}/sdapi/v1/sd-models", timeout=5)
            return True
        except Exception:
            return False

    def generate(self, spec: Spec) -> Optional[bytes]:
        # 高解像度BGはHiRes.fixで拡大（8GB VRAM向けに縮小率を大きめに）
        use_hr = spec.w >= 1280 or spec.h >= 768
        gen_w = int(spec.w / 2.0) if use_hr else spec.w
        gen_h = int(spec.h / 2.0) if use_hr else spec.h

        payload: dict = {
            "prompt": spec.positive,
            "negative_prompt": spec.negative,
            "width": gen_w,
            "height": gen_h,
            "steps": spec.steps,
            "cfg_scale": spec.cfg,
            "sampler_name": "DPM++ 2M Karras",
            "batch_size": 1,
            "n_iter": 1,
            "seed": -1,
        }

        if use_hr:
            payload.update({
                "enable_hr": True,
                "hr_scale": 2.0,
                "hr_upscaler": "4x-UltraSharp",
                "hr_second_pass_steps": 15,
                "denoising_strength": 0.4,
            })

        data = json.dumps(payload).encode()
        req = urllib.request.Request(
            f"{self.url}/sdapi/v1/txt2img",
            data=data,
            headers={"Content-Type": "application/json"},
            method="POST",
        )
        with urllib.request.urlopen(req, timeout=600) as r:
            result = json.loads(r.read())

        return base64.b64decode(result["images"][0])


# ============================================================
# メインループ
# ============================================================

def run(backend: SDWebUI, specs: list[Spec], skip_existing: bool = True):
    total = len(specs)
    ok, ng = 0, []

    for i, s in enumerate(specs, 1):
        path = s.out_dir / s.filename

        if skip_existing and path.exists():
            print(f"[{i:03}/{total}] SKIP  {s.filename}")
            ok += 1
            continue

        print(f"[{i:03}/{total}] GEN   {s.id}  {s.w}x{s.h}  steps={s.steps}")
        try:
            data = backend.generate(s)
            if data:
                s.out_dir.mkdir(parents=True, exist_ok=True)
                path.write_bytes(data)
                print(f"       → {path}")
                ok += 1
            else:
                ng.append(s.id)
        except Exception as e:
            print(f"       ✗ {e}")
            ng.append(s.id)
            # WebUI がクラッシュした可能性 → 再起動を待つ
            print("       WebUI再起動を待機中 (30秒)...")
            for _ in range(6):
                time.sleep(5)
                if backend.alive():
                    print("       WebUI 復帰確認")
                    break

        time.sleep(1)

    print(f"\n完了: {ok}/{total}  失敗: {ng if ng else 'なし'}")


def dump_prompts(specs: list[Spec]):
    out = [{"id": s.id, "file": s.filename, "dir": str(s.out_dir.relative_to(BASE_DIR.parent)),
            "size": f"{s.w}x{s.h}", "prompt": s.positive} for s in specs]
    p = BASE_DIR.parent / "prompts_dump.json"
    p.write_text(json.dumps(out, ensure_ascii=False, indent=2), encoding="utf-8")
    print(f"プロンプト一覧: {p}  (計 {len(specs)} 件)")


# ============================================================
# エントリーポイント
# ============================================================

def main():
    ap = argparse.ArgumentParser(description="神の降る街 イラスト自動生成 (SD WebUI)")
    ap.add_argument("--url", default="http://127.0.0.1:7860", help="SD WebUI URL")
    ap.add_argument("--only", choices=["characters", "backgrounds", "eventcg", "all"], default="all")
    ap.add_argument("--overwrite", action="store_true", help="既存ファイルも再生成")
    ap.add_argument("--dump", action="store_true", help="プロンプト確認のみ（APIなし）")
    args = ap.parse_args()

    specs: list[Spec] = []
    if args.only in ("characters", "all"):
        c = get_characters(); specs += c; print(f"キャラクター: {len(c)} 件")
    if args.only in ("backgrounds", "all"):
        b = get_backgrounds(); specs += b; print(f"背景: {len(b)} 件")
    if args.only in ("eventcg", "all"):
        e = get_eventcg(); specs += e; print(f"イベントCG: {len(e)} 件")
    print(f"合計: {len(specs)} 件\n")

    if args.dump:
        dump_prompts(specs)
        return

    backend = SDWebUI(args.url)
    print(f"SD WebUI 接続確認: {args.url}")
    if not backend.alive():
        print("✗ 接続できません。")
        print("  → AUTOMATIC1111 を --api フラグ付きで起動してください")
        print("  例: webui-user.bat に set COMMANDLINE_ARGS=--api を追加")
        sys.exit(1)
    print("✓ 接続OK\n")

    run(backend, specs, skip_existing=not args.overwrite)


if __name__ == "__main__":
    main()
