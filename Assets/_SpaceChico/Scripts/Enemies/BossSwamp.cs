using System;
using System.Collections;
using UnityEngine;

namespace SpaceChico
{
    // Chefe do Pântano estilo Super Mario: derrotado pulando 3 vezes na cabeça dele
    public class BossSwamp : EnemyBase
    {
        public float arenaMinX = 86f, arenaMaxX = 114f;
        public Transform playerTransform;
        public float moveSpeedPhase1 = 3.0f, moveSpeedPhase2 = 4.4f;
        public float leapHorizontalSpeed = 6.0f, leapJumpForce = 12.5f;
        public GameObject spitProjectilePrefab;
        public Transform spitMouthPoint;
        public float spitCooldownPhase1 = 2.2f, spitCooldownPhase2 = 1.5f;
        public Sprite spriteIdle, spriteAttack, spriteHurt;
        public WeakPoint weakPoint;
        public GameObject shipPartPrefab, arenaBarrier;
        public Transform shipPartSpawnPoint;

        public event Action<int, int> HealthChanged;
        public event Action Phase2Started;
        public event Action BossDefeated;
        public bool IsPhase2 => isPhase2;

        private bool active, isPhase2, animFrame;
        private float actionTimer = 1.8f, animTimer;
        private Vector3 baseScale;

        protected override void Awake()
        {
            maxHealth = 6; // 3 pulos na cabeça (2 de dano cada)
            currentHealth = 6;
            contactDamage = 1;
            base.Awake();
            baseScale = transform.localScale;
            if (weakPoint == null) weakPoint = GetComponentInChildren<WeakPoint>();
            if (weakPoint != null) { weakPoint.stompDamage = 2; weakPoint.bounceForce = 14.5f; }
        }

        void Start()
        {
            if (playerTransform == null && GameManager.Instance != null && GameManager.Instance.player != null)
                playerTransform = GameManager.Instance.player.transform;
            if (spriteIdle && spriteRenderer) spriteRenderer.sprite = spriteIdle;
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
            if (playerTransform == null && GameManager.Instance?.player != null)
                playerTransform = GameManager.Instance.player.transform;

            if (!active)
            {
                if (playerTransform != null && Mathf.Abs(playerTransform.position.x - transform.position.x) < 18f) ActivateBoss();
                return;
            }
            if (playerTransform == null) return;

            float dx = playerTransform.position.x - transform.position.x;
            float dirX = Mathf.Abs(dx) > 0.4f ? Mathf.Sign(dx) : -1f;
            if (spriteRenderer) spriteRenderer.flipX = dirX < 0f;

            rb.linearVelocity = new Vector2(dirX * (isPhase2 ? moveSpeedPhase2 : moveSpeedPhase1), rb.linearVelocity.y);

            if ((animTimer += Time.deltaTime) >= (isPhase2 ? 0.16f : 0.24f))
            {
                animTimer = 0f;
                animFrame = !animFrame;
                if (spriteRenderer && spriteIdle && spriteAttack) spriteRenderer.sprite = animFrame ? spriteAttack : spriteIdle;
            }
            float sq = Mathf.Sin(Time.time * 10f) * 0.08f;
            transform.localScale = new Vector3(baseScale.x * (1f + sq), baseScale.y * (1f - sq), 1f);

            if ((actionTimer -= Time.deltaTime) <= 0f)
            {
                actionTimer = isPhase2 ? spitCooldownPhase2 : spitCooldownPhase1;
                if (Mathf.Abs(rb.linearVelocity.y) < 0.2f && UnityEngine.Random.value > 0.45f)
                {
                    rb.linearVelocity = new Vector2(dirX * leapHorizontalSpeed, leapJumpForce);
                    AudioManager.Play(Sfx.Jump);
                }
                else if (spitProjectilePrefab != null)
                {
                    var go = Instantiate(spitProjectilePrefab, transform.position + new Vector3(dirX * 1.4f, 1.3f, 0f), Quaternion.identity);
                    go.GetComponent<BossSpitProjectile>()?.Launch(new Vector2(dirX * 9.5f, 4.8f));
                    AudioManager.Play(Sfx.BossSpit);
                }
            }

            transform.position = new Vector3(Mathf.Clamp(transform.position.x, arenaMinX, arenaMaxX), transform.position.y, transform.position.z);
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
            if (spriteHurt && spriteRenderer) spriteRenderer.sprite = spriteHurt;
            AudioManager.Play(Sfx.EnemyDie);
            yield return new WaitForSeconds(0.7f);
            BossDefeated?.Invoke();
            if (arenaBarrier) arenaBarrier.SetActive(false);
            if (shipPartPrefab)
            {
                Instantiate(shipPartPrefab, shipPartSpawnPoint ? shipPartSpawnPoint.position : transform.position + Vector3.up * 1.2f, Quaternion.identity);
                AudioManager.Play(Sfx.ShipPart);
            }
            Destroy(gameObject, 0.5f);
        }
    }
}
