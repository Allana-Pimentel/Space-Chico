using UnityEngine;

namespace SpaceChico
{
    // Classe base para todos os inimigos (Lesma e Boss)
    [RequireComponent(typeof(Rigidbody2D))]
    public abstract class EnemyBase : MonoBehaviour, IDamageable
    {
        public int maxHealth = 1, currentHealth = 1, contactDamage = 1;
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
            if (hitFlashTimer > 0f && (hitFlashTimer -= Time.deltaTime) <= 0f && spriteRenderer != null)
                spriteRenderer.color = originalColor;
        }

        protected virtual void FixedUpdate()
        {
            if (isDead || bodyCollider == null) return;
            PlayerController player = GameManager.Instance?.player;
            if (player == null || player.State == PlayerState.Dead) return;

            Bounds eb = bodyCollider.bounds;
            Vector3 pPos = player.transform.position;
            float margemX = eb.extents.x + 0.42f;
            float dx = pPos.x - eb.center.x;

            if (Mathf.Abs(dx) < margemX && pPos.y >= eb.min.y - 0.15f && pPos.y <= eb.max.y + 0.35f)
            {
                if (player.CurrentVelocity.y < -1.5f && pPos.y >= eb.max.y - 0.15f) return;
                float dirX = Mathf.Abs(dx) > 0.05f ? Mathf.Sign(dx) : -player.Facing;
                player.TakeDamage(contactDamage, new Vector2(dirX, 0.65f).normalized, gameObject);
            }
        }

        public virtual void TakeDamage(int damage, Vector2 hitDirection, GameObject damageSource)
        {
            if (isDead) return;
            currentHealth -= damage;
            FlashHit();
            if (currentHealth <= 0) Die();
        }

        protected virtual void FlashHit()
        {
            if (spriteRenderer != null) { spriteRenderer.color = hitColor; hitFlashTimer = 0.12f; }
        }

        public abstract void Die();

        protected virtual void OnCollisionEnter2D(Collision2D c) => TryAttackPlayer(c.gameObject);
        protected virtual void OnCollisionStay2D(Collision2D c) => TryAttackPlayer(c.gameObject);

        protected void TryAttackPlayer(GameObject obj)
        {
            if (isDead || obj == null) return;
            var player = obj.GetComponent<PlayerController>() ?? obj.GetComponentInParent<PlayerController>();
            if (player == null) return;

            float topoY = bodyCollider != null ? bodyCollider.bounds.max.y : transform.position.y + 0.5f;
            if (player.CurrentVelocity.y < -1.5f && player.transform.position.y >= topoY - 0.15f) return;

            float dx = player.transform.position.x - transform.position.x;
            float dirX = Mathf.Abs(dx) > 0.05f ? Mathf.Sign(dx) : -player.Facing;
            player.TakeDamage(contactDamage, new Vector2(dirX, 0.65f).normalized, gameObject);
        }
    }
}
