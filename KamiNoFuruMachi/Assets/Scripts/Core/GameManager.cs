// Assets/Scripts/Core/GameManager.cs
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace KamiNoFuruMachi
{
    /// <summary>
    /// ゲーム全体のライフサイクルを管理する MonoBehaviour シングルトン。
    /// FlagManager / SaveLoadManager / AudioManager を所有・初期化し、
    /// シーン遷移（タイトル / ゲーム / エンディング）の窓口となる。
    /// DontDestroyOnLoad によりシーンをまたいで永続する。
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        // =========================================================================
        // シングルトン
        // =========================================================================
        public static GameManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            InitializeManagers();
        }

        // =========================================================================
        // サブマネージャへの公開参照
        // =========================================================================
        public FlagManager     Flags  { get; private set; }
        public SaveLoadManager Save   { get; private set; }
        public AudioManager    Audio  { get; private set; }

        // =========================================================================
        // 初期化
        // =========================================================================
        private void InitializeManagers()
        {
            // --- FlagManager (Pure C#) ---
            Flags = new FlagManager();

            // --- SaveLoadManager (MonoBehaviour: 子 GameObject にアタッチ) ---
            var saveGo = new GameObject("SaveLoadManager");
            saveGo.transform.SetParent(transform);
            Save = saveGo.AddComponent<SaveLoadManager>();
            Save.Initialize(Flags);

            // --- AudioManager (MonoBehaviour: 子 GameObject にアタッチ) ---
            var audioGo = new GameObject("AudioManager");
            audioGo.transform.SetParent(transform);
            Audio = audioGo.AddComponent<AudioManager>();
            Audio.Initialize();

            Debug.Log("[GameManager] 全マネージャの初期化が完了しました");
        }

        // =========================================================================
        // シーン遷移
        // =========================================================================

        /// <summary>タイトルシーンへ遷移する。</summary>
        public void GoToTitle()
        {
            TransitionToSceneAsync(GameConstants.Scenes.Title).Forget();
        }

        /// <summary>ゲームシーンへ遷移する。</summary>
        public void GoToGame()
        {
            TransitionToSceneAsync(GameConstants.Scenes.Game).Forget();
        }

        /// <summary>エンディングシーンへ遷移する。</summary>
        public void GoToEnding()
        {
            TransitionToSceneAsync(GameConstants.Scenes.Ending).Forget();
        }

        /// <summary>
        /// シーン遷移の共通処理。
        /// BGM フェードアウト → シーンロード の順に非同期で実行する。
        /// </summary>
        private async UniTaskVoid TransitionToSceneAsync(string sceneName)
        {
            Debug.Log($"[GameManager] シーン遷移開始: {sceneName}");

            // BGM をフェードアウトしてからシーンをロードする
            await Audio.FadeOutBGMAsync(duration: 0.8f);

            await SceneManager.LoadSceneAsync(sceneName);

            Debug.Log($"[GameManager] シーン遷移完了: {sceneName}");
        }

        // =========================================================================
        // 章オートセーブ
        // =========================================================================

        /// <summary>
        /// 章の切り替わり時に呼び出すオートセーブのショートカット。
        /// </summary>
        public void TriggerAutoSave(int chapterNumber)
        {
            Save.AutoSave(chapterNumber);
        }
    }
}
