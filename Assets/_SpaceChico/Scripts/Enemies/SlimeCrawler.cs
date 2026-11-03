using UnityEngine;

namespace SpaceChico
{
    public enum SlimeState { Patrol, Chase, Dead }

    // Inimigo básico que patrulha entre dois pontos e persegue/ataca o Chico
    public class SlimeCrawler : EnemyBase
    {
        [Header("Patrulha")]
        public float patrolSpeed = 1.8f;
        public float chaseSpeed = 3.2f;
        public Transform pointA;
        public Transform pointB;
        public float waypointDistanceThreshold = 0.3f;

        [Header("Detecção")]
        public float detectionRadius = 5.0f;
        public LayerMask playerLayer;

        [Header("Sprites")]
        public Sprite spriteMoveA;
        public Sprite spriteMoveB;
        public Sprite spriteSquished;
        public float animSpeed = 0.25f;

        public SlimeState State { get; private set; } = SlimeState.Patrol;

        private Transform targetPoint;
        private Transform playerTarget;
        private float animTimer;
        private bool animFrame;
        private float initialScaleX;
        private float moveDir = 1f;

        protected override void Awake()
        {
            base.Awake();
            maxHealth = 1;
            currentHealth = 1;
            contactDamage = 1;
            initialScaleX = Mathf.Abs(transform.localScale.x);

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

        public void SetPatrolRange(float minX, float maxX)
        {
            var goA = new GameObject(name + "_WaypointA");
            goA.transform.position = new Vector3(minX, transform.position.y, 0f);
            pointA = goA.transform;

            var goB = new GameObject(name + "_WaypointB");
            goB.transform.position = new Vector3(maxX, transform.position.y, 0f);
            pointB = goB.transform;

            targetPoint = pointB;
        }

        void Start()
        {
            if (playerLayer.value == 0)
                playerLayer = LayerMask.GetMask(Layers.Player);
        }

        protected override void Update()
        {
            if (isDead) return;
            base.Update();

            // Verifica se o jogador está perto para perseguir e atacar
            Collider2D hit = Physics2D.OverlapCircle(transform.position, detectionRadius, playerLayer);
            if (hit != null)
            {
                playerTarget = hit.transform;
                State = SlimeState.Chase;
            }
            else
            {
                playerTarget = null;
                State = SlimeState.Patrol;
            }

            if (State == SlimeState.Patrol)
                UpdatePatrol();
            else if (State == SlimeState.Chase)
                UpdateChase();

            UpdateAnimation();
        }

        void UpdatePatrol()
        {
            if (targetPoint == null) return;

            if (Mathf.Abs(transform.position.x - targetPoint.position.x) <= waypointDistanceThreshold)
                targetPoint = (targetPoint == pointA) ? pointB : pointA;

            moveDir = Mathf.Sign(targetPoint.position.x - transform.position.x);
            Move(moveDir, patrolSpeed);
        }

        void UpdateChase()
        {
            if (playerTarget == null) return;

            float dx = playerTarget.position.x - transform.position.x;
            // Só inverte a direção quando o jogador está realmente de um lado (evita travar na mesma posição X!)
            if (Mathf.Abs(dx) > 0.35f)
            {
                moveDir = Mathf.Sign(dx);
            }

            // Quando está bem perto do Chico, dá um bote mais rápido para atacar
            float speed = Mathf.Abs(dx) < 1.2f ? chaseSpeed * 1.25f : chaseSpeed;
            Move(moveDir, speed);
        }

        void Move(float dir, float speed)
        {
            rb.linearVelocity = new Vector2(dir * speed, rb.linearVelocity.y);
            if (Mathf.Abs(dir) > 0.01f)
            {
                float sign = (dir > 0) ? -1f : 1f;
                transform.localScale = new Vector3(initialScaleX * sign, transform.localScale.y, transform.localScale.z);
            }
        }

        void UpdateAnimation()
        {
            if (spriteRenderer == null) return;
            animTimer += Time.deltaTime;
            if (animTimer >= animSpeed)
            {
                animTimer = 0f;
                animFrame = !animFrame;
                if (spriteMoveA != null && spriteMoveB != null)
                    spriteRenderer.sprite = animFrame ? spriteMoveB : spriteMoveA;
            }
        }

        public override void Die()
        {
            if (isDead) return;
            isDead = true;
            State = SlimeState.Dead;

            rb.linearVelocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Kinematic;

            foreach (var col in GetComponentsInChildren<Collider2D>())
                col.enabled = false;

            if (spriteSquished != null && spriteRenderer != null)
                spriteRenderer.sprite = spriteSquished;

            AudioManager.Play(Sfx.EnemyDie);
            Destroy(gameObject, 0.45f);
        }
    }
}
