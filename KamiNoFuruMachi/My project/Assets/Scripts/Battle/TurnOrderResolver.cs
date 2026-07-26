// Assets/Scripts/Battle/TurnOrderResolver.cs
// 仕様: specs/gamespec_コマンドRPG化差分_v0.2_20260726.md 2章。
// 差分v0.2の推奨案（速度値ベース単純ターン制）を採用。Speed降順で行動順を固定する。
using System;
using System.Collections.Generic;

namespace KamiNoFuruMachi
{
    public class TurnOrderResolver
    {
        public BattleUnit[] ResolveOrder(BattleUnit[] units)
        {
            if (units == null) throw new ArgumentNullException(nameof(units));

            var ordered = new List<BattleUnit>(units);
            ordered.Sort((a, b) => b.Speed.CompareTo(a.Speed));
            return ordered.ToArray();
        }
    }
}
