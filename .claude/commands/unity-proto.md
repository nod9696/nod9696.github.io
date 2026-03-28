神の降る街のUnityプロトタイプコードを生成します。
最新の仕様書を読み込み、即座に動作するC#スクリプト群を生成してUnityプロジェクトに出力します。

## チームメンバー
- 🗂️ プロジェクト構成担当 — フォルダ構造・asmdef・ScriptableObject定義
- 💬 ダイアログシステム担当 — シナリオエンジン・テキスト表示・フラグ管理のC#実装
- 🖼️ UI実装担当 — TextMeshPro・選択肢UI・バックログのC#実装
- ✨ 演出実装担当 — スプライト管理・トランジション・エフェクトのC#実装
- 🎵 オーディオ実装担当 — AudioManagerのC#実装
- 📦 データ設計担当 — シナリオJSON・ScriptableObjectのサンプルデータ生成

## 実行手順

1. Unityプロジェクトのルートパスを取得する：
   - `$ARGUMENTS` にパスが含まれていればそれを使用
   - なければ `f:/Claude/KamiNoFuruMachi` をデフォルトとして使用

2. 最新の仕様書ファイルを取得する：
   - `f:/Claude/` 以下の `gamespec_*.md` を Glob ツールで検索
   - 最新のファイルを Read ツールで読み込む
   - 仕様書が存在しない場合は「先に /game-spec でテーマを指定してください」と伝える

3. Unityプロジェクトのフォルダ構造を作成する（Bash ツール）：
   ```
   mkdir -p {PROJECT_ROOT}/Assets/Scripts/Core
   mkdir -p {PROJECT_ROOT}/Assets/Scripts/Dialogue
   mkdir -p {PROJECT_ROOT}/Assets/Scripts/UI
   mkdir -p {PROJECT_ROOT}/Assets/Scripts/Visual
   mkdir -p {PROJECT_ROOT}/Assets/Scripts/Audio
   mkdir -p {PROJECT_ROOT}/Assets/ScriptableObjects/Characters
   mkdir -p {PROJECT_ROOT}/Assets/ScriptableObjects/Scenarios
   mkdir -p {PROJECT_ROOT}/Assets/StreamingAssets/Scenarios
   mkdir -p {PROJECT_ROOT}/Assets/Resources/Prefabs
   ```

