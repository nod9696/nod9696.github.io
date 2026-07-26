// Assets/Scripts/Core/ActionScoreMapper.cs
// 仕様: specs/gamespec_Unity実装仕様書_v0.1_20260726.md 3-3。PlayerActionEvent → ScoreDelta変換。
namespace KamiNoFuruMachi
{
    public struct ScoreDelta
    {
        public float SurvivalDelta;
        public float ComprehensionDelta;
    }

    public class ActionScoreMapper
    {
        public ScoreDelta Map(PlayerActionEvent evt) => throw new System.NotImplementedException();
    }
}
