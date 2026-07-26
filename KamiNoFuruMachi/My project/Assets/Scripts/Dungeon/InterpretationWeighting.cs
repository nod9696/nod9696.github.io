// Assets/Scripts/Dungeon/InterpretationWeighting.cs
// 仕様: specs/gamespec_Unity実装仕様書_v0.1_20260726.md 3-1。WorldStateスコア→抽選重みテーブル生成。
using System.Collections.Generic;

namespace KamiNoFuruMachi
{
    public class InterpretationWeighting
    {
        public Dictionary<RoomModuleSO, float> BuildWeightTable(WorldState state, IEnumerable<RoomModuleSO> candidates)
            => throw new System.NotImplementedException();
    }
}
