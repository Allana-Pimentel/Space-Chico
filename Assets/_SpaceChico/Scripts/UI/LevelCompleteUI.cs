using UnityEngine;
using UnityEngine.UI;

namespace SpaceChico
{
    /// <summary>
    /// Tela de Conclusão da Fase.
    /// Mostra item recuperado, cristais, tempo de exploração e bônus de vida máxima.
    /// </summary>
    public class LevelCompleteUI : MonoBehaviour
    {
        public GameObject panelRoot;
        public Text titleText;
        public Text partRecoveredText;
        public Text statsSummaryText;
        public Text bonusLifeText;
        public Button continueButton;

        private void Awake()
        {
            if (continueButton != null) continueButton.onClick.AddListener(OnContinue);
        }

        public void Show(string levelName, int crystals, float timeSeconds, int newMaxLife)
        {
            if (panelRoot != null) panelRoot.SetActive(true);

            if (titleText != null) titleText.text = $"{levelName} Concluído!";
            if (partRecoveredText != null) partRecoveredText.text = "★ Propulsor Quântico Recuperado! ★";

            int minutes = Mathf.FloorToInt(timeSeconds / 60f);
            int seconds = Mathf.FloorToInt(timeSeconds % 60f);

            if (statsSummaryText != null)
            {
                statsSummaryText.text = $"Cristais Coletados: {crystals}\nTempo: {minutes:D2}:{seconds:D2}";
            }

            if (bonusLifeText != null)
            {
                bonusLifeText.text = $"Recompensa: +1 Vida Máxima (Total: {newMaxLife} corações)";
            }
        }

        private void OnContinue()
        {
            if (GameManager.Instance != null) GameManager.Instance.QuitToMenu();
        }
    }
}
