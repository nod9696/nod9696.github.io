// Assets/Scripts/Visual/CharacterData.cs
using System;
using System.Collections.Generic;
using UnityEngine;

namespace KirieSaki
{
    [Serializable]
    public class SpriteEntry
    {
        public string poseId;
        public Sprite sprite;
    }

    [CreateAssetMenu(fileName = "CharacterData", menuName = "KamiNoFuruMachi/CharacterData")]
    public class CharacterData : ScriptableObject
    {
        public string       characterId;
        public string       displayName;
        public Color        nameColor = Color.white;
        public List<SpriteEntry> sprites = new();

        public Sprite GetSprite(string poseId)
        {
            var entry = sprites.Find(e => string.Equals(e.poseId, poseId, StringComparison.OrdinalIgnoreCase));
            if (entry == null) { Debug.LogWarning($"[CharacterData] Pose not found: {poseId} in {characterId}"); return null; }
            return entry.sprite;
        }

        public bool HasPose(string poseId)
            => sprites.Exists(e => string.Equals(e.poseId, poseId, StringComparison.OrdinalIgnoreCase));

#if UNITY_EDITOR
        private void OnValidate()
        {
            var seen = new HashSet<string>();
            foreach (var e in sprites)
            {
                if (string.IsNullOrEmpty(e.poseId)) { Debug.LogWarning($"[CharacterData] Empty poseId in {characterId}"); continue; }
                if (!seen.Add(e.poseId)) Debug.LogWarning($"[CharacterData] Duplicate poseId: {e.poseId} in {characterId}");
            }
        }
#endif
    }
}
