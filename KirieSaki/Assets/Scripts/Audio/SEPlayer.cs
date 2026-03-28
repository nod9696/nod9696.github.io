// Assets/Scripts/Audio/SEPlayer.cs
using UnityEngine;

namespace KirieSaki
{
    public class SEPlayer : MonoBehaviour
    {
        private const int ChannelCount = 4;
        private AudioSource[] _sources;
        private int _nextChannel;

        internal void Initialize(float volume)
        {
            _sources = new AudioSource[ChannelCount];
            for (int i = 0; i < ChannelCount; i++)
            {
                _sources[i]             = gameObject.AddComponent<AudioSource>();
                _sources[i].volume      = volume;
                _sources[i].loop        = false;
                _sources[i].playOnAwake = false;
            }
        }

        public void Play(AudioClip clip, float volume)
        {
            if (clip == null) return;
            var src    = GetNextChannel();
            src.volume = volume;
            src.clip   = clip;
            src.Play();
        }

        public void SetVolume(float v)
        {
            v = Mathf.Clamp01(v);
            foreach (var s in _sources) s.volume = v;
        }

        public void StopAll() { foreach (var s in _sources) s.Stop(); }

        private AudioSource GetNextChannel()
        {
            for (int i = 0; i < ChannelCount; i++)
            {
                int idx = (_nextChannel + i) % ChannelCount;
                if (!_sources[idx].isPlaying) { _nextChannel = (idx + 1) % ChannelCount; return _sources[idx]; }
            }
            var result = _sources[_nextChannel];
            _nextChannel = (_nextChannel + 1) % ChannelCount;
            return result;
        }
    }
}
