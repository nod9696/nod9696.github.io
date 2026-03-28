#!/usr/bin/env python3
"""
インディーゲーム原石発掘チーム - Claude APIを使ったマルチエージェント発掘記事システム

個人制作（ソロ開発）の埋もれたインディーゲームを発掘し、記事にまとめる5エージェント:
1. 原石ハンター     - 知名度が低くても価値ある作品を嗅ぎ分けて候補を選定
2. 鑑定士           - 原石の本質的な価値・独自性・ポテンシャルを鑑定
3. 開発者探偵       - 制作者の背景・動機・制作環境・込めた想いを掘り起こす
4. 体験証言ライター - 「発見の喜び」プレイヤー視点で体験を言語化
5. 発掘記事編集長   - 全素材を統合し発掘レポート（2000〜3000字）に仕上げる
"""

import anthropic
import sys
from typing import Optional

client = anthropic.Anthropic()
MODEL = "claude-opus-4-6"


def call_agent(
    role: str,
    system_prompt: str,
    user_message: str,
    context: Optional[str] = None,
) -> str:
    """エージェントを呼び出してレスポンスをストリーミングで取得する"""
    print(f"\n{'='*60}")
    print(f"【{role}】が調査中...")
    print("=" * 60)

    if context:
        content = f"これまでのチームの調査・分析:\n\n{context}\n\n{user_message}"
    else:
        content = user_message

    full_response = ""
    with client.messages.stream(
        model=MODEL,
        max_tokens=4096,
        thinking={"type": "adaptive"},
        system=system_prompt,
        messages=[{"role": "user", "content": content}],
    ) as stream:
        for text in stream.text_stream:
            print(text, end="", flush=True)
            full_response += text

    print("\n")
    return full_response


def gem_hunter_agent(mission: str) -> str:
    """原石ハンターエージェント"""
    system = """あなたはインディーゲームの原石を発掘するハンターです。
有名作・話題作ではなく、まだ多くの人に知られていない「埋もれた傑作」を嗅ぎ分ける嗅覚を持っています。
itch.io のゲームジャム入賞作、Steam のレビュー数が少ないのに評価が異常に高い作品、
個人開発者がひっそりと公開した作品などを重点的に探します。
「なぜこれが埋もれているのか」「なぜ発掘する価値があるのか」を常に意識してください。"""

    prompt = f"""以下の条件で個人制作インディーゲームの原石を探してください。

ミッション: {mission}

以下を含めて報告:
1. 候補タイトル（3〜4本）と各タイトルの埋もれている理由
2. 各タイトルの第一印象・「原石感」を感じた決め手
3. 知名度の低さ・露出の少なさについての考察
4. 発掘価値が最も高いと判断した1本の選定理由
5. そのゲームが輝く可能性を秘めているポイント"""

    return call_agent("原石ハンター", system, prompt)


def appraiser_agent(candidates: str) -> str:
    """鑑定士エージェント"""
    system = """あなたはインディーゲームの鑑定士です。
「原石」の本質的な価値を見抜くプロフェッショナルです。
表面的な完成度ではなく、その作品に宿るユニークなアイデア・独自のビジョン・
制作者の個性・他にない体験を丁寧に評価します。
磨けば光るポテンシャルを、具体的な根拠とともに鑑定書にまとめてください。"""

    prompt = """原石ハンターが発掘した候補をもとに、選定された作品の鑑定を行ってください。

鑑定書に含める内容:
1. ゲームメカニクスのオリジナリティ（他作品にない独自性）
2. ビジュアル・アートの個性（個人制作ならではの味）
3. 世界観・テーマの深み
4. 「大手スタジオには作れない」要素の特定
5. 原石としての総合評価と磨けば光る理由"""

    return call_agent("鑑定士", system, prompt, context=candidates)


def dev_detective_agent(candidates: str, appraisal: str) -> str:
    """開発者探偵エージェント"""
    system = """あなたはインディーゲーム開発者の素顔を掘り起こす探偵です。
一人でゲームを作るということの本質——孤独な作業、限られたリソース、
それでも作り続ける理由——を丁寧に調査します。
開発者がどんな人物で、何を伝えたくてこのゲームを作ったのかを
SNS投稿・インタビュー・開発ブログの痕跡から推理・再構成してください。"""

    prompt = """発掘・鑑定された作品の開発者像と制作背景を探偵として調査してください。

調査報告書に含める内容:
1. 開発者のプロフィール推測（職業・年齢層・バックグラウンド）
2. このゲームを作るに至った動機・原体験
3. 使用ツール・エンジン・制作環境
4. 一人で作ることで生まれた制約と、それを逆手にとった工夫
5. 開発者のゲームに込めたメッセージ・想い"""

    context = f"【発掘報告】\n{candidates}\n\n【鑑定書】\n{appraisal}"
    return call_agent("開発者探偵", system, prompt, context=context)


