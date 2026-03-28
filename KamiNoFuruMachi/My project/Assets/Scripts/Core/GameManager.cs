// Assets/Scripts/Core/GameManager.cs
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace KamiNoFuruMachi
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public FlagManager     Flags { get; private set; }
        public SaveLoadManager Save  { get; private set; }
        public AudioManager    Audio { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeManagers();
        }

        private void InitializeManagers()
        {
            Flags = new FlagManager();

            var saveGo = new GameObject("SaveLoadManager");
            saveGo.transform.SetParent(transform);
            Save = saveGo.AddComponent<SaveLoadManager>();
            Save.Initialize(Flags);

            // AudioManager は自身でシングルトンを持つが参照を保持
            Audio = GetComponentInChildren<AudioManager>()
                    ?? new GameObject("AudioManager").AddComponent<AudioManager>();
            Audio.transform.SetParent(transform);
        }

        public void GoToTitle()  => TransitionAsync(GameConstants.Scenes.Title).Forget();
        public void GoToGame()   => TransitionAsync(GameConstants.Scenes.Game).Forget();
        public void GoToEnding() => TransitionAsync(GameConstants.Scenes.Ending).Forget();

        private async UniTaskVoid TransitionAsync(string sceneName)
        {
            if (Audio != null) await Audio.StopBGM(0.8f);
            await SceneManager.LoadSceneAsync(sceneName);
        }

        public void TriggerAutoSave(int chapterNumber) => Save.AutoSave(chapterNumber);
    }
}
