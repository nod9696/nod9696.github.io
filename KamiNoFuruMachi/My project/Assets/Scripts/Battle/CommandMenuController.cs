// Assets/Scripts/Battle/CommandMenuController.cs
// 仕様: specs/gamespec_コマンドRPG化差分_v0.2_20260726.md 2章。
// 差分v0.2の推奨案（B案：UI駆動型、3Dモデル表示なし）を採用。
// 実際のボタン生成・ポートレート表示はUIプレハブ側の責務とし、本クラスは選択結果の仲介のみ行う
// （Canvas/プレハブは未作成のため、ボタンのOnClickから SelectCommand を呼ぶ形で接続する想定）。
// SelectCommand の引数はスタブ時点の CommandType から BattleActionSO に変更した
// （同じ種別のコマンドが複数ある場合に、どのアクションが選ばれたか区別できないため）。
using System;
using UnityEngine;

namespace KamiNoFuruMachi
{
    public class CommandMenuController : MonoBehaviour
    {
        public event Action<BattleUnit, BattleActionSO> OnActionSelected;

        private BattleUnit _actor;

        public void ShowCommands(BattleUnit actor)
        {
            _actor = actor;
            gameObject.SetActive(true);
        }

        public void SelectCommand(BattleActionSO action)
        {
            if (_actor == null || action == null) return;

            var actor = _actor;
            _actor = null;
            gameObject.SetActive(false);
            OnActionSelected?.Invoke(actor, action);
        }
    }
}
