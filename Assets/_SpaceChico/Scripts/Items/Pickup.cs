using UnityEngine;

namespace SpaceChico
{
    public enum PickupType
    {
        Crystal,          // Pontuação (+1 cristal)
        Oxygen,           // Restaura barra de oxigênio
        NeonFruit,        // Restaura 1 de vida
        PowerUpShield,    // Ativa e adiciona Escudo
        PowerUpDoubleJump // Ativa e adiciona Pulo Duplo
    }

    // Item coletável instantaneamente ao encostar
    [RequireComponent(typeof(Collider2D))]
    public class Pickup : MonoBehaviour
    {
        public PickupType type = PickupType.Crystal;
        public int value = 1;
        public ItemData powerItemData;
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
                    VFX.PopText(transform.position, "+O2", new Color(0.2f, 0.8f, 1f), 1.0f);
                    break;

                case PickupType.NeonFruit:
                    player.Stats.Heal(value > 0 ? value : 1);
                    AudioManager.Play(Sfx.Pickup);
                    VFX.PopText(transform.position, "+1 VIDA", new Color(0.2f, 1f, 0.4f), 1.0f);
                    break;

                case PickupType.PowerUpShield:
                    if (powerItemData != null && player.Inventory != null)
                    {
                        player.Inventory.Add(powerItemData, 1);
                        player.Inventory.Equip(powerItemData);
                    }
                    if (player.Powers != null)
                    {
                        player.Powers.ActivateShield(15f);
                    }
                    AudioManager.Play(Sfx.PowerUp);
                    VFX.PopText(transform.position, "ESCUDO ATIVO!", burstColor, 1.1f);
                    break;

                case PickupType.PowerUpDoubleJump:
                    if (powerItemData != null && player.Inventory != null)
                    {
                        player.Inventory.Add(powerItemData, 1);
                        player.Inventory.Equip(powerItemData);
                    }
                    if (player.Powers != null)
                    {
                        player.Powers.ActivateDoubleJump(45f);
                    }
                    player.EnableDoubleJump();
                    AudioManager.Play(Sfx.PowerUp);
                    VFX.PopText(transform.position, "PULO DUPLO ATIVO!", burstColor, 1.1f);
                    break;
            }

            VFX.Burst(transform.position, burstColor, 14, 3.5f, 0.2f, 0.4f);
            Destroy(gameObject);
        }
    }
}
