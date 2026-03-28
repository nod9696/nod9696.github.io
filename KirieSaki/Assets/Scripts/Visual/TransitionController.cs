// Assets/Scripts/Visual/TransitionController.cs
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace KirieSaki
{
    public class TransitionController : MonoBehaviour
    {
        public enum TransitionType { Fade, Flash, CrossFade }

        [SerializeField] private Image         _fadeOverlay;
        [SerializeField] private float         _beforeDuration = 0.5f;
        [SerializeField] private float         _afterDuration  = 0.5f;
        [SerializeField] private EffectManager _effectManager;
        [SerializeField] private TransitionType _transitionType = TransitionType.Fade;

        private void Awake()
        {
            if (_fadeOverlay == null) return;
            _fadeOverlay.color = new Color(0, 0, 0, 0);
            _fadeOverlay.raycastTarget = false;
            _fadeOverlay.gameObject.SetActive(false);
        }

        public async UniTask BeforeSceneTransition()
        {
            if (_transitionType == TransitionType.Flash)
                await FadeOverlay(0f, 1f, _beforeDuration * 0.3f, Color.white);
            else
                await FadeOverlay(0f, 1f, _beforeDuration, Color.black);
        }

        public async UniTask AfterSceneTransition()
            => await FadeOverlay(1f, 0f, _afterDuration, Color.black);

        public void SetTransitionType(TransitionType t) => _transitionType = t;

        public async UniTask PlayDescentTransition()
        {
            if (_effectManager != null) await _effectManager.FlashScreen(Color.white, 0.3f);
            else await FadeOverlay(0f, 1f, 0.15f, Color.white);
            await FadeOverlay(1f, 0f, _afterDuration, Color.black);
        }

        private async UniTask FadeOverlay(float from, float to, float dur, Color c)
        {
            if (_fadeOverlay == null) return;
            _fadeOverlay.color = new Color(c.r, c.g, c.b, from);
            _fadeOverlay.raycastTarget = true;
            _fadeOverlay.gameObject.SetActive(true);
            if (dur > 0f)
                await _fadeOverlay.DOFade(to, dur).SetEase(to > from ? Ease.InQuad : Ease.OutQuad).ToUniTask();
            else
                _fadeOverlay.color = new Color(c.r, c.g, c.b, to);
            if (to <= 0f) { _fadeOverlay.gameObject.SetActive(false); _fadeOverlay.raycastTarget = false; }
        }
    }
}
