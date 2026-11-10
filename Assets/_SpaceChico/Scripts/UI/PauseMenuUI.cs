using UnityEngine;
using UnityEngine.UI;

namespace SpaceChico
{
    public class PauseMenuUI : MonoBehaviour
    {
        public GameObject panelRoot;
        public Button resumeButton;
        public Button restartButton;
        public Button quitButton;

        private void Awake()
        {
            if (resumeButton != null) resumeButton.onClick.AddListener(OnResume);
            if (restartButton != null) restartButton.onClick.AddListener(OnRestart);
            if (quitButton != null) quitButton.onClick.AddListener(OnQuit);
        }

        public void Open()
        {
            if (panelRoot != null) panelRoot.SetActive(true);
        }

        public void Close()
        {
            if (panelRoot != null) panelRoot.SetActive(false);
        }

        private void Update()
        {
            if (panelRoot != null && panelRoot.activeSelf)
            {
                if (GameInput.Pressed(GameAction.Pause))
                {
                    OnResume();
                }
            }
        }

        private void OnResume()
        {
            Close();
            if (GameManager.Instance != null) GameManager.Instance.Resume();
        }

        private void OnRestart()
        {
            Close();
            if (GameManager.Instance != null) GameManager.Instance.RestartLevel();
        }

        private void OnQuit()
        {
            Close();
            if (GameManager.Instance != null) GameManager.Instance.QuitToMenu();
        }
    }
}
