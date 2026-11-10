using System;
using UnityEngine;

namespace SpaceChico
{
    // Controla os power-ups temporários de Escudo e Pulo Duplo (ativados ao coletar na fase)
    public class PlayerPowers : MonoBehaviour
    {
        public GameObject shieldBubbleVisual;

        public bool IsShieldActive { get; private set; }
        public bool IsDoubleJumpActive { get; private set; }

        public float ShieldTimeRemaining { get; private set; }
        public float DoubleJumpTimeRemaining { get; private set; }

        public event Action<PowerType, float, float> PowerActivated;
        public event Action<PowerType> PowerExpired;

        private void Update()
        {
            if (IsShieldActive)
            {
                ShieldTimeRemaining -= Time.deltaTime;
                if (ShieldTimeRemaining <= 0f) DeactivateShield();
            }

            if (IsDoubleJumpActive)
            {
                DoubleJumpTimeRemaining -= Time.deltaTime;
                if (DoubleJumpTimeRemaining <= 0f) DeactivateDoubleJump();
            }
        }

        public void ActivateShield(float duration)
        {
            IsShieldActive = true;
            ShieldTimeRemaining = duration;
            if (shieldBubbleVisual != null) shieldBubbleVisual.SetActive(true);
            PowerActivated?.Invoke(PowerType.Shield, duration, duration);
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
            PowerActivated?.Invoke(PowerType.DoubleJump, duration, duration);
        }

        public void DeactivateDoubleJump()
        {
            IsDoubleJumpActive = false;
            DoubleJumpTimeRemaining = 0f;
            PowerExpired?.Invoke(PowerType.DoubleJump);
        }
    }
}
