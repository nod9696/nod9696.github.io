ゲーム仕様書プロジェクトを新規初期化してください。

## 概要
`f:/Claude/specs/{project}/` に仕様書管理構造を作成し、ソース資料を読み込んで全セクションの初版を並列生成する。

## 引数
- `$ARGUMENTS` にプロジェクト名が含まれていればそれを使用
- なければユーザーに「プロジェクト名を教えてください（例: Delight, KamiNoFuruMachi）」と尋ねる

## 手順

### 0. 既存仕様書のインポート確認
プロジェクトが「KamiNoFuruMachi」の場合、既存の仕様書ファイルを確認：
- `f:/Claude/gamespec_v1.md`
- `f:/Claude/gamespec_基本システム設計_20260322.md`
- `f:/Claude/gamespec_第1話ナインフォールの声詳細_20260322.md`

存在する場合、「既存の仕様書をベースにインポートしますか？」と確認。
YESの場合、これらの内容を各セクションの初期値として取り込む（ゼロから生成せず既存内容をセクション分割する）。

### 1. ソース資料の検出
Glob ツールで以下のパターンからソース資料を検索する：
- `f:/Claude/Delight/**/*.md` `f:/Claude/Delight/**/*.txt`（Delightの場合）
- `f:/Claude/KamiNoFuruMachi/**/*`（神の降る街の場合）
- `f:/Claude/*.md` `f:/Claude/*.txt`（ルート直下の関連ファイル）
- `f:/Claude/gamespec_*.md`（既存仕様書）
- `f:/Claude/character_voice_guide*.md`

見つかったファイルの一覧をユーザーに提示し、「これらをソース資料として使用しますか？追加/除外するファイルはありますか？」と確認する。

### 2. マニフェスト作成
確認後、`f:/Claude/specs/{project}/manifest.json` を以下の形式で作成：

```json
{
  "project": "プロジェクト名",
  "title": "作品タイトル",
  "genre": "ジャンル",
  "created": "YYYY-MM-DD",
  "updated": "YYYY-MM-DD",
  "sources": [
    {
      "path": "f:/Claude/Delight/character_sheet_polished_v2.md",
      "type": "character",
      "description": "キャラクターシート（ポリッシュv2）"
    }
  ],
  "sections": [
    {
      "id": "system_design",
      "name": "基本システム設計",
      "file": "sections/system_design.md",
      "status": "draft",
      "version": 1,
      "updated": "YYYY-MM-DD",
      "depends_on_sources": ["character", "plot", "novel"]
    },
    {
      "id": "scenario",
      "name": "シナリオ・分岐設計",
      "file": "sections/scenario.md",
      "status": "draft",
      "version": 1,
      "updated": "YYYY-MM-DD",
      "depends_on_sources": ["plot", "novel"]
    },
    {
      "id": "characters",
      "name": "キャラクター仕様",
      "file": "sections/characters.md",
      "status": "draft",
      "version": 1,
      "updated": "YYYY-MM-DD",
      "depends_on_sources": ["character"]
    },
    {
      "id": "visual",
      "name": "ビジュアル・演出仕様",
      "file": "sections/visual.md",
      "status": "draft",
      "version": 1,
      "updated": "YYYY-MM-DD",
      "depends_on_sources": ["character", "plot"]
    },
    {
      "id": "sound",
      "name": "サウンド仕様",
      "file": "sections/sound.md",
      "status": "draft",
      "version": 1,
      "updated": "YYYY-MM-DD",
      "depends_on_sources": ["plot", "novel"]
    },
    {
      "id": "flags",
      "name": "フラグ・変数管理",
      "file": "sections/flags.md",
      "status": "draft",
      "version": 1,
      "updated": "YYYY-MM-DD",
      "depends_on_sources": ["plot", "novel", "character"]
    }
  ]
}
```

### 3. セクションディレクトリ作成
```
mkdir -p f:/Claude/specs/{project}/sections
```

### 4. ソース資料の読み込みと全セクション並列生成

