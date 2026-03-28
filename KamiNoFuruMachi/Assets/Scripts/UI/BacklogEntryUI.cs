using UnityEngine;
using TMPro;

namespace KamiNoFuruMachi
{
    /// <summary>
    /// バックログ1行分のUIコンポーネント。
    /// BacklogUI によって生成・プール管理される。
    /// </summary>
    public class BacklogEntryUI : MonoBehaviour
    {
        // ----------------------------------------------------------------
        // Inspector バインド
        // ----------------------------------------------------------------

        [Header("テキスト")]
        [Tooltip("キャラ名表示（太字）。ナレーターの場合は非表示になる。")]
        [SerializeField] private TextMeshProUGUI _charNameText;

        [Tooltip("セリフ本文。")]
        [SerializeField] private TextMeshProUGUI _bodyText;

        [Header("ナレーター識別子")]
        [Tooltip("この文字列と一致するキャラ名はナレーター扱いになる。大文字小文字を無視。")]
        [SerializeField] private string _narratorId = "narrator";

        // ----------------------------------------------------------------
        // 公開 API
        // ----------------------------------------------------------------

        /// <summary>
        /// エントリにデータをセットする。
        /// </summary>
        /// <param name="charName">キャラ名。"narrator"（大文字小文字無視）の場合はキャラ名行を非表示にする。</param>
        /// <param name="body">セリフ本文。</param>
        public void SetEntry(string charName, string body)
        {
            bool isNarrator = string.IsNullOrEmpty(charName)
                              || string.Equals(charName, _narratorId,
                                               System.StringComparison.OrdinalIgnoreCase);

            // キャラ名エリア
            if (_charNameText != null)
            {
                _charNameText.gameObject.SetActive(!isNarrator);
                if (!isNarrator)
                {
                    // <b> タグで太字化（フォントスタイルを FontStyle.Bold に設定しても可）
                    _charNameText.text = $"<b>{charName}</b>";
                }
            }

            // 本文
            if (_bodyText != null)
            {
                _bodyText.text = body ?? string.Empty;
            }
        }
    }
}
