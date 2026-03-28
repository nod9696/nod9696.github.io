# note.com 自動投稿セットアップ手順

## 必要なもの
- Node.js 20以上（https://nodejs.org/ja/ からインストール）
- note.com アカウント（Googleログイン）

---

## セットアップ（初回のみ）

### 1. Node.js をインストール
https://nodejs.org/ja/ から LTS版をダウンロードしてインストール。
インストール後、PowerShell を再起動して確認:
```powershell
node --version   # v20.x.x などと表示されればOK
```

### 2. パッケージをインストール
```powershell
cd f:\Claude\note-poster
npm install
npx playwright install chromium
```

### 3. 設定ファイルを作成
```powershell
Copy-Item .env.example .env
```
`.env` をテキストエディタで開き、`NOTE_USERNAME` に自分のnote.comユーザー名を設定。

### 4. Googleログインのセッションを保存（初回のみ）
```powershell
node setup-session.js
```
ブラウザが開くので、Googleアカウントでnote.comにログインする。
ログイン完了後、自動的に `session.json` が保存される。

### 5. 動作確認（ドライラン）
```powershell
node post-to-note.js --dry-run
```
エラーなく完了すれば準備OK。

### 6. Task Scheduler に登録（毎週日曜22:00の自動投稿）
PowerShell を **管理者として実行** して:
```powershell
cd f:\Claude\note-poster
Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass
.\register-task.ps1
```

---

## 週次ワークフロー

| タイミング | 作業 |
|---|---|
| 日曜 21:00頃 | Claude Code で `/indie-blog` を実行 → 記事生成 |
| 日曜 22:00 | Task Scheduler が自動で note.com に投稿 |

---

## トラブルシューティング

**セッションが切れた場合:**
```powershell
node setup-session.js
```

**手動で今すぐ投稿:**
```powershell
node post-to-note.js
```

**ログ確認:**
```powershell
Get-Content post.log -Tail 20
```

**エラー時のスクリーンショット:**
エラー発生時に `error_[timestamp].png` が自動保存されます。

**Task Schedulerの確認・手動実行:**
```powershell
Get-ScheduledTask -TaskName "IndieBlog-NotePost"
Start-ScheduledTask -TaskName "IndieBlog-NotePost"
```

---

## ファイル構成

```
f:\Claude\note-poster\
  package.json         # 依存パッケージ定義
  setup-session.js     # 初回ログイン・セッション保存
  post-to-note.js      # note.com への自動投稿メイン
  register-task.ps1    # Windows Task Scheduler 登録
  .env                 # 設定ファイル（要作成）
  .env.example         # 設定ファイルのテンプレート
  session.json         # Googleセッション（自動生成）
  post.log             # 投稿ログ（自動生成）
```
