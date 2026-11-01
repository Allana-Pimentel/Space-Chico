using System;
using System.Collections;
using UnityEngine;

namespace SpaceChico
{
    // Script do Chefe do Pântano (10 de vida, 2 fases de ataque)
    public class BossSwamp : EnemyBase
    {
        public enum BossState
        {
            Dormant, Intro, Idle, MoveToPlayer, SpitAttack,
            LeapWindup, LeapAirborne, LeapSlam, EnrageTransition, Dead
        }

        [Header("Arena")]
        public float arenaMinX = 86f;
        public float arenaMaxX = 114f;
        public Transform playerTransform;

        [Header("Movimentação")]
        public float moveSpeedPhase1 = 2.6f;
        public float moveSpeedPhase2 = 4.0f;
        public float leapHorizontalSpeed = 6.5f;
        public float leapJumpForce = 13.0f;

        [Header("Ataques")]
        public GameObject spitProjectilePrefab;
        public Transform spitMouthPoint;
        public float spitCooldownPhase1 = 2.2f;
        public float spitCooldownPhase2 = 1.4f;

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

        private BossState state = BossState.Dormant;
        private bool isPhase2 = false;
        private float attackTimer;
        private float stateTimer;
        private int leapDirection = -1;
        private float moveDirX = -1f;

        protected override void Awake()
        {
            maxHealth = 10;
            currentHealth = 10;
            contactDamage = 1;
            base.Awake();

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

            attackTimer = 2.0f;
            SetSprite(spriteIdle);
        }

        public void ActivateBoss()
        {
            if (state != BossState.Dormant) return;
            var bar = FindAnyObjectByType<BossHealthBarUI>();
            if (bar != null) bar.Show();
            StartCoroutine(IntroSequence());
        }

        IEnumerator IntroSequence()
        {
            state = BossState.Intro;
            rb.linearVelocity = Vector2.zero;

            FacePlayer();
            AudioManager.Play(Sfx.BossRoar);
            CameraFollow.Shake(0.45f, 0.35f);
            SetSprite(spriteAttack);

            yield return new WaitForSeconds(1.0f);

            // Já dispara a primeira gosma logo ao despertar
            StartSpitAttack();
        }

        protected override void Update()
        {
            base.Update();

            if (playerTransform == null && GameManager.Instance != null && GameManager.Instance.player != null)
                playerTransform = GameManager.Instance.player.transform;

            if (state == BossState.Dormant)
            {
                if (playerTransform != null && Mathf.Abs(playerTransform.position.x - transform.position.x) < 18f)
                    ActivateBoss();
                return;
            }

            if (isDead || state == BossState.Intro || state == BossState.EnrageTransition)
                return;

            attackTimer -= Time.deltaTime;

            switch (state)
            {
                case BossState.MoveToPlayer:
                    UpdateMoveToPlayer();
                    break;
                case BossState.SpitAttack:
                    stateTimer -= Time.deltaTime;
                    if (stateTimer <= 0f)
                    {
                        SetSprite(spriteIdle);
                        state = BossState.MoveToPlayer;
                    }
                    break;
                case BossState.LeapWindup:
                    stateTimer -= Time.deltaTime;
                    if (stateTimer <= 0f) ExecuteLeap();
                    break;
                case BossState.LeapAirborne:
                    rb.linearVelocity = new Vector2(leapDirection * leapHorizontalSpeed, rb.linearVelocity.y);
                    break;
            }

            // Mantém o Boss dentro dos limites da arena
            if (transform.position.x < arenaMinX)
            {
                transform.position = new Vector3(arenaMinX, transform.position.y, transform.position.z);
                moveDirX = 1f;
            }
            else if (transform.position.x > arenaMaxX)
            {
                transform.position = new Vector3(arenaMaxX, transform.position.y, transform.position.z);
                moveDirX = -1f;
            }
        }

        float FacePlayer()
        {
            if (playerTransform != null)
            {
                float dx = playerTransform.position.x - transform.position.x;
                if (Mathf.Abs(dx) > 0.6f) moveDirX = Mathf.Sign(dx);
            }
            if (spriteRenderer != null) spriteRenderer.flipX = moveDirX < 0f;
            return moveDirX;
        }

        void UpdateMoveToPlayer()
        {
            if (playerTransform == null) return;

            float dx = playerTransform.position.x - transform.position.x;
            float dirX = FacePlayer();

            // Se o Chico estiver muito perto ou tentando ficar na mesma posição, o Boss avança com mais força para atropelar
            float baseSpeed = isPhase2 ? moveSpeedPhase2 : moveSpeedPhase1;
            float speed = Mathf.Abs(dx) < 2.2f ? baseSpeed * 1.35f : baseSpeed;
            rb.linearVelocity = new Vector2(dirX * speed, rb.linearVelocity.y);

            // Se o Chico ficar colado no Boss, o Boss salta ou cospe mais rápido
            if (Mathf.Abs(dx) < 1.8f && attackTimer > 0.45f)
            {
                attackTimer = 0.45f;
            }

            if (attackTimer <= 0f)
            {
                if ((isPhase2 || Mathf.Abs(dx) < 2.5f) && UnityEngine.Random.value > 0.45f)
                    StartLeapAttack();
                else
                    StartSpitAttack();
            }
        }

