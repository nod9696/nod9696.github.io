// Assets/Scripts/UI/UIManager.cs
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace KamiNoFuruMatchi
{
    /// <summary>
    /// UIサブシステム全体を一元管理するシングルトンMonoBehaviour。
    /// DialogueUI / ChoiceUI / BacklogUI への参照を持ち、
    /// フェードオーバーレイによる画面全体のフェードイン/アウトを提供する。
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        // ---------------------------------------------------------------
        // Singleton
        // ---------------------------------------------------------------

        private static UIManager _instance;

        /// <summary>UIManager のシングルトンインスタンス。</summary>
        public static UIManager Instance
        {
            get
            {
                if (_instance == null)
                    Debug.LogWarning("[UIManager] Instance is not yet initialized.");
                return _instance;
            }
        }

        // ---------------------------------------------------------------
        // Inspector fields – UI subsystems
        // ---------------------------------------------------------------

        [Header("UI Subsystems")]
        [SerializeField] private DialogueUI dialogueUI;
        [SerializeField] private ChoiceUI   choiceUI;
        [SerializeField] private BacklogUI  backlogUI;

        // ---------------------------------------------------------------
        // Inspector fields – Fade overlay
        // ---------------------------------------------------------------

        [Header("Fade Overlay")]
        [SerializeField] private Image fadeOverlay;
        [SerializeField] [Min(0f)] private float defaultFadeDuration = 0.5f;
        [SerializeField] private Color fadeColor = Color.black;

        // ---------------------------------------------------------------
        // Properties
        // ---------------------------------------------------------------

        /// <summary>ダイアログUIへの参照。</summary>
        public DialogueUI Dialogue => dialogueUI;

        /// <summary>選択肢UIへの参照。</summary>
        public ChoiceUI   Choice   => choiceUI;

        /// <summary>バックログUIへの参照。</summary>
        public BacklogUI  Backlog  => backlogUI;

        // ---------------------------------------------------------------
        // Unity lifecycle
        // ---------------------------------------------------------------

        private void Awake()
        {
            // シングルトン設定
            if (_instance != null && _instance != this)
            {
                Debug.LogWarning("[UIManager] Duplicate instance detected. Destroying new one.");
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);

            // フェードオーバーレイ初期化
            InitializeFadeOverlay();
        }

        private void OnDestroy()
        {
            if (_instance == this)
                _instance = null;

            fadeOverlay?.DOKill();
        }

        // ---------------------------------------------------------------
        // Public API – Panel visibility
        // ---------------------------------------------------------------

        /// <summary>ダイアログパネルを表示/非表示にする。</summary>
        public void SetDialogueVisible(bool visible)
        {
            if (dialogueUI != null)
                dialogueUI.SetVisible(visible);
        }

        /// <summary>バックログパネルを開く。</summary>
        public void OpenBacklog()
        {
            backlogUI?.Open();
        }

        /// <summary>バックログパネルを閉じる。</summary>
        public void CloseBacklog()
        {
            backlogUI?.Close();
        }

        /// <summary>バックログパネルのトグル。</summary>
        public void ToggleBacklog()
        {
            backlogUI?.Toggle();
        }

        /// <summary>
        /// すべてのUIパネルを一括で表示/非表示にする。
        /// フェードオーバーレイには影響しない。
        /// </summary>
        public void SetAllPanelsVisible(bool visible)
        {
            SetDialogueVisible(visible);

            if (!visible)
                backlogUI?.Close();
        }

        // ---------------------------------------------------------------
        // Public API – Fade
        // ---------------------------------------------------------------

        /// <summary>
        /// 画面をフェードインさせる（オーバーレイを不透明→透明）。
        /// </summary>
        /// <param name="duration">フェード秒数。0以下のとき defaultFadeDuration を使用。</param>
        /// <param name="cancellationToken">外部キャンセルトークン。</param>
        public async UniTask FadeIn(
            float             duration          = -1f,
            CancellationToken cancellationToken = default)
        {
            if (fadeOverlay == null) return;

            float d = duration <= 0f ? defaultFadeDuration : duration;

            SetFadeOverlayActive(true);

            await fadeOverlay
                .DOFade(0f, d)
                .SetEase(Ease.InQuad)
                .ToUniTask(cancellationToken: cancellationToken)
                .SuppressCancellationThrow();

            // フェードイン完了後はオーバーレイを非活性化（レイキャストをブロックしない）
            if (!cancellationToken.IsCancellationRequested)
                SetFadeOverlayActive(false);
        }

        /// <summary>
        /// 画面をフェードアウトさせる（オーバーレイを透明→不透明）。
        /// </summary>
        /// <param name="duration">フェード秒数。0以下のとき defaultFadeDuration を使用。</param>
        /// <param name="cancellationToken">外部キャンセルトークン。</param>
        public async UniTask FadeOut(
            float             duration          = -1f,
            CancellationToken cancellationToken = default)
        {
            if (fadeOverlay == null) return;

            float d = duration <= 0f ? defaultFadeDuration : duration;

            // 透明から開始
            Color c   = fadeColor;
            c.a        = 0f;
            fadeOverlay.color = c;
            SetFadeOverlayActive(true);

            await fadeOverlay
                .DOFade(1f, d)
                .SetEase(Ease.OutQuad)
                .ToUniTask(cancellationToken: cancellationToken)
                .SuppressCancellationThrow();
        }

        /// <summary>
        /// フェードアウト→フェードインを連続して行う（シーン遷移演出など）。
        /// </summary>
        /// <param name="holdDuration">暗転維持秒数。</param>
        /// <param name="fadeDuration">各フェードの秒数。</param>
        /// <param name="cancellationToken">外部キャンセルトークン。</param>
        public async UniTask FadeOutAndIn(
            float             holdDuration      = 0f,
            float             fadeDuration      = -1f,
            CancellationToken cancellationToken = default)
        {
            await FadeOut(fadeDuration, cancellationToken);

            if (cancellationToken.IsCancellationRequested) return;

            if (holdDuration > 0f)
                await UniTask.WaitForSeconds(holdDuration, cancellationToken: cancellationToken);

            if (cancellationToken.IsCancellationRequested) return;

            await FadeIn(fadeDuration, cancellationToken);
        }

        // ---------------------------------------------------------------
        // Public API – Convenience dialogue wrapper
        // ---------------------------------------------------------------

        /// <summary>
        /// DialogueUI.ShowDialogueAsync のショートカット。
        /// 表示完了後に BacklogUI へ自動的にエントリを追加する。
        /// </summary>
        public async UniTask<bool> ShowDialogueAsync(
            string            characterName,
            string            body,
            bool              addToBacklog      = true,
            CancellationToken cancellationToken = default)
        {
            if (dialogueUI == null) return false;

            bool result = await dialogueUI.ShowDialogueAsync(characterName, body, cancellationToken);

            if (addToBacklog && result)
                backlogUI?.AddLog(characterName, body);

            return result;
        }

        // ---------------------------------------------------------------
        // Private helpers
        // ---------------------------------------------------------------

        private void InitializeFadeOverlay()
        {
            if (fadeOverlay == null) return;

            Color c   = fadeColor;
            c.a        = 1f; // 起動時は暗転状態
            fadeOverlay.color     = c;
            fadeOverlay.raycastTarget = true;
            fadeOverlay.gameObject.SetActive(true);
        }

        private void SetFadeOverlayActive(bool active)
        {
            if (fadeOverlay == null) return;
            fadeOverlay.gameObject.SetActive(active);
            fadeOverlay.raycastTarget = active;
        }
    }
}
