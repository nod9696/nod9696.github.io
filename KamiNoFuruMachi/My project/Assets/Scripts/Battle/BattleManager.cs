// Assets/Scripts/Battle/BattleManager.cs
// 仕様: specs/gamespec_コマンドRPG化差分_v0.2_20260726.md 2章。ターン進行の統括、勝敗判定。
using UnityEngine;

namespace KamiNoFuruMachi
{
    public class BattleManager : MonoBehaviour
    {
        public TurnOrderResolver TurnOrder;
        public CommandMenuController CommandMenu;

        public void StartBattle(BattleUnit[] party, BattleUnit[] enemies) => throw new System.NotImplementedException();

        public void AdvanceTurn() => throw new System.NotImplementedException();

        public bool CheckBattleResult() => throw new System.NotImplementedException();
    }
}
