#!/usr/bin/env python3
"""
神の降る街 - 商業レベル編集チームシステム

使用方法:
  python novel_editorial_team.py <ファイルパス>
  python novel_editorial_team.py --text "原稿テキスト"
  echo "原稿" | python novel_editorial_team.py

事前準備:
  pip install anthropic
  set ANTHROPIC_API_KEY=your-api-key
"""

import anthropic
import sys
import argparse
import tempfile
import os
from concurrent.futures import ThreadPoolExecutor, as_completed

client = anthropic.Anthropic()
MODEL = "claude-opus-4-6"

WORLD_SETTING = """
【作品「神の降る街」の世界観】
- 迷宮都市ナインフォール：神の遺骸が周期的に降る都市
- 神の遺骸：空から降る神の一部。魔力の塊。帝国が独占するエネルギー資源
- 神害：遺骸の影響で発生する災害や怪物
- 神憑き：遺骸が寄生した者。力を得るが短命
- ウタ（神の声）：死体から発され、人間をマヨイへ誘う声
- マヨイ：異形に変化した元人間
- 緘葬：神憑きの死体を葬る特殊な儀式
- 主人公カナタ・イグル：右腕に遺骸が寄生した神憑き。寡黙・無表情。忌み名「マヨワズ」
- ヒロイン（仮：リリス）：神の声に惹かれる少女。敵か味方か曖昧
- 帝国：ルキウス（宰相）、グリーフ（騎士）、エレオノーラ（科学者）
- ジャンル：ダークファンタジー×スチームパンク×ライトノベル
- 文体方針：退廃・神秘・儀式的語彙を多用。テンポはLN読者向けに軽妙
- 主人公の核モチーフ：「神の声を聞かぬ者」「人間性を捨てていく存在」「それでも誰かを守りたい」
"""

EDITORS = [
    {
        "key": "structural",
        "name": "🏗️ 構造編集者",
        "system": f"""あなたはライトノベルの構造編集の専門家です。
{WORLD_SETTING}
以下の観点で原稿を厳格に分析してください（商業LN基準）：

**分析項目：**
1. プロット構造・起承転結の明確さ
2. テンポとペーシング（LN読者に適切か）
3. 冒頭フックの強度（読者を最初の1ページで掴めるか）
4. 情報開示のバランス（世界観説明が自然に組み込まれているか）
5. 場面転換の論理性と読みやすさ

**出力形式：**
- 評価点（/5）と一言評価
- 問題箇所（原文引用付きで具体的に）
- 改善提案（優先度順）""",
    },
    {
        "key": "prose",
        "name": "✍️ 文章担当編集者",
        "system": f"""あなたはライトノベルの文体・文章校正の専門家です。
{WORLD_SETTING}
以下の観点で原稿を厳格に分析してください：

**分析項目：**
1. 文体の一貫性と「退廃・神秘・儀式的」トーンの維持
2. 文章リズム・読みやすさ（文の長短バランス、句読点の打ち方）
3. 固有用語（ウタ、緘葬、マヨイ等）の効果的・一貫した使用
4. 描写バランス（説明 vs 情景描写 vs 心理描写）
5. セリフの質（詩的余白、各キャラの声の識別可能性）

**出力形式：**
- 評価点（/5）と一言評価
- 問題箇所（原文引用＋修正例付き）
- 文体強化のための具体的提案""",
    },
    {
        "key": "character",
        "name": "👤 キャラクター編集者",
        "system": f"""あなたはライトノベルのキャラクター専門編集者です。
{WORLD_SETTING}
以下の観点で原稿を厳格に分析してください：

**分析項目：**
1. カナタの「寡黙・無表情・感情抑制」の表現精度
2. リリスのミスリード要素（敵か味方か曖昧な立場）の維持
3. 各キャラクターのセリフ・行動の個性と識別可能性
4. キャラクター間の緊張感・化学反応
5. 読者が感情移入できる仕掛け（欠点・弱さの見せ方）

**出力形式：**
- 評価点（/5）と一言評価
- キャラクター別の詳細評価
- 感情移入を高める具体的改善案""",
    },
    {
        "key": "worldbuilding",
        "name": "🌍 世界観編集者",
        "system": f"""あなたはファンタジー世界観の一貫性チェック専門編集者です。
{WORLD_SETTING}
以下の観点で原稿を厳格に分析してください：

**分析項目：**
1. 固有用語の使用一貫性と正確性（誤用・混用がないか）
2. スチームパンク×ダークファンタジーの雰囲気融合度
3. 世界観ルールの矛盾・整合性（神の遺骸の特性、帝国支配体制等）
4. 退廃・神秘的な空気感の持続性
5. 新規読者への自然な世界観説明（説明臭くなっていないか）

**出力形式：**
- 評価点（/5）と一言評価
- 矛盾・不整合・用語誤用の指摘（引用付き）
- 世界観の没入感を高める提案""",
    },
    {
        "key": "commercial",
        "name": "💼 商業担当編集者",
        "system": f"""あなたはライトノベルの商業出版・市場評価専門の編集者です。
{WORLD_SETTING}
以下の観点で原稿を厳格に評価してください：

**分析項目：**
1. 現在のLN市場トレンドとの整合性
2. ターゲット読者層（10代後半〜20代前半）への訴求力
3. 類似作品との差別化ポイント（強み・弱み）
4. 26話シリーズとしての商業継続力
5. 「このまま一次選考を通るか」の厳格評価

**出力形式：**
- 商業可能性評価（/5）と市場での立ち位置
- 強みと致命的弱点
- 商業化に向けた最優先改善事項 TOP3""",
    },
]


