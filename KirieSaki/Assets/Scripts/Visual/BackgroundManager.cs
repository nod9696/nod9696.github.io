// Assets/Scripts/Visual/BackgroundManager.cs
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace KirieSaki
{
    public class BackgroundManager : MonoBehaviour
    {
        [SerializeField] private Image _frontImage;
        [SerializeField] private Image _backImage;
        [SerializeField] private float _fadeDuration      = 0.6f;
        [SerializeField] private float _crossFadeDuration = 0.8f;

        private bool _isFront = true;

        private void Awake()
        {
            _frontImage.color = Color.white;
            _backImage.color  = new Color(1, 1, 1, 0);
        }

        public async UniTask SetBackground(string bgId, string transition)
        {
            var req = Resources.LoadAsync<Sprite>($"Sprites/Backgrounds/{bgId}");
            await req.ToUniTask();
            var sprite = req.asset as Sprite;
            if (sprite == null) { Debug.LogWarning($"[BackgroundManager] Not found: {bgId}"); return; }

            switch (transition)
            {
                case "crossfade": await DoCrossFade(sprite); break;
                case "instant":   DoInstant(sprite); break;
                default:          await DoFade(sprite); break;
            }
        }

        private async UniTask DoFade(Sprite s)
        {
            var img = Active();
            await img.DOFade(0f, _fadeDuration * 0.5f).SetEase(Ease.InQuad).ToUniTask();
            img.sprite = s; img.SetNativeSize();
            await img.DOFade(1f, _fadeDuration * 0.5f).SetEase(Ease.OutQuad).ToUniTask();
        }

        private async UniTask DoCrossFade(Sprite s)
        {
            var incoming = Inactive(); var outgoing = Active();
            incoming.sprite = s; incoming.SetNativeSize(); incoming.color = new Color(1, 1, 1, 0);
            incoming.transform.SetSiblingIndex(outgoing.transform.GetSiblingIndex() + 1);
            await UniTask.WhenAll(
                incoming.DOFade(1f, _crossFadeDuration).SetEase(Ease.OutQuad).ToUniTask(),
                outgoing.DOFade(0f, _crossFadeDuration).SetEase(Ease.InQuad).ToUniTask());
            _isFront = !_isFront;
        }

        private void DoInstant(Sprite s)
        {
            Active().sprite = s; Active().SetNativeSize(); Active().color = Color.white;
            Inactive().color = new Color(1, 1, 1, 0);
        }

        private Image Active()   => _isFront ? _frontImage : _backImage;
        private Image Inactive() => _isFront ? _backImage  : _frontImage;
    }
}
