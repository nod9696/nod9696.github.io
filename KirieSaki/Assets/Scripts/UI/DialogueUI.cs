// Assets/Scripts/UI/DialogueUI.cs
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace KirieSaki
{
    public class DialogueUI : MonoBehaviour, IPointerClickHandler, ITextPresenter
    {
        [SerializeField] private GameObject      _windowRoot;
        [SerializeField] private TextMeshProUGUI _nameText;
        [SerializeField] private GameObject      _namePanelRoot;
        [SerializeField] private TextMeshProUGUI _bodyText;
        [SerializeField] private GameObject      _nextIndicator;
        [SerializeField] private float           _charactersPerSecond = 30f;

        private CancellationTokenSource          _typewriterCts;
        private UniTaskCompletionSource<bool>    _advanceTcs;
        private bool                             _isTyping;

        private void Awake()
        {
            if (_nextIndicator != null) _nextIndicator.SetActive(false);
        }

        public async UniTask ShowTextAsync(string charName, string body, bool isRead, CancellationToken ct)
        {
            SetCharacterName(charName);
            _bodyText.text = body;
            _bodyText.maxVisibleCharacters = 0;
            _windowRoot.SetActive(true);
            if (_nextIndicator != null) _nextIndicator.SetActive(false);

            if (isRead)
            {
                _bodyText.maxVisibleCharacters = body.Length;
                return;
            }

            _typewriterCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            _isTyping = true;

            try
            {
                float interval = 1f / System.Math.Max(1f, _charactersPerSecond);
                for (int i = 0; i <= body.Length; i++)
                {
                    _typewriterCts.Token.ThrowIfCancellationRequested();
                    _bodyText.maxVisibleCharacters = i;
                    await UniTask.Delay(System.TimeSpan.FromSeconds(interval),
                        cancellationToken: _typewriterCts.Token);
                }
            }
            catch (System.OperationCanceledException)
            {
                _bodyText.maxVisibleCharacters = body.Length;
            }
            finally
            {
                _isTyping = false;
                _typewriterCts?.Dispose();
                _typewriterCts = null;
            }

            if (_nextIndicator != null) _nextIndicator.SetActive(true);
        }

        public async UniTask WaitForAdvanceAsync(CancellationToken ct)
        {
            _advanceTcs = new UniTaskCompletionSource<bool>();
            using var reg = ct.Register(() => _advanceTcs.TrySetCanceled());
            await _advanceTcs.Task;
            if (_nextIndicator != null) _nextIndicator.SetActive(false);
        }

        public void HideWindow()
        {
            if (_windowRoot != null) _windowRoot.SetActive(false);
        }

        public void OnPointerClick(PointerEventData _) => Advance();

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Z))
                Advance();
        }

        private void Advance()
        {
            if (_isTyping) _typewriterCts?.Cancel();
            else           _advanceTcs?.TrySetResult(true);
        }

        private void SetCharacterName(string charName)
        {
            bool isNarrator = string.IsNullOrEmpty(charName)
                              || charName.Equals("narrator", System.StringComparison.OrdinalIgnoreCase);
            if (_namePanelRoot != null) _namePanelRoot.SetActive(!isNarrator);
            if (_nameText != null && !isNarrator) _nameText.text = ResolveDisplayName(charName);
        }

        // キリエ/サキ キャラクター名マッピング
        private static string ResolveDisplayName(string charId) => charId switch
        {
            "kirie"    => "逆真キリエ",
            "saki"     => "入江サキ",
            "suzumura" => "鈴村",
            _          => charId
        };

        public void SetCharactersPerSecond(float cps) => _charactersPerSecond = Mathf.Max(1f, cps);
    }
}
