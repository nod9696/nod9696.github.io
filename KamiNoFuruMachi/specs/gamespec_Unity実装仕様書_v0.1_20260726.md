# 「神の降る街（ナインフォール）」Unity実装仕様書 v0.1

前提：Unity 6 LTS / URP / Claude Code併用開発

---

## 0. 開発基盤

| 項目 | 選定 | 理由 |
|---|---|---|
| エンジンバージョン | Unity 6 LTS（6000.x系） | 長期サポート、GPU Resident Drawerでレンダリング性能向上済み |
| レンダーパイプライン | URP | HDRPより軽量。TPS+ローグライトの反復ビルドで有利。モバイル展開の余地も残る |
| Scripting Backend | IL2CPP | ビルド最終形はこちらに統一（開発中はMono併用可） |
| バージョン管理 | Git + Unity Text Serialization（Force Text） + Git LFS | 既存のGit/Confluide運用と親和。シーン/プレハブをテキスト化しdiff可能に |
| プロジェクト設定 | Assembly Definition分割必須 | Editor拡張／Runtime／Testsを分離し、Claude Codeでの部分ビルド・テストがしやすくなる |

**Claude Code運用上の要点**：Unityのロジックの大半をC#（MonoBehaviour / 純粋POCOクラス）に寄せることで、Claude Codeが直接読み書きできる範囲を最大化する。逆にVisual Scripting（Bolt系）は極力使わない方針にする。

---

## 1. 必須パッケージ（Unity Package Manager）

| パッケージ | 用途 |
|---|---|
| Input System | TPS操作、コントローラー対応 |
| Cinemachine（3.x） | TPS三人称カメラ、ダンジョン内演出カメラ |
| AI Navigation（NavMeshComponents） | 敵AI、ランタイムNavMeshベイク（生成ダンジョン対応必須） |
| Addressables | ダンジョンモジュール／街シーンのアディティブロード、蓄積型コンテンツ管理 |
| Animation Rigging | 武器エイムのIK補正 |
| 2D Sprite / 2D Animation | 街パートの立ち絵・表情差分 |
| Timeline | 遺骸取得時などの短尺演出、街の状態変化演出 |
| Unity Test Framework | エンディング判定・スコア計算ロジックの単体テスト（純C#なのでUnity依存なしでテスト可能） |
| TextMeshPro | 日本語UI表示（フォントは別途用意） |

## 2. 推奨サードパーティ（アセットストア／OSS）

| アセット | 用途 | 備考 |
|---|---|---|
| **Yarn Spinner**（無料・OSS） | NPC会話・分岐管理 | C#変数と直結できるため、WorldStateの二軸スコアを参照した動的分岐に最適。フラグ地獄を避ける設計方針と相性が良い |
| **DOTween**（無料版で十分） | UI演出・トゥイーン全般 | 街パートの2D演出、UIフィードバック |
| **Synty Studios POLYGONシリーズ**（有料） | ダンジョン用モジュラー環境キット | モジュール構造の実装コストを大幅短縮。スタイライズ統一感も出しやすい |
| **Dungeon Architect**（有料・任意） | 手続き生成の土台 | 自前実装でも良いが、ConnectorGraphGenerator部分を検証目的で先に動かしたい場合は導入検討の価値あり。ただし世界観固有の重み付けロジック（1-4章）は結局自前実装が必要 |
| **Feel / MMFeedbacks**（有料・任意） | ヒットストップ、カメラシェイク等の「手触り」 | TPSの快感演出を素早く積み上げたい場合に有効 |

---

## 3. システム別実装仕様

### 3-1. ダンジョン生成システム

**構成ファイル**
```
Scripts/Dungeon/
 ├─ RoomModuleSO.cs          // ScriptableObject: tags, prefab, connectorSockets, combatDensity, narrativeWeight
 ├─ DungeonModulePool.cs     // 全RoomModuleSOの登録・検索
 ├─ InterpretationWeighting.cs // WorldStateスコア→抽選重みテーブル生成
 ├─ ConnectorGraphGenerator.cs // モジュールをソケット接続でグラフ化
 ├─ DungeonRuntimeBuilder.cs  // 生成グラフを実際にInstantiate、NavMeshRuntimeビルド
 └─ DungeonLayout.cs          // 生成結果データ（DominantTag含む）
```

