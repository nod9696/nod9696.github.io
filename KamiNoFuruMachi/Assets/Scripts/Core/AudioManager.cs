// Assets/Scripts/Core/AudioManager.cs
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace KamiNoFuruMachi
{
    /// <summary>
    /// BGM / SE の再生・停止・フェードを管理する MonoBehaviour。
    /// GameManager から初期化される。
    /// AudioClip のロードは AddressableAssets 等の外部システムへの差し替えを想定し、
    /// 現時点では Resources.Load によるプレースホルダ実装とする。
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        // -------------------------------------------------------------------------
        // AudioSource 参照
        // -------------------------------------------------------------------------
        private AudioSource _bgmSource;
        private AudioSource _seSource;

        // -------------------------------------------------------------------------
        // 初期化
        // -------------------------------------------------------------------------
        public void Initialize()
        {
            _bgmSource             = gameObject.AddComponent<AudioSource>();
            _bgmSource.loop        = true;
            _bgmSource.playOnAwake = false;

            _seSource              = gameObject.AddComponent<AudioSource>();
            _seSource.loop         = false;
            _seSource.playOnAwake  = false;

            Debug.Log("[AudioManager] 初期化完了");
        }

        // =========================================================================
        // BGM
        // =========================================================================

        /// <summary>
        /// 指定 BGM ID を再生する。既に再生中の場合はクロスフェードする。
        /// </summary>
        public async UniTask PlayBGMAsync(string bgmId, float fadeDuration = 0.5f)
        {
            AudioClip clip = LoadClip("BGM/" + bgmId);
            if (clip == null)
            {
                Debug.LogWarning($"[AudioManager] BGM クリップが見つかりません: {bgmId}");
                return;
            }

            if (_bgmSource.isPlaying)
            {
                await FadeOutBGMAsync(fadeDuration);
            }

            _bgmSource.clip = clip;
            _bgmSource.volume = 0f;
            _bgmSource.Play();
            await _bgmSource.DOFade(1f, fadeDuration).ToUniTask();
        }

        /// <summary>
        /// BGM をフェードアウトして停止する。
        /// </summary>
        public async UniTask FadeOutBGMAsync(float duration = 0.5f)
        {
            if (!_bgmSource.isPlaying) return;

            await _bgmSource.DOFade(0f, duration).ToUniTask();
            _bgmSource.Stop();
            _bgmSource.clip = null;
        }

        /// <summary>
        /// BGM を即停止する。
        /// </summary>
        public void StopBGM()
        {
            _bgmSource.Stop();
            _bgmSource.clip = null;
        }

        // =========================================================================
        // SE
        // =========================================================================

        /// <summary>
        /// 指定 SE ID を一発再生する。
        /// </summary>
        public void PlaySE(string seId)
        {
            AudioClip clip = LoadClip("SE/" + seId);
            if (clip == null)
            {
                Debug.LogWarning($"[AudioManager] SE クリップが見つかりません: {seId}");
                return;
            }

            _seSource.PlayOneShot(clip);
        }

        // =========================================================================
        // 音量設定
        // =========================================================================

        public void SetBGMVolume(float volume)
        {
            _bgmSource.volume = Mathf.Clamp01(volume);
        }

        public void SetSEVolume(float volume)
        {
            _seSource.volume = Mathf.Clamp01(volume);
        }

        // =========================================================================
        // 内部ユーティリティ
        // =========================================================================
        private static AudioClip LoadClip(string resourcePath)
        {
            // TODO: Addressables 等に差し替える場合はここを変更する
            return Resources.Load<AudioClip>(resourcePath);
        }
    }
}
