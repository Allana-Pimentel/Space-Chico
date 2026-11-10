using UnityEngine;

namespace SpaceChico
{
    // Peça final da nave (igual à Estrela do Mario no fim da fase)
    [RequireComponent(typeof(BoxCollider2D))]
    public class ShipPart : MonoBehaviour
    {
        public Color glowColor = new Color(0.2f, 0.9f, 1f, 1f);
        public GameObject visualObject;

        private bool collected;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (collected) return;

            var player = other.GetComponent<PlayerController>();
            if (player == null) player = other.GetComponentInParent<PlayerController>();

            if (player != null)
            {
                collected = true;
                player.TriggerCelebrate();

                AudioManager.Play(Sfx.ShipPart);
                VFX.Burst(transform.position, glowColor, 20, 4.5f, 0.3f, 0.6f);
                VFX.PopText(transform.position + Vector3.up, "FASE CONCLUÍDA!", glowColor, 1.2f);

                ProgressData.BonusMaxLife += 1;
                player.Stats.IncreaseMaxLife(1);
                GameManager.Instance.CompleteLevel();

                Destroy(gameObject, 0.1f);
            }
        }
    }
}
