// Assets/Scripts/UI/ChoiceUI.cs
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KirieSaki
{
    public class ChoiceUI : MonoBehaviour, IChoicePresenter
    {
        [SerializeField] private GameObject _panelRoot;
        [SerializeField] private Button     _buttonPrefab;
        [SerializeField] private Transform  _buttonContainer;
        [SerializeField] private float      _staggerDelay    = 0.08f;
        [SerializeField] private float      _animDuration    = 0.25f;
        [SerializeField] private float      _slideOffsetY    = -30f;

        private readonly List<Button> _buttons = new();

        private void Awake()
        {
            if (_panelRoot != null) _panelRoot.SetActive(false);
        }

        // ── IChoicePresenter ────────────────────────────────────────

        public async UniTask<int> PresentAsync(List<ChoiceOption> options, CancellationToken ct)
        {
            ClearButtons();
            _panelRoot.SetActive(true);

            var tcs = new UniTaskCompletionSource<int>();
            using var reg = ct.Register(() => tcs.TrySetCanceled());

            for (int i = 0; i < options.Count; i++)
            {
                int index = i;
                var btn   = Instantiate(_buttonPrefab, _buttonContainer);
                var label = btn.GetComponentInChildren<TextMeshProUGUI>();
                if (label != null) label.text = options[i].label;

                // 入場アニメーション準備
                var cg = btn.gameObject.AddComponent<CanvasGroup>();
                cg.alpha = 0f;
                cg.blocksRaycasts = false;
                var rt = btn.GetComponent<RectTransform>();
                var originY = rt.anchoredPosition.y;
                rt.anchoredPosition += new Vector2(0, _slideOffsetY);

                btn.onClick.AddListener(() =>
                {
                    foreach (var b in _buttons) b.interactable = false;
                    tcs.TrySetResult(index);
                });
                _buttons.Add(btn);

                // ステガーアニメーション（並行起動）
                AnimateButtonIn(btn, rt, cg, originY, i).Forget();
            }

            // 全ボタンのアニメーション完了後にクリック有効化
            await UniTask.Delay(
                System.TimeSpan.FromSeconds(_staggerDelay * options.Count + _animDuration),
                cancellationToken: ct);
            foreach (var b in _buttons)
                if (b.TryGetComponent<CanvasGroup>(out var cg2)) cg2.blocksRaycasts = true;

            int result;
            try   { result = await tcs.Task; }
            catch { result = 0; }

            await HideAsync();
            return result;
        }

        // ── 内部処理 ─────────────────────────────────────────────────

        private async UniTaskVoid AnimateButtonIn(Button btn, RectTransform rt, CanvasGroup cg, float targetY, int index)
        {
            await UniTask.Delay(System.TimeSpan.FromSeconds(_staggerDelay * index));
            var seq = DOTween.Sequence();
            seq.Join(cg.DOFade(1f, _animDuration).SetEase(Ease.OutQuad));
            seq.Join(rt.DOAnchorPosY(targetY, _animDuration).SetEase(Ease.OutBack));
            await seq.ToUniTask();
        }

        private async UniTask HideAsync()
        {
            if (_buttonContainer != null)
            {
                var cg = _buttonContainer.GetComponent<CanvasGroup>()
                         ?? _buttonContainer.gameObject.AddComponent<CanvasGroup>();
                await cg.DOFade(0f, 0.15f).ToUniTask();
                cg.alpha = 1f;
            }
            _panelRoot.SetActive(false);
            ClearButtons();
        }

        private void ClearButtons()
        {
            foreach (var b in _buttons) if (b != null) Destroy(b.gameObject);
            _buttons.Clear();
        }
    }
}
