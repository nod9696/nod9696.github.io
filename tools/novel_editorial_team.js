#!/usr/bin/env node
/**
 * 神の降る街 - 商業レベル編集チームシステム (Node.js版)
 *
 * 使用方法:
 *   node novel_editorial_team.js <ファイルパス>
 *   node novel_editorial_team.js --text "原稿テキスト"
 *   echo "原稿" | node novel_editorial_team.js
 *
 * 事前準備:
 *   npm install @anthropic-ai/sdk
 *   set ANTHROPIC_API_KEY=your-api-key  (Windows)
 *   export ANTHROPIC_API_KEY=your-api-key  (Mac/Linux)
 */

const Anthropic = require('@anthropic-ai/sdk');
const fs = require('fs');
const path = require('path');

// .env ファイルからAPIキーを読み込む（ANTHROPIC_API_KEY が未設定の場合）
function loadEnvFile() {
  const envPath = path.join(__dirname, '.env');
  if (!process.env.ANTHROPIC_API_KEY && fs.existsSync(envPath)) {
    const lines = fs.readFileSync(envPath, 'utf-8').split('\n');
    for (const line of lines) {
      const m = line.match(/^ANTHROPIC_API_KEY\s*=\s*(.+)$/);
      if (m) {
        process.env.ANTHROPIC_API_KEY = m[1].trim().replace(/^["']|["']$/g, '');
        break;
      }
    }
  }
  if (!process.env.ANTHROPIC_API_KEY) {
    console.error(`
[エラー] ANTHROPIC_API_KEY が設定されていません。

以下のいずれかで設定してください：

1. .env ファイルを作成（推奨）:
   f:\\Claude\\.env に以下を記述:
   ANTHROPIC_API_KEY=sk-ant-あなたのキー

2. 環境変数で設定:
   set ANTHROPIC_API_KEY=sk-ant-あなたのキー  (Windows)

APIキーは https://console.anthropic.com/ から取得できます。
`);
    process.exit(1);
  }
}

loadEnvFile();

const client = new Anthropic();
const MODEL = 'claude-opus-4-6';

const WORLD_SETTING = `
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
`;

const EDITORS = [
  {
    key: 'structural',
    name: '🏗️ 構造編集者',
    system: `あなたはライトノベルの構造編集の専門家です。
${WORLD_SETTING}
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
- 改善提案（優先度順）`,
  },
  {
    key: 'prose',
    name: '✍️ 文章担当編集者',
    system: `あなたはライトノベルの文体・文章校正の専門家です。
${WORLD_SETTING}
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
- 文体強化のための具体的提案`,
  },
  {
    key: 'character',
    name: '👤 キャラクター編集者',
    system: `あなたはライトノベルのキャラクター専門編集者です。
${WORLD_SETTING}
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
- 感情移入を高める具体的改善案`,
  },
  {
    key: 'worldbuilding',
    name: '🌍 世界観編集者',
    system: `あなたはファンタジー世界観の一貫性チェック専門編集者です。
${WORLD_SETTING}
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
- 世界観の没入感を高める提案`,
  },
  {
    key: 'commercial',
    name: '💼 商業担当編集者',
    system: `あなたはライトノベルの商業出版・市場評価専門の編集者です。
${WORLD_SETTING}
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
- 商業化に向けた最優先改善事項 TOP3`,
  },
];

async function runEditor(editor, manuscript) {
  let fullText = '';
  const stream = await client.messages.stream({
    model: MODEL,
    max_tokens: 2000,
    thinking: { type: 'adaptive' },
    system: editor.system,
    messages: [
      {
        role: 'user',
        content: `以下の原稿を分析してください：\n\n---\n${manuscript}\n---`,
      },
    ],
  });
  const response = await stream.finalMessage();
  fullText = response.content.find(b => b.type === 'text')?.text ?? '（出力なし）';
  return { name: editor.name, feedback: fullText };
}

async function chiefEditorSynthesis(manuscript, feedbacks) {
  const feedbackText = feedbacks
    .map(fb => `【${fb.name}の評価】\n${fb.feedback}`)
    .join('\n\n');

  const stream = await client.messages.stream({
    model: MODEL,
    max_tokens: 1500,
    system: `あなたはライトノベル出版社のチーフ編集者です。
${WORLD_SETTING}
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
（著者が次の執筆で意識すべき点）`,
    messages: [
      {
        role: 'user',
        content: `原稿：\n---\n${manuscript}\n---\n\n各編集者の評価：\n${feedbackText}`,
      },
    ],
  });
  const response = await stream.finalMessage();
  return response.content.find(b => b.type === 'text')?.text ?? '（出力なし）';
}

async function rewrite(manuscript, synthesis) {
  const stream = await client.messages.stream({
    model: MODEL,
    max_tokens: 4000,
    thinking: { type: 'adaptive' },
    system: `あなたは商業ライトノベルの凄腕リライターです。
${WORLD_SETTING}
チーフ編集者の指示に従い、原稿を商業レベルにリライトしてください。

**リライトの絶対原則：**
- 固有用語（ウタ、緘葬、マヨイ、神憑き等）は必ず原文のまま使用
- カナタの「寡黙・無表情・感情抑制」を一切崩さない
- 退廃・神秘・儀式的な雰囲気を一段階強化する
- LN読者向けのテンポ感を意識（テンポが死ぬ説明は削る）
- セリフの詩的余白を生かす（説明的なセリフは避ける）
- 原文の長さの±30%以内に収める
- リライト稿のみを出力（解説・コメント不要）`,
    messages: [
      {
        role: 'user',
        content: `【チーフ編集者の指示】\n${synthesis}\n\n【リライト対象の原稿】\n---\n${manuscript}\n---`,
      },
    ],
  });
  const response = await stream.finalMessage();
  return response.content.find(b => b.type === 'text')?.text ?? '（出力なし）';
}

async function main() {
  // 原稿の取得
  let manuscript = '';
  const args = process.argv.slice(2);

  if (args.includes('--text') || args.includes('-t')) {
    const idx = args.indexOf('--text') !== -1 ? args.indexOf('--text') : args.indexOf('-t');
    manuscript = args[idx + 1] || '';
  } else if (args.length > 0 && !args[0].startsWith('-')) {
    try {
      manuscript = fs.readFileSync(args[0], 'utf-8');
    } catch (e) {
      console.error(`エラー: ファイルが見つかりません: ${args[0]}`);
      process.exit(1);
    }
  } else if (!process.stdin.isTTY) {
    manuscript = fs.readFileSync('/dev/stdin', 'utf-8');
  } else {
    console.error('使用方法: node novel_editorial_team.js <ファイル> または --text "テキスト"');
    process.exit(1);
  }

  manuscript = manuscript.trim();
  if (!manuscript) {
    console.error('エラー: 原稿が空です。');
    process.exit(1);
  }

  const sep = '='.repeat(60);
  console.log(`\n📚 編集チーム起動 — 原稿 ${manuscript.length} 文字\n${sep}\n`);

  // 5人の編集者を並行実行
  console.log('5人の編集者が並行分析中...\n');
  const editorPromises = EDITORS.map(async editor => {
    const result = await runEditor(editor, manuscript);
    console.log(`✓ ${result.name} 完了`);
    return result;
  });

  const feedbacks = await Promise.all(editorPromises);

  console.log(`\n📋 チーフ編集者が統合評価中...`);
  const synthesis = await chiefEditorSynthesis(manuscript, feedbacks);

  console.log(`✍️  リライター修正稿を生成中...\n`);
  const rewritten = await rewrite(manuscript, synthesis);

  // ===== 出力 =====
  console.log(`\n${sep}`);
  console.log('📊 各編集者のフィードバック');
  console.log(sep);
  for (const fb of feedbacks) {
    console.log(`\n${fb.name}`);
    console.log('-'.repeat(40));
    console.log(fb.feedback);
  }

  console.log(`\n${sep}`);
  console.log('📋 チーフ編集者 総合評価');
  console.log(sep);
  console.log(synthesis);

  console.log(`\n${sep}`);
  console.log('✍️  リライト稿');
  console.log(sep);
  console.log(rewritten);

  console.log(`\n${sep}`);
  console.log('✅ 編集チームの作業が完了しました。');
  console.log(sep);
}

main().catch(err => {
  console.error('エラー:', err.message);
  process.exit(1);
});
