using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SpaceChico
{
    /// <summary>Estados globais da partida.</summary>
    public enum GameState
    {
        Playing,        // TimeScale = 1
        Cutscene,       // TimeScale = 1, mas o jogador não tem controle (intro, comemoração, morte)
        Paused,         // TimeScale = 0 (Esc)
        Backpack,       // TimeScale = 0 (B ou coleta de peça)
        LevelComplete,  // TimeScale = 0
        GameOver        // TimeScale = 0
    }

    /// <summary>
    /// Ponto central de controle do fluxo do jogo. É o ÚNICO lugar que altera
    /// Time.timeScale:
    /// física, Animators, projéteis e partículas param juntos, enquanto a UI
    /// usa tempo "unscaled" para continuar animando.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Fase")]
        public string levelName = "Pântano Bioluminescente";
        public string menuSceneName = "00_Intro";

        [Header("Referências (preenchidas pelo builder)")]
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

        void Awake()
        {
            Instance = this;
            Time.timeScale = 1f;
            Layers.ApplyCollisionMatrix();
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

        // ------------------------------------------------------------------
        public void SetState(GameState s)
        {
            State = s;
            Time.timeScale = (s == GameState.Playing || s == GameState.Cutscene) ? 1f : 0f;
            StateChanged?.Invoke(s);
        }

        /// <summary>
        /// Micro-congelamento tátil de impacto (Hitstop / Game Feel).
        /// Dá peso físico imediato aos acertos e danos sem quebrar a taxa de quadros.
        /// </summary>
        public static void Hitstop(float duration = 0.06f)
        {
            if (Instance == null || Instance.State != GameState.Playing) return;
            Instance.StartCoroutine(Instance.HitstopRoutine(duration));
        }

        private System.Collections.IEnumerator HitstopRoutine(float duration)
        {
            Time.timeScale = 0.05f;
            yield return new WaitForSecondsRealtime(duration);
            if (State == GameState.Playing)
            {
                Time.timeScale = 1f;
            }
        }

        /// <summary>Abre a mochila (pausa o jogo). <paramref name="highlight"/> destaca um item recém-coletado.</summary>
        public void OpenBackpack(ItemData highlight, Action onClosed)
        {
            if (backpackUI == null) { onClosed?.Invoke(); return; }
            pendingBackpackClosed = onClosed;
            SetState(GameState.Backpack);
            AudioManager.Play(Sfx.UiOpen);
            backpackUI.Open(player != null ? player.Inventory : null, highlight);
        }

        /// <summary>Chamado pela BackpackUI quando o jogador fecha a mochila.</summary>
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
            SetState(GameState.GameOver);
            AudioManager.Play(Sfx.GameOver);
            if (gameOverUI != null) gameOverUI.Show(reason);
        }

        public void RestartLevel()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        /// <summary>Desistência: o jogador encerra a partida e volta ao menu.</summary>
        public void QuitToMenu()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(menuSceneName);
        }
    }
}
