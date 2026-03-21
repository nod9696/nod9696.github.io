#!/usr/bin/env python3
"""
インディーゲーム記事チーム - Claude APIを使ったマルチエージェント記事執筆システム

個人制作（ソロ開発）インディーゲームに特化した記事を5つのエージェントが協力して執筆:
1. トレンド調査員   - Steam/itch.io/SNSでの話題・トレンドをリサーチ
2. ゲーム分析家     - メカニクス・アート・ストーリー・サウンドを多角分析
3. 開発者取材班     - 開発者バックグラウンド・開発ストーリー・ツールを調査
4. 読者体験ライター - プレイヤー感情・コミュニティ反響・ターゲット層を言語化
5. 記事編集長       - 全素材を統合し完成記事（2000〜3000字）に仕上げる
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
    print(f"【{role}】が取材中...")
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


def trend_scout_agent(topic: str) -> str:
    """トレンド調査員エージェント"""
    system = """あなたはインディーゲーム専門のトレンドリサーチャーです。
個人開発者（ソロデベロッパー）が手がけたゲームに特化した調査を行います。
Steam、itch.io、SNS（X/Twitter、Reddit、TikTok）でのバズり具合、
ユーザーレビューの傾向、インディーコミュニティでの評判を詳しく調査します。
「なぜ今これが注目されているのか」という背景も含めてまとめてください。"""

    prompt = f"""以下のテーマに関する注目インディーゲームについてリサーチしてください。

テーマ: {topic}

以下を含めてください:
1. 注目されているタイトルとその概要（3〜5本）
2. 各タイトルのSNS・コミュニティでの反響
3. 注目を集めている理由・トレンドの背景
4. 個人制作ならではの特徴・売り
5. ユーザー評価の傾向"""

    return call_agent("トレンド調査員", system, prompt)


def game_analyst_agent(trends: str) -> str:
    """ゲーム分析家エージェント"""
    system = """あなたはインディーゲームの専門アナリストです。
ゲームメカニクス、アートスタイル、ストーリーテリング、サウンドデザインなど
多角的な視点でゲームを深く分析します。
大手スタジオ作品と比較したインディーゲームの独自性や革新性に特に注目してください。"""

    prompt = """トレンド調査員のリサーチをもとに、ピックアップされたゲームを詳細分析してください。

最も注目すべきタイトル（1〜2本）を以下の観点で深掘り分析:
1. ゲームメカニクスの特徴と革新性
2. ビジュアル・アートスタイルの魅力
3. ストーリー・世界観の深み
4. サウンド・音楽の演出
5. インディーゲームとしての独自ポジション
6. 同ジャンルの競合作との差別化ポイント"""

    return call_agent("ゲーム分析家", system, prompt, context=trends)


def dev_story_agent(trends: str, analysis: str) -> str:
    """開発者取材班エージェント"""
    system = """あなたはインディーゲーム開発者専門のジャーナリストです。
一人または少人数での開発の苦労や工夫、使用ツール・エンジン、開発期間、
資金調達の方法（クラウドファンディング、Early Accessなど）、
開発者のバックグラウンドや動機について詳しく調べます。
「普通の人がゲームを作った」という感動的な側面を丁寧に伝えます。"""

    prompt = """これまでの調査をもとに、ピックアップされたゲームの開発ストーリーを掘り下げてください。

以下を含めた開発者ストーリーをまとめてください:
1. 開発者のプロフィールとバックグラウンド
2. 開発を始めたきっかけ・動機
3. 使用した開発ツール・エンジン（Unity/Godot/RPGツクール等）
4. 開発期間と一人開発ならではの苦労
5. マーケティング・販売戦略（SNS活用など）
6. 印象的なエピソード・名言"""

    context = f"【トレンド調査】\n{trends}\n\n【ゲーム分析】\n{analysis}"
    return call_agent("開発者取材班", system, prompt, context=context)


def player_exp_agent(trends: str, analysis: str, dev_story: str) -> str:
    """読者体験ライターエージェント"""
    system = """あなたはゲーム体験・ユーザー感情の専門ライターです。
