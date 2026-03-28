Notion同期スクリプトを実行してください。

## 実行手順

1. 引数を確認する：
   - `--force` が含まれていれば全ファイルを強制再アップロード
   - `kirie` / `kami` / `delight` のいずれかが含まれていればそのプロジェクトのみ対象
   - 何もなければ全プロジェクトの差分のみ同期

2. 以下のコマンドを実行する：

引数なし（差分のみ）:
```
py f:/Claude/notion_sync.py
```

強制再アップロード:
```
py f:/Claude/notion_sync.py --force
```

特定プロジェクトのみ:
```
py f:/Claude/notion_sync.py kirie
py f:/Claude/notion_sync.py kami
py f:/Claude/notion_sync.py delight
```

3. 結果を報告する。更新ファイル数と、変更なしの場合はその旨を伝える。

## 対象プロジェクト
| プロジェクト | ディレクトリ | Notionページ |
|---|---|---|
| キリエとサキ | KirieSaki/Assets/StreamingAssets/Scenarios/ | キリエとサキ |
| 神の降る街 | KamiNoFuruMachi/.../Scenarios/ | 神の降る街 |
| Delight | Delight/ | Delight |

## 仕組み
- `f:/Claude/notion_sync_manifest.json` にファイルのmtimeとNotionページIDを記録
- 前回同期からmtimeが変わったファイルのみ再アップロード
- 既存のNotionページをarchiveして新しいページを作成
