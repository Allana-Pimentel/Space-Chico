using System;
using UnityEngine;

namespace SpaceChico
{
    /// <summary>
    /// Gerenciador de Poderes Temporários do Chico.
    /// Controla Escudo (Shield) e Pulo Duplo (DoubleJump).
    /// </summary>
    public class PlayerPowers : MonoBehaviour
    {
        [Header("Efeitos Visuais")]
        public GameObject shieldBubbleVisual;

        public bool IsShieldActive { get; private set; }
        public bool IsDoubleJumpActive { get; private set; }

        public float ShieldTimeRemaining { get; private set; }
        public float DoubleJumpTimeRemaining { get; private set; }

        public event Action<PowerType, float, float> PowerActivated;
        public event Action<PowerType> PowerExpired;

        private PlayerController player;
        private Inventory inventory;

        private void Awake()
        {
            player = GetComponent<PlayerController>();
            inventory = GetComponent<Inventory>();
        }

        private void Update()
        {
            if (GameManager.Instance != null && !GameManager.Instance.IsGameplayRunning) return;

            // Acionamento com Tecla F
            if (GameInput.Pressed(GameAction.Power))
            {
                TryActivateEquippedPower();
            }

            // Atualização dos temporizadores
            if (IsShieldActive)
            {
                ShieldTimeRemaining -= Time.deltaTime;
                if (ShieldTimeRemaining <= 0f)
                {
                    DeactivateShield();
                }
            }

            if (IsDoubleJumpActive)
            {
                DoubleJumpTimeRemaining -= Time.deltaTime;
                if (DoubleJumpTimeRemaining <= 0f)
                {
                    DeactivateDoubleJump();
                }
            }
        }

        public void TryActivateEquippedPower()
        {
            if (inventory == null) return;

            var powerItem = inventory.equippedPower;
            if (powerItem == null)
            {
                powerItem = inventory.FirstPower();
            }

            if (powerItem == null)
            {
                AudioManager.Play(Sfx.Empty);
                return;
            }

            ActivatePower(powerItem);
            inventory.Consume(powerItem);
        }

        public void ActivatePower(ItemData item)
        {
            if (item == null) return;

            float duration = item.powerDuration > 0 ? item.powerDuration : 6f;
            AudioManager.Play(Sfx.PowerActivate);

            switch (item.power)
            {
                case PowerType.Shield:
                    ActivateShield(duration);
                    VFX.PopText(transform.position + Vector3.up, "ESCUDO ATIVADO!", item.accent, 1.1f);
                    break;

                case PowerType.DoubleJump:
                    ActivateDoubleJump(duration);
                    VFX.PopText(transform.position + Vector3.up, "PULO DUPLO ATIVADO!", item.accent, 1.1f);
                    break;
            }

            PowerActivated?.Invoke(item.power, duration, duration);
        }

        public void ActivateShield(float duration)
        {
            IsShieldActive = true;
            ShieldTimeRemaining = duration;
            if (shieldBubbleVisual != null) shieldBubbleVisual.SetActive(true);
            VFX.Burst(transform.position, new Color(0.3f, 0.7f, 1f), 20, 3f, 0.3f, 0.5f);
        }

        public void DeactivateShield()
        {
            IsShieldActive = false;
            ShieldTimeRemaining = 0f;
            if (shieldBubbleVisual != null) shieldBubbleVisual.SetActive(false);
            PowerExpired?.Invoke(PowerType.Shield);
        }

        public void ActivateDoubleJump(float duration)
        {
            IsDoubleJumpActive = true;
            DoubleJumpTimeRemaining = duration;
            VFX.Burst(transform.position, new Color(1f, 0.9f, 0.3f), 15, 3f, 0.25f, 0.5f);
        }

        public void DeactivateDoubleJump()
        {
            IsDoubleJumpActive = false;
            DoubleJumpTimeRemaining = 0f;
            PowerExpired?.Invoke(PowerType.DoubleJump);
        }
    }
}