**実装ポイント**

- 各RoomModuleにソケット（空のTransform）を複数配置し、接続時に位置・回転を自動整合させるソケット接続方式（インディー向け手続き生成の定番手法）
- `NavMeshSurface`をルームごとではなく生成完了後にダンジョン全体で1回ランタイムベイク（`BuildNavMeshAsync`使用でフレーム負荷分散）
- 敵・オブジェクトはUnity標準の`ObjectPool<T>`でプーリングし、Run終了時に一括返却

### 3-2. TPS戦闘システム

**構成ファイル**
```
Scripts/Combat/
 ├─ PlayerController.cs       // CharacterController + Input System連携
 ├─ CharacterWeaponHandler.cs // 射撃・リロード、キャラ別武器差分
 ├─ IDamageable.cs            // 被弾インターフェース
 ├─ EnemyAIController.cs      // NavMeshAgentベースの基本AI（索敵→接近/牽制→攻撃）
 └─ PlayerActionEventBus.cs   // アクションログ発行（スコア計算の入力元）
```

**実装ポイント**

- CinemachineのThirdPersonFollow + Aimコンポーネントで肩越しカメラを構築
- マヨワズ／リリスで武器種・エイムFOV・移動速度などパラメータをScriptableObjectで差分定義（`CharacterCombatProfileSO`）
- 全アクション（撃破・遺骸操作・弔い演出実行など）は`PlayerActionEventBus`を経由して発行し、スコア計算層（3-3）が購読する設計にする。戦闘コードとスコア計算コードを直接結合させない（疎結合にしておくとバランス調整がしやすい）

> **注記（2026-07-26）**: 本セクション（TPS戦闘）はコマンドRPG化に伴い `specs/gamespec_コマンドRPG化差分_v0.2_20260726.md` で `Scripts/Battle/` に全面差し替え済み。`Scripts/Combat/` は実装しない。詳細は差分仕様書を参照。

### 3-3. 周回状態管理・スコア計算

**構成ファイル**
```
Scripts/Core/
 ├─ WorldState.cs             // 永続データ（POCO、Unity非依存）
 ├─ RunState.cs                // Run単位一時データ
 ├─ ActionScoreMapper.cs       // PlayerActionEvent → ScoreDelta変換
 ├─ SaveSystem.cs              // JSON直列化・Application.persistentDataPath保存
 ├─ GameStateService.cs        // シーン跨ぎで状態保持する非MonoBehaviourサービス
 └─ EndingJudge.cs             // 閾値・ヒステリシス判定（Unity非依存、単体テスト対象）
```

**実装ポイント**

- `WorldState`と`EndingJudge`はUnityEngine名前空間に依存しない純粋C#にする。これによりUnity Test Frameworkで**エディタを開かずにロジックだけ**をテストでき、数値バランス調整のイテレーションが速くなる
- セーブはNewtonsoft.Json（Unity公式パッケージあり）でシリアライズ。スキーマバージョン番号をWorldStateに持たせ、開発中の構造変更に耐える
- シーン間の状態保持は`DontDestroyOnLoad`の単一Singletonではなく、`GameStateService`を明示的に注入する構成にする（テスト容易性とデバッグのしやすさのため）

### 3-4. 街パート（2D）

**構成ファイル**
```
Scripts/Town/
 ├─ TownSceneController.cs
 ├─ NpcInteractable.cs         // クリック/インタラクトでYarn Spinner起動
 ├─ NpcRuntimeState.cs
 └─ TownVisualStateBinder.cs   // WorldDecayLevel等を背景演出に反映
```

**実装ポイント**

- 1画面2D構成なので、背景・NPCスプライトはCanvas上のUI要素またはSpriteRendererのどちらかに統一（混在させると演出のレイヤー管理が煩雑になるため、方針決めが必要）→ **提案：スプライトはワールド空間、UIは会話ウィンドウのみに限定**すると、背景の視差演出などを後から足しやすい
- Yarn SpinnerのVariable StorageをカスタムクラスでラップしWorldStateのスコアを直接参照させる。これにより会話側は「フラグ」ではなく「スコアの現在値」を見て台詞を出し分けられる

