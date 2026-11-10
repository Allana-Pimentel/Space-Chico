using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SpaceChico
{
    // HUD simples da fase: exibe apenas Vida (corações), Cristais e Guia de Controles
    public class HUD : MonoBehaviour
    {
        public PlayerStats stats;
        public Transform heartsContainer;
        public GameObject heartPrefab;
        public Sprite fullHeartSprite, emptyHeartSprite;
        public Image oxygenFillImage, powerIcon;
        public Text oxygenText, crystalsText, powerLabel;

        private readonly List<Image> spawnedHearts = new List<Image>();
        private Text hpTitleLabel;

        void Awake()
        {
            if (oxygenFillImage?.transform.parent != null) oxygenFillImage.transform.parent.gameObject.SetActive(false);
            if (oxygenText != null) oxygenText.gameObject.SetActive(false);
            if (powerIcon?.transform.parent != null) powerIcon.transform.parent.gameObject.SetActive(false);

            // Elemento 3: Atualiza o Guia de Controles na tela para o Estilo Mario (sem tiro/mochila)
            var guide = transform.Find("ControlsGuide")?.GetComponent<Text>();
            if (guide != null)
                guide.text = "Setas / WASD: Mover e Subir Vinhas\nEspaço: Pular e Pisar nos Inimigos";

            if (heartsContainer?.parent != null)
            {
                var card = heartsContainer.parent.GetComponent<RectTransform>();
                if (card != null)
                {
                    card.anchorMin = card.anchorMax = card.pivot = new Vector2(0f, 1f);
                    card.anchoredPosition = new Vector2(20f, -20f);
                    card.sizeDelta = new Vector2(300f, 105f);
                    hpTitleLabel = card.Find("HPText")?.GetComponent<Text>();
                }
            }
        }

        void Start()
        {
            if (stats == null && GameManager.Instance?.player != null) stats = GameManager.Instance.player.Stats;
            if (stats != null)
            {
                stats.LifeChanged += UpdateLife;
                stats.CrystalsChanged += UpdateCrystals;
                UpdateLife(stats.CurrentLife, stats.MaxLife);
                UpdateCrystals(stats.Crystals);
            }
        }

        public void UpdateLife(int current, int max)
        {
            if (heartsContainer == null) return;
            while (spawnedHearts.Count < max)
            {
                var obj = heartPrefab != null ? Instantiate(heartPrefab, heartsContainer)
                                              : new GameObject("Heart", typeof(RectTransform), typeof(Image));
                obj.transform.SetParent(heartsContainer, false);
                obj.GetComponent<RectTransform>().sizeDelta = new Vector2(28, 28);
                var img = obj.GetComponent<Image>();
                if (fullHeartSprite != null) img.sprite = fullHeartSprite;
                spawnedHearts.Add(img);
            }
            for (int i = 0; i < spawnedHearts.Count; i++)
            {
                spawnedHearts[i].gameObject.SetActive(i < max);
                spawnedHearts[i].color = (i < current) ? new Color(1f, 0.25f, 0.4f, 1f) : new Color(0.25f, 0.25f, 0.32f, 0.4f);
            }
            if (hpTitleLabel != null) hpTitleLabel.text = $"VIDA ({Mathf.Max(0, current)}/{max})";
        }

        public void UpdateCrystals(int count)
        {
            if (crystalsText != null) crystalsText.text = $"CRISTAIS: {count}";
        }
    }
}
