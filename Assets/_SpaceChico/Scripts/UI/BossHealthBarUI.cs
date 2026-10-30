using UnityEngine;
using UnityEngine.UI;

namespace SpaceChico
{
    /// <summary>
    /// Barra de vida imponente para o Chefe do Pântano (GDD Seção 13).
    /// Mostra o nome, a fase atual (Fase 1 vs Fase 2 Enfurecido) e anima o preenchimento de HP.
    /// </summary>
    public class BossHealthBarUI : MonoBehaviour
    {
        [Header("Referências")]
        public BossSwamp boss;
        public CanvasGroup canvasGroup;
        public Image fillImage;
        public Text bossNameText;
        public Text phaseText;
        public Text hpText;

        [Header("Cores")]
        public Color phase1Color = new Color(0.2f, 0.9f, 0.4f);
        public Color phase2Color = new Color(1f, 0.35f, 0.15f);

        private float targetFill = 1f;
        private bool isVisible = false;

        private void Awake()
        {
            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }
        }

        private void Start()
        {
            if (boss == null)
            {
                boss = FindAnyObjectByType<BossSwamp>();
            }

            if (boss != null)
            {
                boss.HealthChanged += OnHealthChanged;
                boss.Phase2Started += OnPhase2Started;
                boss.BossDefeated += OnBossDefeated;

                UpdateUI(boss.currentHealth, boss.maxHealth);
            }
        }

        private void OnDestroy()
        {
            if (boss != null)
            {
                boss.HealthChanged -= OnHealthChanged;
                boss.Phase2Started -= OnPhase2Started;
                boss.BossDefeated -= OnBossDefeated;
            }
        }

        private void Update()
        {
            if (canvasGroup != null)
            {
                float targetAlpha = isVisible ? 1f : 0f;
                canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, targetAlpha, Time.unscaledDeltaTime * 2.5f);
            }

            if (fillImage != null)
            {
                fillImage.fillAmount = Mathf.MoveTowards(fillImage.fillAmount, targetFill, Time.unscaledDeltaTime * 1.5f);
            }
        }

        public void Show()
        {
            isVisible = true;
            if (boss != null)
            {
                UpdateUI(boss.currentHealth, boss.maxHealth);
            }
        }

        public void Hide()
        {
            isVisible = false;
        }

        private void OnHealthChanged(int current, int max)
        {
            UpdateUI(current, max);
        }

        private void OnPhase2Started()
        {
            if (phaseText != null)
            {
                phaseText.text = "FASE 2: ENFURECIDO";
                phaseText.color = phase2Color;
            }
            if (fillImage != null)
            {
                fillImage.color = phase2Color;
            }
        }

        private void OnBossDefeated()
        {
            if (phaseText != null)
            {
                phaseText.text = "DERROTADO!";
                phaseText.color = new Color(1f, 0.9f, 0.2f);
            }
            Invoke(nameof(Hide), 2.5f);
        }

        private void UpdateUI(int current, int max)
        {
            targetFill = Mathf.Clamp01((float)current / max);

            if (hpText != null)
            {
                hpText.text = $"{Mathf.Max(0, current)} / {max} HP";
            }

            if (phaseText != null && boss != null)
            {
                if (boss.IsPhase2)
                {
                    phaseText.text = "FASE 2: ENFURECIDO";
                    phaseText.color = phase2Color;
                    if (fillImage != null) fillImage.color = phase2Color;
                }
                else
                {
                    phaseText.text = "Bioluminescent Colossus • Fase 1";
                    phaseText.color = new Color(0.8f, 1f, 0.8f, 0.8f);
                    if (fillImage != null) fillImage.color = phase1Color;
                }
            }
        }
    }
}
