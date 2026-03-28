// Assets/Scripts/Visual/EffectManager.cs
using System.Collections;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace KirieSaki
{
    public class EffectManager : MonoBehaviour
    {
        [SerializeField] private Camera   _mainCamera;
        [SerializeField] private Image    _overlayImage;
        [SerializeField] private RawImage _glitchRawImage;
        [SerializeField] private int   _shakeVibrato    = 20;
        [SerializeField] private float _shakeRandomness = 90f;
        [SerializeField] private float _glitchInterval  = 0.05f;
        [SerializeField] private float _glitchMaxOffset = 0.04f;

        private Vector3  _cameraOriginalPos;
        private Coroutine _glitchCoroutine;

        private void Awake()
        {
            if (_mainCamera == null) _mainCamera = Camera.main;
            _cameraOriginalPos = _mainCamera != null ? _mainCamera.transform.localPosition : Vector3.zero;
            if (_overlayImage    != null) _overlayImage.color = new Color(0, 0, 0, 0);
            if (_glitchRawImage  != null) _glitchRawImage.gameObject.SetActive(false);
        }

        public async UniTask ShakeScreen(float intensity, float duration)
        {
            if (_mainCamera == null) return;
            await _mainCamera.transform.DOShakePosition(duration, intensity, _shakeVibrato, _shakeRandomness).ToUniTask();
            _mainCamera.transform.localPosition = _cameraOriginalPos;
        }

        public async UniTask FlashScreen(Color color, float duration)
        {
            if (_overlayImage == null) return;
            float half = duration * 0.5f;
            _overlayImage.color = new Color(color.r, color.g, color.b, 0);
            _overlayImage.gameObject.SetActive(true);
            _overlayImage.raycastTarget = false;
            await _overlayImage.DOFade(1f, half).SetEase(Ease.OutQuad).ToUniTask();
            await _overlayImage.DOFade(0f, half).SetEase(Ease.InQuad).ToUniTask();
            _overlayImage.gameObject.SetActive(false);
        }

        public async UniTask GlitchEffect(float duration)
        {
            if (_glitchRawImage == null) return;
            _glitchRawImage.gameObject.SetActive(true);
            _glitchCoroutine = StartCoroutine(GlitchCoroutine());
            await UniTask.Delay(System.TimeSpan.FromSeconds(duration));
            if (_glitchCoroutine != null) { StopCoroutine(_glitchCoroutine); _glitchCoroutine = null; }
            _glitchRawImage.uvRect = new Rect(0, 0, 1, 1);
            _glitchRawImage.gameObject.SetActive(false);
        }

        public async UniTask FadeToBlack(float duration)   => await FadeOverlay(Color.black, 0f, 1f, duration);
        public async UniTask FadeFromBlack(float duration)  => await FadeOverlay(Color.black, 1f, 0f, duration);

        private async UniTask FadeOverlay(Color c, float from, float to, float dur)
        {
            if (_overlayImage == null) return;
            _overlayImage.color = new Color(c.r, c.g, c.b, from);
            _overlayImage.raycastTarget = to >= 1f;
            _overlayImage.gameObject.SetActive(true);
            await _overlayImage.DOFade(to, dur).SetEase(to > from ? Ease.InQuad : Ease.OutQuad).ToUniTask();
            if (to <= 0f) { _overlayImage.gameObject.SetActive(false); _overlayImage.raycastTarget = false; }
        }

        private IEnumerator GlitchCoroutine()
        {
            while (true)
            {
                _glitchRawImage.uvRect = new Rect(
                    Random.Range(-_glitchMaxOffset, _glitchMaxOffset),
                    Random.Range(-_glitchMaxOffset, _glitchMaxOffset), 1f, 1f);
                yield return new WaitForSeconds(_glitchInterval);
            }
        }

        public async UniTask PlayDescentEffect(float intensity = 0.8f, float duration = 3.0f)
            => await UniTask.WhenAll(ShakeScreen(intensity, duration), FlashScreen(Color.white, duration * 0.4f), GlitchEffect(duration));
    }
}
