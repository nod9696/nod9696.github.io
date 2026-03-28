// Assets/Scripts/Core/SaveLoadManager.cs
using System;
using UnityEngine;

namespace KamiNoFuruMachi
{
    [Serializable]
    public class SaveData
    {
        public string      chapterId;
        public int         chapterNumber;
        public string      saveTime;
        public FlagSnapshot flags;
    }

    public class SaveLoadManager : MonoBehaviour
    {
        private FlagManager _flagManager;

        public void Initialize(FlagManager flagManager) => _flagManager = flagManager;

        // 任意セーブ（0〜4スロット）
        public void Save(int slotIndex, string chapterId, int chapterNumber)
        {
            if (slotIndex < 0 || slotIndex >= GameConstants.SaveKeys.SlotCount) return;
            var data = CreateSaveData(chapterId, chapterNumber);
            var json = JsonUtility.ToJson(data);
            PlayerPrefs.SetString(GameConstants.SaveKeys.SlotPrefix + slotIndex, json);
            PlayerPrefs.Save();
            Debug.Log($"[SaveLoadManager] Saved to slot {slotIndex}");
        }

        public SaveData Load(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= GameConstants.SaveKeys.SlotCount) return null;
            var json = PlayerPrefs.GetString(GameConstants.SaveKeys.SlotPrefix + slotIndex, null);
            if (string.IsNullOrEmpty(json)) return null;
            return JsonUtility.FromJson<SaveData>(json);
        }

        // オートセーブ
        public void AutoSave(int chapterNumber)
        {
            var data = CreateSaveData($"chapter{chapterNumber:D2}", chapterNumber);
            PlayerPrefs.SetString(GameConstants.SaveKeys.AutoSaveKey, JsonUtility.ToJson(data));
            PlayerPrefs.Save();
            Debug.Log($"[SaveLoadManager] AutoSaved chapter {chapterNumber}");
        }

        public SaveData LoadAutoSave()
        {
            var json = PlayerPrefs.GetString(GameConstants.SaveKeys.AutoSaveKey, null);
            return string.IsNullOrEmpty(json) ? null : JsonUtility.FromJson<SaveData>(json);
        }

        public bool HasSaveData(int slotIndex)
            => PlayerPrefs.HasKey(GameConstants.SaveKeys.SlotPrefix + slotIndex);

        public void ApplySaveData(SaveData data)
        {
            if (data?.flags == null) return;
            _flagManager?.RestoreSnapshot(data.flags);
        }

        private SaveData CreateSaveData(string chapterId, int chapterNumber) => new SaveData
        {
            chapterId     = chapterId,
            chapterNumber = chapterNumber,
            saveTime      = DateTime.Now.ToString("yyyy/MM/dd HH:mm"),
            flags         = _flagManager?.TakeSnapshot()
        };
    }
}
