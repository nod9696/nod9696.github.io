#!/usr/bin/env node
/**
 * 神の降る街 - ノベルADVゲーム仕様書作成チーム (Node.js版)
 *
 * 使用方法:
 *   node game_spec_team.js "基本システム設計"
 *   node game_spec_team.js --section scenario system "第1章シナリオ"
 *
 * 事前準備:
 *   npm install @anthropic-ai/sdk
 *   set ANTHROPIC_API_KEY=your-api-key
 */

const Anthropic = require('@anthropic-ai/sdk');
const fs = require('fs');
const path = require('path');

// .env ファイルからAPIキーを読み込む
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
f:\\Claude\\.env に ANTHROPIC_API_KEY=sk-ant-... を記述してください。
`);
    process.exit(1);
  }
}

loadEnvFile();

const client = new Anthropic();
const MODEL = 'claude-opus-4-6';

const GAME_WORLD = `
【作品「神の降る街」ゲーム概要】
ジャンル: ノベルADV（アドベンチャーゲーム）

■ 世界観
- 迷宮都市ナインフォール：神の遺骸が周期的に降る都市
- 神の遺骸：空から降る神の一部。魔力の塊。帝国が独占するエネルギー資源
- 神害：遺骸の影響で発生する災害や怪物
- 神憑き：遺骸が寄生した者。力を得るが短命
- ウタ（神の声）：死体から発され、人間をマヨイへ誘う声
- マヨイ：異形に変化した元人間
- 緘葬（かんそう）：神憑きの死体を葬る特殊な儀式

■ 登場人物
- 主人公カナタ・イグル：右腕に遺骸が寄生した神憑き。寡黙・無表情。忌み名「マヨワズ」
- ヒロイン（仮：リリス）：神の声に惹かれる少女。敵か味方か曖昧
- ルキウス：帝国宰相 / グリーフ：帝国騎士 / エレオノーラ：帝国科学者

■ ゲームデザイン方針
- ジャンル：ダークファンタジー×スチームパンク×ライトノベル
- ターゲット：10代後半〜20代前半
- プレイ時間：全ルート約20〜30時間
- 分岐構造：マルチルート・マルチエンディング（グッドエンド/バッドエンド/トゥルーエンド）
- 主人公の核モチーフ：「神の声を聞かぬ者」「人間性を捨てていく存在」「それでも誰かを守りたい」
- 演出方針：退廃・神秘・儀式的語彙を多用。テンポはLN読者向けに軽妙
`;

const SPEC_WRITERS = [
  {
    key: 'scenario',
    name: '📖 シナリオ仕様担当',
    system: `あなたはノベルADVゲームのシナリオ設計専門家です。
${GAME_WORLD}
与えられたテーマに対して、以下の観点でゲーム仕様書のシナリオセクションを作成してください：

**作成項目：**
1. シナリオ構造（チャプター/シーン分割）
2. 選択肢とフラグ管理（分岐条件の明示）
3. ルート分岐図（テキストベースのフロー図）
4. キーシーン一覧（タイトル・概要・感情的目標）
5. エンディング条件と種類
6. テキスト量の目安（文字数・ページ数の概算）

**出力形式：**
Markdownで構造的に記述。フロー図はASCIIアートで表現。
実装者が迷わないよう、具体的かつ詳細に記述してください。`,
  },
  {
    key: 'system',
    name: '⚙️ システム仕様担当',
    system: `あなたはノベルADVゲームのシステム設計専門家です。
${GAME_WORLD}
与えられたテーマに対して、以下の観点でゲーム仕様書のシステムセクションを作成してください：

**作成項目：**
1. ゲームエンジン仕様（推奨エンジン・フレームワーク・理由）
2. 基本システム仕様（セーブ/ロード・既読スキップ・オートモード・バックログ）
3. フラグ・変数管理システム（データ構造の提案）
4. 画面遷移仕様（状態機械図）
5. 設定メニュー仕様（音量・テキスト速度・解像度等）
6. セキュリティ・データ保存仕様

