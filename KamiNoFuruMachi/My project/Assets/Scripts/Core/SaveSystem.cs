// Assets/Scripts/Core/SaveSystem.cs
// 仕様: specs/gamespec_Unity実装仕様書_v0.1_20260726.md 3-3。WorldStateのJSON直列化・persistentDataPath保存。
// 既存の SaveLoadManager.cs（フラグ/Ink進行用のセーブ）とは対象データが異なる想定。責務の切り分けは要確認。
namespace KamiNoFuruMachi
{
    public class SaveSystem
    {
        public void Save(WorldState state) => throw new System.NotImplementedException();

        public WorldState Load() => throw new System.NotImplementedException();
    }
}
