// Assets/Scripts/Dialogue/ScenarioLoader.cs
using System;
using System.Collections.Generic;
using System.IO;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace KamiNoFuruMachi
{
    public class ScenarioLoader : MonoBehaviour
    {
        private const string SubDir    = "Scenarios";
        private const string Extension = ".json";

        private readonly Dictionary<string, ScenarioData> _cache = new();

        public async UniTask<ScenarioData> LoadAsync(string chapterId)
        {
            if (string.IsNullOrEmpty(chapterId))
            {
                Debug.LogError("[ScenarioLoader] chapterId is empty.");
                return null;
            }

            if (_cache.TryGetValue(chapterId, out var cached))
                return cached;

            var filePath = Path.Combine(Application.streamingAssetsPath, SubDir, chapterId + Extension);
            var uri      = filePath.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                           ? filePath : "file://" + filePath;

            using var req = UnityWebRequest.Get(uri);
            try { await req.SendWebRequest().ToUniTask(); }
            catch (Exception ex)
            {
                Debug.LogError($"[ScenarioLoader] Load failed ({chapterId}): {ex.Message}");
                return null;
            }

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[ScenarioLoader] {req.error} ({filePath})");
                return null;
            }

            ScenarioData data;
            try { data = JsonUtility.FromJson<ScenarioData>(req.downloadHandler.text); }
            catch (Exception ex)
            {
                Debug.LogError($"[ScenarioLoader] JSON parse failed ({chapterId}): {ex.Message}");
                return null;
            }

            if (data == null) return null;
            if (string.IsNullOrEmpty(data.chapterId)) data.chapterId = chapterId;

            _cache[chapterId] = data;
            Debug.Log($"[ScenarioLoader] Loaded: {chapterId} ({data.CommandCount} commands)");
            return data;
        }

        public void InvalidateCache(string chapterId) => _cache.Remove(chapterId);
        public void ClearCache() => _cache.Clear();
        public bool IsCached(string chapterId) => _cache.ContainsKey(chapterId);
    }
}
