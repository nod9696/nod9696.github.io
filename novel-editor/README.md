# 硯 -Suzuri- 小説執筆アプリ

章・シーン単位で書き進める、ローカルファースト（IndexedDB保存）の小説執筆PWAです。
ビルド不要の静的ファイルのみで構成されているので、そのままホスティングするだけで動きます。

## 構成

```
novel-editor/
├── index.html         # アプリ本体（DOM構造）
├── styles.css          # デザイン（原稿用紙の升目をモチーフにした文字数レール）
├── app.js              # アプリロジック（ツリー・エディタ・自動保存・集中モード）
├── db.js               # IndexedDBラッパー（作品・章・シーンの永続化）
├── drive-sync.js        # Google Drive同期（OAuth + Drive API）
├── notion-sync.js        # Notion同期（GASプロキシ経由でNotion APIを呼び出し）
├── notion-proxy.gs       # ↑用に自分のGoogle Apps Scriptへデプロイするコード
├── manifest.json        # PWAマニフェスト（インストール用）
├── service-worker.js    # オフラインキャッシュ
└── icons/                # アプリアイコン
```

## ローカルで試す

ビルド不要ですが、Service WorkerはHTTP(S)サーバー経由でないと動作しないため、
ファイルを直接開く（`file://`）のではなく簡易サーバーを立てて確認してください。

```bash
cd novel-editor
python3 -m http.server 8000
# ブラウザで http://localhost:8000 を開く
```

## デプロイ（GitHub Pages）

お使いの `nod9696.github.io` リポジトリを想定した手順です。

1. リポジトリ内に `novel-editor/`（または任意のフォルダ名）としてこのフォルダをコピー
2. コミットしてpush
3. リポジトリの Settings → Pages でルート、または該当フォルダを公開設定
4. `https://nod9696.github.io/novel-editor/` のようなURLでアクセス可能に

スマホでは、そのURLをブラウザで開いて「ホーム画面に追加」でアプリのようにインストールできます。
PC（Chrome/Edge）では、アドレスバー右側のインストールアイコンからインストールできます。

## Google Drive 同期の設定（任意）

ローカル保存だけでも通常利用に問題ありませんが、複数端末で同期したい場合は
自分のGoogle CloudプロジェクトでOAuthクライアントIDを発行してください。

1. [Google Cloud Console](https://console.cloud.google.com/) で新規プロジェクトを作成
2. 「APIとサービス」→「ライブラリ」から **Google Drive API** を有効化
3. 「APIとサービス」→「認証情報」→「認証情報を作成」→「OAuthクライアントID」
   - アプリケーションの種類: **ウェブアプリケーション**
   - 「承認済みのJavaScript生成元」に、デプロイ先のURL（例: `https://nod9696.github.io`）を追加
   - ローカル確認用に `http://localhost:8000` も追加しておくと便利です
4. 発行された「クライアントID」をコピー
5. アプリの右上「設定」アイコン → Google Drive 同期欄に貼り付けて保存
6. 「Googleアカウントで接続」→ 権限は `drive.file` スコープのみ（このアプリが作成したファイルにのみアクセス）

同期は自動ではなく、設定画面の「今すぐDriveに保存」「Driveから復元」ボタンによる手動実行です。
バックアップは Drive 内の「Suzuri Novel Backups」フォルダに、作品ごとのJSONファイルとして保存されます。

OAuth同意画面を「テスト」モードのままにしておけば、自分のGoogleアカウントだけで使う分には
Googleによる審査なしで動作します（テストユーザーとして自分を追加してください）。

## Notion 同期の設定（任意）

DriveとNotionは独立した設定で、どちらか一方だけでも両方でも使えます。
Notion APIはブラウザから直接呼び出せない（CORS非対応）ため、中継役として
小さなGoogle Apps ScriptのWebアプリを自分のアカウントにデプロイします。

1. [notion.so/my-integrations](https://www.notion.so/my-integrations) で「New integration」→
   種類は「Internal」を選んで作成し、「Internal Integration Secret」をコピー
2. Notion側にバックアップ用の新規ページを作成（例：`Suzuri Novel Backups`）
3. そのページの右上「…」メニュー →「コネクト」→ 作成したインテグレーションを追加
4. ページを開いた状態でブラウザのURLからページIDをコピー
   （`https://www.notion.so/xxxx/ページ名-<32文字のID>` の `<32文字のID>` の部分。
   ハイフンの有無はどちらでも構いません）
5. [script.google.com](https://script.google.com/) で新規プロジェクトを作成し、
   同梱の `notion-proxy.gs` の中身をそのまま貼り付け
6. 「デプロイ」→「新しいデプロイ」→ 種類「ウェブアプリ」
   - 実行ユーザー: 自分
   - アクセスできるユーザー: 全員
7. 発行された `/exec` で終わるURLをコピー
8. アプリの設定画面 →「Notion 同期」欄に、上記のシークレット・ページID・GASプロキシURLの
   3つを貼り付けて保存 →「接続確認」で疎通をテスト

同期は自動ではなく、設定画面の「今すぐNotionに保存」「Notionから復元」ボタンによる手動実行です。
バックアップは指定した親ページの下に、作品ごとの子ページ（`suzuri-<projectId> — 作品名`）として
保存されます。本文を含む作品データ全体がJSON形式でコードブロックに格納されます
（Notionの1ブロックあたりの文字数制限があるため、長い本文は自動的に複数ブロックに分割されます）。

`notion-proxy.gs` はあなたのNotionトークンを保存しません。アプリから送られてきた
トークンをそのままNotionへ転送し、レスポンスを送り返すだけの単純な中継役です。

## データについて

- 通常の執筆データはすべて端末内のIndexedDBに保存されます（サーバー送信なし）
- 設定 → 「JSONをエクスポート」で作品ごとのバックアップファイルを手元に保存できます
- 別端末やブラウザに移す場合は、エクスポートしたJSONを「JSONをインポート」で読み込んでください

## カスタマイズのヒント

- `styles.css` 冒頭の `:root` 変数でカラーパレットを一括変更できます
- 目標文字数は作品ごとに設定画面で設定でき、サイドバー下部に進捗バーとして表示されます
- 右側の縦レールは、現在のシーンの文字数を400字＝原稿用紙1枚単位で可視化したものです
