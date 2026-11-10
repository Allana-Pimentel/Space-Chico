using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SpaceChico
{
    public enum GameState { Playing, Cutscene, Paused, LevelComplete, GameOver }

    [DefaultExecutionOrder(-100)]
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }
        public string levelName = "Pântano Bioluminescente";
        public string menuSceneName = "00_Intro";
        public PlayerController player;
        public HUD hud;
        public PauseMenuUI pauseMenuUI;
        public GameOverUI gameOverUI;
        public LevelCompleteUI levelCompleteUI;

        public GameState State { get; private set; } = GameState.Playing;
        public event Action<GameState> StateChanged;
        public float LevelTime { get; private set; }
        public bool IsGameplayRunning => State == GameState.Playing;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void EnsureValidScene()
        {
            string s = SceneManager.GetActiveScene().name;
            if (s != "00_Intro" && s != "01_Pantano") SceneManager.LoadScene("01_Pantano");
        }

        void Awake()
        {
            Instance = this;
            Time.timeScale = 1f;
            Layers.ApplyCollisionMatrix();
            LevelExpander.ExpandIfPantano();

            var ship = GameObject.Find("Prop_CrashedShip");
            if (ship != null) { ship.transform.localScale = new Vector3(2.35f, 2.35f, 1f); ship.transform.position = new Vector3(-5.2f, 2.15f, 0f); }
        }

        void Start()
        {
            if (IntroShipCutscene.DeveTocarIntro())
                gameObject.AddComponent<IntroShipCutscene>().IniciarCutscene();
        }

        void OnDestroy() { if (Instance == this) Instance = null; }

        void Update()
        {
            if (State != GameState.Playing) return;
            LevelTime += Time.deltaTime;
            if (GameInput.Pressed(GameAction.Pause)) Pause();
        }

        public void SetState(GameState s)
        {
            State = s;
            Time.timeScale = (s == GameState.Playing || s == GameState.Cutscene) ? 1f : 0f;
            StateChanged?.Invoke(s);
        }

        public void Pause() { if (State == GameState.Playing) { SetState(GameState.Paused); AudioManager.Play(Sfx.UiOpen); pauseMenuUI?.Open(); } }
        public void Resume() { if (State == GameState.Paused) { AudioManager.Play(Sfx.UiClose); SetState(GameState.Playing); } }

        public void CompleteLevel()
        {
            SetState(GameState.LevelComplete);
            AudioManager.Play(Sfx.LevelComplete);
            if (levelCompleteUI != null && player != null)
                levelCompleteUI.Show(levelName, player.Stats.Crystals, LevelTime, player.Stats.MaxLife);
        }

        public void TriggerGameOver(string reason)
        {
            if (State == GameState.GameOver) return;
            State = GameState.GameOver;
            StateChanged?.Invoke(State);
            AudioManager.Play(Sfx.GameOver);
            gameOverUI?.Show(reason);
            StartCoroutine(ReiniciarFaseAposMorte());
        }

        IEnumerator ReiniciarFaseAposMorte()
        {
            Time.timeScale = 0.25f;
            yield return new WaitForSecondsRealtime(1.1f);
            RestartLevel();
        }

        public void RestartLevel() { Time.timeScale = 1f; SceneManager.LoadScene(SceneManager.GetActiveScene().name); }
        public void QuitToMenu() { Time.timeScale = 1f; SceneManager.LoadScene(menuSceneName); }
    }
}
