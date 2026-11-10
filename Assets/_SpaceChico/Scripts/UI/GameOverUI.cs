using UnityEngine;
using UnityEngine.UI;

namespace SpaceChico
{
    public class GameOverUI : MonoBehaviour
    {
        public GameObject panelRoot;
        public Text reasonText;
        public Button restartButton;
        public Button quitButton;

        private void Awake()
        {
            if (restartButton != null) restartButton.onClick.AddListener(OnRestart);
            if (quitButton != null) quitButton.onClick.AddListener(OnQuit);
        }

        public void Show(string reason)
        {
            if (panelRoot != null) panelRoot.SetActive(true);
            if (reasonText != null) reasonText.text = reason;
        }

        private void OnRestart()
        {
            if (GameManager.Instance != null) GameManager.Instance.RestartLevel();
        }

        private void OnQuit()
        {
            if (GameManager.Instance != null) GameManager.Instance.QuitToMenu();
        }
    }
}
