// Assets/Scripts/Core/FlagManager.cs
using System;
using System.Collections.Generic;

namespace KamiNoFuruMachi
{
    /// <summary>
    /// ゲーム進行フラグを一元管理する Pure C# クラス。
    /// bool / int / string の 3 種類のフラグを Dictionary で保持し、
    /// 変更時に OnFlagChanged イベントを発火する。
    /// </summary>
    public class FlagManager
    {
        // -------------------------------------------------------------------------
        // Flag Key 定数
        // -------------------------------------------------------------------------
        public static class Keys
        {
            // int フラグ
            public const string KanataErosionLevel     = "FLAG_KANATA_EROSION_LEVEL";
            public const string KanataCalledLilithName = "FLAG_KANATA_CALLED_LILITH_NAME";

            // bool フラグ
            public const string LilithErosionSealed    = "FLAG_LILITH_EROSION_SEALED";
            public const string LilithUtaAudible       = "FLAG_LILITH_UTA_AUDIBLE";
            public const string LeftNainfall           = "FLAG_LEFT_NAINFALL";
            public const string EleonoraJoined         = "FLAG_ELEONORA_JOINED";
            public const string DreamSeen              = "FLAG_DREAM_SEEN";
        }

        // -------------------------------------------------------------------------
        // イベント
        // -------------------------------------------------------------------------
        /// <summary>
        /// フラグが変更されたときに発火する。引数はフラグキー文字列。
        /// </summary>
        public event Action<string> OnFlagChanged;

        // -------------------------------------------------------------------------
        // 内部ストレージ
        // -------------------------------------------------------------------------
        private readonly Dictionary<string, bool>   _boolFlags   = new();
        private readonly Dictionary<string, int>    _intFlags    = new();
        private readonly Dictionary<string, string> _stringFlags = new();

        // -------------------------------------------------------------------------
        // コンストラクタ — 仕様書の初期値を設定
        // -------------------------------------------------------------------------
        public FlagManager()
        {
            InitializeDefaults();
        }

        private void InitializeDefaults()
        {
            // --- int フラグ ---
            // 0=右腕, 1=肩, 2=胸 (現在 2)
            _intFlags[Keys.KanataErosionLevel]     = 2;
            // 奏がリリスの名前を呼んだ回数 (現在 2)
            _intFlags[Keys.KanataCalledLilithName] = 2;

            // --- bool フラグ ---
            _boolFlags[Keys.LilithErosionSealed]   = true;
            _boolFlags[Keys.LilithUtaAudible]      = false;
            _boolFlags[Keys.LeftNainfall]          = true;
            _boolFlags[Keys.EleonoraJoined]        = true;
            _boolFlags[Keys.DreamSeen]             = true;
        }

        // -------------------------------------------------------------------------
        // Bool フラグ
        // -------------------------------------------------------------------------
        public bool GetBool(string key, bool defaultValue = false)
        {
            return _boolFlags.TryGetValue(key, out bool value) ? value : defaultValue;
        }

        public void SetBool(string key, bool value)
        {
            if (_boolFlags.TryGetValue(key, out bool current) && current == value) return;
            _boolFlags[key] = value;
            OnFlagChanged?.Invoke(key);
        }

        // -------------------------------------------------------------------------
        // Int フラグ
        // -------------------------------------------------------------------------
        public int GetInt(string key, int defaultValue = 0)
        {
            return _intFlags.TryGetValue(key, out int value) ? value : defaultValue;
        }

        public void SetInt(string key, int value)
        {
            if (_intFlags.TryGetValue(key, out int current) && current == value) return;
            _intFlags[key] = value;
            OnFlagChanged?.Invoke(key);
        }

        // -------------------------------------------------------------------------
        // String フラグ
        // -------------------------------------------------------------------------
        public string GetString(string key, string defaultValue = "")
        {
            return _stringFlags.TryGetValue(key, out string value) ? value : defaultValue;
        }

        public void SetString(string key, string value)
        {
            if (_stringFlags.TryGetValue(key, out string current) && current == value) return;
            _stringFlags[key] = value;
            OnFlagChanged?.Invoke(key);
        }

        // -------------------------------------------------------------------------
        // スナップショット取得・復元 (SaveLoadManager から使用)
        // -------------------------------------------------------------------------
        public FlagSnapshot TakeSnapshot()
        {
            return new FlagSnapshot(
                new Dictionary<string, bool>(_boolFlags),
                new Dictionary<string, int>(_intFlags),
                new Dictionary<string, string>(_stringFlags)
            );
        }

        public void RestoreSnapshot(FlagSnapshot snapshot)
        {
            _boolFlags.Clear();
            _intFlags.Clear();
            _stringFlags.Clear();

            foreach (var kv in snapshot.BoolFlags)   _boolFlags[kv.Key]   = kv.Value;
            foreach (var kv in snapshot.IntFlags)    _intFlags[kv.Key]    = kv.Value;
            foreach (var kv in snapshot.StringFlags) _stringFlags[kv.Key] = kv.Value;

            // 復元後にすべてのキーで変更通知を送る
            foreach (var key in _boolFlags.Keys)   OnFlagChanged?.Invoke(key);
            foreach (var key in _intFlags.Keys)    OnFlagChanged?.Invoke(key);
            foreach (var key in _stringFlags.Keys) OnFlagChanged?.Invoke(key);
        }
    }

    // -------------------------------------------------------------------------
    // フラグスナップショット (JSON シリアライズ対象)
    // -------------------------------------------------------------------------
    [Serializable]
    public class FlagSnapshot
    {
        public Dictionary<string, bool>   BoolFlags;
        public Dictionary<string, int>    IntFlags;
        public Dictionary<string, string> StringFlags;

        public FlagSnapshot() { }

        public FlagSnapshot(
            Dictionary<string, bool>   boolFlags,
            Dictionary<string, int>    intFlags,
            Dictionary<string, string> stringFlags)
        {
            BoolFlags   = boolFlags;
            IntFlags    = intFlags;
            StringFlags = stringFlags;
        }
    }
}