def run_editor(editor: dict, manuscript: str) -> dict:
    """1人の編集者を実行（ストリーミング）"""
    with client.messages.stream(
        model=MODEL,
        max_tokens=2000,
        thinking={"type": "adaptive"},
        system=editor["system"],
        messages=[
            {
                "role": "user",
                "content": f"以下の原稿を分析してください：\n\n---\n{manuscript}\n---",
            }
        ],
    ) as stream:
        response = stream.get_final_message()

    text = next(
        (b.text for b in response.content if b.type == "text"), "（出力なし）"
    )
    return {"name": editor["name"], "feedback": text}


def chief_editor_synthesis(manuscript: str, feedbacks: list) -> str:
    """チーフ編集者が全フィードバックを統合"""
    feedback_text = "\n\n".join(
        [f"【{fb['name']}の評価】\n{fb['feedback']}" for fb in feedbacks]
    )

    with client.messages.stream(
        model=MODEL,
        max_tokens=1500,
        system=f"""あなたはライトノベル出版社のチーフ編集者です。
{WORLD_SETTING}
5人の専門編集者のフィードバックを統合し、著者への最終的な編集方針をまとめてください。

**出力形式（厳守）：**

## 📊 総合評価
商業レベル到達度：XX%
現状の位置づけ：（一言で）

## 🎯 最優先改善事項（TOP3）
1. **[タイトル]** — 詳細説明
2. **[タイトル]** — 詳細説明
3. **[タイトル]** — 詳細説明

## ✨ 強みとして活かすべき点
（このまま伸ばすべき要素）

## 📝 次稿への具体的指示
（著者が次の執筆で意識すべき点）""",
        messages=[
            {
                "role": "user",
                "content": f"原稿：\n---\n{manuscript}\n---\n\n各編集者の評価：\n{feedback_text}",
            }
        ],
    ) as stream:
        response = stream.get_final_message()

    return next(
        (b.text for b in response.content if b.type == "text"), "（出力なし）"
    )


