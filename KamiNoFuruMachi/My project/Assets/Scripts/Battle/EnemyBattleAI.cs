// Assets/Scripts/Battle/EnemyBattleAI.cs
// 仕様: specs/gamespec_コマンドRPG化差分_v0.2_20260726.md 2章。
// 「シンプルな重み抽選 or 状態依存ルール」のうち、最小構成として一様ランダム抽選を採用。
// targets は将来の対象選択ロジック拡張用に残す（Phase 1では BattleManager 側が対象を決定する）。
using System;

namespace KamiNoFuruMachi
{
    public class EnemyBattleAI
    {
        private static readonly Random _random = new();

        public BattleActionSO ChooseAction(BattleUnit self, BattleUnit[] targets)
        {
            if (self?.AvailableActions == null || self.AvailableActions.Length == 0) return null;
            return self.AvailableActions[_random.Next(self.AvailableActions.Length)];
        }
    }
}
