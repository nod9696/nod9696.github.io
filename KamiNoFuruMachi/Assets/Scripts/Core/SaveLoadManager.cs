// Assets/Scripts/Core/SaveLoadManager.cs
using System;
using UnityEngine;

namespace KamiNoFuruMachi
{
    // =========================================================================
    // SaveData — FlagManager の全フラグを JSON シリアライズするデータクラス
    // =========================================================================
    [Serializable]
    public class SaveData
    {
        // メタ情報
        public int    SlotIndex;
        public string SavedAt;          // ISO 8601 形式
        public int    ChapterNumber;
        public bool   IsAutoSave;

        // フラグスナップショット
        public FlagSnapshot FlagSnapshot;
    }

    // =========================================================================
    // SaveLoadManager
    // =========================================================================
    /// <summary>
    /// PlayerPrefs + JSON によるセーブ・ロードを管理する MonoBehaviour。
    /// 任意セーブ 5 スロット + 章オートセーブ 1 スロットをサポートする。
    /// GameManager から初期化される。
    /// </summary>
    public class SaveLoadManager : MonoBehaviour
    {
        // -------------------------------------------------------------------------
        // 参照
        // -------------------------------------------------------------------------
        private FlagManager _flagManager;

        // -------------------------------------------------------------------------
        // 初期化
        // -------------------------------------------------------------------------
        /// <summary>
        /// GameManager から呼び出す初期化メソッド。
        /// </summary>
        public void Initialize(FlagManager flagManager)
        {
            _flagManager = flagManager;
        }

        // =========================================================================
        // 任意セーブ (スロット 0 〜 SlotCount-1)
        // =========================================================================

        /// <summary>
        /// 指定スロットに現在のゲーム状態をセーブする。
        /// </summary>
        /// <param name="slotIndex">0 〜 SlotCount-1</param>
        /// <param name="chapterNumber">現在の章番号</param>
        public void Save(int slotIndex, int chapterNumber = 0)
        {
            if (!IsValidSlot(slotIndex))
            {
                Debug.LogWarning($"[SaveLoadManager] 無効なスロット番号: {slotIndex}");
                return;
            }

            SaveData data = BuildSaveData(slotIndex, chapterNumber, isAutoSave: false);
            WriteToPrefs(GameConstants.SaveKeys.SlotPrefix + slotIndex, data);
            Debug.Log($"[SaveLoadManager] スロット {slotIndex} にセーブしました (章 {chapterNumber})");
        }

        /// <summary>
        /// 指定スロットからゲーム状態をロードする。
        /// </summary>
        /// <returns>ロードに成功した場合 true</returns>
        public bool Load(int slotIndex)
        {
            if (!IsValidSlot(slotIndex))
            {
                Debug.LogWarning($"[SaveLoadManager] 無効なスロット番号: {slotIndex}");
                return false;
            }

            SaveData data = ReadFromPrefs(GameConstants.SaveKeys.SlotPrefix + slotIndex);
            if (data == null)
            {
                Debug.LogWarning($"[SaveLoadManager] スロット {slotIndex} にセーブデータが存在しません");
                return false;
            }

            ApplySaveData(data);
            Debug.Log($"[SaveLoadManager] スロット {slotIndex} からロードしました (章 {data.ChapterNumber})");
            return true;
        }

        /// <summary>
        /// 指定スロットのセーブデータを削除する。
        /// </summary>
        public void DeleteSave(int slotIndex)
        {
            if (!IsValidSlot(slotIndex))
            {
                Debug.LogWarning($"[SaveLoadManager] 無効なスロット番号: {slotIndex}");
                return;
            }

            string key = GameConstants.SaveKeys.SlotPrefix + slotIndex;
            if (PlayerPrefs.HasKey(key))
            {
                PlayerPrefs.DeleteKey(key);
                PlayerPrefs.Save();
                Debug.Log($"[SaveLoadManager] スロット {slotIndex} のセーブデータを削除しました");
            }
        }

        /// <summary>
        /// 指定スロットにセーブデータが存在するか確認する。
        /// </summary>
        public bool HasSaveData(int slotIndex)
        {
            if (!IsValidSlot(slotIndex)) return false;
            return PlayerPrefs.HasKey(GameConstants.SaveKeys.SlotPrefix + slotIndex);
        }

        /// <summary>
        /// 指定スロットのセーブデータを読み取り専用で取得する（ロードは行わない）。
        /// UI 表示用途などに使用する。
        /// </summary>
        public SaveData PeekSaveData(int slotIndex)
        {
            if (!IsValidSlot(slotIndex)) return null;
            return ReadFromPrefs(GameConstants.SaveKeys.SlotPrefix + slotIndex);
        }

        // =========================================================================
        // 章オートセーブ
        // =========================================================================

        /// <summary>
        /// 章が切り替わるタイミングでオートセーブを実行する。
        /// </summary>
        /// <param name="chapterNumber">新しい章番号</param>
        public void AutoSave(int chapterNumber)
        {
            SaveData data = BuildSaveData(slotIndex: -1, chapterNumber, isAutoSave: true);
            WriteToPrefs(GameConstants.SaveKeys.AutoSaveKey, data);
            Debug.Log($"[SaveLoadManager] 章 {chapterNumber} のオートセーブを実行しました");
        }

        /// <summary>
        /// オートセーブからゲーム状態をロードする。
        /// </summary>
        public bool LoadAutoSave()
        {
            SaveData data = ReadFromPrefs(GameConstants.SaveKeys.AutoSaveKey);
            if (data == null)
            {
                Debug.LogWarning("[SaveLoadManager] オートセーブデータが存在しません");
                return false;
            }

            ApplySaveData(data);
            Debug.Log($"[SaveLoadManager] オートセーブ (章 {data.ChapterNumber}) からロードしました");
            return true;
        }

        /// <summary>
        /// オートセーブデータが存在するか確認する。
        /// </summary>
        public bool HasAutoSaveData()
        {
            return PlayerPrefs.HasKey(GameConstants.SaveKeys.AutoSaveKey);
        }

        // =========================================================================
        // 内部ユーティリティ
        // =========================================================================

        private SaveData BuildSaveData(int slotIndex, int chapterNumber, bool isAutoSave)
        {
            return new SaveData
            {
                SlotIndex     = slotIndex,
                SavedAt       = DateTime.UtcNow.ToString("o"),
                ChapterNumber = chapterNumber,
                IsAutoSave    = isAutoSave,
                FlagSnapshot  = _flagManager.TakeSnapshot(),
            };
        }

        private void ApplySaveData(SaveData data)
        {
            if (data.FlagSnapshot != null)
            {
                _flagManager.RestoreSnapshot(data.FlagSnapshot);
            }
        }

        private static void WriteToPrefs(string key, SaveData data)
        {
            string json = JsonUtility.ToJson(data, prettyPrint: false);
            PlayerPrefs.SetString(key, json);
            PlayerPrefs.Save();
        }

        private static SaveData ReadFromPrefs(string key)
        {
            if (!PlayerPrefs.HasKey(key)) return null;

            string json = PlayerPrefs.GetString(key);
            if (string.IsNullOrEmpty(json)) return null;

            try
            {
                return JsonUtility.FromJson<SaveData>(json);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveLoadManager] JSON のデシリアライズに失敗しました (key={key}): {ex.Message}");
                return null;
            }
        }

        private static bool IsValidSlot(int slotIndex)
        {
            return slotIndex >= 0 && slotIndex < GameConstants.SaveKeys.SlotCount;
        }
    }
}
