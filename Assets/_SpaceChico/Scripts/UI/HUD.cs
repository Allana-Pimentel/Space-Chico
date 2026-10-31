using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SpaceChico
{
    /// <summary>
    /// HUD do jogo.
    /// Exibe Corações de Vida, Barra de Oxigênio, Cristais, Poder Equipado e Atalhos.
    /// </summary>
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
        public Color oxygenNormalColor = new Color(0.2f, 0.8f, 1f);
        public Color oxygenWarningColor = new Color(1f, 0.3f, 0.3f);

        [Header("Cristais")]
        public Text crystalsText;

        [Header("Poder Equipado")]
        public Image powerIcon;
        public Image powerTimerFill;
        public Text powerLabel;

        private readonly List<Image> spawnedHearts = new List<Image>();

        private void Start()
        {
            if (stats == null && GameManager.Instance != null && GameManager.Instance.player != null)
            {
                stats = GameManager.Instance.player.Stats;
            }
            if (powers == null && GameManager.Instance != null && GameManager.Instance.player != null)
            {
                powers = GameManager.Instance.player.Powers;
            }
            if (inventory == null && GameManager.Instance != null && GameManager.Instance.player != null)
            {
                inventory = GameManager.Instance.player.Inventory;
            }

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

            // Alerta visual pulsante de oxigênio crítico (< 20%)
            if (pct < 0.2f && pct > 0f)
            {
                float pulse = (Mathf.Sin(Time.unscaledTime * 9f) + 1f) * 0.5f;
                oxygenFillImage.color = Color.Lerp(new Color(1f, 0.15f, 0.15f), new Color(0.5f, 0.05f, 0.05f), pulse);
            }
        }

        public void UpdateLife(int current, int max)
        {
            if (heartsContainer == null) return;

            // Garante que existam imagens suficientes para a vida máxima
            while (spawnedHearts.Count < max)
            {
                GameObject obj = null;
                if (heartPrefab != null)
                {
                    obj = Instantiate(heartPrefab, heartsContainer);
                }
                else
                {
                    obj = new GameObject("Heart_" + spawnedHearts.Count, typeof(RectTransform), typeof(Image));
                    obj.transform.SetParent(heartsContainer, false);
                    var rt = obj.GetComponent<RectTransform>();
                    rt.sizeDelta = new Vector2(36, 36);
                }

                var img = obj.GetComponent<Image>();
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
                        spawnedHearts[i].color = isFull ? new Color(1f, 0.25f, 0.35f) : new Color(0.2f, 0.2f, 0.25f, 0.6f);
                    }
                }
                else
                {
                    spawnedHearts[i].gameObject.SetActive(false);
                }
            }
        }

        public void UpdateOxygen(float current, float max)
        {
            float pct = Mathf.Clamp01(current / max);
            if (oxygenFillImage != null)
            {
                oxygenFillImage.fillAmount = pct;
                oxygenFillImage.color = (pct < 0.25f) ? oxygenWarningColor : oxygenNormalColor;
            }
            if (oxygenText != null)
            {
                oxygenText.text = $"O₂ {Mathf.CeilToInt(pct * 100f)}%";
            }
        }

        public void UpdateCrystals(int count)
        {
            if (crystalsText != null)
            {
                crystalsText.text = $"◆ {count}";
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

                if (powerLabel != null) powerLabel.text = $"{power.displayName}\n[F]";
            }
            else if (powerIcon != null)
            {
                powerIcon.enabled = false;
                if (powerLabel != null) powerLabel.text = "Nenhum Poder\n[F]";
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
    }
}
