// Assets/Scripts/Dungeon/RoomModuleSO.cs
// 仕様: specs/gamespec_Unity実装仕様書_v0.1_20260726.md 3-1。
using UnityEngine;

namespace KamiNoFuruMachi
{
    [CreateAssetMenu(menuName = "KamiNoFuruMachi/Dungeon/RoomModule")]
    public class RoomModuleSO : ScriptableObject
    {
        public string[] Tags;
        public GameObject Prefab;
        public Transform[] ConnectorSockets;
        public float CombatDensity;
        public float NarrativeWeight;
    }
}
