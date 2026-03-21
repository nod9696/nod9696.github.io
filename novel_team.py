#!/usr/bin/env python3
"""
小説作成チーム - Claude APIを使ったマルチエージェント小説創作システム

5つの専門エージェントが協力して小説を創作します:
1. プロット作家   - ストーリーの骨格と展開を構築
2. キャラクター設計 - 登場人物の性格・背景・関係性を設計
3. 場面描写家     - 情景・雰囲気・感覚的な描写を担当
4. 対話作家       - キャラクターの会話・セリフを執筆
5. 編集者         - 全体を統合・洗練させて最終稿を完成
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
    print(f"【{role}】が作業中...")
    print("=" * 60)

    messages = []
    if context:
        messages.append({
            "role": "user",
            "content": f"これまでのチームの成果物:\n\n{context}\n\n{user_message}",
        })
    else:
        messages.append({"role": "user", "content": user_message})

    full_response = ""
    with client.messages.stream(
        model=MODEL,
        max_tokens=4096,
        thinking={"type": "adaptive"},
        system=system_prompt,
        messages=messages,
    ) as stream:
        for text in stream.text_stream:
            print(text, end="", flush=True)
            full_response += text

    print("\n")
    return full_response


def plot_agent(theme: str) -> str:
    """プロット作家エージェント"""
    system = """あなたは才能あるプロット作家です。
読者を引き込む魅力的なストーリー構造を設計することが得意です。
三幕構成や起承転結など、物語の骨格を明確に示してください。
設定、葛藤、クライマックス、解決を含む詳細なプロットを作成してください。"""

    prompt = f"""以下のテーマで短編小説（読了時間5〜10分程度）のプロットを作成してください。

テーマ: {theme}

以下の形式で回答してください:
1. タイトル案
2. あらすじ（200字程度）
3. 詳細プロット（起承転結）
4. 重要な伏線・テーマ"""

    return call_agent("プロット作家", system, prompt)


def character_agent(plot: str) -> str:
    """キャラクター設計エージェント"""
    system = """あなたは優れたキャラクター設計の専門家です。
立体的で魅力的な登場人物を生み出すことが得意です。
キャラクターの内面、動機、成長を深く掘り下げてください。
外見描写、性格、バックストーリー、他のキャラクターとの関係性を詳細に設定してください。"""

    prompt = """プロットに基づいて、登場人物を詳細に設計してください。

各キャラクターについて以下を記載:
- 名前・年齢・外見
- 性格・口癖・特徴
- バックストーリー
- 動機・内的葛藤
- 他キャラクターとの関係"""

    return call_agent("キャラクター設計", system, prompt, context=plot)


def scene_agent(plot: str, characters: str) -> str:
    """場面描写家エージェント"""
    system = """あなたは情景描写の巧みな作家です。
五感を使った豊かな描写で読者を物語の世界に引き込みます。
視覚・聴覚・嗅覚・触覚・味覚を駆使して、生き生きとした場面を描いてください。
時代・場所・雰囲気を読者が鮮明にイメージできるよう表現してください。"""

    prompt = """プロットとキャラクター情報をもとに、主要な場面の詳細な描写を書いてください。

以下の場面を描写してください:
1. 物語の冒頭（世界観・主人公の日常を確立）
2. 転換点となる重要な場面
3. クライマックスの場面

各場面で情景・雰囲気・登場人物の感情を五感を使って描写してください。"""

    context = f"【プロット】\n{plot}\n\n【キャラクター設定】\n{characters}"
    return call_agent("場面描写家", system, prompt, context=context)


def dialogue_agent(plot: str, characters: str, scenes: str) -> str:
    """対話作家エージェント"""
    system = """あなたはセリフと対話の名手です。
各キャラクターの個性が光る自然で生き生きとした会話を書きます。
セリフを通じてキャラクターの性格・感情・関係性を表現してください。
読者がキャラクターに感情移入できるような、心に残るセリフを作ってください。"""

    prompt = """プロット・キャラクター・場面描写をもとに、重要な対話シーンを書いてください。

以下の場面の対話を作成してください:
1. 重要な出会いまたは再会の場面
2. 物語の核心となる感情的な対話シーン
3. クライマックスでの決定的なセリフ

各キャラクターの声・話し方の個性を大切にしてください。"""

    context = f"【プロット】\n{plot}\n\n【キャラクター設定】\n{characters}\n\n【場面描写】\n{scenes}"
    return call_agent("対話作家", system, prompt, context=context)


def editor_agent(plot: str, characters: str, scenes: str, dialogues: str) -> str:
    """編集者エージェント - 最終稿を執筆"""
    system = """あなたは経験豊富な文芸編集者兼作家です。
チームが作り上げた素材を統合し、完成度の高い短編小説として仕上げます。
文体の統一感、物語のテンポ、感情の起伏を整えながら、
読者の心に残る作品を完成させてください。
プロット・キャラクター・描写・対話を有機的に組み合わせ、
一つの完成した作品として書き上げてください。"""

    prompt = """チームの全成果物を統合して、完成した短編小説を執筆してください。

要件:
- チームが作った要素を活かしながら、流れるような物語を書く
- プロットに忠実に、かつ読みやすい文体で
- キャラクターの個性を台詞や行動で示す
- 場面転換を自然に行う
- 読後感のある結末にまとめる

完成した小説本文を書いてください（3000〜5000字程度）。"""

    context = (
        f"【プロット】\n{plot}\n\n"
        f"【キャラクター設定】\n{characters}\n\n"
        f"【場面描写】\n{scenes}\n\n"
        f"【対話シーン】\n{dialogues}"
    )
    return call_agent("編集者（最終稿）", system, prompt, context=context)


def write_novel(theme: str) -> str:
    """
    小説作成チームを起動してテーマから小説を生成する

    Args:
        theme: 小説のテーマ（例: "孤独な天才科学者と人工知能の友情"）

    Returns:
        完成した小説のテキスト
    """
    print("\n" + "=" * 60)
    print("🖊️  小説作成チーム 始動")
    print(f"テーマ: {theme}")
    print("=" * 60)

    # Step 1: プロット作家
    plot = plot_agent(theme)

    # Step 2: キャラクター設計
    characters = character_agent(plot)

    # Step 3: 場面描写家
    scenes = scene_agent(plot, characters)

    # Step 4: 対話作家
    dialogues = dialogue_agent(plot, characters, scenes)

    # Step 5: 編集者（最終稿）
    final_novel = editor_agent(plot, characters, scenes, dialogues)

    print("\n" + "=" * 60)
    print("✅ 小説完成！")
    print("=" * 60)

    return final_novel


def save_novel(theme: str, novel: str, filename: Optional[str] = None) -> str:
    """小説をファイルに保存する"""
    if filename is None:
        safe_theme = "".join(c for c in theme[:20] if c.isalnum() or c in "_ ")
        safe_theme = safe_theme.strip().replace(" ", "_")
        filename = f"novel_{safe_theme}.txt"

    with open(filename, "w", encoding="utf-8") as f:
        f.write(f"テーマ: {theme}\n")
        f.write("=" * 60 + "\n\n")
        f.write(novel)

    print(f"\n📄 小説を保存しました: {filename}")
    return filename


if __name__ == "__main__":
    # デフォルトテーマ（引数で変更可能）
    if len(sys.argv) > 1:
        theme = " ".join(sys.argv[1:])
    else:
        theme = "記憶を失った老いた音楽家と、彼の音楽を受け継いだ若者の物語"

    novel = write_novel(theme)
    save_novel(theme, novel)
