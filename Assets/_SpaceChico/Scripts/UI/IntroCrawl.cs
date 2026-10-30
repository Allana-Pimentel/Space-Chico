using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SpaceChico
{
    /// <summary>
    /// Abertura em rolagem vertical estilo Star Wars (GDD Seção 8: Storyboard Quadro 1).
    /// </summary>
    public class IntroCrawl : MonoBehaviour
    {
        [Header("Configurações")]
        public RectTransform crawlTransform;
        public float crawlSpeed = 48f;
        public float autoEndTime = 28f;
        public string nextSceneName = "01_Pantano";
        public Text promptText;

        private float timer;
        private bool isTransitioning;

        private void Start()
        {
            ProgressData.NewGame();
        }

        private void Update()
        {
            if (isTransitioning) return;

            timer += Time.deltaTime;

            if (crawlTransform != null)
            {
                crawlTransform.anchoredPosition += Vector2.up * crawlSpeed * Time.deltaTime;
            }

            // Pular intro com Espaço, Enter ou Tecla E
            if (GameInput.Pressed(GameAction.Confirm) || GameInput.Pressed(GameAction.Jump) || GameInput.Pressed(GameAction.Shoot) || timer >= autoEndTime)
            {
                StartGame();
            }

            // Piscar prompt de pular
            if (promptText != null)
            {
                promptText.color = new Color(1f, 1f, 1f, 0.4f + 0.4f * Mathf.Sin(Time.time * 4f));
            }
        }

        public void StartGame()
        {
            if (isTransitioning) return;
            isTransitioning = true;
            AudioManager.Play(Sfx.UiSelect);
            SceneManager.LoadScene(nextSceneName);
        }
    }
}
