// Assets/Scripts/Battle/BattleManager.cs
// 仕様: specs/gamespec_コマンドRPG化差分_v0.2_20260726.md 2章。ターン進行の統括、勝敗判定。
// 差分v0.2のPhase 1 MVP方針（「最小コマンド（攻撃のみ）で勝敗判定」）に合わせ、
// 現状は AvailableActions に入っている行動をそのまま実行するだけのループとした
// （防御/アイテム/離脱の個別ルールは未仕様化のため未実装）。
// 対象選択は「生存している先頭ユニット」の固定選択（パーティ→敵の先頭を攻撃、敵→パーティの先頭を攻撃）。
// 本格的なターゲット選択UI/AIは別途仕様化が必要。
using System;
using UnityEngine;

namespace KamiNoFuruMachi
{
    public class BattleManager : MonoBehaviour
    {
        public CommandMenuController CommandMenu;

        private readonly TurnOrderResolver _turnOrder = new();
        private readonly EnemyBattleAI _enemyAi = new();

        private BattleUnit[] _party = Array.Empty<BattleUnit>();
        private BattleUnit[] _enemies = Array.Empty<BattleUnit>();
        private BattleUnit[] _turnQueue = Array.Empty<BattleUnit>();
        private int _turnCursor;

        public event Action<BattleUnit, BattleUnit, int> OnDamageDealt; // actor, target, damage
        public event Action<bool> OnBattleEnded; // true = パーティ勝利

        public void StartBattle(BattleUnit[] party, BattleUnit[] enemies)
        {
            _party = party ?? Array.Empty<BattleUnit>();
            _enemies = enemies ?? Array.Empty<BattleUnit>();

            var all = new BattleUnit[_party.Length + _enemies.Length];
            _party.CopyTo(all, 0);
            _enemies.CopyTo(all, _party.Length);
            _turnQueue = _turnOrder.ResolveOrder(all);
            _turnCursor = 0;

            if (CommandMenu != null) CommandMenu.OnActionSelected += HandlePlayerActionSelected;
        }

        public void AdvanceTurn()
        {
            if (CheckBattleResult()) return;

            var actor = NextLivingActor();
            if (actor == null) return;

            if (Array.IndexOf(_party, actor) >= 0)
            {
                CommandMenu?.ShowCommands(actor); // プレイヤーの選択待ち。結果は HandlePlayerActionSelected で受ける
            }
            else
            {
                var target = FirstLivingUnit(_party);
                var action = _enemyAi.ChooseAction(actor, _party);
                if (target != null && action != null) ResolveAction(actor, action, target);
                AdvanceTurn();
            }
        }

        public bool CheckBattleResult()
        {
            if (AllDefeated(_enemies)) { OnBattleEnded?.Invoke(true); return true; }
            if (AllDefeated(_party)) { OnBattleEnded?.Invoke(false); return true; }
            return false;
        }

        private void HandlePlayerActionSelected(BattleUnit actor, BattleActionSO action)
        {
            var target = FirstLivingUnit(_enemies);
            if (target != null) ResolveAction(actor, action, target);
            AdvanceTurn();
        }

        private void ResolveAction(BattleUnit actor, BattleActionSO action, BattleUnit target)
        {
            PlayerActionEventBus.Publish(new PlayerActionEvent { ActionId = action.ActionName, Alignment = actor.Alignment });

            // HitRate 未設定（0）のアクションは「未調整データ」とみなし命中扱いにする
            var hitRate = action.HitRate <= 0f ? 1f : Mathf.Clamp01(action.HitRate);
            if (UnityEngine.Random.value > hitRate) return;

            var damage = DamageFormula.Calculate(actor, target, action);
            target.ApplyDamage(damage);
            OnDamageDealt?.Invoke(actor, target, damage);
        }

        private BattleUnit NextLivingActor()
        {
            for (var i = 0; i < _turnQueue.Length; i++)
            {
                var idx = (_turnCursor + i) % _turnQueue.Length;
                if (!_turnQueue[idx].IsDefeated)
                {
                    _turnCursor = (idx + 1) % _turnQueue.Length;
                    return _turnQueue[idx];
                }
            }
            return null;
        }

        private static BattleUnit FirstLivingUnit(BattleUnit[] units)
        {
            foreach (var u in units) if (!u.IsDefeated) return u;
            return null;
        }

        private static bool AllDefeated(BattleUnit[] units)
        {
            foreach (var u in units) if (!u.IsDefeated) return false;
            return true;
        }
    }
}
