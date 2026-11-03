using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SpaceChico
{
    public enum GameState
    {
        Playing,
        Cutscene,
        Paused,
        Backpack,
        LevelComplete,
        GameOver
    }

    // Controlador geral do fluxo do jogo (pausa, vitória, derrota e reinício da fase)
    [DefaultExecutionOrder(-100)]
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Fase")]
        public string levelName = "Pântano Bioluminescente";
        public string menuSceneName = "00_Intro";

        [Header("Referências")]
        public PlayerController player;
        public HUD hud;
        public BackpackUI backpackUI;
        public PauseMenuUI pauseMenuUI;
        public GameOverUI gameOverUI;
        public LevelCompleteUI levelCompleteUI;

        public GameState State { get; private set; } = GameState.Playing;
        public event Action<GameState> StateChanged;

        public float LevelTime { get; private set; }
        public bool IsGameplayRunning => State == GameState.Playing;

        Action pendingBackpackClosed;

        // Se der Play com uma cena vazia aberta, carrega a fase principal automaticamente
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void EnsureValidScene()
        {
            Scene active = SceneManager.GetActiveScene();
            if (string.IsNullOrEmpty(active.name) || (active.name != "00_Intro" && active.name != "01_Pantano"))
            {
                SceneManager.LoadScene("01_Pantano");
            }
        }

        void Awake()
        {
            Instance = this;
            Time.timeScale = 1f;
            Layers.ApplyCollisionMatrix();
            LevelExpander.ExpandIfPantano();

            var propShip = GameObject.Find("Prop_CrashedShip");
            if (propShip != null)
            {
                propShip.transform.localScale = new Vector3(2.35f, 2.35f, 1f);
                propShip.transform.position = new Vector3(-5.2f, 2.15f, 0f);
            }
        }

        void Start()
        {
            // Na primeira vez que a fase abre, toca o vídeo/cutscene do Chico feliz na nave e caindo
            if (IntroShipCutscene.DeveTocarIntro())
            {
                var intro = gameObject.AddComponent<IntroShipCutscene>();
                intro.IniciarCutscene();
            }
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void Update()
        {
            if (State != GameState.Playing) return;

            LevelTime += Time.deltaTime;

            if (GameInput.Pressed(GameAction.Pause)) Pause();
            else if (GameInput.Pressed(GameAction.Backpack)) OpenBackpack(null, null);
        }

        public void SetState(GameState s)
        {
            State = s;
            Time.timeScale = (s == GameState.Playing || s == GameState.Cutscene) ? 1f : 0f;
            StateChanged?.Invoke(s);
        }

        public static void Hitstop(float duration = 0.06f)
        {
            if (Instance == null || Instance.State != GameState.Playing) return;
            Instance.StartCoroutine(Instance.HitstopRoutine(duration));
        }

        private IEnumerator HitstopRoutine(float duration)
        {
            Time.timeScale = 0.05f;
            yield return new WaitForSecondsRealtime(duration);
            if (State == GameState.Playing)
            {
                Time.timeScale = 1f;
            }
        }

        public void OpenBackpack(ItemData highlight, Action onClosed)
        {
            if (backpackUI == null) { onClosed?.Invoke(); return; }
            pendingBackpackClosed = onClosed;
            SetState(GameState.Backpack);
            AudioManager.Play(Sfx.UiOpen);
            backpackUI.Open(player != null ? player.Inventory : null, highlight);
        }

        public void CloseBackpack()
        {
            SetState(GameState.Playing);
            AudioManager.Play(Sfx.UiClose);
            var cb = pendingBackpackClosed;
            pendingBackpackClosed = null;
            cb?.Invoke();
        }

        public void Pause()
        {
            if (State != GameState.Playing) return;
            SetState(GameState.Paused);
            AudioManager.Play(Sfx.UiOpen);
            if (pauseMenuUI != null) pauseMenuUI.Open();
        }

        public void Resume()
        {
            if (State != GameState.Paused) return;
            AudioManager.Play(Sfx.UiClose);
            SetState(GameState.Playing);
        }

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

            if (gameOverUI != null) gameOverUI.Show(reason);

            // Reinicia a fase automaticamente após uma breve pausa de morte
            StartCoroutine(ReiniciarFaseAposMorte());
        }

        private IEnumerator ReiniciarFaseAposMorte()
        {
            Time.timeScale = 0.25f;
            yield return new WaitForSecondsRealtime(1.1f);
            RestartLevel();
        }

        public void RestartLevel()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        public void QuitToMenu()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(menuSceneName);
        }
    }
}
