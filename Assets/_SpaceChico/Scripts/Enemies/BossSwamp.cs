using System;
using System.Collections;
using UnityEngine;

namespace SpaceChico
{
    // Chefe do Pântano estilo Super Mario: patrulha/salta na arena e é derrotado pulando 3 vezes na cabeça dele
    public class BossSwamp : EnemyBase
    {
        [Header("Arena")]
        public float arenaMinX = 86f;
        public float arenaMaxX = 114f;
        public Transform playerTransform;

        [Header("Movimentação")]
        public float moveSpeedPhase1 = 3.0f;
        public float moveSpeedPhase2 = 4.4f;
        public float leapHorizontalSpeed = 6.0f;
        public float leapJumpForce = 12.5f;

        [Header("Ataques")]
        public GameObject spitProjectilePrefab;
        public Transform spitMouthPoint;
        public float spitCooldownPhase1 = 2.2f;
        public float spitCooldownPhase2 = 1.5f;

        [Header("Sprites")]
        public Sprite spriteIdle;
        public Sprite spriteAttack;
        public Sprite spriteHurt;

        [Header("Recompensa")]
        public WeakPoint weakPoint;
        public GameObject shipPartPrefab;
        public Transform shipPartSpawnPoint;
        public GameObject arenaBarrier;

        public event Action<int, int> HealthChanged;
        public event Action Phase2Started;
        public event Action BossDefeated;
        public bool IsPhase2 => isPhase2;

        private bool active = false;
        private bool isPhase2 = false;
        private float actionTimer = 1.8f;
        private float animTimer;
        private bool animFrame;
        private Vector3 baseScale;

        protected override void Awake()
        {
            // 6 de vida = exatamente 3 pulos na cabeça (cada stomp tira 2 de vida, estilo Mario!)
            maxHealth = 6;
            currentHealth = 6;
            contactDamage = 1;
            base.Awake();

            baseScale = transform.localScale;
            if (weakPoint == null) weakPoint = GetComponentInChildren<WeakPoint>();
            if (weakPoint != null)
            {
                weakPoint.stompDamage = 2;
                weakPoint.bounceForce = 14.5f;
            }
        }

        void Start()
        {
            if (playerTransform == null && GameManager.Instance != null && GameManager.Instance.player != null)
                playerTransform = GameManager.Instance.player.transform;

            if (spriteIdle != null && spriteRenderer != null)
                spriteRenderer.sprite = spriteIdle;
        }

        public void ActivateBoss()
        {
            if (active) return;
            active = true;
            var bar = FindAnyObjectByType<BossHealthBarUI>();
            if (bar != null) bar.Show();
            AudioManager.Play(Sfx.BossRoar);
        }

        protected override void Update()
        {
            base.Update();
            if (isDead) return;

            if (playerTransform == null && GameManager.Instance != null && GameManager.Instance.player != null)
                playerTransform = GameManager.Instance.player.transform;

            if (!active)
            {
                if (playerTransform != null && Mathf.Abs(playerTransform.position.x - transform.position.x) < 18f)
                    ActivateBoss();
                return;
            }

            if (playerTransform == null) return;

            // Caminha em direção ao Chico dentro da arena
            float dx = playerTransform.position.x - transform.position.x;
            float dirX = Mathf.Abs(dx) > 0.4f ? Mathf.Sign(dx) : -1f;
            if (spriteRenderer != null) spriteRenderer.flipX = dirX < 0f;

            float speed = isPhase2 ? moveSpeedPhase2 : moveSpeedPhase1;
            rb.linearVelocity = new Vector2(dirX * speed, rb.linearVelocity.y);

            // Animação simples de caminhada alternando os sprites e leve balanço
            animTimer += Time.deltaTime;
            if (animTimer >= (isPhase2 ? 0.16f : 0.24f))
            {
                animTimer = 0f;
                animFrame = !animFrame;
                if (spriteRenderer != null && spriteIdle != null && spriteAttack != null)
                    spriteRenderer.sprite = animFrame ? spriteAttack : spriteIdle;
            }
            float squash = Mathf.Sin(Time.time * 10f) * 0.08f;
            transform.localScale = new Vector3(baseScale.x * (1f + squash), baseScale.y * (1f - squash), 1f);

            // A cada intervalo, dá um pulo pesado ou cospe uma bola de lodo
            actionTimer -= Time.deltaTime;
            if (actionTimer <= 0f)
            {
                actionTimer = isPhase2 ? spitCooldownPhase2 : spitCooldownPhase1;
                if (Mathf.Abs(rb.linearVelocity.y) < 0.2f && UnityEngine.Random.value > 0.45f)
                {
                    rb.linearVelocity = new Vector2(dirX * leapHorizontalSpeed, leapJumpForce);
                    AudioManager.Play(Sfx.Jump);
                }
                else if (spitProjectilePrefab != null)
                {
                    Vector3 pos = transform.position + new Vector3(dirX * 1.4f, 1.3f, 0f);
                    var go = Instantiate(spitProjectilePrefab, pos, Quaternion.identity);
                    var spit = go.GetComponent<BossSpitProjectile>();
                    if (spit != null) spit.Launch(new Vector2(dirX * 9.5f, 4.8f));
                    AudioManager.Play(Sfx.BossSpit);
                }
            }

            // Limites da arena
            float clampedX = Mathf.Clamp(transform.position.x, arenaMinX, arenaMaxX);
            transform.position = new Vector3(clampedX, transform.position.y, transform.position.z);
        }

        public override void TakeDamage(int damage, Vector2 hitDirection, GameObject damageSource)
        {
            if (isDead || !active) return;

            currentHealth -= damage;
            FlashHit();
            HealthChanged?.Invoke(currentHealth, maxHealth);

            if (!isPhase2 && currentHealth <= 2 && currentHealth > 0)
            {
                isPhase2 = true;
                originalColor = new Color(1f, 0.75f, 0.6f);
                Phase2Started?.Invoke();
                AudioManager.Play(Sfx.BossRoar);
            }

            if (currentHealth <= 0) Die();
        }

        public override void Die()
        {
            if (isDead) return;
            isDead = true;
            rb.linearVelocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Kinematic;

            foreach (var c in GetComponentsInChildren<Collider2D>()) c.enabled = false;
            StartCoroutine(DefeatSequence());
        }

        IEnumerator DefeatSequence()
        {
            if (spriteHurt != null && spriteRenderer != null) spriteRenderer.sprite = spriteHurt;
            AudioManager.Play(Sfx.EnemyDie);
            yield return new WaitForSeconds(0.7f);

            BossDefeated?.Invoke();
            if (arenaBarrier != null) arenaBarrier.SetActive(false);

            Vector3 rewardPos = shipPartSpawnPoint != null ? shipPartSpawnPoint.position : transform.position + Vector3.up * 1.2f;
            if (shipPartPrefab != null)
            {
                Instantiate(shipPartPrefab, rewardPos, Quaternion.identity);
                AudioManager.Play(Sfx.ShipPart);
            }
            Destroy(gameObject, 0.5f);
        }
    }
}
