using System;
using System.Collections;
using UnityEngine;

namespace SpaceChico
{
    /// <summary>
    /// Chefe do Pântano (GDD Seção 13).
    /// Vida: 10 HP. Dano de contato: 2.
    /// Fase 1 (10 a 6 HP): Patrulha pesada e cuspe de lodo tóxico balístico.
    /// Fase 2 (5 a 1 HP - Enfurecido): Transição com rugido e tremor, velocidade ampliada,
    /// salto esmagador com onda de choque e disparo triplo de lodo.
    /// Derrota: Concede o Propulsor Quântico para restauração da nave.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class BossSwamp : EnemyBase
    {
        public enum BossState
        {
            Dormant,
            Intro,
            Idle,
            MoveToPlayer,
            SpitAttack,
            LeapWindup,
            LeapAirborne,
            LeapSlam,
            EnrageTransition,
            Dead
        }

        [Header("Arena & Limites")]
        public float arenaMinX = 86f;
        public float arenaMaxX = 114f;
        public Transform playerTransform;

        [Header("Movimentação")]
        public float moveSpeedPhase1 = 2.2f;
        public float moveSpeedPhase2 = 3.6f;
        public float leapHorizontalSpeed = 6.0f;
        public float leapJumpForce = 13.0f;

        [Header("Ataques & Projéteis")]
        public GameObject spitProjectilePrefab;
        public Transform spitMouthPoint;
        public float spitCooldownPhase1 = 4.2f;
        public float spitCooldownPhase2 = 2.4f;

        [Header("Sprites do Chefe")]
        public Sprite spriteIdle;
        public Sprite spriteAttack;
        public Sprite spriteHurt;

        [Header("Ponto Fraco & Recompensa")]
        public WeakPoint weakPoint;
        public GameObject shipPartPrefab;
        public Transform shipPartSpawnPoint;
        public GameObject arenaBarrier;

        public event Action<int, int> HealthChanged;
        public event Action Phase2Started;
        public event Action BossDefeated;

        private BossState state = BossState.Dormant;
        private bool isPhase2 = false;
        private float attackTimer;
        private float stateTimer;
        private int leapDirection = -1;

        public bool IsPhase2 => isPhase2;
        public BossState State => state;

        protected override void Awake()
        {
            maxHealth = 10;
            currentHealth = 10;
            contactDamage = 2; // GDD Seção 13: 2 de dano por colisão
            base.Awake();

            if (weakPoint == null) weakPoint = GetComponentInChildren<WeakPoint>();
            if (weakPoint != null)
            {
                weakPoint.stompDamage = 2; // 2 de dano no pisão
                weakPoint.bounceForce = 14.5f; // Salto alto após pisar no chefe gigante
            }
        }

        private void Start()
        {
            if (playerTransform == null && GameManager.Instance != null && GameManager.Instance.player != null)
            {
                playerTransform = GameManager.Instance.player.transform;
            }

            attackTimer = 2.5f;
            SetSprite(spriteIdle);
        }

        public void ActivateBoss()
        {
            if (state != BossState.Dormant) return;

            StartCoroutine(IntroSequence());
        }

        private IEnumerator IntroSequence()
        {
            state = BossState.Intro;
            rb.linearVelocity = Vector2.zero;

            // Rugido de despertar
            AudioManager.Play(Sfx.BossRoar);
            CameraFollow.Shake(0.45f, 0.35f);
            SetSprite(spriteAttack);
            VFX.Burst(transform.position + Vector3.up * 1.5f, new Color(0.2f, 1f, 0.4f), 24, 6f, 0.4f, 0.6f);

            yield return new WaitForSeconds(1.6f);

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
            {
                playerTransform = GameManager.Instance.player.transform;
            }

            attackTimer -= Time.deltaTime;

            switch (state)
            {
                case BossState.MoveToPlayer:
                    UpdateMoveToPlayer();
                    break;

                case BossState.SpitAttack:
                    // Espera término da animação do ataque
                    stateTimer -= Time.deltaTime;
                    if (stateTimer <= 0f)
                    {
                        SetSprite(spriteIdle);
                        state = BossState.MoveToPlayer;
                    }
                    break;

                case BossState.LeapWindup:
                    stateTimer -= Time.deltaTime;
                    if (stateTimer <= 0f)
                    {
                        ExecuteLeap();
                    }
                    break;

                case BossState.LeapAirborne:
                    // Enquanto no ar, move horizontalmente
                    rb.linearVelocity = new Vector2(leapDirection * leapHorizontalSpeed, rb.linearVelocity.y);
                    // Quando começar a cair e tocar no chão
                    if (rb.linearVelocity.y < 0.1f && Mathf.Abs(rb.linearVelocity.y) < 0.2f)
                    {
                        // Quase no chão
                    }
                    break;
            }
        }

        private void UpdateMoveToPlayer()
        {
            if (playerTransform == null) return;

            float dirX = Mathf.Sign(playerTransform.position.x - transform.position.x);
            float currentSpeed = isPhase2 ? moveSpeedPhase2 : moveSpeedPhase1;

            // Vira o sprite para o jogador
            if (spriteRenderer != null)
            {
                spriteRenderer.flipX = dirX > 0f;
            }

            // Move horizontalmente respeitando limites da arena
            float newX = transform.position.x + dirX * currentSpeed * Time.deltaTime;
            newX = Mathf.Clamp(newX, arenaMinX + 1.5f, arenaMaxX - 1.5f);
            rb.linearVelocity = new Vector2(dirX * currentSpeed, rb.linearVelocity.y);

            // Seleção de ataque
            if (attackTimer <= 0f)
            {
                if (isPhase2 && UnityEngine.Random.value > 0.45f)
                {
                    StartLeapAttack();
                }
                else
                {
                    StartSpitAttack();
                }
            }
        }

        private void StartSpitAttack()
        {
            state = BossState.SpitAttack;
            rb.linearVelocity = Vector2.zero;
            stateTimer = 0.9f;
            attackTimer = isPhase2 ? spitCooldownPhase2 : spitCooldownPhase1;

            SetSprite(spriteAttack);
            AudioManager.Play(Sfx.BossSpit);

            if (isPhase2)
            {
                // Disparo triplo
                StartCoroutine(FireTripleSpit());
            }
            else
            {
                // Disparo único balístico
                FireSingleSpit(0f);
            }
        }

        private IEnumerator FireTripleSpit()
        {
            FireSingleSpit(-12f);
            yield return new WaitForSeconds(0.2f);
            FireSingleSpit(0f);
            yield return new WaitForSeconds(0.2f);
            FireSingleSpit(+12f);
        }

        private void FireSingleSpit(float angleOffset)
        {
            if (spitProjectilePrefab == null) return;

            Vector3 spawnPos = spitMouthPoint != null ? spitMouthPoint.position : transform.position + Vector3.up * 1.2f;
            var projObj = Instantiate(spitProjectilePrefab, spawnPos, Quaternion.identity);
            var spit = projObj.GetComponent<BossSpitProjectile>();

            if (spit != null)
            {
                Vector2 targetPos = playerTransform != null ? (Vector2)playerTransform.position : (Vector2)transform.position + Vector2.left * 5f;
                Vector2 toTarget = targetPos - (Vector2)spawnPos;
                float dirX = Mathf.Sign(toTarget.x);

                // Cálculo de velocidade balística
                float vx = Mathf.Clamp(toTarget.x * 1.8f, -14f, 14f);
                float vy = Mathf.Clamp(Mathf.Abs(toTarget.x) * 0.45f + 5.5f, 5f, 10f);

                // Aplica offset de ângulo
                Vector2 vel = Quaternion.Euler(0, 0, angleOffset) * new Vector2(vx, vy);
                spit.Launch(vel);
            }

            VFX.Burst(spawnPos, new Color(0.2f, 1f, 0.3f), 8, 2.5f, 0.15f, 0.25f);
        }

        private void StartLeapAttack()
        {
            state = BossState.LeapWindup;
            rb.linearVelocity = Vector2.zero;
            stateTimer = 0.6f;
            attackTimer = spitCooldownPhase2 + 1.2f;

            // Agacha para pegar impulso
            SetSprite(spriteHurt);
            CameraFollow.Shake(0.12f, 0.1f);
        }

        private void ExecuteLeap()
        {
            state = BossState.LeapAirborne;
            float dirX = playerTransform != null ? Mathf.Sign(playerTransform.position.x - transform.position.x) : -1f;
            leapDirection = (int)dirX;

            SetSprite(spriteAttack);
            rb.linearVelocity = new Vector2(leapDirection * leapHorizontalSpeed, leapJumpForce);

            AudioManager.Play(Sfx.Jump);
            VFX.Burst(transform.position, new Color(0.4f, 0.8f, 0.3f), 12, 3.5f, 0.2f, 0.3f);
        }

        protected override void OnCollisionEnter2D(Collision2D collision)
        {
            base.OnCollisionEnter2D(collision);

            // Aterrissagem do Salto Esmagador
            if (state == BossState.LeapAirborne)
            {
                if (collision.gameObject.CompareTag(Layers.TagGround) || collision.gameObject.layer == LayerMask.NameToLayer(Layers.Ground))
                {
                    OnLeapSlamLanded();
                }
            }
        }

        private void OnLeapSlamLanded()
        {
            state = BossState.MoveToPlayer;
            SetSprite(spriteIdle);

            // Impacto e tremor colossal
            AudioManager.Play(Sfx.BossSlam);
            CameraFollow.Shake(0.4f, 0.35f);

            // Ondas de partículas tóxicas para ambos os lados
            VFX.Burst(transform.position, new Color(0.2f, 1f, 0.3f), 24, 6f, 0.3f, 0.5f);
            VFX.PopText(transform.position + Vector3.up * 2f, "SLAM!", new Color(1f, 0.5f, 0.2f), 1.2f);
        }

        public override void TakeDamage(int damage, Vector2 hitDirection, GameObject damageSource)
        {
            if (isDead || state == BossState.Dormant || state == BossState.Intro || state == BossState.EnrageTransition)
                return;

            currentHealth -= damage;
            FlashHit();
            HealthChanged?.Invoke(currentHealth, maxHealth);

            VFX.PopText(transform.position + Vector3.up * 2.2f, $"-{damage}", new Color(1f, 0.3f, 0.3f), 1.0f);

            // Transição para Fase 2 ao atingir 5 HP (GDD Seção 13)
            if (!isPhase2 && currentHealth <= 5 && currentHealth > 0)
            {
                StartCoroutine(TriggerEnragePhase2());
                return;
            }

            if (currentHealth <= 0)
            {
                Die();
            }
        }

        private IEnumerator TriggerEnragePhase2()
        {
            isPhase2 = true;
            state = BossState.EnrageTransition;
            rb.linearVelocity = Vector2.zero;

            Phase2Started?.Invoke();

            // Rugido dramático e tremor
            AudioManager.Play(Sfx.BossRoar);
            CameraFollow.Shake(0.6f, 0.5f);
            SetSprite(spriteAttack);

            // Aura avermelhada / laranja enfurecida
            if (spriteRenderer != null)
            {
                spriteRenderer.color = new Color(1f, 0.45f, 0.2f);
                originalColor = new Color(1f, 0.85f, 0.7f);
            }

            VFX.Burst(transform.position + Vector3.up * 1.5f, new Color(1f, 0.3f, 0.1f), 35, 7f, 0.4f, 0.7f);
            VFX.PopText(transform.position + Vector3.up * 2.8f, "ENFURECIDO!", new Color(1f, 0.2f, 0.2f), 1.8f);

            yield return new WaitForSeconds(1.8f);

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

            // Desativa colisores
            var cols = GetComponentsInChildren<Collider2D>();
            foreach (var c in cols) c.enabled = false;

            StartCoroutine(DefeatSequence());
        }

        private IEnumerator DefeatSequence()
        {
            SetSprite(spriteHurt);
            AudioManager.Play(Sfx.EnemyDie);
            CameraFollow.Shake(0.7f, 0.6f);

            // Múltiplas explosões de vitória
            for (int i = 0; i < 4; i++)
            {
                Vector3 offset = new Vector3(UnityEngine.Random.Range(-1.5f, 1.5f), UnityEngine.Random.Range(0.2f, 2.0f), 0f);
                VFX.Burst(transform.position + offset, new Color(0.3f, 1f, 0.5f), 20, 5f, 0.3f, 0.5f);
                yield return new WaitForSeconds(0.22f);
            }

            VFX.PopText(transform.position + Vector3.up * 2.5f, "VITÓRIA!", new Color(1f, 0.9f, 0.2f), 2.5f);
            BossDefeated?.Invoke();

            // Desbloqueia barreira da arena
            if (arenaBarrier != null)
            {
                arenaBarrier.SetActive(false);
            }

            // Spawna o Propulsor Quântico como recompensa triunfal
            Vector3 rewardPos = shipPartSpawnPoint != null ? shipPartSpawnPoint.position : transform.position + Vector3.up * 1.5f;
            if (shipPartPrefab != null)
            {
                var part = Instantiate(shipPartPrefab, rewardPos, Quaternion.identity);
                VFX.Burst(rewardPos, new Color(1f, 0.85f, 0.2f), 30, 6f, 0.5f, 0.8f);
                AudioManager.Play(Sfx.ShipPart);
            }

            // Destrói o corpo do chefe após a comemoração
            yield return new WaitForSeconds(1.5f);
            Destroy(gameObject);
        }

        private void SetSprite(Sprite s)
        {
            if (s != null && spriteRenderer != null)
            {
                spriteRenderer.sprite = s;
            }
        }
    }
}
