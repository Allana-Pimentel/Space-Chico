using UnityEngine;

namespace SpaceChico
{
    public enum SlimeState { Patrol, Chase, Dead }

    public class SlimeCrawler : EnemyBase
    {
        public float patrolSpeed = 1.8f, chaseSpeed = 3.0f;
        public Transform pointA, pointB;
        public float waypointDistanceThreshold = 0.3f, detectionRadius = 5.0f;
        public LayerMask playerLayer;
        public Sprite spriteMoveA, spriteMoveB, spriteSquished;
        public float animSpeed = 0.25f;

        private Transform targetPoint;
        private float animTimer, initialScaleX, moveDir = 1f;
        private bool animFrame;

        protected override void Awake()
        {
            base.Awake();
            initialScaleX = Mathf.Abs(transform.localScale.x);
            if (pointA == null) { var a = new GameObject("WP_A"); a.transform.position = transform.position + Vector3.left * 3f; pointA = a.transform; }
            if (pointB == null) { var b = new GameObject("WP_B"); b.transform.position = transform.position + Vector3.right * 3f; pointB = b.transform; }
            targetPoint = pointB;
        }

        public void SetPatrolRange(float minX, float maxX)
        {
            if (pointA != null) pointA.position = new Vector3(minX, transform.position.y, 0f);
            if (pointB != null) pointB.position = new Vector3(maxX, transform.position.y, 0f);
            targetPoint = pointB;
        }

        void Start()
        {
            if (playerLayer.value == 0) playerLayer = LayerMask.GetMask(Layers.Player);
        }

        protected override void Update()
        {
            if (isDead) return;
            base.Update();

            Collider2D hit = Physics2D.OverlapCircle(transform.position, detectionRadius, playerLayer);
            if (hit != null)
            {
                float dx = hit.transform.position.x - transform.position.x;
                if (Mathf.Abs(dx) > 0.35f) moveDir = Mathf.Sign(dx);
                Move(moveDir, chaseSpeed);
            }
            else if (targetPoint != null)
            {
                if (Mathf.Abs(transform.position.x - targetPoint.position.x) <= waypointDistanceThreshold)
                    targetPoint = (targetPoint == pointA) ? pointB : pointA;
                moveDir = Mathf.Sign(targetPoint.position.x - transform.position.x);
                Move(moveDir, patrolSpeed);
            }

            if (spriteRenderer != null && (animTimer += Time.deltaTime) >= animSpeed)
            {
                animTimer = 0f;
                animFrame = !animFrame;
                if (spriteMoveA && spriteMoveB) spriteRenderer.sprite = animFrame ? spriteMoveB : spriteMoveA;
            }
        }

        void Move(float dir, float speed)
        {
            rb.linearVelocity = new Vector2(dir * speed, rb.linearVelocity.y);
            if (Mathf.Abs(dir) > 0.01f)
                transform.localScale = new Vector3(initialScaleX * (dir > 0 ? -1f : 1f), transform.localScale.y, transform.localScale.z);
        }

        public override void Die()
        {
            if (isDead) return;
            isDead = true;
            rb.linearVelocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Kinematic;
            foreach (var col in GetComponentsInChildren<Collider2D>()) col.enabled = false;
            if (spriteSquished && spriteRenderer) spriteRenderer.sprite = spriteSquished;
            AudioManager.Play(Sfx.EnemyDie);
            Destroy(gameObject, 0.45f);
        }
    }
}
