// Assets/Scripts/UI/DialogueUI.cs
using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace KamiNoFuruMatchi
{
    /// <summary>
    /// セリフ表示UIコンポーネント。
    /// タイプライター効果で1文字ずつ表示し、クリック/タップで全文表示または次へ進む。
    /// </summary>
    public class DialogueUI : MonoBehaviour, IPointerClickHandler
    {
        // ---------------------------------------------------------------
        // Inspector fields
        // ---------------------------------------------------------------
        [Header("Text Components")]
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI bodyText;

        [Header("Name Panel")]
        [SerializeField] private GameObject namePanelRoot;

        [Header("Next Indicator")]
        [SerializeField] private GameObject nextIndicator;

        [Header("Typewriter Settings")]
        [SerializeField] [Min(1f)] private float charactersPerSecond = 30f;

        // ---------------------------------------------------------------
        // Private state
        // ---------------------------------------------------------------

        /// <summary>現在のタイプライターアニメーションをキャンセルするCTS。</summary>
        private CancellationTokenSource _typewriterCts;

        /// <summary>タイプライターが完了済みかどうか。</summary>
        private bool _isTypingComplete;

        /// <summary>
        /// 「次へ進む」クリックを待っているときに解決されるUniTaskCompletionSource。
        /// </summary>
        private UniTaskCompletionSource _advanceTcs;

        /// <summary>表示中のフルテキスト（スキップ時に使用）。</summary>
        private string _fullText;

        // ---------------------------------------------------------------
        // Unity lifecycle
        // ---------------------------------------------------------------

        private void Awake()
        {
            if (nextIndicator != null)
                nextIndicator.SetActive(false);
        }

        // ---------------------------------------------------------------
        // Public API
        // ---------------------------------------------------------------

        /// <summary>
        /// キャラクター名とセリフを表示し、プレイヤーが「次へ」を押すまで待機する。
        /// </summary>
        /// <param name="characterName">
        ///   表示するキャラクター名。"narrator" (大文字小文字無視) の場合は名前欄を非表示にする。
        /// </param>
        /// <param name="body">本文テキスト。</param>
        /// <param name="cancellationToken">外部キャンセルトークン。</param>
        /// <returns>
        ///   true  : 正常にプレイヤーが「次へ」を選択した。
        ///   false : キャンセルされた。
        /// </returns>
        public async UniTask<bool> ShowDialogueAsync(
            string characterName,
            string body,
            CancellationToken cancellationToken = default)
        {
            // --- 名前欄の表示/非表示 ---
            bool isNarrator = string.IsNullOrEmpty(characterName)
                              || characterName.Equals("narrator", StringComparison.OrdinalIgnoreCase);

            if (namePanelRoot != null)
                namePanelRoot.SetActive(!isNarrator);

            if (nameText != null)
                nameText.text = isNarrator ? string.Empty : characterName;

            // --- タイプライター開始 ---
            _fullText = body ?? string.Empty;
            _isTypingComplete = false;

            if (nextIndicator != null)
                nextIndicator.SetActive(false);

            // 前のアニメーションを安全にキャンセル
            CancelTypewriter();

            _typewriterCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            bool typingCompleted = await RunTypewriterAsync(_fullText, _typewriterCts.Token)
                .SuppressCancellationThrow();

            if (cancellationToken.IsCancellationRequested)
                return false;

            // タイプライター完了（スキップによる場合も含む）後、全文を確実に表示
            if (bodyText != null)
                bodyText.text = _fullText;

            _isTypingComplete = true;

            if (nextIndicator != null)
                nextIndicator.SetActive(true);

            // --- プレイヤーの「次へ」入力を待つ ---
            _advanceTcs = new UniTaskCompletionSource();

            bool advanced = await _advanceTcs.Task
                .AttachExternalCancellation(cancellationToken)
                .SuppressCancellationThrow();

            if (nextIndicator != null)
                nextIndicator.SetActive(false);

            return !cancellationToken.IsCancellationRequested;
        }

        /// <summary>
        /// タイプライター表示速度を実行時に変更する。
        /// </summary>
        public void SetCharactersPerSecond(float cps)
        {
            charactersPerSecond = Mathf.Max(1f, cps);
        }

        /// <summary>
        /// ダイアログパネル全体の表示/非表示を切り替える。
        /// </summary>
        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }

        // ---------------------------------------------------------------
        // IPointerClickHandler
        // ---------------------------------------------------------------

        public void OnPointerClick(PointerEventData eventData)
        {
            HandleAdvanceInput();
        }

        // ---------------------------------------------------------------
        // Input (keyboard / gamepad support hook)
        // ---------------------------------------------------------------

        private void Update()
        {
            // スペース / Enter / Z キーでも進む（ゲームパッド拡張時はここを修正）
            if (Input.GetKeyDown(KeyCode.Space)
                || Input.GetKeyDown(KeyCode.Return)
                || Input.GetKeyDown(KeyCode.Z))
            {
                HandleAdvanceInput();
            }
        }

        // ---------------------------------------------------------------
        // Private helpers
        // ---------------------------------------------------------------

        private void HandleAdvanceInput()
        {
            if (!_isTypingComplete)
            {
                // タイプライター中 → 全文即時表示
                CancelTypewriter();
                // RunTypewriterAsync の SuppressCancellationThrow が制御を返し、
                // ShowDialogueAsync 側で全文表示とフラグ設定が行われる
            }
            else
            {
                // 全文表示済み → 次へ進む
                _advanceTcs?.TrySetResult();
            }
        }

        private void CancelTypewriter()
        {
            if (_typewriterCts != null && !_typewriterCts.IsCancellationRequested)
            {
                _typewriterCts.Cancel();
                _typewriterCts.Dispose();
                _typewriterCts = null;
            }
        }

        /// <summary>
        /// TextMeshProのmaxVisibleCharactersを使って1文字ずつ表示するタイプライター処理。
        /// </summary>
        private async UniTask RunTypewriterAsync(string text, CancellationToken ct)
        {
            if (bodyText == null) return;

            bodyText.text = text;
            bodyText.maxVisibleCharacters = 0;

            // TMP が文字情報を確定するために1フレーム待つ
            await UniTask.Yield(PlayerLoopTiming.Update, ct);

            int totalChars = bodyText.textInfo.characterCount;
            if (totalChars == 0) return;

            float interval = 1f / Mathf.Max(1f, charactersPerSecond);

            for (int i = 1; i <= totalChars; i++)
            {
                bodyText.maxVisibleCharacters = i;
                await UniTask.WaitForSeconds(interval, cancellationToken: ct);
            }

            // 完了後はリセット（maxVisibleCharacters の上限を外す）
            bodyText.maxVisibleCharacters = int.MaxValue;
        }

        private void OnDestroy()
        {
            CancelTypewriter();
            _advanceTcs?.TrySetCanceled();
        }
    }
}