**出力形式：**
Markdownで構造的に記述。データ構造はJSON/擬似コードで表現。
エンジニアが実装できる粒度で詳細に記述してください。`,
  },
  {
    key: 'character',
    name: '👥 キャラクター仕様担当',
    system: `あなたはノベルADVゲームのキャラクター設計専門家です。
${GAME_WORLD}
与えられたテーマに対して、以下の観点でゲーム仕様書のキャラクターセクションを作成してください：

**作成項目：**
1. キャラクター一覧（登場シーン・役割・関係性マップ）
2. スプライト仕様（立ち絵の差分リスト：表情・衣装・ポーズ）
3. キャラクターボイス仕様（声質・口調・感情表現の指定）
4. キャラクター固有演出（登場/退場エフェクトの提案）
5. 記憶・好感度パラメータ仕様（存在する場合）
6. キャラクターシート（デザイン指示書の骨格）

**出力形式：**
Markdownで構造的に記述。スプライトリストは表形式で整理。
イラストレーター・声優ディレクターが参照できる粒度で記述してください。`,
  },
  {
    key: 'visual',
    name: '🎨 ビジュアル仕様担当',
    system: `あなたはノベルADVゲームのビジュアル設計専門家です。
${GAME_WORLD}
与えられたテーマに対して、以下の観点でゲーム仕様書のビジュアルセクションを作成してください：

**作成項目：**
1. 背景（BG）一覧（シーン名・時間帯・天候・バリエーション）
2. イベントCG一覧（シーン番号・内容・感情的役割・ルート条件）
3. UI/UX仕様（テキストボックス・メニュー・選択肢画面のレイアウト提案）
4. 演出エフェクト仕様（画面効果・トランジション・シェイク等）
5. カラーパレット方針（世界観に合ったトーン・禁止色の指定）
6. 解像度・アスペクト比仕様（対応デバイス別）

**出力形式：**
Markdownで構造的に記述。BG/CGリストは表形式。
アーティストが作業を開始できる粒度で詳細に記述してください。`,
  },
  {
    key: 'sound',
    name: '🎵 サウンド仕様担当',
    system: `あなたはノベルADVゲームのサウンド設計専門家です。
${GAME_WORLD}
与えられたテーマに対して、以下の観点でゲーム仕様書のサウンドセクションを作成してください：

**作成項目：**
1. BGM一覧（トラック名・使用シーン・ムード・テンポ・楽器編成の方向性）
2. SE（効果音）一覧（種類・使用タイミング・音の質感の指定）
3. ボイス仕様（収録セリフの優先度・感情タグ付けルール・収録ディレクション）
4. 環境音仕様（アンビエントサウンドの使用箇所）
5. 音楽的世界観定義（参考楽曲・禁止ジャンル・推奨音楽スタイル）
6. 実装仕様（ループポイント・フェード設定・チャンネル数）

**出力形式：**
Markdownで構造的に記述。BGM/SEリストは表形式。
作曲家・音響監督が制作を開始できる粒度で詳細に記述してください。`,
  },
];

const DIRECTOR_SYSTEM = `あなたは「神の降る街」ノベルADVゲームの総括ディレクターです。
${GAME_WORLD}
各担当者が作成した仕様書の各セクションを受け取り、以下の作業を行ってください：

1. **統合チェック**：各セクション間の矛盾・不整合を発見し修正指示を追記
2. **優先度整理**：MVP（最初のビルドで必須）vs 後回し可能な要素を分類
3. **依存関係の明示**：「〇〇の実装が完了してから△△に着手」という順序を整理
4. **表紙・概要ページの作成**：ドキュメント全体のサマリーを冒頭に付与
5. **最終的な仕様書として整形**：全セクションを一つのMarkdownドキュメントに統合

