// Assets/Scripts/Audio/AudioSettings.cs
using System;

namespace KirieSaki
{
    public enum AudioChannelType { BGM, SE, Voice }

    [Serializable]
    public class AudioSettings
    {
        public float bgmVolume   = 0.8f;
        public float seVolume    = 1.0f;
        public float voiceVolume = 1.0f;

        public float GetVolume(AudioChannelType ch) => ch switch
        {
            AudioChannelType.BGM   => bgmVolume,
            AudioChannelType.SE    => seVolume,
            AudioChannelType.Voice => voiceVolume,
            _                      => 1.0f
        };

        public void SetVolume(AudioChannelType ch, float v)
        {
            v = Math.Clamp(v, 0f, 1f);
            switch (ch)
            {
                case AudioChannelType.BGM:   bgmVolume   = v; break;
                case AudioChannelType.SE:    seVolume    = v; break;
                case AudioChannelType.Voice: voiceVolume = v; break;
            }
        }
        public void ResetToDefault() { bgmVolume = 0.8f; seVolume = 1.0f; voiceVolume = 1.0f; }
    }
}
