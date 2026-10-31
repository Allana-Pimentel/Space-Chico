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
        public float moveSpeedPhase1 = 2.2f;
        public float moveSpeedPhase2 = 3.6f;
        public float leapHorizontalSpeed = 6.0f;
        public float leapJumpForce = 13.0f;

        [Header("Ataques")]
        public GameObject spitProjectilePrefab;
        public Transform spitMouthPoint;
        public float spitCooldownPhase1 = 4.2f;
        public float spitCooldownPhase2 = 2.4f;

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

        protected override void Awake()
        {
            maxHealth = 10;
            currentHealth = 10;
            contactDamage = 2;
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

            attackTimer = 2.5f;
            SetSprite(spriteIdle);
        }

        public void ActivateBoss()
        {
            if (state != BossState.Dormant) return;
            StartCoroutine(IntroSequence());
        }

        IEnumerator IntroSequence()
        {
            state = BossState.Intro;
            rb.linearVelocity = Vector2.zero;

            AudioManager.Play(Sfx.BossRoar);
            CameraFollow.Shake(0.45f, 0.35f);
            SetSprite(spriteAttack);

            yield return new WaitForSeconds(1.5f);

            SetSprite(spriteIdle);
            state = BossState.MoveToPlayer;
            attackTimer = 2.0f;
        }

        protected override void Update()
        {
            base.Update();

            if (isDead || state == BossState.Dormant || state == BossState.Intro || state == BossState.EnrageTransition)
                return;

            if (playerTransform == null && GameManager.Instance != null && GameManager.Instance.player != null)
                playerTransform = GameManager.Instance.player.transform;

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
        }

        void UpdateMoveToPlayer()
        {
            if (playerTransform == null) return;

            float dirX = Mathf.Sign(playerTransform.position.x - transform.position.x);
            float speed = isPhase2 ? moveSpeedPhase2 : moveSpeedPhase1;

            if (spriteRenderer != null) spriteRenderer.flipX = dirX > 0f;
            rb.linearVelocity = new Vector2(dirX * speed, rb.linearVelocity.y);

            if (attackTimer <= 0f)
            {
                if (isPhase2 && UnityEngine.Random.value > 0.45f)
                    StartLeapAttack();
                else
                    StartSpitAttack();
            }
        }

        void StartSpitAttack()
        {
            state = BossState.SpitAttack;
            rb.linearVelocity = Vector2.zero;
            stateTimer = 0.9f;
            attackTimer = isPhase2 ? spitCooldownPhase2 : spitCooldownPhase1;

            SetSprite(spriteAttack);
            AudioManager.Play(Sfx.BossSpit);

            if (isPhase2)
                StartCoroutine(FireTripleSpit());
            else
                FireSingleSpit(0f);
        }

        IEnumerator FireTripleSpit()
        {
            FireSingleSpit(-12f);
            yield return new WaitForSeconds(0.2f);
            FireSingleSpit(0f);
            yield return new WaitForSeconds(0.2f);
            FireSingleSpit(12f);
        }

        void FireSingleSpit(float angleOffset)
        {
            if (spitProjectilePrefab == null) return;

            Vector3 spawnPos = spitMouthPoint != null ? spitMouthPoint.position : transform.position + Vector3.up * 1.2f;
            var projObj = Instantiate(spitProjectilePrefab, spawnPos, Quaternion.identity);
            var spit = projObj.GetComponent<BossSpitProjectile>();

            if (spit != null)
            {
                Vector2 targetPos = playerTransform != null ? (Vector2)playerTransform.position : (Vector2)transform.position + Vector2.left * 5f;
                Vector2 toTarget = targetPos - (Vector2)spawnPos;
                float vx = Mathf.Clamp(toTarget.x * 1.8f, -14f, 14f);
                float vy = Mathf.Clamp(Mathf.Abs(toTarget.x) * 0.45f + 5.5f, 5f, 10f);
                Vector2 vel = Quaternion.Euler(0, 0, angleOffset) * new Vector2(vx, vy);
                spit.Launch(vel);
            }
        }

        void StartLeapAttack()
        {
            state = BossState.LeapWindup;
            rb.linearVelocity = Vector2.zero;
            stateTimer = 0.6f;
            attackTimer = spitCooldownPhase2 + 1.2f;
            SetSprite(spriteHurt);
        }

        void ExecuteLeap()
        {
            state = BossState.LeapAirborne;
            float dirX = playerTransform != null ? Mathf.Sign(playerTransform.position.x - transform.position.x) : -1f;
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

            yield return new WaitForSeconds(1.6f);

            SetSprite(spriteIdle);
            state = BossState.MoveToPlayer;
            attackTimer = 1.0f;
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