        void StartSpitAttack()
        {
            state = BossState.SpitAttack;
            rb.linearVelocity = Vector2.zero;
            stateTimer = 0.7f;
            attackTimer = isPhase2 ? spitCooldownPhase2 : spitCooldownPhase1;

            FacePlayer();
            SetSprite(spriteAttack);
            AudioManager.Play(Sfx.BossSpit);

            if (isPhase2)
                StartCoroutine(FireTripleSpit());
            else
                FireSingleSpit(0f);
        }

        IEnumerator FireTripleSpit()
        {
            FireSingleSpit(-10f);
            yield return new WaitForSeconds(0.18f);
            FireSingleSpit(0f);
            yield return new WaitForSeconds(0.18f);
            FireSingleSpit(10f);
        }

        void FireSingleSpit(float angleOffset)
        {
            if (spitProjectilePrefab == null) return;

            float dirX = FacePlayer();
            Vector3 spawnPos = transform.position + new Vector3(dirX * 1.45f, 1.35f, 0f);
            var projObj = Instantiate(spitProjectilePrefab, spawnPos, Quaternion.identity);
            var spit = projObj.GetComponent<BossSpitProjectile>();

            VFX.Burst(spawnPos, new Color(0.3f, 1f, 0.2f), 10, 3.2f, 0.2f, 0.3f);

            if (spit != null)
            {
                Vector2 targetPos = playerTransform != null ? (Vector2)playerTransform.position : (Vector2)transform.position + new Vector2(dirX * 7f, 0f);
                Vector2 toTarget = targetPos - (Vector2)spawnPos;
                float vx = dirX * Mathf.Clamp(Mathf.Abs(toTarget.x) * 1.35f, 7.5f, 13.5f);
                float vy = Mathf.Clamp(toTarget.y * 0.6f + 4.8f, 3.5f, 8.5f);
                Vector2 vel = Quaternion.Euler(0, 0, angleOffset) * new Vector2(vx, vy);
                spit.Launch(vel);
            }
        }

        void StartLeapAttack()
        {
            state = BossState.LeapWindup;
            rb.linearVelocity = Vector2.zero;
            stateTimer = 0.45f;
            attackTimer = (isPhase2 ? spitCooldownPhase2 : spitCooldownPhase1) + 0.8f;
            SetSprite(spriteHurt);
        }

        void ExecuteLeap()
        {
            state = BossState.LeapAirborne;
            float dirX = FacePlayer();
            leapDirection = (int)dirX;

            SetSprite(spriteAttack);
            rb.linearVelocity = new Vector2(leapDirection * leapHorizontalSpeed, leapJumpForce);
            AudioManager.Play(Sfx.Jump);
        }

        protected override void OnCollisionEnter2D(Collision2D collision)
        {
            base.OnCollisionEnter2D(collision);

            if (state == BossState.LeapAirborne)
            {
                if (collision.gameObject.CompareTag(Layers.TagGround) || collision.gameObject.layer == LayerMask.NameToLayer(Layers.Ground))
                {
                    state = BossState.MoveToPlayer;
                    SetSprite(spriteIdle);
                    AudioManager.Play(Sfx.BossSlam);
                    CameraFollow.Shake(0.4f, 0.35f);
                }
            }
        }

        public override void TakeDamage(int damage, Vector2 hitDirection, GameObject damageSource)
        {
            if (isDead || state == BossState.Dormant || state == BossState.Intro || state == BossState.EnrageTransition)
                return;

            currentHealth -= damage;
            FlashHit();
            HealthChanged?.Invoke(currentHealth, maxHealth);

            if (!isPhase2 && currentHealth <= 5 && currentHealth > 0)
            {
                StartCoroutine(TriggerEnragePhase2());
                return;
            }

            if (currentHealth <= 0) Die();
        }

        IEnumerator TriggerEnragePhase2()
        {
            isPhase2 = true;
            state = BossState.EnrageTransition;
            rb.linearVelocity = Vector2.zero;

            Phase2Started?.Invoke();
            AudioManager.Play(Sfx.BossRoar);
            CameraFollow.Shake(0.6f, 0.5f);
            SetSprite(spriteAttack);

            if (spriteRenderer != null)
            {
                spriteRenderer.color = new Color(1f, 0.45f, 0.2f);
                originalColor = new Color(1f, 0.85f, 0.7f);
            }

            yield return new WaitForSeconds(1.4f);

            SetSprite(spriteIdle);
            state = BossState.MoveToPlayer;
            attackTimer = 0.8f;
        }

        public override void Die()
        {
            if (isDead) return;
            isDead = true;
            state = BossState.Dead;
            rb.linearVelocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Kinematic;

            foreach (var c in GetComponentsInChildren<Collider2D>()) c.enabled = false;
            StartCoroutine(DefeatSequence());
        }

        IEnumerator DefeatSequence()
        {
            SetSprite(spriteHurt);
            AudioManager.Play(Sfx.EnemyDie);
            CameraFollow.Shake(0.7f, 0.6f);

            yield return new WaitForSeconds(0.8f);

            BossDefeated?.Invoke();
            if (arenaBarrier != null) arenaBarrier.SetActive(false);

            Vector3 rewardPos = shipPartSpawnPoint != null ? shipPartSpawnPoint.position : transform.position + Vector3.up * 1.5f;
            if (shipPartPrefab != null)
            {
                Instantiate(shipPartPrefab, rewardPos, Quaternion.identity);
                AudioManager.Play(Sfx.ShipPart);
            }

            yield return new WaitForSeconds(1.2f);
            Destroy(gameObject);
        }

        void SetSprite(Sprite s)
        {
            if (s != null && spriteRenderer != null) spriteRenderer.sprite = s;
        }
    }
}
