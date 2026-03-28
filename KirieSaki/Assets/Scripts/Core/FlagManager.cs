// Assets/Scripts/Core/FlagManager.cs
using System;
using System.Collections.Generic;

namespace KirieSaki
{
    public class FlagManager : IFlagManager
    {
        public static class Keys
        {
            // キリエ/サキ 作品固有フラグ
            public const string KirieStayed          = "FLAG_KIRIE_STAYED";
            public const string SugarExperiment      = "FLAG_SUGAR_EXPERIMENT";   // 砂糖実験の到達杯数（int）
            public const string SketchbookFound      = "FLAG_SKETCHBOOK_FOUND";
            public const string SakiCalledSuzumura   = "FLAG_SAKI_CALLED_SUZUMURA";
            public const string HakushokuDefeated    = "FLAG_HAKUSHOKU_DEFEATED";
            public const string KirieSaidStay        = "FLAG_KIRIE_SAID_STAY";
            public const string EndingNormal         = "FLAG_ENDING_NORMAL";
        }

        public event Action<string> OnFlagChanged;

        private readonly Dictionary<string, bool>   _boolFlags   = new();
        private readonly Dictionary<string, int>    _intFlags    = new();
        private readonly Dictionary<string, string> _stringFlags = new();

        public FlagManager() => InitializeDefaults();

        private void InitializeDefaults()
        {
            _boolFlags[Keys.KirieStayed]        = false;
            _intFlags[Keys.SugarExperiment]     = 0;
            _boolFlags[Keys.SketchbookFound]    = false;
            _boolFlags[Keys.SakiCalledSuzumura] = false;
            _boolFlags[Keys.HakushokuDefeated]  = false;
            _boolFlags[Keys.KirieSaidStay]      = false;
            _boolFlags[Keys.EndingNormal]       = false;
        }

        public bool GetBoolFlag(string key) => _boolFlags.TryGetValue(key, out var v) ? v : false;
        public void SetBoolFlag(string key, bool value)
        {
            var old = GetBoolFlag(key);
            _boolFlags[key] = value;
            if (old != value) OnFlagChanged?.Invoke(key);
        }

        public int GetFlag(string key) => _intFlags.TryGetValue(key, out var v) ? v : 0;
        public void SetFlag(string key, int value)
        {
            var old = GetFlag(key);
            _intFlags[key] = value;
            if (old != value) OnFlagChanged?.Invoke(key);
        }

        public string GetStringFlag(string key) => _stringFlags.TryGetValue(key, out var v) ? v : string.Empty;
        public void SetStringFlag(string key, string value)
        {
            var old = GetStringFlag(key);
            _stringFlags[key] = value;
            if (old != value) OnFlagChanged?.Invoke(key);
        }

        public FlagSnapshot TakeSnapshot() => new FlagSnapshot(_boolFlags, _intFlags, _stringFlags);
        public void RestoreSnapshot(FlagSnapshot snap) { snap.ApplyTo(_boolFlags, _intFlags, _stringFlags); }
    }

    [Serializable]
    public class FlagSnapshot
    {
        public List<string> boolKeys   = new(); public List<bool>   boolValues   = new();
        public List<string> intKeys    = new(); public List<int>    intValues    = new();
        public List<string> stringKeys = new(); public List<string> stringValues = new();

        public FlagSnapshot() { }
        public FlagSnapshot(Dictionary<string, bool> b, Dictionary<string, int> i, Dictionary<string, string> s)
        {
            foreach (var kv in b) { boolKeys.Add(kv.Key);   boolValues.Add(kv.Value); }
            foreach (var kv in i) { intKeys.Add(kv.Key);    intValues.Add(kv.Value); }
            foreach (var kv in s) { stringKeys.Add(kv.Key); stringValues.Add(kv.Value); }
        }
        public void ApplyTo(Dictionary<string, bool> b, Dictionary<string, int> i, Dictionary<string, string> s)
        {
            b.Clear(); for (int x = 0; x < boolKeys.Count; x++) b[boolKeys[x]] = boolValues[x];
            i.Clear(); for (int x = 0; x < intKeys.Count; x++)  i[intKeys[x]]  = intValues[x];
            s.Clear(); for (int x = 0; x < stringKeys.Count; x++) s[stringKeys[x]] = stringValues[x];
        }
    }
}
