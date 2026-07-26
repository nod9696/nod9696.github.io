// Assets/Scripts/Battle/DamageFormula.cs
// 仕様: specs/gamespec_コマンドRPG化差分_v0.2_20260726.md 2章。
// ダメージ計算式。Unity非依存の純粋C#、単体テスト対象。
// 数値設計は未確定（差分v0.2 次の論点4）のため、暫定式：Power + 攻撃側Attack - 防御側Defense（最低1）。
using System;

namespace KamiNoFuruMachi
{
    public static class DamageFormula
    {
        public static int Calculate(BattleUnit attacker, BattleUnit defender, BattleActionSO action)
        {
            if (attacker == null) throw new ArgumentNullException(nameof(attacker));
            if (defender == null) throw new ArgumentNullException(nameof(defender));
            if (action == null) throw new ArgumentNullException(nameof(action));

            var raw = action.Power + attacker.Attack - defender.Defense;
            return Math.Max(1, raw);
        }
    }
}
