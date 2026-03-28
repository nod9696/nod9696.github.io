// Assets/Scripts/UI/UIManager.cs
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace KamiNoFuruMachi
{
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        [SerializeField] private DialogueUI _dialogueUI;
        [SerializeField] private ChoiceUI   _choiceUI;
        [SerializeField] private BacklogUI  _backlogUI;
        [SerializeField] private Image      _fadeOverlay;
        [SerializeField] private float      _defaultFadeDuration = 0.5f;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // フェードオーバーレイ初期化：不透明黒→フェードインで開幕
            if (_fadeOverlay != null)
            {
                _fadeOverlay.color         = new Color(0, 0, 0, 1);
                _fadeOverlay.raycastTarget = true;
                _fadeOverlay.gameObject.SetActive(true);
            }
        }

        private void Start() => FadeIn().Forget();

        // ── フェード ────────────────────────────────────────────────

        public async UniTask FadeIn(float duration = -1f, CancellationToken ct = default)
        {
            if (_fadeOverlay == null) return;
            float d = duration < 0 ? _defaultFadeDuration : duration;
            _fadeOverlay.gameObject.SetActive(true);
            _fadeOverlay.raycastTarget = true;
            await _fadeOverlay.DOFade(0f, d).SetEase(Ease.OutQuad).ToUniTask(cancellationToken: ct);
            _fadeOverlay.gameObject.SetActive(false);
            _fadeOverlay.raycastTarget = false;
        }

        public async UniTask FadeOut(float duration = -1f, CancellationToken ct = default)
        {
            if (_fadeOverlay == null) return;
            float d = duration < 0 ? _defaultFadeDuration : duration;
            _fadeOverlay.color = new Color(0, 0, 0, 0);
            _fadeOverlay.gameObject.SetActive(true);
            _fadeOverlay.raycastTarget = false;
            await _fadeOverlay.DOFade(1f, d).SetEase(Ease.InQuad).ToUniTask(cancellationToken: ct);
            _fadeOverlay.raycastTarget = true;
        }

        public async UniTask FadeOutAndIn(float holdDuration = 0f, float fadeDuration = -1f, CancellationToken ct = default)
        {
            await FadeOut(fadeDuration, ct);
            if (holdDuration > 0f)
                await UniTask.Delay(System.TimeSpan.FromSeconds(holdDuration), cancellationToken: ct);
            await FadeIn(fadeDuration, ct);
        }

        // ── ダイアログ ──────────────────────────────────────────────

        /// <summary>テキスト表示＋バックログ自動追記のラッパー</summary>
        public async UniTask ShowDialogueAsync(string charName, string body,
            bool addToBacklog = true, CancellationToken ct = default)
        {
            if (addToBacklog) _backlogUI?.AddLog(charName, body);
            if (_dialogueUI != null)
            {
                await _dialogueUI.ShowTextAsync(charName, body, false, ct);
                await _dialogueUI.WaitForAdvanceAsync(ct);
            }
        }

        public void HideDialogue() => _dialogueUI?.HideWindow();

        // ── バックログ ──────────────────────────────────────────────

        public void OpenBacklog()  => _backlogUI?.Open();
        public void CloseBacklog() => _backlogUI?.Close();
        public void ToggleBacklog() => _backlogUI?.Toggle();

        // ── プロパティアクセス ──────────────────────────────────────

        public DialogueUI DialogueUI => _dialogueUI;
        public ChoiceUI   ChoiceUI   => _choiceUI;
        public BacklogUI  BacklogUI  => _backlogUI;
    }
}
