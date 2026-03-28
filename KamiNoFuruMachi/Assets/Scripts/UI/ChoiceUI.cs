// Assets/Scripts/UI/ChoiceUI.cs
using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KamiNoFuruMatchi
{
    // ---------------------------------------------------------------
    // Data structures
    // ---------------------------------------------------------------

    /// <summary>選択肢1件分のデータ。</summary>
    [Serializable]
    public class ChoiceData
    {
        /// <summary>ボタンに表示するラベル。</summary>
        public string label;

        /// <summary>選択時にFlagManagerへ書き込むフラグキー（省略可）。</summary>
        public string flagKey;

        /// <summary>フラグに書き込む値（省略可）。</summary>
        public object flagValue;
    }

    /// <summary>選択結果。</summary>
    public readonly struct ChoiceResult
    {
        /// <summary>選択されたインデックス（0始まり）。キャンセル時は -1。</summary>
        public readonly int Index;

        /// <summary>選択された選択肢のフラグキー。</summary>
        public readonly string FlagKey;

        /// <summary>選択された選択肢のフラグ値。</summary>
        public readonly object FlagValue;

        public ChoiceResult(int index, string flagKey, object flagValue)
        {
            Index    = index;
            FlagKey  = flagKey;
            FlagValue = flagValue;
        }
    }

    // ---------------------------------------------------------------
    // Component
    // ---------------------------------------------------------------

    /// <summary>
    /// 選択肢UIコンポーネント。
    /// 選択肢ボタンを動的生成し、DOTweenで下からフェードインするアニメーションを行う。
    /// 選択結果をUniTaskで返し、FlagManagerへフラグを書き込む。
    /// </summary>
    public class ChoiceUI : MonoBehaviour
    {
        // ---------------------------------------------------------------
        // Inspector fields
        // ---------------------------------------------------------------

        [Header("Panel")]
        [SerializeField] private GameObject choicePanelRoot;

        [Header("Prefab")]
        [SerializeField] private Button choiceButtonPrefab;

        [Header("Layout")]
        [SerializeField] private Transform buttonContainer;

        [Header("Animation")]
        [SerializeField] [Min(0f)] private float animationDuration   = 0.3f;
        [SerializeField] [Min(0f)] private float buttonStaggerDelay  = 0.05f;
        [SerializeField]           private float slideOffsetY        = 40f;
        [SerializeField] [Range(0f, 1f)] private float fadeOutAlpha  = 0f;

        // ---------------------------------------------------------------
        // Private state
        // ---------------------------------------------------------------

        private readonly List<Button>    _spawnedButtons   = new();
        private readonly List<CanvasGroup> _spawnedGroups  = new();
        private UniTaskCompletionSource<ChoiceResult> _selectionTcs;

        // ---------------------------------------------------------------
        // Unity lifecycle
        // ---------------------------------------------------------------

        private void Awake()
        {
            if (choicePanelRoot != null)
                choicePanelRoot.SetActive(false);
        }

        // ---------------------------------------------------------------
        // Public API
        // ---------------------------------------------------------------

        /// <summary>
        /// 選択肢を表示し、プレイヤーが選択するまで待機する。
        /// 選択後に FlagManager へフラグを書き込み、結果を返す。
        /// </summary>
        /// <param name="choices">表示する選択肢リスト。</param>
        /// <param name="cancellationToken">外部キャンセルトークン。</param>
        /// <returns>選択結果。キャンセル時は Index == -1。</returns>
        public async UniTask<ChoiceResult> ShowChoicesAsync(
            IReadOnlyList<ChoiceData> choices,
            CancellationToken cancellationToken = default)
        {
            if (choices == null || choices.Count == 0)
                return new ChoiceResult(-1, null, null);

            PreparePanel();
            SpawnButtons(choices);

            await AnimateButtonsIn(cancellationToken);

            if (cancellationToken.IsCancellationRequested)
            {
                CleanUp();
                return new ChoiceResult(-1, null, null);
            }

            // プレイヤーの選択を待つ
            _selectionTcs = new UniTaskCompletionSource<ChoiceResult>();

            ChoiceResult result;
            bool cancelled = await _selectionTcs.Task
                .AttachExternalCancellation(cancellationToken)
                .SuppressCancellationThrow();

            if (cancelled || cancellationToken.IsCancellationRequested)
            {
                result = new ChoiceResult(-1, null, null);
            }
            else
            {
                result = _selectionTcs.Task.GetAwaiter().GetResult();

                // フラグ書き込み
                if (!string.IsNullOrEmpty(result.FlagKey) && FlagManager.Instance != null)
                    FlagManager.Instance.SetFlag(result.FlagKey, result.FlagValue);
            }

            CleanUp();
            return result;
        }

        // ---------------------------------------------------------------
        // Private helpers – panel & buttons
        // ---------------------------------------------------------------

        private void PreparePanel()
        {
            ClearButtons();

            if (choicePanelRoot != null)
                choicePanelRoot.SetActive(true);
        }

        private void SpawnButtons(IReadOnlyList<ChoiceData> choices)
        {
            if (choiceButtonPrefab == null || buttonContainer == null) return;

            for (int i = 0; i < choices.Count; i++)
            {
                int capturedIndex = i;
                ChoiceData data   = choices[i];

                Button btn = Instantiate(choiceButtonPrefab, buttonContainer);
                btn.name   = $"ChoiceButton_{i}";

                // ラベル設定
                TextMeshProUGUI label = btn.GetComponentInChildren<TextMeshProUGUI>();
                if (label != null)
                    label.text = data.label;

                // CanvasGroup（フェードイン用）
                CanvasGroup cg = btn.gameObject.GetComponent<CanvasGroup>();
                if (cg == null)
                    cg = btn.gameObject.AddComponent<CanvasGroup>();

                cg.alpha          = fadeOutAlpha;
                cg.blocksRaycasts = false; // アニメーション完了まで無効

                _spawnedButtons.Add(btn);
                _spawnedGroups.Add(cg);

                // クリックリスナー
                btn.onClick.AddListener(() => OnButtonClicked(capturedIndex, data));
            }
        }

        private async UniTask AnimateButtonsIn(CancellationToken ct)
        {
            var sequence = DOTween.Sequence();

            for (int i = 0; i < _spawnedButtons.Count; i++)
            {
                int idx         = i;
                RectTransform rt = _spawnedButtons[i].GetComponent<RectTransform>();
                CanvasGroup cg   = _spawnedGroups[i];

                Vector2 originalPos = rt.anchoredPosition;
                Vector2 startPos    = originalPos + new Vector2(0f, -slideOffsetY);
                rt.anchoredPosition = startPos;

                float delay = buttonStaggerDelay * i;

                sequence.Insert(delay, rt
                    .DOAnchorPos(originalPos, animationDuration)
                    .SetEase(Ease.OutCubic));

                sequence.Insert(delay, cg
                    .DOFade(1f, animationDuration)
                    .SetEase(Ease.OutCubic)
                    .OnComplete(() => _spawnedGroups[idx].blocksRaycasts = true));
            }

            await sequence
                .ToUniTask(cancellationToken: ct)
                .SuppressCancellationThrow();
        }

        private void OnButtonClicked(int index, ChoiceData data)
        {
            // 多重クリック防止
            foreach (Button btn in _spawnedButtons)
                btn.interactable = false;

            _selectionTcs?.TrySetResult(new ChoiceResult(index, data.flagKey, data.flagValue));
        }

        private void CleanUp()
        {
            if (choicePanelRoot != null)
                choicePanelRoot.SetActive(false);

            ClearButtons();
        }

        private void ClearButtons()
        {
            foreach (Button btn in _spawnedButtons)
                if (btn != null) Destroy(btn.gameObject);

            _spawnedButtons.Clear();
            _spawnedGroups.Clear();
        }

        private void OnDestroy()
        {
            _selectionTcs?.TrySetCanceled();
            ClearButtons();
        }
    }
}
