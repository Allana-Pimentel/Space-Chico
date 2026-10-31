using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SpaceChico
{
    // Interface principal (HUD) no canto superior esquerdo da tela
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

        [Header("Oxigênio")]
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
        private static Sprite generatedHeartSprite;

        private void Awake()
        {
            EnsureTopLeftLayout();
        }

        private void Start()
        {
            if (stats == null && GameManager.Instance != null && GameManager.Instance.player != null)
                stats = GameManager.Instance.player.Stats;
            if (powers == null && GameManager.Instance != null && GameManager.Instance.player != null)
                powers = GameManager.Instance.player.Powers;
            if (inventory == null && GameManager.Instance != null && GameManager.Instance.player != null)
                inventory = GameManager.Instance.player.Inventory;

            if (stats != null)
            {
                stats.LifeChanged += UpdateLife;
                stats.OxygenChanged += UpdateOxygen;
                stats.CrystalsChanged += UpdateCrystals;

                UpdateLife(stats.CurrentLife, stats.MaxLife);
                UpdateOxygen(stats.CurrentOxygen, stats.maxOxygen);
                UpdateCrystals(stats.Crystals);
            }

            if (inventory != null)
            {
                inventory.Changed += UpdateEquippedPower;
            }

            UpdateEquippedPower();
        }

        // Garante que o painel e o card de status fiquem fixos e organizados no canto superior esquerdo
        private void EnsureTopLeftLayout()
        {
            var panelRect = GetComponent<RectTransform>();
            if (panelRect != null)
            {
                panelRect.anchorMin = Vector2.zero;
                panelRect.anchorMax = Vector2.one;
                panelRect.anchoredPosition = Vector2.zero;
                panelRect.sizeDelta = Vector2.zero;
            }

            if (heartsContainer != null && heartsContainer.parent != null)
            {
                var cardRect = heartsContainer.parent.GetComponent<RectTransform>();
                if (cardRect != null)
                {
                    cardRect.anchorMin = new Vector2(0f, 1f);
                    cardRect.anchorMax = new Vector2(0f, 1f);
                    cardRect.pivot = new Vector2(0f, 1f);
                    cardRect.anchoredPosition = new Vector2(20f, -20f);
                    cardRect.sizeDelta = new Vector2(355f, 142f);

                    var hpTransform = cardRect.Find("HPText");
                    if (hpTransform != null)
                        hpTitleLabel = hpTransform.GetComponent<Text>();
                }
            }
        }

        private void OnDestroy()
        {
            if (stats != null)
            {
                stats.LifeChanged -= UpdateLife;
                stats.OxygenChanged -= UpdateOxygen;
                stats.CrystalsChanged -= UpdateCrystals;
            }
            if (inventory != null)
            {
                inventory.Changed -= UpdateEquippedPower;
            }
        }

        private void Update()
        {
            UpdatePowerTimers();
            UpdateOxygenAlert();
        }

        private void UpdateOxygenAlert()
        {
            if (stats == null || oxygenFillImage == null) return;
            float pct = stats.CurrentOxygen / Mathf.Max(1f, stats.maxOxygen);

            if (pct < 0.25f && pct > 0f)
            {
                float pulse = (Mathf.Sin(Time.unscaledTime * 8f) + 1f) * 0.5f;
                oxygenFillImage.color = Color.Lerp(new Color(1f, 0.2f, 0.2f), new Color(0.55f, 0.05f, 0.05f), pulse);
            }
        }

        public void UpdateLife(int current, int max)
        {
            if (heartsContainer == null) return;

            if (generatedHeartSprite == null)
                generatedHeartSprite = CreateHeartSprite();

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
                    var rt = obj.GetComponent<RectTransform>();
                    rt.sizeDelta = new Vector2(30, 30);
                }

                var img = obj.GetComponent<Image>();
                img.sprite = fullHeartSprite != null ? fullHeartSprite : generatedHeartSprite;
                img.preserveAspect = true;
                spawnedHearts.Add(img);
            }

            for (int i = 0; i < spawnedHearts.Count; i++)
            {
                if (i < max)
                {
                    spawnedHearts[i].gameObject.SetActive(true);
                    bool isFull = i < current;
                    if (fullHeartSprite != null && emptyHeartSprite != null)
                    {
                        spawnedHearts[i].sprite = isFull ? fullHeartSprite : emptyHeartSprite;
                    }
                    else
                    {
                        spawnedHearts[i].sprite = generatedHeartSprite;
                        spawnedHearts[i].color = isFull
                            ? new Color(1f, 0.25f, 0.4f, 1f)
                            : new Color(0.25f, 0.25f, 0.32f, 0.45f);
                    }
                }
                else
                {
                    spawnedHearts[i].gameObject.SetActive(false);
                }
            }

            if (hpTitleLabel != null)
            {
                hpTitleLabel.text = $"VIDA  ({Mathf.Max(0, current)}/{max})";
            }
        }

        public void UpdateOxygen(float current, float max)
        {
            float pct = Mathf.Clamp01(current / Mathf.Max(1f, max));
            if (oxygenFillImage != null)
            {
                oxygenFillImage.fillAmount = pct;
                oxygenFillImage.rectTransform.anchorMax = new Vector2(pct, 1f);
                oxygenFillImage.color = (pct < 0.25f) ? oxygenWarningColor : oxygenNormalColor;
            }
            if (oxygenText != null)
            {
                oxygenText.text = $"OXIGÊNIO: {Mathf.CeilToInt(pct * 100f)}%";
            }
        }

        public void UpdateCrystals(int count)
        {
            if (crystalsText != null)
            {
                crystalsText.text = $"◆ CRISTAIS: {count}";
            }
        }

        public void UpdateEquippedPower()
        {
            if (inventory == null) return;

            var power = inventory.equippedPower;
            if (power != null && powerIcon != null)
            {
                powerIcon.enabled = true;
                if (power.icon != null) powerIcon.sprite = power.icon;
                powerIcon.color = power.accent;

                if (powerLabel != null) powerLabel.text = $"{power.displayName}\n[Tecla E]";
            }
            else if (powerIcon != null)
            {
                powerIcon.enabled = false;
                if (powerLabel != null) powerLabel.text = "Sem Poder\n[Tecla E]";
            }
        }

        private void UpdatePowerTimers()
        {
            if (powers == null || powerTimerFill == null) return;

            if (powers.IsShieldActive)
            {
                powerTimerFill.gameObject.SetActive(true);
                powerTimerFill.fillAmount = Mathf.Clamp01(powers.ShieldTimeRemaining / 6f);
                powerTimerFill.color = new Color(0.3f, 0.8f, 1f);
            }
            else if (powers.IsDoubleJumpActive)
            {
                powerTimerFill.gameObject.SetActive(true);
                powerTimerFill.fillAmount = Mathf.Clamp01(powers.DoubleJumpTimeRemaining / 8f);
                powerTimerFill.color = new Color(1f, 0.9f, 0.3f);
            }
            else
            {
                powerTimerFill.gameObject.SetActive(false);
            }
        }

        // Desenha um ícone de coração para o HUD
        private static Sprite CreateHeartSprite()
        {
            int size = 32;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float nx = (x - size * 0.5f + 0.5f) / (size * 0.38f);
                    float ny = (y - size * 0.42f + 0.5f) / (size * 0.38f);
                    // Equação clássica de coração: (x^2 + y^2 - 1)^3 - x^2 * y^3 <= 0
                    float a = nx * nx + ny * ny - 1f;
                    float val = a * a * a - (nx * nx) * (ny * ny * ny);
                    if (val <= 0f)
                        tex.SetPixel(x, y, Color.white);
                    else
                        tex.SetPixel(x, y, Color.clear);
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 32f);
        }
    }
}
