// Assets/Scripts/Battle/BattleActionSO.cs
// 仕様: specs/gamespec_コマンドRPG化差分_v0.2_20260726.md 2章
using UnityEngine;

namespace KamiNoFuruMachi
{
    public enum CommandType { Attack, Skill, Defend, Item, Escape }

    public enum CharacterAlignment { Mayowazu, Lilith, Neutral }

    [CreateAssetMenu(menuName = "KamiNoFuruMachi/Battle/BattleAction")]
    public class BattleActionSO : ScriptableObject
    {
        public string ActionName;
        public CommandType Type;
        public CharacterAlignment Alignment;
        public int Power;
        public float HitRate;

        // 威力・命中率・スコア寄与度の数値化方針は未確定（差分v0.2 次の論点4）
        public float ScoreContribution;
    }
}
