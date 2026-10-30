using System.Collections;
using UnityEngine;

namespace SpaceChico
{
    public enum SlimeState
    {
        Patrol,
        Chase,
        Dead
    }

    /// <summary>
    /// Rastejante de Gosma (GDD Sprint 2 e Seção 13).
    /// Inimigo básico da Fase 1 (1 HP, 1 Dano).
    /// FSM com estados Patrol, Chase e Dead.
    /// </summary>
    public class SlimeCrawler : EnemyBase
    {
        [Header("Patrulha e Movimentação")]
        public float patrolSpeed = 1.6f;
        public float chaseSpeed = 2.8f;
        public Transform pointA;
        public Transform pointB;
        public float waypointDistanceThreshold = 0.3f;

        [Header("Detecção (Chase)")]
        public float detectionRadius = 4.5f;
        public LayerMask playerLayer;

        [Header("Sprites e Animação")]
        public Sprite spriteMoveA;
        public Sprite spriteMoveB;
        public Sprite spriteSquished;
        public float animSpeed = 0.3f;

        public SlimeState State { get; private set; } = SlimeState.Patrol;

        private Transform targetPoint;
        private Transform playerTarget;
        private float animTimer;
        private bool animFrame;
        private float initialScaleX;

        protected override void Awake()
        {
            base.Awake();
            maxHealth = 1;
            currentHealth = 1;
            contactDamage = 1;
            initialScaleX = Mathf.Abs(transform.localScale.x);

            // Se os waypoints não forem definidos, cria pontos relativos
            if (pointA == null)
            {
                var goA = new GameObject("Waypoint_A");
                goA.transform.position = transform.position + Vector3.left * 3f;
                pointA = goA.transform;
            }
            if (pointB == null)
            {
                var goB = new GameObject("Waypoint_B");
                goB.transform.position = transform.position + Vector3.right * 3f;
                pointB = goB.transform;
            }

            targetPoint = pointB;
        }

        private void Start()
        {
            if (playerLayer.value == 0)
            {
                playerLayer = LayerMask.GetMask(Layers.Player);
            }
        }

        protected override void Update()
        {
            if (isDead) return;
            base.Update();

            CheckPlayerDetection();

            switch (State)
            {
                case SlimeState.Patrol:
                    UpdatePatrol();
                    break;
                case SlimeState.Chase:
                    UpdateChase();
                    break;
            }

            UpdateAnimation();
        }

        private void CheckPlayerDetection()
        {
            Collider2D hit = Physics2D.OverlapCircle(transform.position, detectionRadius, playerLayer);
            if (hit != null)
            {
                playerTarget = hit.transform;
                if (State != SlimeState.Chase)
                {
                    State = SlimeState.Chase;
                    AudioManager.Play(Sfx.Alert, 0.4f);
                }
            }
            else
            {
                playerTarget = null;
                if (State == SlimeState.Chase)
                {
                    State = SlimeState.Patrol;
                }
            }
        }

        private void UpdatePatrol()
        {
            if (targetPoint == null) return;

            float dist = Mathf.Abs(transform.position.x - targetPoint.position.x);
            if (dist <= waypointDistanceThreshold)
            {
                targetPoint = (targetPoint == pointA) ? pointB : pointA;
            }

            float dir = Mathf.Sign(targetPoint.position.x - transform.position.x);
            Move(dir, patrolSpeed);
        }

        private void UpdateChase()
        {
            if (playerTarget == null)
            {
                State = SlimeState.Patrol;
                return;
            }

            float dir = Mathf.Sign(playerTarget.position.x - transform.position.x);
            Move(dir, chaseSpeed);
        }

        private void Move(float dir, float speed)
        {
            rb.linearVelocity = new Vector2(dir * speed, rb.linearVelocity.y);

            // Inverte o sprite baseado na direção (olhando para a esquerda se dir < 0, ou direita se dir > 0)
            // Como o sprite original olha para a esquerda, se dir > 0 invertemos o scaleX
            if (Mathf.Abs(dir) > 0.01f)
            {
                float sign = (dir > 0) ? -1f : 1f;
                transform.localScale = new Vector3(initialScaleX * sign, transform.localScale.y, transform.localScale.z);
            }
        }

        private void UpdateAnimation()
        {
            if (spriteRenderer == null) return;

            animTimer += Time.deltaTime;
            if (animTimer >= animSpeed)
            {
                animTimer = 0f;
                animFrame = !animFrame;
                if (spriteMoveA != null && spriteMoveB != null)
                {
                    spriteRenderer.sprite = animFrame ? spriteMoveB : spriteMoveA;
                }
            }
        }

        public override void Die()
        {
            if (isDead) return;
            isDead = true;
            State = SlimeState.Dead;

            // Sprint 2.2: Desativa física e colisões
            rb.linearVelocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Kinematic;

            foreach (var col in GetComponentsInChildren<Collider2D>())
            {
                col.enabled = false;
            }

            // Sprint 5.1: Muda para animação Squished
            if (spriteSquished != null && spriteRenderer != null)
            {
                spriteRenderer.sprite = spriteSquished;
            }

            AudioManager.Play(Sfx.EnemyDie);
            VFX.Burst(transform.position, deathColor, 20, 3.8f, 0.28f, 0.6f);

            // Destrói após breve exibição da gosma esmagada
            StartCoroutine(SquishAndFade());
        }

        private IEnumerator SquishAndFade()
        {
            Vector3 originalScale = transform.localScale;
            Vector3 flatScale = new Vector3(originalScale.x * 1.3f, originalScale.y * 0.35f, originalScale.z);

            float elapsed = 0f;
            float duration = 0.5f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                transform.localScale = Vector3.Lerp(originalScale, flatScale, t * 2f);

                if (spriteRenderer != null)
                {
                    Color c = originalColor;
                    c.a = Mathf.Lerp(1f, 0f, t);
                    spriteRenderer.color = c;
                }
                yield return null;
            }

            Destroy(gameObject);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectionRadius);

            if (pointA != null && pointB != null)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(pointA.position, pointB.position);
                Gizmos.DrawWireSphere(pointA.position, 0.2f);
                Gizmos.DrawWireSphere(pointB.position, 0.2f);
            }
        }
    }
}
