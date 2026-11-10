using UnityEngine;

namespace SpaceChico
{
    public enum PickupType
    {
        Crystal = 0,   // Pontuação (+1 cristal, igual moeda do Mario)
        NeonFruit = 2  // Restaura +1 de vida (igual cogumelo do Mario)
    }

    // Item coletável da fase (Cristal ou Fruta de Vida)
    [RequireComponent(typeof(Collider2D))]
    public class Pickup : MonoBehaviour
    {
        public PickupType type = PickupType.Crystal;
        public int value = 1;
        public Color burstColor = Color.cyan;

        private bool collected;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (collected) return;

            var player = other.GetComponent<PlayerController>();
            if (player == null) player = other.GetComponentInParent<PlayerController>();

            if (player != null)
            {
                collected = true;
                if (type == PickupType.NeonFruit)
                {
                    player.Stats.Heal(value > 0 ? value : 1);
                    AudioManager.Play(Sfx.Pickup);
                    VFX.PopText(transform.position, "+1 VIDA", new Color(0.2f, 1f, 0.4f), 1.0f);
                }
                else
                {
                    player.Stats.AddCrystals(value);
                    AudioManager.Play(Sfx.Crystal);
                    VFX.PopText(transform.position, $"+{value}", burstColor, 0.9f);
                }

                VFX.Burst(transform.position, burstColor, 10, 3f, 0.2f, 0.35f);
                Destroy(gameObject);
            }
        }
    }
}
