using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SpaceChico
{
    // Redireciona a cena 00_Intro para a fase principal onde roda a abertura completa (Star Wars + Queda da Nave)
    public class IntroCrawl : MonoBehaviour
    {
        public RectTransform crawlTransform;
        public float crawlSpeed = 48f;
        public float autoEndTime = 28f;
        public string nextSceneName = "01_Pantano";
        public Text promptText;

        private void Start()
        {
            ProgressData.NewGame();
            SceneManager.LoadScene(nextSceneName);
        }
    }
}
