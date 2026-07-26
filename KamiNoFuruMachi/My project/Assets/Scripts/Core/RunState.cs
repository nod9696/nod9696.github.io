// Assets/Scripts/Core/RunState.cs
// 仕様: specs/gamespec_Unity実装仕様書_v0.1_20260726.md 3-3。Run単位一時データ。
using System.Collections.Generic;

namespace KamiNoFuruMachi
{
    public class RunState
    {
        public DungeonLayout CurrentDungeon;
        public List<PlayerActionEvent> ActionLog = new();
    }
}
