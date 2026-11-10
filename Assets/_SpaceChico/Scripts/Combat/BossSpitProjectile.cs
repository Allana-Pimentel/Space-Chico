using UnityEngine;

namespace SpaceChico
{
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public class BossSpitProjectile : MonoBehaviour
    {
        public float speed = 11f, lifeTime = 5.0f;
        public int damage = 1;
        public Color slimeColor = new Color(0.2f, 1f, 0.3f, 1f);

        private Rigidbody2D rb;

        void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            rb.gravityScale = 1.2f;
            Destroy(gameObject, lifeTime);
        }

        public void Launch(Vector2 velocity) => rb.linearVelocity = velocity;

        void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag(Layers.TagEnemy) || other.gameObject.layer == LayerMask.NameToLayer(Layers.Enemy)) return;

            if (other.CompareTag(Layers.TagPlayer) || other.gameObject.layer == LayerMask.NameToLayer(Layers.Player))
            {
                var dmg = other.GetComponent<IDamageable>() ?? other.GetComponentInParent<IDamageable>();
                if (dmg != null)
                {
                    dmg.TakeDamage(damage, Vector2.up, gameObject);
                    VFX.Burst(transform.position, slimeColor, 10, 3.5f, 0.2f, 0.35f);
                    Destroy(gameObject);
                }
            }
            else if (other.CompareTag(Layers.TagGround) || other.gameObject.layer == LayerMask.NameToLayer(Layers.Ground))
            {
                VFX.Burst(transform.position, slimeColor, 8, 2.5f, 0.18f, 0.25f);
                Destroy(gameObject);
            }
        }
    }
}
