// Assets/Scripts/Audio/BGMController.cs
using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace KamiNoFuruMachi
{
    public class BGMController : MonoBehaviour
    {
        private AudioSource _sourceA, _sourceB;
        private AudioSource _current, _previous;
        private string _currentBgmId = string.Empty;
        private float  _masterVolume = 0.8f;
        private CancellationTokenSource _fadeCts;

        public string CurrentBgmId => _currentBgmId;
        public bool   IsPlaying    => _current != null && _current.isPlaying;

        internal void Initialize(float volume)
        {
            _masterVolume = volume;
            _sourceA = gameObject.AddComponent<AudioSource>();
            _sourceB = gameObject.AddComponent<AudioSource>();
            foreach (var s in new[] { _sourceA, _sourceB })
                { s.loop = true; s.playOnAwake = false; s.volume = 0f; }
            _current  = _sourceA;
            _previous = _sourceB;
        }

        public async UniTask CrossFade(AudioClip clip, string nextId, float duration, CancellationToken externalToken = default)
        {
            if (clip == null) return;
            if (_currentBgmId == nextId && IsPlaying) return;

            CancelFade();
            _fadeCts = CancellationTokenSource.CreateLinkedTokenSource(externalToken, this.GetCancellationTokenOnDestroy());
            var ct = _fadeCts.Token;

            try
            {
                (_current, _previous) = (_previous, _current);
                _current.clip = clip; _current.loop = true; _current.volume = 0f; _current.Play();
                _currentBgmId = nextId;

                float elapsed = 0f, prevStart = _previous.volume;
                while (elapsed < duration)
                {
                    ct.ThrowIfCancellationRequested();
                    elapsed += Time.deltaTime;
                    float t = Mathf.Clamp01(elapsed / duration);
                    _current.volume  = Mathf.Lerp(0f,        _masterVolume, t);
                    _previous.volume = Mathf.Lerp(prevStart, 0f,            t);
                    await UniTask.Yield(PlayerLoopTiming.Update, ct);
                }
                _current.volume = _masterVolume; _previous.volume = 0f;
                _previous.Stop(); _previous.clip = null;
            }
            catch (OperationCanceledException) { }
            finally { _fadeCts?.Dispose(); _fadeCts = null; }
        }

        public async UniTask FadeOut(float duration, CancellationToken externalToken = default)
        {
            if (!IsPlaying) return;
            CancelFade();
            _fadeCts = CancellationTokenSource.CreateLinkedTokenSource(externalToken, this.GetCancellationTokenOnDestroy());
            var ct = _fadeCts.Token;
            try
            {
                float elapsed = 0f, start = _current.volume;
                while (elapsed < duration)
                {
                    ct.ThrowIfCancellationRequested();
                    elapsed += Time.deltaTime;
                    _current.volume = Mathf.Lerp(start, 0f, Mathf.Clamp01(elapsed / duration));
                    await UniTask.Yield(PlayerLoopTiming.Update, ct);
                }
                _current.volume = 0f; _current.Stop(); _current.clip = null; _currentBgmId = string.Empty;
            }
            catch (OperationCanceledException) { }
            finally { _fadeCts?.Dispose(); _fadeCts = null; }
        }

        public void SetVolume(float v) { _masterVolume = Mathf.Clamp01(v); if (_fadeCts == null && IsPlaying) _current.volume = _masterVolume; }

        private void CancelFade() { _fadeCts?.Cancel(); _fadeCts?.Dispose(); _fadeCts = null; }
        private void OnDestroy()  => CancelFade();
    }
}
