// Assets/Scripts/Audio/AudioManager.cs
using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace KamiNoFuruMachi
{
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        private BGMController _bgm;
        private SEPlayer      _se;
        private AudioSource   _voice;
        private AudioSettings _settings;
        private CancellationTokenSource _voiceCts;

        private const string PathBGM   = "Audio/BGM/";
        private const string PathSE    = "Audio/SE/";
        private const string PathVoice = "Audio/Voice/";

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitComponents();
        }

        private void InitComponents()
        {
            _settings = new AudioSettings();

            var bgmGo = new GameObject("BGMController"); bgmGo.transform.SetParent(transform);
            _bgm = bgmGo.AddComponent<BGMController>(); _bgm.Initialize(_settings.bgmVolume);

            var seGo = new GameObject("SEPlayer"); seGo.transform.SetParent(transform);
            _se = seGo.AddComponent<SEPlayer>(); _se.Initialize(_settings.seVolume);

            _voice = gameObject.AddComponent<AudioSource>();
            _voice.loop = false; _voice.playOnAwake = false; _voice.volume = _settings.voiceVolume;
        }

        // BGM
        public async UniTask PlayBGM(string id, float fadeDuration = 1f)
        {
            var clip = await LoadClipAsync(PathBGM + id);
            if (clip != null) await _bgm.CrossFade(clip, id, fadeDuration, this.GetCancellationTokenOnDestroy());
        }
        public async UniTask StopBGM(float fadeDuration = 1f) => await _bgm.FadeOut(fadeDuration, this.GetCancellationTokenOnDestroy());

        // SE
        public void PlaySE(string id) => PlaySEAsync(id).Forget();
        private async UniTaskVoid PlaySEAsync(string id)
        {
            var clip = await LoadClipAsync(PathSE + id);
            if (clip != null) _se.Play(clip, _settings.seVolume);
        }

        // Voice
        public async UniTask PlayVoice(string id)
        {
            _voiceCts?.Cancel(); _voiceCts?.Dispose();
            _voiceCts = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());
            var ct = _voiceCts.Token;
            var clip = await LoadClipAsync(PathVoice + id);
            if (clip == null) return;
            try
            {
                ct.ThrowIfCancellationRequested();
                _voice.Stop(); _voice.clip = clip; _voice.volume = _settings.voiceVolume; _voice.Play();
                await UniTask.WaitWhile(() => _voice.isPlaying, PlayerLoopTiming.Update, ct);
            }
            catch (OperationCanceledException) { _voice.Stop(); }
        }

        // Volume
        public void SetVolume(AudioChannelType ch, float v)
        {
            _settings.SetVolume(ch, v);
            switch (ch)
            {
                case AudioChannelType.BGM:   _bgm.SetVolume(_settings.bgmVolume); break;
                case AudioChannelType.SE:    _se.SetVolume(_settings.seVolume); break;
                case AudioChannelType.Voice: _voice.volume = _settings.voiceVolume; break;
            }
        }
        public float GetVolume(AudioChannelType ch) => _settings.GetVolume(ch);
        public AudioSettings GetSettings() => _settings;
        public void ApplySettings(AudioSettings s)
        {
            if (s == null) return; _settings = s;
            _bgm.SetVolume(s.bgmVolume); _se.SetVolume(s.seVolume); _voice.volume = s.voiceVolume;
        }

        private async UniTask<AudioClip> LoadClipAsync(string path)
        {
            var req = Resources.LoadAsync<AudioClip>(path);
            await req.ToUniTask(cancellationToken: this.GetCancellationTokenOnDestroy());
            if (req.asset == null) { Debug.LogError($"[AudioManager] Not found: Resources/{path}"); return null; }
            return req.asset as AudioClip;
        }

        private void OnDestroy() { _voiceCts?.Cancel(); _voiceCts?.Dispose(); if (Instance == this) Instance = null; }
    }
}
