// Assets/Scripts/Core/WorldState.cs
// 仕様: specs/gamespec_Unity実装仕様書_v0.1_20260726.md 3-3。永続データ（POCO、Unity非依存）。
using System;

namespace KamiNoFuruMachi
{
    public enum DominantTag
    {
        // タグの種類・命名は未確定。設計確定後にここへ追加する。
        Undetermined
    }

    [Serializable]
    public class WorldState
    {
        public int SchemaVersion = 1;
        public float SurvivalScore;
        public float ComprehensionScore;
        public DominantTag DominantTag;
    }
}
