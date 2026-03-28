// Assets/Scripts/UI/BacklogUI.cs
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KamiNoFuruMachi
{
    public class BacklogUI : MonoBehaviour
    {
        [SerializeField] private GameObject  _panelRoot;
        [SerializeField] private ScrollRect  _scrollRect;
        [SerializeField] private GameObject  _logEntryPrefab;
        [SerializeField] private int         _maxEntries   = 100;
        [SerializeField] private float       _animDuration = 0.2f;

        private readonly List<(string charName, string body)> _entries = new();
        private readonly List<GameObject> _entryObjects = new();
        private CanvasGroup _cg;
        private bool _isOpen;

        private void Awake()
        {
            _cg = _panelRoot.GetComponent<CanvasGroup>()
                  ?? _panelRoot.AddComponent<CanvasGroup>();
            _cg.alpha = 0f;
            _panelRoot.SetActive(false);
        }

        public void AddLog(string charName, string body)
        {
            _entries.Add((charName, body));
            if (_entries.Count > _maxEntries)
            {
                _entries.RemoveAt(0);
                if (_entryObjects.Count > 0)
                {
                    Destroy(_entryObjects[0]);
                    _entryObjects.RemoveAt(0);
                }
            }
            if (_isOpen) AppendEntryObject(charName, body);
        }

        public void Open()  => SetOpen(true).Forget();
        public void Close() => SetOpen(false).Forget();
        public void Toggle() { if (_isOpen) Close(); else Open(); }

        private async UniTaskVoid SetOpen(bool open)
        {
            _isOpen = open;
            if (open)
            {
                _panelRoot.SetActive(true);
                RebuildAllEntries();
                await _cg.DOFade(1f, _animDuration).ToUniTask();
                ScrollToBottom();
            }
            else
            {
                await _cg.DOFade(0f, _animDuration).ToUniTask();
                _panelRoot.SetActive(false);
            }
        }

        private void RebuildAllEntries()
        {
            foreach (var go in _entryObjects) if (go != null) Destroy(go);
            _entryObjects.Clear();
            foreach (var (charName, body) in _entries)
                AppendEntryObject(charName, body);
        }

        private void AppendEntryObject(string charName, string body)
        {
            if (_logEntryPrefab == null || _scrollRect == null) return;
            var go = Instantiate(_logEntryPrefab, _scrollRect.content);

            // "NameText" / "BodyText" という名前の子オブジェクトを検索してバインド
            var nameText = FindTMP(go, "NameText");
            var bodyText = FindTMP(go, "BodyText");

            bool isNarrator = string.IsNullOrEmpty(charName)
                              || charName.Equals("narrator", System.StringComparison.OrdinalIgnoreCase);
            if (nameText != null)
            {
                nameText.text = isNarrator ? "" : charName;
                nameText.gameObject.SetActive(!isNarrator);
            }
            if (bodyText != null) bodyText.text = body;

            _entryObjects.Add(go);
        }

        private static TextMeshProUGUI FindTMP(GameObject root, string targetName)
        {
            foreach (var tmp in root.GetComponentsInChildren<TextMeshProUGUI>(true))
                if (tmp.gameObject.name == targetName) return tmp;
            return null;
        }

        private void ScrollToBottom()
        {
            Canvas.ForceUpdateCanvases();
            if (_scrollRect != null) _scrollRect.verticalNormalizedPosition = 0f;
        }
    }
}