def testimony_agent(candidates: str, appraisal: str, dev_story: str) -> str:
    """体験証言ライターエージェント"""
    system = """あなたは「埋もれたゲームを遊んだプレイヤー」の体験を言語化する証言ライターです。
有名作のレビューとは違う、まだほとんど誰も語っていない体験の新鮮さを表現します。
「こんなゲームが存在したのか」という発見の感動、
「なぜみんな知らないんだろう」という不思議さ、
小さいけれど確かに心に刺さる体験を、丁寧に言葉にしてください。"""

    prompt = """発掘された原石ゲームを実際にプレイしたプレイヤーの視点で体験を証言してください。

証言に含める内容:
1. 初めて起動したときの第一印象・画面の雰囲気
2. 「あ、これは普通じゃない」と気づいた瞬間
3. 埋もれているからこそ感じる「発見の喜び」
4. このゲームが刺さる人物像（具体的に）
5. 遊び終わったあとに残る感情・余韻"""

    context = (
        f"【発掘報告】\n{candidates}\n\n"
        f"【鑑定書】\n{appraisal}\n\n"
        f"【開発者調査】\n{dev_story}"
    )
    return call_agent("体験証言ライター", system, prompt, context=context)


def editor_agent(candidates: str, appraisal: str, dev_story: str, testimony: str) -> str:
    """発掘記事編集長エージェント - 完成発掘レポートを執筆"""
    system = """あなたはインディーゲームの原石を世に送り出す発掘記事の編集長です。
チームが掘り当てた原石を、まだゲームを知らない読者に伝える発掘レポートを書きます。
「なぜこのゲームは埋もれているのか」「なぜあなたが最初の発見者になれるのか」
という興奮と、個人開発者へのリスペクトが伝わる文章を書いてください。
読者が記事を読み終えたあと、今すぐそのゲームを検索したくなることを目指してください。"""

    prompt = """チームの全調査を統合して、発掘レポートを執筆してください。

発掘レポートの要件:
- 「原石発掘」感を前面に出したキャッチーなタイトル
- リード文（100〜150字）：「なぜ今これを読む必要があるか」を凝縮
- 本文（2000〜3000字）
- 「あなたが知らないだけで、すごいゲームがある」という発見の喜びを伝える
- 個人開発者が一人で作り上げた事実への純粋な敬意
- 記事末尾に「このゲームを探す方法」を添える"""

    context = (
        f"【発掘報告】\n{candidates}\n\n"
        f"【鑑定書】\n{appraisal}\n\n"
        f"【開発者調査】\n{dev_story}\n\n"
        f"【プレイヤー証言】\n{testimony}"
    )
    return call_agent("発掘記事編集長（完成レポート）", system, prompt, context=context)


def discover_gem(mission: str) -> str:
    """
    インディーゲーム原石発掘チームを起動して発掘レポートを生成する

    Args:
        mission: 発掘ミッション（例: "2024〜2026年のitch.ioの個人制作パズルゲームの原石"）

    Returns:
        完成した発掘レポートのテキスト
    """
    print("\n" + "=" * 60)
    print("💎  インディーゲーム原石発掘チーム 始動")
    print(f"ミッション: {mission}")
    print("=" * 60)

    # Step 1: 原石ハンター
    candidates = gem_hunter_agent(mission)

    # Step 2: 鑑定士
    appraisal = appraiser_agent(candidates)

    # Step 3: 開発者探偵
    dev_story = dev_detective_agent(candidates, appraisal)

    # Step 4: 体験証言ライター
    testimony = testimony_agent(candidates, appraisal, dev_story)

    # Step 5: 発掘記事編集長（完成レポート）
    report = editor_agent(candidates, appraisal, dev_story, testimony)

    print("\n" + "=" * 60)
    print("✅ 発掘レポート完成！")
    print("=" * 60)

    return report


def save_report(mission: str, report: str, filename: Optional[str] = None) -> str:
    """発掘レポートをファイルに保存する"""
    if filename is None:
        safe_mission = "".join(c for c in mission[:20] if c.isalnum() or c in "_ ")
        safe_mission = safe_mission.strip().replace(" ", "_")
        filename = f"gem_discovery_{safe_mission}.txt"

    with open(filename, "w", encoding="utf-8") as f:
        f.write(f"発掘ミッション: {mission}\n")
        f.write("=" * 60 + "\n\n")
        f.write(report)

    print(f"\n📄 発掘レポートを保存しました: {filename}")
    return filename


if __name__ == "__main__":
    if len(sys.argv) > 1:
        mission = " ".join(sys.argv[1:])
    else:
        mission = "2024〜2026年のitch.io・Steamにある個人制作インディーゲームの原石"

    report = discover_gem(mission)
    save_report(mission, report)