### 3-5. エンディング/演出まわり

- Timelineでトゥルーエンド演出（次元崩壊〜地球再出発）を構築。EndingJudgeの結果を受けてTimelineアセットを切り替える形にすれば、演出は非エンジニアでも差し替えやすい

---

## 4. 必要アセット洗い出し

### 4-1. キャラクター（3D・ダンジョンパート用）

| アセット | 内容 | 備考 |
|---|---|---|
| マヨワズ 3Dモデル＋リグ | フルボディ、表情モーフ（重要カットシーン用） | 自作 or 発注が基本。既製アセットの流用は世界観上の主人公なので非推奨 |
| リリス 3Dモデル＋リグ | 同上 | 同上 |
| 共通アニメーションセット | 移動（歩/走）、エイム、射撃、リロード、被弾、回避、死亡、遺骸操作モーション（取得/破壊/封印の3種、キャラ別に差分） | モーションキャプチャ資産（Mixamo等）をベースにカスタム調整するのが現実的 |
| 敵キャラクター（複数種） | 最低3〜4種類（近接/遠距離/特殊）、使い回し前提のプーリング対応 | Synty等のスタイライズキットで揃えると統一感とコスト効率が良い |

### 4-2. NPC（2D・街パート用）

| アセット | 内容 |
|---|---|
| NPC立ち絵 5〜7人分 | 各表情差分3〜5種（通常/驚き/悲哀/決意等、AwarenessLevel演出用） |
| 背景（街の状態別） | WorldDecayLevelに応じた複数バリエーション（初期/中期/崩壊進行） |

### 4-3. 環境・ダンジョン素材

| アセット | 内容 |
|---|---|
| モジュラー環境キット | 戦闘部屋/探索部屋/演出部屋/遺骸部屋、各テーマタグ分（最低4〜6テーマ×複数モジュール） |
| 遺骸オブジェクト | 取得/破壊/封印それぞれの見た目差分とVFX |
| ライティングプリセット | URP向け、ダンジョンのテーマタグごとの雰囲気差別化用 |

### 4-4. UI/フォント

| アセット | 内容 |
|---|---|
| 日本語対応フォント | 源ノ角ゴシック / Noto Sans JP等（商用利用可ライセンス確認） |
| UIアイコン・ゲージ素材 | HPバー、状態アイコン等 |
| 会話ウィンドウUI | Yarn Spinner連携前提のレイアウト |

### 4-5. サウンド

| アセット | 内容 |
|---|---|
| BGM | 街（複数状態）、ダンジョン（テーマタグ別）、エンディング |
| SE | 射撃、被弾、遺骸操作、UI操作音 |
| ボイス（任意） | 主要カットシーンのみでも効果大。予算次第で後回し可 |

### 4-6. VFX

| アセット | 内容 |
|---|---|
| 銃撃・被弾エフェクト | Particle System / VFX Graph（URP対応） |
| 遺骸関連特殊効果 | 破壊/封印/取得それぞれで差別化した演出 |
| 世界の歪み演出 | CumulativeDistortionの可視化（ポストプロセスのエフェクト強度を連動させる） |

---

## 5. 次に詰めるべき論点（提案）

1. **街パートをUI主体にするかワールド空間主体にするか**の最終決定（4-4のレイヤー設計に直結）
2. **敵AIの複雑度**：シンプルなFSMで十分か、Behavior Designer等の導入が要るか
3. **キャラモデルの調達方針**：自作／外注／既製ベース＋カスタムのどれで進めるか（予算・スケジュールに直結する重要論点）
4. **セーブデータのスキーマ設計の詳細化**（WorldStateのバージョニング方針を先に固めておくと後が楽）

どれから詰めますか。あるいはPhase 1（MVPループ骨格）のコード自体をClaude Codeで書き始めるのも良いタイミングだと思います。
