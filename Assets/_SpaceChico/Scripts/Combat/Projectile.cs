using UnityEngine;

namespace SpaceChico
{
    /// <summary>
    /// Projétil disparado pela arma do Chico (GDD Sprint 4).
    /// Move-se em velocidade constante e causa 1 de dano ao colidir com inimigos.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public class Projectile : MonoBehaviour
    {
        [Header("Configurações")]
        public float speed = 14f;
        public int damage = 1;
        public float lifeTime = 2.5f;
        public Color trailColor = new Color(0.2f, 0.9f, 1f, 1f);

        private Rigidbody2D rb;
        private float spawnTime;
        private bool hasHit;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            var col = GetComponent<Collider2D>();
            if (col != null) col.isTrigger = true;
        }

        public void Launch(Vector2 direction)
        {
            spawnTime = Time.time;
            direction = direction.normalized;
            rb.linearVelocity = direction * speed;

            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle);
        }

        private void Update()
        {
            if (Time.time - spawnTime >= lifeTime && !hasHit)
            {
                DestroySelf();
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (hasHit) return;

            // Ignora o jogador e outros projéteis
            if (other.CompareTag(Layers.TagPlayer) || other.gameObject.layer == LayerMask.NameToLayer(Layers.Player))
                return;

            // Verifica se atingiu um inimigo (Sprint 4.3: tag "Inimigo" ou componente IDamageable)
            var damageable = other.GetComponent<IDamageable>();
            if (damageable == null)
            {
                damageable = other.GetComponentInParent<IDamageable>();
            }

            if (damageable != null || other.CompareTag(Layers.TagEnemy))
            {
                hasHit = true;
                Vector2 hitDir = rb.linearVelocity.normalized;
                if (damageable != null)
                {
                    damageable.TakeDamage(damage, hitDir, gameObject);
                }

                VFX.Burst(transform.position, trailColor, 12, 3.5f, 0.2f, 0.35f);
                AudioManager.Play(Sfx.EnemyHit);
                DestroySelf();
                return;
            }

            // Se colidiu com o chão/parede
            if (other.CompareTag(Layers.TagGround) || other.gameObject.layer == LayerMask.NameToLayer(Layers.Ground))
            {
                hasHit = true;
                VFX.Burst(transform.position, trailColor * 0.8f, 6, 2f, 0.15f, 0.25f);
                DestroySelf();
            }
        }

        private void DestroySelf()
        {
            Destroy(gameObject);
        }
    }
}
