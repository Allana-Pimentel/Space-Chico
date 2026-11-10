using System;
using UnityEngine;

namespace SpaceChico
{
    public class PlayerStats : MonoBehaviour
    {
        public int baseMaxLife = 3, currentLife, maxLife, crystals;
        public float invulnerabilityDuration = 1.5f;
        public bool isInvulnerable;

        private float invulnerableTimer;
        private bool isDead;

        public int CurrentLife => currentLife;
        public int MaxLife => maxLife;
        public int Crystals => crystals;
        public bool IsInvulnerable => isInvulnerable;

        public event Action<int, int> LifeChanged;
        public event Action<int> CrystalsChanged;
        public event Action<bool> InvulnerabilityChanged;

        void Awake()
        {
            maxLife = baseMaxLife + ProgressData.BonusMaxLife;
            currentLife = maxLife;
            crystals = ProgressData.TotalCrystals;
        }

        void Start()
        {
            LifeChanged?.Invoke(currentLife, maxLife);
            CrystalsChanged?.Invoke(crystals);
        }

        void Update()
        {
            if (isDead || (GameManager.Instance != null && !GameManager.Instance.IsGameplayRunning)) return;
            if (isInvulnerable && (invulnerableTimer -= Time.deltaTime) <= 0f)
            {
                isInvulnerable = false;
                InvulnerabilityChanged?.Invoke(false);
            }
        }

        public void TakeDamage(int damage)
        {
            if (isDead || isInvulnerable) return;
            currentLife = Mathf.Max(0, currentLife - damage);
            LifeChanged?.Invoke(currentLife, maxLife);
            if (currentLife <= 0) { Die("Chico ficou sem vida!"); return; }

            isInvulnerable = true;
            invulnerableTimer = invulnerabilityDuration;
            InvulnerabilityChanged?.Invoke(true);
        }

        public void Heal(int amount) { if (!isDead) { currentLife = Mathf.Min(maxLife, currentLife + amount); LifeChanged?.Invoke(currentLife, maxLife); } }
        public void IncreaseMaxLife(int amount) { maxLife += amount; currentLife = Mathf.Min(maxLife, currentLife + amount); LifeChanged?.Invoke(currentLife, maxLife); }
        public void AddCrystals(int amount)
        {
            crystals += amount;
            ProgressData.TotalCrystals = crystals;
            CrystalsChanged?.Invoke(crystals);

            if (crystals >= 20)
            {
                crystals -= 20;
                ProgressData.TotalCrystals = crystals;
                CrystalsChanged?.Invoke(crystals);
                Heal(1);
                AudioManager.Play(Sfx.PowerUp);
                VFX.PopText(transform.position + Vector3.up * 1.2f, "+1 VIDA!", new Color(0.2f, 1f, 0.4f), 1.2f);
            }
        }

        void Die(string reason)
        {
            if (isDead) return;
            isDead = true;
            var p = GetComponent<PlayerController>();
            if (p != null) p.TriggerDeath(reason);
            else GameManager.Instance?.TriggerGameOver(reason);
        }
    }
}
