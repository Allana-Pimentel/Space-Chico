using UnityEngine;
using UnityEngine.UI;

namespace SpaceChico
{
    // Barra de vida do Chefe do Pântano
    public class BossHealthBarUI : MonoBehaviour
    {
        public BossSwamp boss;
        public CanvasGroup canvasGroup;
        public Image fillImage;
        public Text bossNameText, phaseText, hpText;
        public Color phase1Color = new Color(0.2f, 0.9f, 0.4f);
        public Color phase2Color = new Color(1f, 0.35f, 0.15f);

        private float targetFill = 1f;
        private bool isVisible;

        void Awake()
        {
            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup != null) canvasGroup.alpha = 0f;
        }

        void Start()
        {
            if (boss == null) boss = FindAnyObjectByType<BossSwamp>();
            if (boss != null)
            {
                boss.HealthChanged += UpdateUI;
                boss.Phase2Started += () => UpdateUI(boss.currentHealth, boss.maxHealth);
                boss.BossDefeated += () => Invoke(nameof(Hide), 2.0f);
                UpdateUI(boss.currentHealth, boss.maxHealth);
            }
        }

        void Update()
        {
            if (canvasGroup != null)
                canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, isVisible ? 1f : 0f, Time.unscaledDeltaTime * 2.5f);
            if (fillImage != null)
            {
                fillImage.fillAmount = Mathf.MoveTowards(fillImage.fillAmount, targetFill, Time.unscaledDeltaTime * 1.5f);
                fillImage.rectTransform.anchorMax = new Vector2(fillImage.fillAmount, 1f);
            }
        }

        public void Show()
        {
            isVisible = true;
            if (boss != null) UpdateUI(boss.currentHealth, boss.maxHealth);
        }

        public void Hide() => isVisible = false;

        void UpdateUI(int current, int max)
        {
            targetFill = Mathf.Clamp01((float)current / Mathf.Max(1, max));
            if (hpText != null) hpText.text = $"{Mathf.Max(0, current)} / {max} HP";
            if (fillImage != null && boss != null) fillImage.color = boss.IsPhase2 ? phase2Color : phase1Color;
            if (phaseText != null && boss != null) phaseText.text = boss.IsPhase2 ? "FASE 2: ENFURECIDO" : "Guardião do Pântano";
        }
    }
}
