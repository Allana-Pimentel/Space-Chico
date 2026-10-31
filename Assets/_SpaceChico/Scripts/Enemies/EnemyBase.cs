using System;
using UnityEngine;

namespace SpaceChico
{
    /// <summary>
    /// Classe base para inimigos alienígenas no Space Chico.
    /// Implementa IDamageable, tratamento de vida, knockback, flash de dano e colisão com o jogador.
    /// </summary>
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
        protected bool isDead;
        protected Color originalColor = Color.white;
        protected float hitFlashTimer;

        public bool IsDead => isDead;

        protected virtual void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            if (spriteRenderer != null) originalColor = spriteRenderer.color;
            currentHealth = maxHealth;
            gameObject.tag = Layers.TagEnemy;
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

        public virtual void TakeDamage(int damage, Vector2 hitDirection, GameObject damageSource)
        {
            if (isDead) return;

            currentHealth -= damage;
            FlashHit();

            if (currentHealth <= 0)
            {
                Die();
            }
            else
            {
                // Leve empurrão
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

        /// <summary>
        /// Colisão lateral com o jogador (): retira vida do jogador caso não seja no ponto fraco.
        /// </summary>
        protected virtual void OnCollisionEnter2D(Collision2D collision)
        {
            if (isDead) return;

            if (collision.gameObject.CompareTag(Layers.TagPlayer) || collision.gameObject.layer == LayerMask.NameToLayer(Layers.Player))
            {
                var damageable = collision.gameObject.GetComponent<IDamageable>();
                if (damageable != null)
                {
                    Vector2 hitDir = (collision.transform.position - transform.position).normalized;
                    if (hitDir.sqrMagnitude < 0.01f) hitDir = Vector2.up + Vector2.right;
                    damageable.TakeDamage(contactDamage, hitDir, gameObject);
                }
            }
        }
    }
}
