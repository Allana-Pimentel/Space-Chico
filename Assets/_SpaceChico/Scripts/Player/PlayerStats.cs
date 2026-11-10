using System;
using UnityEngine;

namespace SpaceChico
{
    // Controla os pontos de vida (corações), cristais e invulnerabilidade do Chico
    public class PlayerStats : MonoBehaviour
    {
        [Header("Vida")]
        public int baseMaxLife = 3;
        public int currentLife;
        public int maxLife;

        [Header("Oxigênio (Desativado no modo Arcade)")]
        public float maxOxygen = 100f;
        public float currentOxygen = 100f;
        public float oxygenDrainRate = 0f;

        [Header("Pontuação / Cristais")]
        public int crystals;

        [Header("Invulnerabilidade")]
        public float invulnerabilityDuration = 1.5f;
        public bool isInvulnerable;

        private float invulnerableTimer;
        private bool isDead;

        public int CurrentLife => currentLife;
        public int MaxLife => maxLife;
        public float CurrentOxygen => currentOxygen;
        public int Crystals => crystals;
        public bool IsInvulnerable => isInvulnerable;

        public event Action<int, int> LifeChanged;
        public event Action<float, float> OxygenChanged;
        public event Action<int> CrystalsChanged;
        public event Action<bool> InvulnerabilityChanged;

        private void Awake()
        {
            maxLife = baseMaxLife + ProgressData.BonusMaxLife;
            currentLife = maxLife;
            currentOxygen = maxOxygen;
            crystals = ProgressData.TotalCrystals;
        }

        private void Start()
        {
            LifeChanged?.Invoke(currentLife, maxLife);
            CrystalsChanged?.Invoke(crystals);
        }

        private void Update()
        {
            if (isDead) return;
            if (GameManager.Instance != null && !GameManager.Instance.IsGameplayRunning) return;

            if (isInvulnerable)
            {
                invulnerableTimer -= Time.deltaTime;
                if (invulnerableTimer <= 0f)
                {
                    isInvulnerable = false;
                    InvulnerabilityChanged?.Invoke(false);
                }
            }
        }

        public void TakeDamage(int damage)
        {
            if (isDead || isInvulnerable) return;

            currentLife = Mathf.Max(0, currentLife - damage);
            LifeChanged?.Invoke(currentLife, maxLife);

            if (currentLife <= 0)
            {
                Die("Chico ficou sem vida!");
                return;
            }

            TriggerInvulnerability(invulnerabilityDuration);
        }

        public void TriggerInvulnerability(float duration)
        {
            isInvulnerable = true;
            invulnerableTimer = duration;
            InvulnerabilityChanged?.Invoke(true);
        }

        public void Heal(int amount)
        {
            if (isDead) return;
            currentLife = Mathf.Min(maxLife, currentLife + amount);
            LifeChanged?.Invoke(currentLife, maxLife);
        }

        public void IncreaseMaxLife(int amount)
        {
            maxLife += amount;
            currentLife = Mathf.Min(maxLife, currentLife + amount);
            LifeChanged?.Invoke(currentLife, maxLife);
        }

        public void RestoreOxygen(float amount)
        {
            if (isDead) return;
            OxygenChanged?.Invoke(currentOxygen, maxOxygen);
            // No modo Arcade sem barra de oxigênio, cápsulas recuperam 1 de vida ou dão +5 cristais
            if (currentLife < maxLife) Heal(1);
            else AddCrystals(5);
        }

        public void AddCrystals(int amount)
        {
            crystals += amount;
            ProgressData.TotalCrystals = crystals;
            CrystalsChanged?.Invoke(crystals);
        }

        private void Die(string reason)
        {
            if (isDead) return;
            isDead = true;

            var player = GetComponent<PlayerController>();
            if (player != null) player.TriggerDeath(reason);
            else GameManager.Instance.TriggerGameOver(reason);
        }
    }
}