**出力形式：**
完成した仕様書をMarkdown形式で出力してください。
見出し構造：H1（仕様書タイトル）> H2（各セクション）> H3（サブセクション）
`;

async function runSpecWriter(writer, theme) {
  const stream = await client.messages.stream({
    model: MODEL,
    max_tokens: 4000,
    thinking: { type: 'adaptive' },
    system: writer.system,
    messages: [
      {
        role: 'user',
        content: `以下のテーマについてゲーム仕様書のセクションを作成してください：\n\n【テーマ】${theme}`,
      },
    ],
  });
  const response = await stream.finalMessage();
  const text = response.content.find(b => b.type === 'text')?.text ?? '（出力なし）';
  console.error(`✅ ${writer.name} 完了`);
  return { key: writer.key, name: writer.name, content: text };
}

async function synthesizeSpec(theme, sections) {
  console.error(`\n📋 統括ディレクター が最終仕様書を統合中...`);
  const sectionsText = sections
    .map(s => `## ${s.name}からの仕様\n\n${s.content}`)
    .join('\n\n');

  const stream = await client.messages.stream({
    model: MODEL,
    max_tokens: 8000,
    thinking: { type: 'adaptive' },
    system: DIRECTOR_SYSTEM,
    messages: [
      {
        role: 'user',
        content: `以下のテーマについて、各担当者が作成した仕様書セクションを受け取りました。
これらを統合して、完成した「神の降る街」ノベルADV仕様書を作成してください。

【テーマ】${theme}

【各担当者の仕様書セクション】
${sectionsText}`,
      },
    ],
  });
  const response = await stream.finalMessage();
  console.error(`✅ 統括ディレクター 統合完了`);
  return response.content.find(b => b.type === 'text')?.text ?? '（出力なし）';
}

function saveSpec(theme, content) {
  const now = new Date();
  const ts = now.toISOString().replace(/[:.]/g, '-').slice(0, 19);
  const safeTheme = theme.replace(/[^\w\u3040-\u9fff]/g, '_').slice(0, 30);
  const filename = `gamespec_${safeTheme}_${ts}.md`;
  const filepath = path.join(__dirname, filename);

  const header = `# 神の降る街 ノベルADV ゲーム仕様書
**テーマ：** ${theme}
**作成日時：** ${now.toLocaleString('ja-JP')}

---

`;
  fs.writeFileSync(filepath, header + content, 'utf-8');
  return filepath;
}

async function main() {
  const args = process.argv.slice(2);

  // --section オプションの解析
  let sectionFilter = null;
  let remainingArgs = args;
  const sectionIdx = args.indexOf('--section');
  if (sectionIdx !== -1) {
    const validKeys = ['scenario', 'system', 'character', 'visual', 'sound'];
    sectionFilter = [];
    let i = sectionIdx + 1;
    while (i < args.length && validKeys.includes(args[i])) {
      sectionFilter.push(args[i]);
      i++;
    }
    remainingArgs = args.filter((_, idx) => idx !== sectionIdx && !sectionFilter.includes(args[idx]));
  }

  const theme = remainingArgs.join(' ').trim();
  if (!theme) {
    console.error('使用方法: node game_spec_team.js "テーマ"');
    console.error('例: node game_spec_team.js "基本システム設計"');
    process.exit(1);
  }

  const writers = sectionFilter
    ? SPEC_WRITERS.filter(w => sectionFilter.includes(w.key))
    : SPEC_WRITERS;

  const sep = '='.repeat(60);
  console.error(`\n🎮 神の降る街 ノベルADV 仕様書作成チーム 起動`);
  console.error(`📝 テーマ：${theme}`);
  console.error(`👥 担当者：${writers.map(w => w.name).join(' / ')}`);
  console.error(`${sep}\n`);
  console.error(`${writers.length}人の担当者が並行執筆中...\n`);

  // 並列実行
  const results = await Promise.all(writers.map(w => runSpecWriter(w, theme)));

  // 元の順序に並び替え
  const keyOrder = SPEC_WRITERS.map(w => w.key);
  results.sort((a, b) => keyOrder.indexOf(a.key) - keyOrder.indexOf(b.key));

  // 統括ディレクターが統合
  const finalSpec = await synthesizeSpec(theme, results);

  // 保存
  const filepath = saveSpec(theme, finalSpec);

  console.error(`\n${sep}`);
  console.error(`💾 仕様書を保存しました：${filepath}`);
  console.error(`${sep}\n`);

  // stdout に最終仕様書を出力（Claudeが読み取れるように）
  console.log(`SAVED:${filepath}`);
  console.log(finalSpec);
}

main().catch(err => {
  console.error('エラー:', err.message);
  process.exit(1);
});
