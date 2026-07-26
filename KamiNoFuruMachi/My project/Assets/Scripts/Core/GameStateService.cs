// Assets/Scripts/Core/GameStateService.cs
// 仕様: specs/gamespec_Unity実装仕様書_v0.1_20260726.md 3-3。シーン跨ぎで状態保持する非MonoBehaviourサービス。
namespace KamiNoFuruMachi
{
    public class GameStateService
    {
        public WorldState WorldState { get; private set; }
        public RunState CurrentRun { get; private set; }

        public GameStateService(WorldState worldState) => WorldState = worldState;

        public void BeginRun() => throw new System.NotImplementedException();

        public void EndRun() => throw new System.NotImplementedException();
    }
}