4. Agentツールで6人の担当を**並行**（同一メッセージで複数のAgentツール呼び出し）起動する。
   各担当に以下を渡す：
   - 仕様書の内容（全文）
   - 出力先パス: `{PROJECT_ROOT}/Assets/Scripts/`
   - 制約：UniTask使用・MonoBehaviourとPure C#の役割分担を守る・名前空間 `KamiNoFuruMachi`

   **プロジェクト構成担当**のプロンプト：
   「あなたはUnityプロジェクト構成の専門エンジニアです。以下の仕様書を読み、名前空間KamiNoFuruMatchiでプロジェクトの骨格を実装してください。出力するファイル：①GameManager.cs（シングルトン・シーン管理・初期化）②FlagManager.cs（bool/int/stringフラグの読み書き・イベント通知）③SaveLoadManager.cs（JSON形式のセーブ・ロード・PlayerPrefs）④GameConstants.cs（定数・列挙型定義）。各ファイルの完全なC#コードをコードブロックで出力し、ファイルパス（Assets/Scripts/Core/ファイル名.cs）をコメントで明示してください。仕様書：[仕様書全文]」

   **ダイアログシステム担当**のプロンプト：
   「あなたはUnityノベルエンジン実装の専門エンジニアです。以下の仕様書を読み、名前空間KamiNoFuruMatchiでダイアログシステムを実装してください。出力するファイル：①ScenarioData.cs（JsonUtility対応のシナリオデータ構造体）②ScenarioLoader.cs（StreamingAssetsからJSONを非同期ロード・UniTask使用）③DialogueEngine.cs（シナリオ進行・コマンド解析・フラグ評価）④DialogueCommand.cs（テキスト/選択肢/BGM/演出コマンドの定義）。各ファイルの完全なC#コードをコードブロックで出力し、ファイルパス（Assets/Scripts/Dialogue/ファイル名.cs）をコメントで明示してください。仕様書：[仕様書全文]」

   **UI実装担当**のプロンプト：
   「あなたはUnity uGUI/TextMeshPro実装の専門エンジニアです。以下の仕様書を読み、名前空間KamiNoFuruMatchiでUI システムを実装してください。出力するファイル：①DialogueUI.cs（TextMeshPro使用・逐次テキスト表示・クリック待ち・UniTask）②ChoiceUI.cs（選択肢ボタンの動的生成・DOTweenアニメーション）③BacklogUI.cs（スクロール表示・ログ蓄積）④UIManager.cs（各UIパネルの表示制御）。各ファイルの完全なC#コードをコードブロックで出力し、ファイルパス（Assets/Scripts/UI/ファイル名.cs）をコメントで明示してください。仕様書：[仕様書全文]」

   **演出実装担当**のプロンプト：
   「あなたはUnityビジュアル演出実装の専門エンジニアです。以下の仕様書を読み、名前空間KamiNoFuruMatchiで演出システムを実装してください。出力するファイル：①SpriteManager.cs（キャラクタースプライトの表示・差分切り替え・フェードイン/アウト・UniTask）②BackgroundManager.cs（背景切り替え・クロスフェードトランジション）③EffectManager.cs（画面シェイク・フラッシュ・フェード・DOTween使用）④TransitionController.cs（シーン間トランジション）。各ファイルの完全なC#コードをコードブロックで出力し、ファイルパス（Assets/Scripts/Visual/ファイル名.cs）をコメントで明示してください。仕様書：[仕様書全文]」

   **オーディオ実装担当**のプロンプト：
   「あなたはUnityオーディオシステム実装の専門エンジニアです。以下の仕様書を読み、名前空間KamiNoFuruMatchiでオーディオシステムを実装してください。出力するファイル：①AudioManager.cs（BGM・SE・ボイス用AudioSourceを分離管理・シングルトン）②BGMController.cs（BGMクロスフェード・ループ・UniTask使用）③SEPlayer.cs（効果音の重ね再生・プール管理）④AudioSettings.cs（音量設定のSaveLoad連携）。各ファイルの完全なC#コードをコードブロックで出力し、ファイルパス（Assets/Scripts/Audio/ファイル名.cs）をコメントで明示してください。仕様書：[仕様書全文]」

   **データ設計担当**のプロンプト：
   「あなたはUnityゲームデータ設計の専門家です。以下の仕様書を読み、即座にゲームで使えるサンプルデータを作成してください。出力するファイル：①scenario_chapter01.json（第1シーン分のサンプルシナリオJSON・テキスト/選択肢/BGM/演出コマンドを含む）②CharacterData.cs（キャラクターのScriptableObject定義）③sample_character_kanata.json（カナタのキャラクターデータサンプル）。各ファイルの完全な内容をコードブロックで出力し、ファイルパス（Assets/StreamingAssets/Scenarios/ または Assets/ScriptableObjects/）をコメントで明示してください。仕様書：[仕様書全文]」

5. 全エージェントの出力が揃ったら、各ファイルを Write ツールで実際に出力する：
   - コードブロックからファイル内容を抽出
   - `{PROJECT_ROOT}/Assets/Scripts/Core/*.cs` 等、指定パスに保存
   - 保存したファイル一覧を表示

6. セットアップ手順を出力する：
   ```
   ## ✅ 生成完了

   ### 次のステップ
   1. Unity 2022 LTS でプロジェクトを開く
   2. Package Manager で以下をインストール：
      - UniTask (Cysharp/UniTask via git URL)
      - DOTween (Asset Store or git)
      - TextMeshPro (Unity 6はコアに統合済み・不要)
   3. Assets/Scripts/ フォルダを確認
   4. シーンに GameManager, DialogueEngine, UIManager を配置
   ```

## 注意事項
- 外部スクリプト・APIキー不要。Claude Code内で完結
- 6人の担当は必ず並行起動（同一メッセージで複数Agent呼び出し）
- 生成コードの名前空間は必ず `KamiNoFuruMachi` を使用
- UniTask の using は `Cysharp.Threading.Tasks`
