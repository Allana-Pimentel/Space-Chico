using UnityEngine;

namespace SpaceChico
{
    public enum PickupType
    {
        Crystal,        // Pontuação (+10 ou +1 cristal)
        Oxygen,         // Restaura barra de oxigênio (+25%)
        NeonFruit,      // Restaura 1 de vida ou oxigênio
        PowerUpShield,  // Adiciona poder de Escudo ao inventário
        PowerUpDoubleJump // Adiciona poder de Pulo Duplo ao inventário
    }

    /// <summary>
    /// Item secundário coletável instantaneamente ao contato (GDD Seção 5).
    /// Não pausa o jogo (TimeScale = 1). Destruído no contato.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class Pickup : MonoBehaviour
    {
        public PickupType type = PickupType.Crystal;
        public int value = 1;
        public ItemData powerItemData; // Para adicionar à mochila caso seja power-up
        public Color burstColor = Color.cyan;

        private bool collected;

        private void Reset()
        {
            var col = GetComponent<Collider2D>();
            if (col != null) col.isTrigger = true;
            gameObject.layer = LayerMask.NameToLayer(Layers.Pickup);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (collected) return;

            var player = other.GetComponent<PlayerController>();
            if (player == null)
            {
                player = other.GetComponentInParent<PlayerController>();
            }

            if (player != null)
            {
                Collect(player);
            }
        }

        private void Collect(PlayerController player)
        {
            collected = true;

            switch (type)
            {
                case PickupType.Crystal:
                    player.Stats.AddCrystals(value);
                    AudioManager.Play(Sfx.Crystal);
                    VFX.PopText(transform.position, $"+{value}", burstColor, 0.9f);
                    break;

                case PickupType.Oxygen:
                    player.Stats.RestoreOxygen(value > 0 ? value : 30f);
                    AudioManager.Play(Sfx.Oxygen);
                    VFX.PopText(transform.position, "+O₂", new Color(0.2f, 0.8f, 1f), 1.0f);
                    break;

                case PickupType.NeonFruit:
                    player.Stats.Heal(value > 0 ? value : 1);
                    AudioManager.Play(Sfx.Pickup);
                    VFX.PopText(transform.position, "+1 HP", new Color(0.2f, 1f, 0.4f), 1.0f);
                    break;

                case PickupType.PowerUpShield:
                case PickupType.PowerUpDoubleJump:
                    if (powerItemData != null && player.Inventory != null)
                    {
                        player.Inventory.Add(powerItemData, 1);
                    }
                    AudioManager.Play(Sfx.PowerUp);
                    string label = type == PickupType.PowerUpShield ? "ESCUDO [F]" : "PULO DUPLO [F]";
                    VFX.PopText(transform.position, label, burstColor, 1.1f);
                    break;
            }

            VFX.Burst(transform.position, burstColor, 14, 3.5f, 0.2f, 0.4f);
            Destroy(gameObject);
        }
    }
}