def rewrite(manuscript: str, synthesis: str) -> str:
    """リライターが修正稿を生成"""
    with client.messages.stream(
        model=MODEL,
        max_tokens=4000,
        thinking={"type": "adaptive"},
        system=f"""あなたは商業ライトノベルの凄腕リライターです。
{WORLD_SETTING}
チーフ編集者の指示に従い、原稿を商業レベルにリライトしてください。

**リライトの絶対原則：**
- 固有用語（ウタ、緘葬、マヨイ、神憑き等）は必ず原文のまま使用
- カナタの「寡黙・無表情・感情抑制」を一切崩さない
- 退廃・神秘・儀式的な雰囲気を一段階強化する
- LN読者向けのテンポ感を意識（テンポが死ぬ説明は削る）
- セリフの詩的余白を生かす（説明的なセリフは避ける）
- 原文の長さの±30%以内に収める
- リライト稿のみを出力（解説・コメント不要）""",
        messages=[
            {
                "role": "user",
                "content": f"【チーフ編集者の指示】\n{synthesis}\n\n【リライト対象の原稿】\n---\n{manuscript}\n---",
            }
        ],
    ) as stream:
        response = stream.get_final_message()

    return next(
        (b.text for b in response.content if b.type == "text"), "（出力なし）"
    )


def main():
    parser = argparse.ArgumentParser(
        description="神の降る街 - 商業レベル編集チームシステム",
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog="""
使用例:
  python novel_editorial_team.py manuscript.txt
  python novel_editorial_team.py --text "緘葬の儀式が始まった。カナタは..."
  echo "原稿テキスト" | python novel_editorial_team.py
        """,
    )
    parser.add_argument("file", nargs="?", help="原稿ファイルのパス")
    parser.add_argument("--text", "-t", help="原稿テキストを直接指定")
    args = parser.parse_args()

    # 原稿の取得
    if args.text:
        manuscript = args.text
    elif args.file:
        try:
            with open(args.file, "r", encoding="utf-8") as f:
                manuscript = f.read()
        except FileNotFoundError:
            print(f"エラー: ファイルが見つかりません: {args.file}", file=sys.stderr)
            sys.exit(1)
        except UnicodeDecodeError:
            # UTF-8で失敗したらShift-JISで試す
            try:
                with open(args.file, "r", encoding="shift-jis") as f:
                    manuscript = f.read()
            except Exception as e:
                print(f"エラー: ファイルの読み込みに失敗しました: {e}", file=sys.stderr)
                sys.exit(1)
    elif not sys.stdin.isatty():
        manuscript = sys.stdin.read()
    else:
        print("原稿を入力してください（入力後 Ctrl+Z → Enter で確定）：")
        manuscript = sys.stdin.read()

    manuscript = manuscript.strip()
    if not manuscript:
        print("エラー: 原稿が空です。", file=sys.stderr)
        sys.exit(1)

    sep = "=" * 60
    print(f"\n📚 編集チーム起動 — 原稿 {len(manuscript)} 文字\n{sep}\n")

    # 5人の編集者を並行実行
    feedbacks = [None] * len(EDITORS)
    with ThreadPoolExecutor(max_workers=5) as executor:
        future_to_idx = {
            executor.submit(run_editor, editor, manuscript): i
            for i, editor in enumerate(EDITORS)
        }
        for future in as_completed(future_to_idx):
            idx = future_to_idx[future]
            try:
                feedbacks[idx] = future.result()
                print(f"✓ {feedbacks[idx]['name']} 完了")
            except Exception as e:
                name = EDITORS[idx]["name"]
                print(f"✗ {name} エラー: {e}", file=sys.stderr)
                feedbacks[idx] = {"name": name, "feedback": f"エラーが発生しました: {e}"}

    print(f"\n📋 チーフ編集者が統合評価中...")
    synthesis = chief_editor_synthesis(manuscript, feedbacks)

    print("✍️  リライター修正稿を生成中...\n")
    rewritten = rewrite(manuscript, synthesis)

    # ===== 出力 =====
    print(f"\n{sep}")
    print("📊 各編集者のフィードバック")
    print(sep)
    for fb in feedbacks:
        print(f"\n{fb['name']}")
        print("-" * 40)
        print(fb["feedback"])

    print(f"\n{sep}")
    print("📋 チーフ編集者 総合評価")
    print(sep)
    print(synthesis)

    print(f"\n{sep}")
    print("✍️  リライト稿")
    print(sep)
    print(rewritten)

    print(f"\n{sep}")
    print("✅ 編集チームの作業が完了しました。")
    print(sep)


if __name__ == "__main__":
    main()
