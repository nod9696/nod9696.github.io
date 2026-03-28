// Assets/Scripts/Visual/CharacterData.cs
// namespace: KamiNoFuruMatchi

using System;
using System.Collections.Generic;
using UnityEngine;

namespace KamiNoFuruMatchi
{
    /// <summary>
    /// 1ポーズ分のスプライト情報を保持するシリアライズ可能クラス。
    /// poseId には ScenarioCommand の pose 文字列（例: "normal", "smile"）を対応させる。
    /// </summary>
    [Serializable]
    public class SpriteEntry
    {
        [Tooltip("シナリオコマンドの pose フィールドと一致させる文字列 (例: normal / smile / arm / surprised / anxious / soliloquy)")]
        public string poseId;

        [Tooltip("対応するスプライト画像")]
        public Sprite sprite;
    }

    /// <summary>
    /// キャラクター1人分のマスターデータ。
    /// Project ウィンドウで右クリック → Create → KamiNoFuruMatchi → CharacterData から生成できる。
    /// </summary>
    [CreateAssetMenu(
        fileName = "CharacterData_New",
        menuName  = "KamiNoFuruMatchi/CharacterData",
        order     = 10
    )]
    public class CharacterData : ScriptableObject
    {
        // -----------------------------------------------------------------------
        // 基本情報
        // -----------------------------------------------------------------------

        [Header("識別子")]
        [Tooltip("シナリオ JSON の char フィールドと一致させる小文字 ID (例: kanata / lilith / grief)")]
        public string characterId;

        [Header("表示名")]
        [Tooltip("テキストウィンドウのネームプレートに表示する名前")]
        public string displayName;

        [Header("名前カラー")]
        [Tooltip("ネームプレートのテキスト色")]
        public Color nameColor = Color.white;

        // -----------------------------------------------------------------------
        // スプライト一覧
        // -----------------------------------------------------------------------

        [Header("ポーズ別スプライト")]
        [Tooltip("poseId をキーにしたスプライトのリスト。シナリオコマンドの pose 値と対応させること。")]
        public List<SpriteEntry> sprites = new List<SpriteEntry>();

        // -----------------------------------------------------------------------
        // ユーティリティ
        // -----------------------------------------------------------------------

        /// <summary>
        /// poseId に対応する Sprite を返す。
        /// 見つからない場合は null を返す。
        /// </summary>
        /// <param name="poseId">ポーズ文字列 (例: "normal")</param>
        /// <returns>対応 Sprite、または null</returns>
        public Sprite GetSprite(string poseId)
        {
            if (sprites == null) return null;

            foreach (var entry in sprites)
            {
                if (string.Equals(entry.poseId, poseId, StringComparison.OrdinalIgnoreCase))
                {
                    return entry.sprite;
                }
            }

            Debug.LogWarning($"[CharacterData] '{characterId}' に poseId='{poseId}' のスプライトが見つかりませんでした。");
            return null;
        }

        /// <summary>
        /// poseId が登録済みかどうかを確認する。
        /// </summary>
        /// <param name="poseId">ポーズ文字列</param>
        /// <returns>登録されていれば true</returns>
        public bool HasPose(string poseId)
        {
            if (sprites == null) return false;

            foreach (var entry in sprites)
            {
                if (string.Equals(entry.poseId, poseId, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

#if UNITY_EDITOR
        /// <summary>
        /// エディタ検証: characterId や displayName が空の場合に警告を出す。
        /// </summary>
        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(characterId))
                Debug.LogWarning($"[CharacterData] {name}: characterId が未設定です。", this);

            if (string.IsNullOrWhiteSpace(displayName))
                Debug.LogWarning($"[CharacterData] {name}: displayName が未設定です。", this);

            // 重複 poseId チェック
            if (sprites != null)
            {
                var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var entry in sprites)
                {
                    if (!seen.Add(entry.poseId))
                        Debug.LogWarning($"[CharacterData] '{characterId}' に重複した poseId='{entry.poseId}' があります。", this);
                }
            }
        }
#endif
    }
}
