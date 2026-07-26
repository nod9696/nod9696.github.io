// Assets/Scripts/Battle/BattleUnit.cs
// 仕様: specs/gamespec_コマンドRPG化差分_v0.2_20260726.md 2章。プレイヤー・敵共通基底。
// DamageFormula等からの参照を想定し、Unity非依存のPOCOとする。
// Attack/Defense/Speedと AvailableActions は仕様書に明記が無いため、
// ダメージ計算・ターン順・行動選択に最低限必要な値として今回の実装で追加した（数値バランスは未調整）。
using System;

namespace KamiNoFuruMachi
{
    public class BattleUnit
    {
        public string UnitName;
        public int MaxHp;
        public int CurrentHp;
        public int Attack;
        public int Defense;
        public int Speed;
        public CharacterAlignment Alignment;
        public BattleActionSO[] AvailableActions = Array.Empty<BattleActionSO>();

        public bool IsDefeated => CurrentHp <= 0;

        public void ApplyDamage(int amount)
        {
            if (amount < 0) amount = 0;
            CurrentHp = Math.Max(0, CurrentHp - amount);
        }
    }
}
