// Assets/Scripts/Town/NpcInteractable.cs
// 仕様: specs/gamespec_Unity実装仕様書_v0.1_20260726.md 3-4。クリック/インタラクトでYarn Spinner起動。
// Yarn Spinnerは未導入（v0.1 2章の推奨サードパーティ）。パッケージ追加後に実装する。
using UnityEngine;

namespace KamiNoFuruMachi
{
    public class NpcInteractable : MonoBehaviour
    {
        public NpcRuntimeState State;

        public void Interact() => throw new System.NotImplementedException();
    }
}
