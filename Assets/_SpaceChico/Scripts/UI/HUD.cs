using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SpaceChico
{
    // HUD simples da fase: exibe apenas Vida (corações) e Cristais coletados
    public class HUD : MonoBehaviour
    {
        [Header("Referências")]
        public PlayerStats stats;
        public PlayerPowers powers;
        public Inventory inventory;

        [Header("Elementos de Vida")]
        public Transform heartsContainer;
        public GameObject heartPrefab;
        public Sprite fullHeartSprite;
        public Sprite emptyHeartSprite;

        [Header("Oxigênio (Oculto no modo Arcade)")]
        public Image oxygenFillImage;
        public Text oxygenText;
        public Color oxygenNormalColor = new Color(0.2f, 0.85f, 1f);
        public Color oxygenWarningColor = new Color(1f, 0.3f, 0.3f);

        [Header("Cristais")]
        public Text crystalsText;

        [Header("Poder Equipado")]
        public Image powerIcon;
        public Image powerTimerFill;
        public Text powerLabel;

        private readonly List<Image> spawnedHearts = new List<Image>();
        private Text hpTitleLabel;

        private void Awake()
        {
            // Esconde a barra de oxigênio e o painel de mochila/poder para deixar o HUD limpo
            if (oxygenFillImage != null && oxygenFillImage.transform.parent != null)
                oxygenFillImage.transform.parent.gameObject.SetActive(false);
            if (oxygenText != null)
                oxygenText.gameObject.SetActive(false);
            if (powerIcon != null && powerIcon.transform.parent != null)
                powerIcon.transform.parent.gameObject.SetActive(false);

            if (heartsContainer != null && heartsContainer.parent != null)
            {
                var cardRect = heartsContainer.parent.GetComponent<RectTransform>();
                if (cardRect != null)
                {
                    cardRect.anchorMin = new Vector2(0f, 1f);
                    cardRect.anchorMax = new Vector2(0f, 1f);
                    cardRect.pivot = new Vector2(0f, 1f);
                    cardRect.anchoredPosition = new Vector2(20f, -20f);
                    cardRect.sizeDelta = new Vector2(300f, 105f);

                    var hpTransform = cardRect.Find("HPText");
                    if (hpTransform != null)
                        hpTitleLabel = hpTransform.GetComponent<Text>();
                }
            }
        }

        private void Start()
        {
            if (stats == null && GameManager.Instance != null && GameManager.Instance.player != null)
                stats = GameManager.Instance.player.Stats;

            if (stats != null)
            {
                stats.LifeChanged += UpdateLife;
                stats.CrystalsChanged += UpdateCrystals;

                UpdateLife(stats.CurrentLife, stats.MaxLife);
                UpdateCrystals(stats.Crystals);
            }
        }

        private void OnDestroy()
        {
            if (stats != null)
            {
                stats.LifeChanged -= UpdateLife;
                stats.CrystalsChanged -= UpdateCrystals;
            }
        }

        public void UpdateLife(int current, int max)
        {
            if (heartsContainer == null) return;

            if (fullHeartSprite == null)
                fullHeartSprite = Resources.Load<Sprite>("icon_heart_full");

            while (spawnedHearts.Count < max)
            {
                GameObject obj;
                if (heartPrefab != null)
                {
                    obj = Instantiate(heartPrefab, heartsContainer);
                }
                else
                {
                    obj = new GameObject("Heart_" + spawnedHearts.Count, typeof(RectTransform), typeof(Image));
                    obj.transform.SetParent(heartsContainer, false);
                    obj.GetComponent<RectTransform>().sizeDelta = new Vector2(28, 28);
                }

                var img = obj.GetComponent<Image>();
                if (fullHeartSprite != null) img.sprite = fullHeartSprite;
                img.preserveAspect = true;
                spawnedHearts.Add(img);
            }

            for (int i = 0; i < spawnedHearts.Count; i++)
            {
                bool active = i < max;
                spawnedHearts[i].gameObject.SetActive(active);
                if (active)
                {
                    bool isFull = i < current;
                    spawnedHearts[i].color = isFull
                        ? new Color(1f, 0.25f, 0.4f, 1f)
                        : new Color(0.25f, 0.25f, 0.32f, 0.4f);
                }
            }

            if (hpTitleLabel != null)
                hpTitleLabel.text = $"VIDA ({Mathf.Max(0, current)}/{max})";
        }

        public void UpdateOxygen(float current, float max) { }

        public void UpdateCrystals(int count)
        {
            if (crystalsText != null)
                crystalsText.text = $"CRISTAIS: {count}";
        }

        public void UpdateEquippedPower() { }
    }
}
