// Assets/Scripts/Dungeon/DungeonModulePool.cs
// 仕様: specs/gamespec_Unity実装仕様書_v0.1_20260726.md 3-1。全RoomModuleSOの登録・検索。
using System.Collections.Generic;

namespace KamiNoFuruMachi
{
    public class DungeonModulePool
    {
        public void Register(RoomModuleSO module) => throw new System.NotImplementedException();

        public IEnumerable<RoomModuleSO> FindByTag(string tag) => throw new System.NotImplementedException();
    }
}