**ソース資料をすべて Read ツールで読み込んだ後**、Agent ツールで5つの担当エージェントを **必ず同一メッセージで並列起動** する：

#### 📖 シナリオ・分岐設計エージェント
```
あなたはノベルADVのシナリオ設計専門家です。
以下のソース資料を基に、ゲーム仕様書の「シナリオ・分岐設計」セクションを作成してください。

【ソース資料】
{読み込んだソース資料の内容}

【出力項目】
1. シナリオ構造（チャプター/シーン分割）
2. 選択肢とフラグ管理（分岐条件を明示）
3. ルート分岐図（ASCII図）
4. キーシーン一覧（タイトル・概要・感情的目標）
5. エンディング条件と種類
6. テキスト量の目安

フラグは細粒度で設計し、後からシーンを追加しやすい構造にすること。
結果をMarkdownで出力してください。Writeツールは使わず、テキストとして返してください。
```

#### ⚙️ 基本システム設計エージェント
```
あなたはノベルADVのシステム設計専門家です。
以下のソース資料を基に、ゲーム仕様書の「基本システム設計」セクションを作成してください。

【ソース資料】
{読み込んだソース資料の内容}

【出力項目】
1. ゲームエンジン選定・理由
2. コアシステム仕様（セーブ/ロード・スキップ・バックログ等）
3. フラグ・変数管理システム（JSON構造案）
4. 画面遷移仕様（状態機械図）
5. 設定メニュー仕様
6. 演出コマンド仕様

結果をMarkdownで出力してください。Writeツールは使わず、テキストとして返してください。
```

#### 👥 キャラクター仕様エージェント
```
あなたはノベルADVのキャラクター設計専門家です。
以下のソース資料を基に、ゲーム仕様書の「キャラクター仕様」セクションを作成してください。

【ソース資料】
{読み込んだソース資料の内容}

【出力項目】
1. キャラクター一覧（登場シーン・役割・関係性）
2. スプライト差分リスト（表情×衣装×ポーズ）
3. ボイス仕様（声質・口調・感情タグ）
4. キャラクター固有演出
5. 好感度・フラグパラメータ仕様

結果をMarkdownで出力してください。Writeツールは使わず、テキストとして返してください。
```

#### 🎨 ビジュアル・演出仕様エージェント
```
あなたはノベルADVのビジュアル設計専門家です。
以下のソース資料を基に、ゲーム仕様書の「ビジュアル・演出仕様」セクションを作成してください。

【ソース資料】
{読み込んだソース資料の内容}

【出力項目】
1. 背景一覧（シーン名・時間帯・天候）
2. イベントCG一覧（内容・感情的役割）
3. UI/UX仕様
4. 演出エフェクト仕様
5. カラーパレット方針
6. 解像度・アスペクト比仕様

結果をMarkdownで出力してください。Writeツールは使わず、テキストとして返してください。
```

#### 🎵 サウンド仕様エージェント
```
あなたはノベルADVのサウンド設計専門家です。
以下のソース資料を基に、ゲーム仕様書の「サウンド仕様」セクションを作成してください。

【ソース資料】
{読み込んだソース資料の内容}

【出力項目】
1. BGM一覧（トラック名・使用シーン・ムード）
2. SE一覧（種類・使用タイミング）
3. ボイス収録仕様
4. 環境音仕様
5. 音楽的世界観定義
6. 実装仕様（ループ・フェード設定）

結果をMarkdownで出力してください。Writeツールは使わず、テキストとして返してください。
```

### 5. 統合・保存
全エージェントの出力が揃ったら：

1. 各出力を対応するセクションファイルに Write ツールで保存
2. **フラグ・変数管理セクション** をシナリオ+システム+キャラの出力から統合して作成・保存
3. manifest.json の `updated` と各セクションの `updated` を更新
4. 全セクションの矛盾・不整合をチェックし、問題があれば報告

### 6. 完了報告
以下を報告する：
- 作成されたファイル一覧
- 各セクションの概要（1行ずつ）
- 検出された矛盾・要確認事項
- 次のステップの提案（`/spec-update` での個別更新や `/spec-status` での整合性チェック）
