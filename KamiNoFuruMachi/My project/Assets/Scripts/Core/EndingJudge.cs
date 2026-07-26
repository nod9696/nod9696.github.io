// Assets/Scripts/Core/EndingJudge.cs
// 仕様: specs/gamespec_Unity実装仕様書_v0.1_20260726.md 3-3。閾値・ヒステリシス判定。Unity非依存、単体テスト対象。
namespace KamiNoFuruMachi
{
    public enum EndingType
    {
        // エンディング種別・閾値は未確定。設計確定後にここへ追加する。
        Undetermined
    }

    public class EndingJudge
    {
        public EndingType Judge(WorldState state) => throw new System.NotImplementedException();
    }
}
