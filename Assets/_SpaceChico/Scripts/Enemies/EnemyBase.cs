using UnityEngine;

namespace SpaceChico
{
    // Classe base para todos os inimigos (Lesma e Boss)
    [RequireComponent(typeof(Rigidbody2D))]
    public abstract class EnemyBase : MonoBehaviour, IDamageable
    {
        [Header("Estatísticas Básicas")]
        public int maxHealth = 1;
        public int currentHealth = 1;
        public int contactDamage = 1;

        [Header("Feedback Visual")]
        public SpriteRenderer spriteRenderer;
        public Color hitColor = new Color(1f, 0.4f, 0.4f, 1f);
        public Color deathColor = new Color(0.3f, 1f, 0.5f, 0.9f);

        protected Rigidbody2D rb;
        protected Collider2D bodyCollider;
        protected bool isDead;
        protected Color originalColor = Color.white;
        protected float hitFlashTimer;

        public bool IsDead => isDead;

        protected virtual void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            bodyCollider = GetComponent<Collider2D>();
            if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            if (spriteRenderer != null) originalColor = spriteRenderer.color;
            currentHealth = maxHealth;
            gameObject.tag = Layers.TagEnemy;

            int enemyLayer = LayerMask.NameToLayer(Layers.Enemy);
            if (enemyLayer >= 0) gameObject.layer = enemyLayer;
        }

        protected virtual void Update()
        {
            if (hitFlashTimer > 0f)
            {
                hitFlashTimer -= Time.deltaTime;
                if (hitFlashTimer <= 0f && spriteRenderer != null)
                {
                    spriteRenderer.color = originalColor;
                }
            }
        }

        protected virtual void FixedUpdate()
        {
            if (isDead || bodyCollider == null) return;

            PlayerController player = GameManager.Instance != null ? GameManager.Instance.player : null;
            if (player == null || player.State == PlayerState.Dead) return;

            Bounds eb = bodyCollider.bounds;
            Vector3 pPos = player.transform.position;

            // Verifica se o jogador está encostando, sobreposto ou parado em cima do inimigo
            float margemX = eb.extents.x + 0.42f;
            float dx = pPos.x - eb.center.x;
            bool dentroX = Mathf.Abs(dx) < margemX;
            bool dentroY = pPos.y >= eb.min.y - 0.15f && pPos.y <= eb.max.y + 0.35f;

            if (dentroX && dentroY)
            {
                // Se o jogador estiver caindo rápido de cima para esmagar o WeakPoint, deixa o StompFoot agir
                bool caindoParaStomp = player.CurrentVelocity.y < -1.5f && pPos.y >= eb.max.y - 0.15f;
                if (!caindoParaStomp)
                {
                    // Ataca o jogador imediatamente
                    float dirX = Mathf.Abs(dx) > 0.05f ? Mathf.Sign(dx) : -player.Facing;
                    Vector2 hitDir = new Vector2(dirX, 0.65f).normalized;
                    player.TakeDamage(contactDamage, hitDir, gameObject);

                    // Impede que o jogador fique parado em cima da cabeça do inimigo ou se sobreponha a ele
                    Rigidbody2D prb = player.GetComponent<Rigidbody2D>();
                    if (prb != null)
                    {
                        float alvoX = eb.center.x + dirX * (margemX + 0.08f);
                        float novoX = Mathf.MoveTowards(pPos.x, alvoX, 14f * Time.fixedDeltaTime);
                        player.transform.position = new Vector3(novoX, pPos.y, pPos.z);
                        if (Mathf.Sign(prb.linearVelocity.x) != dirX || Mathf.Abs(prb.linearVelocity.x) < 4.5f)
                        {
                            prb.linearVelocity = new Vector2(dirX * 6.0f, Mathf.Max(prb.linearVelocity.y, 3.5f));
                        }
                    }
                }
            }
        }

        public virtual void TakeDamage(int damage, Vector2 hitDirection, GameObject damageSource)
        {
            if (isDead) return;

            currentHealth -= damage;
            FlashHit();

            if (currentHealth <= 0)
            {
                Die();
            }
            else if (rb != null)
            {
                rb.AddForce(hitDirection * 3f, ForceMode2D.Impulse);
            }
        }

        protected virtual void FlashHit()
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.color = hitColor;
                hitFlashTimer = 0.12f;
            }
        }

        public abstract void Die();

        protected virtual void OnCollisionEnter2D(Collision2D collision)
        {
            TryAttackPlayer(collision.gameObject);
        }

        protected virtual void OnCollisionStay2D(Collision2D collision)
        {
            TryAttackPlayer(collision.gameObject);
        }

        protected void TryAttackPlayer(GameObject obj)
        {
            if (isDead || obj == null) return;

            PlayerController player = obj.GetComponent<PlayerController>();
            if (player == null) player = obj.GetComponentInParent<PlayerController>();

            if (player != null)
            {
                float topoY = bodyCollider != null ? bodyCollider.bounds.max.y : transform.position.y + 0.5f;
                if (player.CurrentVelocity.y < -1.5f && player.transform.position.y >= topoY - 0.15f)
                {
                    return;
                }

                float dx = player.transform.position.x - transform.position.x;
                float dirX = Mathf.Abs(dx) > 0.05f ? Mathf.Sign(dx) : -player.Facing;
                Vector2 hitDir = new Vector2(dirX, 0.65f).normalized;
                player.TakeDamage(contactDamage, hitDir, gameObject);
            }
        }
    }
}
