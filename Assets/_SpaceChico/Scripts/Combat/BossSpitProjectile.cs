using UnityEngine;

namespace SpaceChico
{
    /// <summary>
    /// Projétil de lodo tóxico disparado pelo Chefe do Pântano.
    /// Voa em arco parabólico (com gravidade) ou em linha reta e causa 1 de dano ao Chico.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public class BossSpitProjectile : MonoBehaviour
    {
        [Header("Configurações")]
        public float speed = 11f;
        public int damage = 1;
        public float lifeTime = 5.0f;
        public Color slimeColor = new Color(0.2f, 1f, 0.3f, 1f);

        private Rigidbody2D rb;
        private float spawnTime;
        private bool hasHit;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            rb.gravityScale = 1.2f; // Arco parabólico realista de cuspe
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            var col = GetComponent<Collider2D>();
            if (col != null) col.isTrigger = true;
        }

        public void Launch(Vector2 velocity)
        {
            spawnTime = Time.time;
            rb.linearVelocity = velocity;
        }

        private void Update()
        {
            if (Time.time - spawnTime >= lifeTime && !hasHit)
            {
                DestroySelf();
            }

            // Gira o projétil na direção da velocidade
            if (rb.linearVelocity.sqrMagnitude > 0.1f)
            {
                float angle = Mathf.Atan2(rb.linearVelocity.y, rb.linearVelocity.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Euler(0, 0, angle);
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (hasHit) return;

            // Ignora o chefe e inimigos
            if (other.CompareTag(Layers.TagEnemy) || other.gameObject.layer == LayerMask.NameToLayer(Layers.Enemy) || other.gameObject.layer == LayerMask.NameToLayer(Layers.EnemyTrigger))
                return;

            // Atingiu o jogador
            if (other.CompareTag(Layers.TagPlayer) || other.gameObject.layer == LayerMask.NameToLayer(Layers.Player))
            {
                var damageable = other.GetComponent<IDamageable>();
                if (damageable == null) damageable = other.GetComponentInParent<IDamageable>();

                if (damageable != null)
                {
                    hasHit = true;
                    Vector2 hitDir = rb.linearVelocity.normalized;
                    if (hitDir.sqrMagnitude < 0.01f) hitDir = Vector2.up + Vector2.right;
                    damageable.TakeDamage(damage, hitDir, gameObject);

                    VFX.Burst(transform.position, slimeColor, 16, 4.0f, 0.25f, 0.4f);
                    CameraFollow.Shake(0.15f, 0.12f);
                    DestroySelf();
                    return;
                }
            }

            // Colidiu com o chão
            if (other.CompareTag(Layers.TagGround) || other.gameObject.layer == LayerMask.NameToLayer(Layers.Ground))
            {
                hasHit = true;
                VFX.Burst(transform.position, slimeColor, 10, 2.5f, 0.2f, 0.3f);
                DestroySelf();
            }
        }

        private void DestroySelf()
        {
            Destroy(gameObject);
        }
    }
}
