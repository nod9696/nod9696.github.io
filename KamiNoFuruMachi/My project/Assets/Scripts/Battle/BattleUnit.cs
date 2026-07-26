// Assets/Scripts/Battle/BattleUnit.cs
// 仕様: specs/gamespec_コマンドRPG化差分_v0.2_20260726.md 2章。プレイヤー・敵共通基底。
// DamageFormula等からの参照を想定し、Unity非依存のPOCOとする。
namespace KamiNoFuruMachi
{
    public class BattleUnit
    {
        public string UnitName;
        public int MaxHp;
        public int CurrentHp;
        public CharacterAlignment Alignment;

        public bool IsDefeated => CurrentHp <= 0;

        public void ApplyDamage(int amount) => throw new System.NotImplementedException();
    }
}
