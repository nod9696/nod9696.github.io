// Assets/Scripts/Battle/PlayerActionEventBus.cs
// 仕様: specs/gamespec_Unity実装仕様書_v0.1_20260726.md 3-2。
// 差分v0.2により、コマンド選択イベントもこの経由で発行する（内容は変更なし）。
using System;

namespace KamiNoFuruMachi
{
    public struct PlayerActionEvent
    {
        public string ActionId;
        public CharacterAlignment Alignment;
    }

    public static class PlayerActionEventBus
    {
        public static event Action<PlayerActionEvent> OnActionPerformed;

        public static void Publish(PlayerActionEvent evt) => OnActionPerformed?.Invoke(evt);
    }
}
