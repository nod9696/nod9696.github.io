// Assets/Scripts/UI/BacklogUI.cs
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KamiNoFuruMatchi
{
    // ---------------------------------------------------------------
    // Data
    // ---------------------------------------------------------------

    /// <summary>バックログの1エントリ。</summary>
    public readonly struct BacklogEntry
    {
        public readonly string CharacterName;
        public readonly string Body;

        public BacklogEntry(string characterName, string body)
        {
            CharacterName = characterName;
            Body          = body;
        }
    }

    // ---------------------------------------------------------------
    // Component
    // ---------------------------------------------------------------

    /// <summary>
    /// バックログ（既読ログ）UIコンポーネント。
    /// ログエントリを最大100件保持し、ScrollRect 上に表示する。
    /// DOTween によるパネル開閉アニメーション付き。
    /// </summary>
    public class BacklogUI : MonoBehaviour
    {
        // ---------------------------------------------------------------
        // Inspector fields
        // ---------------------------------------------------------------

        [Header("Panel")]
        [SerializeField] private GameObject backlogPanelRoot;
        [SerializeField] private CanvasGroup backlogCanvasGroup;

        [Header("Scroll")]
        [SerializeField] private ScrollRect scrollRect;

        [Header("Entry Prefab")]
        [SerializeField] private GameObject logEntryPrefab;

        [Header("Animation")]
        [SerializeField] [Min(0f)] private float animationDuration = 0.25f;

        [Header("Limits")]
        [SerializeField] [Min(1)] private int maxEntries = 100;

        // ---------------------------------------------------------------
        // Private state
        // ---------------------------------------------------------------

        private readonly List<BacklogEntry>   _entries         = new();
        private readonly List<GameObject>     _entryObjects    = new();
        private bool _isOpen;

        // ---------------------------------------------------------------
        // Unity lifecycle
        // ---------------------------------------------------------------

        private void Awake()
        {
            // 初期は非表示・非インタラクティブ
            if (backlogPanelRoot != null)
                backlogPanelRoot.SetActive(false);

            EnsureCanvasGroup();
            SetCanvasGroupState(false, instant: true);
        }

        // ---------------------------------------------------------------
        // Public API
        // ---------------------------------------------------------------

        /// <summary>
        /// ログエントリを追加する。最大件数を超えた場合は古いものから削除。
        /// </summary>
        /// <param name="charName">キャラクター名（ナレーターなら空文字でも可）。</param>
        /// <param name="body">本文テキスト。</param>
        public void AddLog(string charName, string body)
        {
            // 上限超過時は先頭（最古）を削除
            if (_entries.Count >= maxEntries)
            {
                _entries.RemoveAt(0);

                if (_entryObjects.Count > 0)
                {
                    Destroy(_entryObjects[0]);
                    _entryObjects.RemoveAt(0);
                }
            }

            var entry = new BacklogEntry(charName ?? string.Empty, body ?? string.Empty);
            _entries.Add(entry);

            // UIが開いていれば即時追加、閉じていれば次回Open時に再構築
            if (_isOpen)
                AppendEntryObject(entry);
        }

        /// <summary>バックログパネルを開く（DOTweenアニメーション付き）。</summary>
        public void Open()
        {
            if (_isOpen) return;
            _isOpen = true;

            if (backlogPanelRoot != null)
                backlogPanelRoot.SetActive(true);

            // 開いた時点で全エントリを再構築（最新状態に同期）
            RebuildAllEntries();

            EnsureCanvasGroup();
            backlogCanvasGroup.DOKill();
            backlogCanvasGroup.alpha          = 0f;
            backlogCanvasGroup.interactable   = false;
            backlogCanvasGroup.blocksRaycasts = false;

            backlogCanvasGroup
                .DOFade(1f, animationDuration)
                .SetEase(Ease.OutQuad)
                .OnComplete(() =>
                {
                    backlogCanvasGroup.interactable   = true;
                    backlogCanvasGroup.blocksRaycasts = true;

                    // 最下部（最新エントリ）へスクロール
                    ScrollToBottom();
                });
        }

        /// <summary>バックログパネルを閉じる（DOTweenアニメーション付き）。</summary>
        public void Close()
        {
            if (!_isOpen) return;
            _isOpen = false;

            EnsureCanvasGroup();
            backlogCanvasGroup.DOKill();
            backlogCanvasGroup.interactable   = false;
            backlogCanvasGroup.blocksRaycasts = false;

            backlogCanvasGroup
                .DOFade(0f, animationDuration)
                .SetEase(Ease.InQuad)
                .OnComplete(() =>
                {
                    if (backlogPanelRoot != null)
                        backlogPanelRoot.SetActive(false);
                });
        }

        /// <summary>現在のオープン/クローズ状態を切り替える。</summary>
        public void Toggle()
        {
            if (_isOpen) Close();
            else Open();
        }

        /// <summary>全ログを消去する。</summary>
        public void ClearAll()
        {
            _entries.Clear();
            ClearEntryObjects();
        }

        /// <summary>現在保持しているエントリ件数。</summary>
        public int EntryCount => _entries.Count;

        // ---------------------------------------------------------------
        // Private helpers
        // ---------------------------------------------------------------

        private void RebuildAllEntries()
        {
            ClearEntryObjects();

            foreach (BacklogEntry entry in _entries)
                AppendEntryObject(entry);
        }

        private void AppendEntryObject(BacklogEntry entry)
        {
            if (logEntryPrefab == null || scrollRect == null) return;

            Transform container = scrollRect.content;
            if (container == null) return;

            GameObject obj = Instantiate(logEntryPrefab, container);
            obj.name = $"LogEntry_{_entryObjects.Count}";

            // キャラクター名ラベル（"NameText" タグのTMPを探す）
            TextMeshProUGUI nameLabel = FindChildTMP(obj, "NameText");
            if (nameLabel != null)
            {
                bool isNarrator = string.IsNullOrEmpty(entry.CharacterName)
                                  || entry.CharacterName.Equals("narrator",
                                      System.StringComparison.OrdinalIgnoreCase);
                nameLabel.text    = isNarrator ? string.Empty : entry.CharacterName;
                nameLabel.gameObject.SetActive(!isNarrator);
            }

            // 本文ラベル（"BodyText" タグのTMPを探す）
            TextMeshProUGUI bodyLabel = FindChildTMP(obj, "BodyText");
            if (bodyLabel != null)
                bodyLabel.text = entry.Body;

            _entryObjects.Add(obj);
        }

        private void ClearEntryObjects()
        {
            foreach (GameObject obj in _entryObjects)
                if (obj != null) Destroy(obj);

            _entryObjects.Clear();
        }

        /// <summary>子オブジェクトから指定名のTextMeshProUGUIを再帰検索する。</summary>
        private static TextMeshProUGUI FindChildTMP(GameObject root, string childName)
        {
            Transform found = root.transform.Find(childName);
            if (found != null)
                return found.GetComponent<TextMeshProUGUI>();

            // 再帰検索（深いヒエラルキーにも対応）
            foreach (Transform child in root.transform)
            {
                TextMeshProUGUI result = FindChildTMP(child.gameObject, childName);
                if (result != null) return result;
            }

            return null;
        }

        private void ScrollToBottom()
        {
            if (scrollRect == null) return;
            // Canvas の更新を反映させてからスクロール値をセット
            Canvas.ForceUpdateCanvases();
            scrollRect.verticalNormalizedPosition = 0f;
        }

        private void EnsureCanvasGroup()
        {
            if (backlogCanvasGroup != null) return;
            if (backlogPanelRoot != null)
                backlogCanvasGroup = backlogPanelRoot.GetComponent<CanvasGroup>()
                                     ?? backlogPanelRoot.AddComponent<CanvasGroup>();
        }

        private void SetCanvasGroupState(bool visible, bool instant = false)
        {
            EnsureCanvasGroup();
            if (instant)
            {
                backlogCanvasGroup.alpha          = visible ? 1f : 0f;
                backlogCanvasGroup.interactable   = visible;
                backlogCanvasGroup.blocksRaycasts = visible;
            }
        }

        private void OnDestroy()
        {
            backlogCanvasGroup?.DOKill();
            ClearEntryObjects();
        }
    }
}