プレイヤーがゲームを通じて何を感じ、何を得るかを言語化するのが得意です。
感情的な没入感、達成感、驚き、コミュニティでの共有体験など、
プレイヤー視点から見たゲームの魅力を生き生きと表現します。
読者が「このゲームを遊んでみたい！」と感じるような文章を書いてください。"""

    prompt = """これまでの調査・分析をもとに、プレイヤー体験の観点からゲームの魅力を言語化してください。

以下を含めたプレイヤー体験レポートを作成:
1. 実際のプレイヤーレビュー・感想の傾向
2. 「このゲームならでは」の感動・驚きの体験
3. どんな人に刺さるか（ターゲット層・刺さりポイント）
4. コミュニティやSNSでの盛り上がり方
5. プレイ前後での感情の変化"""

    context = (
        f"【トレンド調査】\n{trends}\n\n"
        f"【ゲーム分析】\n{analysis}\n\n"
        f"【開発者ストーリー】\n{dev_story}"
    )
    return call_agent("読者体験ライター", system, prompt, context=context)


def editor_agent(trends: str, analysis: str, dev_story: str, player_exp: str) -> str:
    """記事編集長エージェント - 完成記事を執筆"""
    system = """あなたはインディーゲーム専門メディアの編集長です。
個人制作ゲームの魅力を広く伝えることに情熱を持っています。
チームが集めた素材を統合し、読者がゲームを遊びたくなるような
完成度の高い記事に仕上げます。
見出し・リード文・本文の構成を整え、インディーゲームへの愛と敬意が伝わる文章を書いてください。"""

    prompt = """チームの全調査・分析を統合して、完成した記事を執筆してください。

要件:
- キャッチーな記事タイトル（日本語）
- リード文（100〜150字）
- 本文（2000〜3000字）
- 「個人制作」「ソロ開発」の偉大さを伝える
- 開発者への敬意と読者への推薦を込める
- 読者が読後にゲームを調べたくなる構成"""

    context = (
        f"【トレンド調査】\n{trends}\n\n"
        f"【ゲーム分析】\n{analysis}\n\n"
        f"【開発者ストーリー】\n{dev_story}\n\n"
        f"【プレイヤー体験】\n{player_exp}"
    )
    return call_agent("記事編集長（完成記事）", system, prompt, context=context)


def write_article(topic: str) -> str:
    """
    インディーゲーム記事チームを起動してトピックから記事を生成する

    Args:
        topic: 記事のテーマ（例: "2025年のSteamで話題の一人開発ローグライクゲーム"）

    Returns:
        完成した記事のテキスト
    """
    print("\n" + "=" * 60)
    print("🎮  インディーゲーム記事チーム 始動")
    print(f"テーマ: {topic}")
    print("=" * 60)

    # Step 1: トレンド調査員
    trends = trend_scout_agent(topic)

    # Step 2: ゲーム分析家
    analysis = game_analyst_agent(trends)

    # Step 3: 開発者取材班
    dev_story = dev_story_agent(trends, analysis)

    # Step 4: 読者体験ライター
    player_exp = player_exp_agent(trends, analysis, dev_story)

    # Step 5: 記事編集長（完成記事）
    final_article = editor_agent(trends, analysis, dev_story, player_exp)

    print("\n" + "=" * 60)
    print("✅ 記事完成！")
    print("=" * 60)

    return final_article


def save_article(topic: str, article: str, filename: Optional[str] = None) -> str:
    """記事をファイルに保存する"""
    if filename is None:
        safe_topic = "".join(c for c in topic[:20] if c.isalnum() or c in "_ ")
        safe_topic = safe_topic.strip().replace(" ", "_")
        filename = f"indie_game_article_{safe_topic}.txt"

    with open(filename, "w", encoding="utf-8") as f:
        f.write(f"テーマ: {topic}\n")
        f.write("=" * 60 + "\n\n")
        f.write(article)

    print(f"\n📄 記事を保存しました: {filename}")
    return filename


if __name__ == "__main__":
    if len(sys.argv) > 1:
        topic = " ".join(sys.argv[1:])
    else:
        topic = "2025年〜2026年のSteamで話題の個人制作インディーゲーム"

    article = write_article(topic)
    save_article(topic, article)
