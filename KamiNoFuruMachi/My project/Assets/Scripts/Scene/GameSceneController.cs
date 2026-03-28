// Assets/Scripts/Scene/GameSceneController.cs
// GameScene のエントリーポイント。DialogueEngine を初期化し、各Presenterアダプターを繋ぎ込む。
// MonoBehaviour として GameScene の GameObject にアタッチして使用する。

using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace KamiNoFuruMachi
{
    /// <summary>
    /// GameScene のライフサイクル管理と DialogueEngine への依存注入を担うコントローラー。
    /// Inspector から chapterId を指定し、Start() で再生を開始する。
    /// </summary>
    public class GameSceneController : MonoBehaviour
    {
        // -------------------------------------------------------------------------
        // Inspector フィールド
        // -------------------------------------------------------------------------

        [Header("Dialogue")]
        [Tooltip("再生するチャプターID（例: chapter01）")]
        [SerializeField] private string _chapterId = "chapter01";

        [Tooltip("DialogueEngine コンポーネントへの参照")]
        [SerializeField] private DialogueEngine _dialogueEngine;

        [Header("Presenters — Visual")]
        [Tooltip("SpriteManager コンポーネントへの参照")]
        [SerializeField] private SpriteManager _spriteManager;

        [Tooltip("BackgroundManager コンポーネントへの参照")]
        [SerializeField] private BackgroundManager _backgroundManager;

        [Tooltip("EffectManager コンポーネントへの参照")]
        [SerializeField] private EffectManager _effectManager;

        [Header("Presenters — UI")]
        [Tooltip("DialogueUI コンポーネントへの参照（ITextPresenter 実装済み）")]
        [SerializeField] private DialogueUI _dialogueUI;

        [Tooltip("ChoiceUI コンポーネントへの参照（IChoicePresenter 実装済み）")]
        [SerializeField] private ChoiceUI _choiceUI;

        [Header("Chapter Navigation")]
        [Tooltip("次のチャプターID。空文字の場合はエンディングへ遷移する")]
        [SerializeField] private string _nextChapterId = "";

        // -------------------------------------------------------------------------
        // イベント
        // -------------------------------------------------------------------------

        /// <summary>チャプター完了時に発行される。引数はチャプターID。</summary>
        public event Action<string> OnChapterComplete;

        // -------------------------------------------------------------------------
        // 内部状態
        // -------------------------------------------------------------------------

        private CancellationTokenSource _sceneCts;

        // -------------------------------------------------------------------------
        // Unity ライフサイクル
        // -------------------------------------------------------------------------

        private void Start()
        {
            // シーン破棄時にキャンセルされる CTS を生成
            _sceneCts = new CancellationTokenSource();

            // GameManager が存在するか確認
            if (GameManager.Instance == null)
            {
                Debug.LogError("[GameSceneController] GameManager.Instance が見つかりません。PersistentScene がロードされているか確認してください。");
                return;
            }

            // DialogueEngine への依存を注入して再生開始
            InitializeAndPlay().Forget();
        }

        private void OnDestroy()
        {
            // シーン破棄時に進行中の非同期処理をキャンセル
            _sceneCts?.Cancel();
            _sceneCts?.Dispose();
            _sceneCts = null;
        }

        // -------------------------------------------------------------------------
        // 初期化 & 再生
        // -------------------------------------------------------------------------

        /// <summary>
        /// アダプターを生成して DialogueEngine を初期化し、PlayChapterAsync を実行する。
        /// </summary>
        private async UniTaskVoid InitializeAndPlay()
        {
            // --- コンポーネントの存在チェック ---
            if (_dialogueEngine == null)
            {
                Debug.LogError("[GameSceneController] DialogueEngine が未設定です。");
                return;
            }
            if (_dialogueUI == null)
            {
                Debug.LogError("[GameSceneController] DialogueUI が未設定です。");
                return;
            }
            if (_choiceUI == null)
            {
                Debug.LogError("[GameSceneController] ChoiceUI が未設定です。");
                return;
            }

            // --- 各アダプターを生成 ---
            var bgPresenter     = new BgPresenterAdapter(_backgroundManager);
            var spritePresenter = new SpritePresenterAdapter(_spriteManager);
            var bgmPresenter    = new BgmPresenterAdapter(AudioManager.Instance);
            var sePresenter     = new SePresenterAdapter(AudioManager.Instance);
            var effectPresenter = new EffectPresenterAdapter(_effectManager);

            // GameManager が保持する FlagManager（IFlagManager 実装済み）を直接渡す
            IFlagManager flagManager = GameManager.Instance.Flags;

            // --- DialogueEngine に依存を注入 ---
            _dialogueEngine.Initialize(
                bgPresenter,
                spritePresenter,
                _dialogueUI,       // ITextPresenter 実装済み
                _choiceUI,         // IChoicePresenter 実装済み
                bgmPresenter,
                sePresenter,
                effectPresenter,
                flagManager
            );

            // --- チャプター完了ハンドラーを購読 ---
            _dialogueEngine.OnChapterComplete += HandleChapterComplete;

            // --- チャプター再生開始 ---
            Debug.Log($"[GameSceneController] チャプター再生開始: {_chapterId}");
            try
            {
                await _dialogueEngine.PlayChapterAsync(_chapterId);
            }
            catch (OperationCanceledException)
            {
                // シーン破棄などによるキャンセルは正常終了とみなす
                Debug.Log("[GameSceneController] PlayChapterAsync がキャンセルされました。");
            }
            finally
            {
                _dialogueEngine.OnChapterComplete -= HandleChapterComplete;
            }
        }

        // -------------------------------------------------------------------------
        // チャプター完了ハンドラー
        // -------------------------------------------------------------------------

        /// <summary>
        /// DialogueEngine.OnChapterComplete から呼ばれる。
        /// オートセーブを実行した後、次章またはエンディングへ遷移する。
        /// </summary>
        private void HandleChapterComplete(string completedChapterId)
        {
            Debug.Log($"[GameSceneController] チャプター完了: {completedChapterId}");

            // ユーザー向けイベントを発行
            OnChapterComplete?.Invoke(completedChapterId);

            // オートセーブ（章番号をIDから抽出して渡す）
            int chapterNumber = ParseChapterNumber(completedChapterId);
            GameManager.Instance.TriggerAutoSave(chapterNumber);
            Debug.Log($"[GameSceneController] オートセーブ完了: chapter {chapterNumber}");

            // 次章 or エンディングへ遷移
            if (!string.IsNullOrEmpty(_nextChapterId))
            {
                // 同じ GameScene 内で次のチャプターを再生
                _chapterId = _nextChapterId;
                _nextChapterId = "";
                InitializeAndPlay().Forget();
            }
            else
            {
                // 次章なし → エンディングへ遷移
                Debug.Log("[GameSceneController] 全チャプター終了。エンディングへ遷移します。");
                GameManager.Instance.GoToEnding();
            }
        }

        /// <summary>
        /// チャプターIDから章番号を抽出する（例: "chapter01" → 1）。
        /// 抽出失敗時は 0 を返す。
        /// </summary>
        private static int ParseChapterNumber(string chapterId)
        {
            if (string.IsNullOrEmpty(chapterId)) return 0;
            // "chapter" プレフィックスを取り除いて数値化
            var numPart = chapterId.Replace("chapter", "").Trim();
            return int.TryParse(numPart, out var n) ? n : 0;
        }

        // =========================================================================
        // アダプタークラス群（ネストクラス）
        // =========================================================================

        // -------------------------------------------------------------------------
        // BgPresenterAdapter
        // -------------------------------------------------------------------------

        /// <summary>
        /// IBgPresenter → BackgroundManager ラッパー。
        /// BackgroundManager.SetBackground(bgId, transition) を呼び出す。
        /// duration は BackgroundManager 内部の設定値（_fadeDuration/_crossFadeDuration）が使われるため、
        /// インターフェース引数の duration は現在のアダプター層では無視される。
        /// duration を動的に反映させたい場合は BackgroundManager に SetDuration(float) を追加すること。
        /// </summary>
        private sealed class BgPresenterAdapter : IBgPresenter
        {
            private readonly BackgroundManager _bg;

            public BgPresenterAdapter(BackgroundManager bg)
            {
                _bg = bg;
            }

            /// <inheritdoc/>
            public async UniTask ShowAsync(string bgId, string transition, float duration, CancellationToken ct)
            {
                if (_bg == null)
                {
                    Debug.LogWarning("[BgPresenterAdapter] BackgroundManager が未設定です。スキップします。");
                    return;
                }

                // transition 文字列を BackgroundManager の規約に合わせてマッピング
                // DialogueCommand.Transition.CrossFade = "crossfade"
                // DialogueCommand.Transition.Cut       = "cut" → instant 扱い
                // DialogueCommand.Transition.Fade      = "fade"（デフォルト）
                string bgTransition = transition switch
                {
                    DialogueCommand.Transition.CrossFade => "crossfade",
                    DialogueCommand.Transition.Cut       => "instant",
                    _                                    => "fade",
                };

                // CancellationToken を BackgroundManager に渡せないため、
                // ct が既にキャンセル済みであれば早期リターン
                ct.ThrowIfCancellationRequested();

                await _bg.SetBackground(bgId, bgTransition);
            }
        }

        // -------------------------------------------------------------------------
        // SpritePresenterAdapter
        // -------------------------------------------------------------------------

        /// <summary>
        /// ISpritePresenter → SpriteManager ラッパー。
        /// UniTask を返す ShowCharacter / HideCharacter をそのまま委譲する。
        /// </summary>
        private sealed class SpritePresenterAdapter : ISpritePresenter
        {
            private readonly SpriteManager _sprite;

            public SpritePresenterAdapter(SpriteManager sprite)
            {
                _sprite = sprite;
            }

            /// <inheritdoc/>
            public async UniTask ShowAsync(string charId, string pose, string position, CancellationToken ct)
            {
                if (_sprite == null)
                {
                    Debug.LogWarning("[SpritePresenterAdapter] SpriteManager が未設定です。スキップします。");
                    return;
                }

                ct.ThrowIfCancellationRequested();
                await _sprite.ShowCharacter(charId, pose, position);
            }

            /// <inheritdoc/>
            public async UniTask HideAsync(string charId, CancellationToken ct)
            {
                if (_sprite == null)
                {
                    Debug.LogWarning("[SpritePresenterAdapter] SpriteManager が未設定です。スキップします。");
                    return;
                }

                ct.ThrowIfCancellationRequested();
                await _sprite.HideCharacter(charId);
            }
        }

        // -------------------------------------------------------------------------
        // BgmPresenterAdapter
        // -------------------------------------------------------------------------

        /// <summary>
        /// IBgmPresenter → AudioManager ラッパー。
        /// AudioManager.PlayBGM / StopBGM を UniTask で委譲する。
        /// </summary>
        private sealed class BgmPresenterAdapter : IBgmPresenter
        {
            private readonly AudioManager _audio;

            public BgmPresenterAdapter(AudioManager audio)
            {
                _audio = audio;
            }

            /// <inheritdoc/>
            public async UniTask PlayAsync(string bgmId, float fadeDuration, CancellationToken ct)
            {
                if (_audio == null)
                {
                    Debug.LogWarning("[BgmPresenterAdapter] AudioManager が未設定です。スキップします。");
                    return;
                }

                ct.ThrowIfCancellationRequested();
                // AudioManager.PlayBGM は内部でキャンセルトークンを GetCancellationTokenOnDestroy() から生成するため、
                // ここでは ct のキャンセル確認のみ行う
                await _audio.PlayBGM(bgmId, fadeDuration);
            }

            /// <inheritdoc/>
            public async UniTask StopAsync(float fadeDuration, CancellationToken ct)
            {
                if (_audio == null)
                {
                    Debug.LogWarning("[BgmPresenterAdapter] AudioManager が未設定です。スキップします。");
                    return;
                }

                ct.ThrowIfCancellationRequested();
                await _audio.StopBGM(fadeDuration);
            }
        }

        // -------------------------------------------------------------------------
        // SePresenterAdapter
        // -------------------------------------------------------------------------

        /// <summary>
        /// ISePresenter → AudioManager ラッパー。
        /// AudioManager.PlaySE(string id) はクリップロードを内部で行うため、同期的に呼び出す。
        /// </summary>
        private sealed class SePresenterAdapter : ISePresenter
        {
            private readonly AudioManager _audio;

            public SePresenterAdapter(AudioManager audio)
            {
                _audio = audio;
            }

            /// <inheritdoc/>
            public void Play(string seId)
            {
                if (_audio == null)
                {
                    Debug.LogWarning("[SePresenterAdapter] AudioManager が未設定です。スキップします。");
                    return;
                }

                // AudioManager.PlaySE は内部で PlaySEAsync を Forget() しているため非ブロッキング
                _audio.PlaySE(seId);
            }
        }

        // -------------------------------------------------------------------------
        // EffectPresenterAdapter
        // -------------------------------------------------------------------------

        /// <summary>
        /// IEffectPresenter → EffectManager ラッパー。
        /// 各エフェクトメソッドのシグネチャ差異を吸収する。
        /// 注意: EffectManager.GlitchEffect は intensity を持たない（duration のみ）。
        ///       intensity は無視され、duration のみ転送される。
        /// </summary>
        private sealed class EffectPresenterAdapter : IEffectPresenter
        {
            private readonly EffectManager _effect;

            public EffectPresenterAdapter(EffectManager effect)
            {
                _effect = effect;
            }

            /// <inheritdoc/>
            public async UniTask ShakeAsync(float intensity, float duration, CancellationToken ct)
            {
                if (_effect == null)
                {
                    Debug.LogWarning("[EffectPresenterAdapter] EffectManager が未設定です。スキップします。");
                    return;
                }

                ct.ThrowIfCancellationRequested();
                await _effect.ShakeScreen(intensity, duration);
            }

            /// <inheritdoc/>
            public async UniTask FlashAsync(Color color, float duration, CancellationToken ct)
            {
                if (_effect == null)
                {
                    Debug.LogWarning("[EffectPresenterAdapter] EffectManager が未設定です。スキップします。");
                    return;
                }

                ct.ThrowIfCancellationRequested();
                await _effect.FlashScreen(color, duration);
            }

            /// <inheritdoc/>
            /// <remarks>
            /// EffectManager.GlitchEffect(float duration) は intensity 引数を持たない。
            /// intensity はグリッチの見た目には影響せず、duration のみが有効。
            /// </remarks>
            public async UniTask GlitchAsync(float intensity, float duration, CancellationToken ct)
            {
                if (_effect == null)
                {
                    Debug.LogWarning("[EffectPresenterAdapter] EffectManager が未設定です。スキップします。");
                    return;
                }

                ct.ThrowIfCancellationRequested();
                // intensity は現 EffectManager API に引数がないため渡せない
                await _effect.GlitchEffect(duration);
            }

            /// <inheritdoc/>
            /// <remarks>
            /// fadeIn == true  → FadeFromBlack（黒から復帰）
            /// fadeIn == false → FadeToBlack  （黒へフェードアウト）
            /// </remarks>
            public async UniTask FadeAsync(float duration, bool fadeIn, CancellationToken ct)
            {
                if (_effect == null)
                {
                    Debug.LogWarning("[EffectPresenterAdapter] EffectManager が未設定です。スキップします。");
                    return;
                }

                ct.ThrowIfCancellationRequested();

                if (fadeIn)
                    await _effect.FadeFromBlack(duration);
                else
                    await _effect.FadeToBlack(duration);
            }
        }
    }
}
