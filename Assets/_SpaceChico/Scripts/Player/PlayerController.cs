using System.Collections;
using UnityEngine;

namespace SpaceChico
{
    public enum PlayerState
    {
        Idle, Run, Jump, Fall, Hurt, Dead, Climbing, Celebrate
    }

    // Controlador do Chico estilo Super Mario (correr, pular na cabeça dos inimigos e subir cipó)
    public class PlayerController : MonoBehaviour, IDamageable
    {
        [Header("Movimentação")]
        public float moveSpeed = 7.5f;

        [Header("Pulo")]
        public float jumpForce = 13.8f;
        public float jumpCutMultiplier = 0.5f;
        public float fallGravityMultiplier = 2.2f;

        [Header("Chão e Cipó")]
        public Transform groundCheck;
        public Vector2 groundCheckSize = new Vector2(0.55f, 0.12f);
        public LayerMask groundLayer;
        public float climbSpeed = 4.5f;
        public GameObject climbHintObject;

        [Header("Sprites")]
        public SpriteRenderer spriteRenderer;
        public Sprite spriteIdle;
        public Sprite spriteRunA;
        public Sprite spriteRunB;
        public Sprite spriteJump;
        public Sprite spriteFall;
        public Sprite spriteHurt;
        public Sprite spriteClimb;
        public Sprite spriteCelebrate;
        public float runAnimSpeed = 0.14f;

        public PlayerState State { get; private set; } = PlayerState.Idle;
        public float Facing { get; private set; } = 1f;
        public bool IsGrounded { get; private set; }
        public Vector2 CurrentVelocity => rb != null ? rb.linearVelocity : Vector2.zero;
        public PlayerStats Stats { get; private set; }

        private Rigidbody2D rb;
        private Collider2D mainCollider;
        private float initialGravity;
        private float initialScaleX;
        private float initialScaleY;
        private float runTimer;
        private bool runFrame;
        private float hurtTimer;
        private Vine activeVine;
        private Collider2D ignoredPlatform;

        void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            mainCollider = GetComponent<Collider2D>();
            Stats = GetComponent<PlayerStats>();
            if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();

            initialGravity = rb.gravityScale > 0 ? rb.gravityScale : 3.2f;
            initialScaleX = Mathf.Abs(transform.localScale.x);
            initialScaleY = transform.localScale.y;
        }

        void Start()
        {
            if (groundLayer.value == 0) groundLayer = Layers.GroundMask;
            if (Stats != null) Stats.InvulnerabilityChanged += OnInvulnerabilityChanged;
        }

        void OnDestroy()
        {
            if (Stats != null) Stats.InvulnerabilityChanged -= OnInvulnerabilityChanged;
        }

        void Update()
        {
            if (GameManager.Instance != null && !GameManager.Instance.IsGameplayRunning) return;
            if (State == PlayerState.Dead || State == PlayerState.Celebrate) return;

            if (transform.position.y < -6.5f)
            {
                TriggerDeath("Chico caiu no abismo!");
                return;
            }

            CheckGrounded();

            if (State == PlayerState.Hurt)
            {
                hurtTimer -= Time.deltaTime;
                if (hurtTimer <= 0f) State = IsGrounded ? PlayerState.Idle : PlayerState.Fall;
                return;
            }

            HandleJumpAndClimb();
            UpdateState();
            UpdateSprite();
        }

        void FixedUpdate()
        {
            if (GameManager.Instance != null && !GameManager.Instance.IsGameplayRunning) return;
            if (State == PlayerState.Dead || State == PlayerState.Celebrate || State == PlayerState.Hurt) return;

            if (State == PlayerState.Climbing)
                FixedUpdateClimbing();
            else
                FixedUpdateMovement();
        }

        void CheckGrounded()
        {
            Vector2 pos = groundCheck != null ? (Vector2)groundCheck.position : (Vector2)transform.position + Vector2.down * 0.05f;
            Collider2D[] hits = Physics2D.OverlapBoxAll(pos, groundCheckSize, 0f, groundLayer);

            IsGrounded = false;
            foreach (var hit in hits)
            {
                if (hit != null && hit != ignoredPlatform && rb.linearVelocity.y <= 0.5f && transform.position.y >= hit.bounds.max.y - 0.22f)
                {
                    IsGrounded = true;
                    break;
                }
            }
        }

        void HandleJumpAndClimb()
        {
            if (State != PlayerState.Climbing && IsGrounded && GameInput.Pressed(GameAction.Jump))
                Jump();

            if (GameInput.Released(GameAction.Jump) && rb.linearVelocity.y > 0f)
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * jumpCutMultiplier);

            if (activeVine != null && State != PlayerState.Climbing && Mathf.Abs(GameInput.Vertical) > 0.1f)
            {
                if (GameInput.Vertical < -0.1f && transform.position.y >= activeVine.topY - 0.2f)
                    transform.position = new Vector3(activeVine.transform.position.x, activeVine.topY - 0.35f, transform.position.z);

                State = PlayerState.Climbing;
            }
        }

        void Jump()
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            State = PlayerState.Jump;
            AudioManager.Play(Sfx.Jump);
        }

        // Impulso clássico estilo Mario ao pular na cabeça de um inimigo
        public void BounceFromStomp(float force)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, force);
            State = PlayerState.Jump;
        }

        void FixedUpdateMovement()
        {
            float inputH = GameInput.Horizontal;
            if (Mathf.Abs(inputH) > 0.05f)
            {
                Facing = Mathf.Sign(inputH);
                transform.localScale = new Vector3(initialScaleX * Facing, initialScaleY, 1f);
            }

            rb.gravityScale = (rb.linearVelocity.y < 0f) ? initialGravity * fallGravityMultiplier : initialGravity;
            rb.linearVelocity = new Vector2(inputH * moveSpeed, rb.linearVelocity.y);
        }

        void FixedUpdateClimbing()
        {
            if (activeVine == null) { ExitClimbing(); return; }

            rb.gravityScale = 0f;
            SetPlatformIgnore(activeVine.platformCollider, true);

            float v = GameInput.Vertical;
            float h = GameInput.Horizontal;
            rb.linearVelocity = new Vector2(h * moveSpeed * 0.4f, v * climbSpeed);

            if (v > 0.1f && transform.position.y >= activeVine.topY - 0.08f)
            {
                transform.position = new Vector3(transform.position.x, activeVine.topY + 0.04f, transform.position.z);
                ExitClimbing();
            }
            else if (v < -0.1f && transform.position.y <= activeVine.bottomY + 0.15f)
            {
                ExitClimbing();
            }
            else if (GameInput.Pressed(GameAction.Jump))
            {
                ExitClimbing();
                Jump();
            }
        }

        void ExitClimbing()
        {
            SetPlatformIgnore(null, false);
            rb.gravityScale = initialGravity;
            rb.linearVelocity = Vector2.zero;
            State = PlayerState.Idle;
        }

        void SetPlatformIgnore(Collider2D col, bool ignore)
        {
            if (mainCollider == null) return;
            if (ignoredPlatform != null)
            {
                Physics2D.IgnoreCollision(mainCollider, ignoredPlatform, false);
                ignoredPlatform = null;
            }
            if (ignore && col != null)
            {
                Physics2D.IgnoreCollision(mainCollider, col, true);
                ignoredPlatform = col;
            }
        }

        void UpdateState()
        {
            if (State == PlayerState.Climbing) return;
            if (IsGrounded)
                State = Mathf.Abs(rb.linearVelocity.x) > 0.2f ? PlayerState.Run : PlayerState.Idle;
            else
                State = rb.linearVelocity.y > 0.1f ? PlayerState.Jump : PlayerState.Fall;
        }

        void UpdateSprite()
        {
            if (spriteRenderer == null) return;
            switch (State)
            {
                case PlayerState.Idle:
                    if (spriteIdle != null) spriteRenderer.sprite = spriteIdle;
                    break;
                case PlayerState.Run:
                    runTimer += Time.deltaTime;
                    if (runTimer >= runAnimSpeed)
                    {
                        runTimer = 0f;
                        runFrame = !runFrame;
                        spriteRenderer.sprite = runFrame ? spriteRunB : spriteRunA;
                    }
                    break;
                case PlayerState.Jump:
                    if (spriteJump != null) spriteRenderer.sprite = spriteJump;
                    break;
                case PlayerState.Fall:
                    if (spriteFall != null) spriteRenderer.sprite = spriteFall;
                    break;
                case PlayerState.Hurt:
                    if (spriteHurt != null) spriteRenderer.sprite = spriteHurt;
                    break;
                case PlayerState.Climbing:
                    if (spriteClimb != null) spriteRenderer.sprite = spriteClimb;
                    break;
                case PlayerState.Celebrate:
                    if (spriteCelebrate != null) spriteRenderer.sprite = spriteCelebrate;
                    break;
            }
        }

        public void TakeDamage(int damage, Vector2 hitDirection, GameObject damageSource)
        {
            if (State == PlayerState.Dead || Stats.IsInvulnerable) return;

            Stats.TakeDamage(damage);
            if (Stats.CurrentLife <= 0) return;

            State = PlayerState.Hurt;
            hurtTimer = 0.25f;
            float dirX = Mathf.Abs(hitDirection.x) > 0.1f ? Mathf.Sign(hitDirection.x) : -Facing;
            rb.gravityScale = initialGravity;
            rb.linearVelocity = new Vector2(dirX * 6.5f, 6.0f);
            AudioManager.Play(Sfx.Hurt);
        }

        void OnInvulnerabilityChanged(bool invuln)
        {
            if (invuln) StartCoroutine(FlashInvulnerable());
        }

        IEnumerator FlashInvulnerable()
        {
            while (Stats != null && Stats.IsInvulnerable)
            {
                if (spriteRenderer != null) spriteRenderer.color = new Color(1f, 1f, 1f, 0.35f);
                yield return new WaitForSeconds(0.08f);
                if (spriteRenderer != null) spriteRenderer.color = Color.white;
                yield return new WaitForSeconds(0.08f);
            }
            if (spriteRenderer != null) spriteRenderer.color = Color.white;
        }

        public void TriggerCelebrate() { State = PlayerState.Celebrate; rb.linearVelocity = Vector2.zero; }

        public void TriggerDeath(string reason)
        {
            if (State == PlayerState.Dead) return;
            State = PlayerState.Dead;
            rb.linearVelocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Kinematic;
            if (spriteHurt != null && spriteRenderer != null) spriteRenderer.sprite = spriteHurt;
            if (GameManager.Instance != null) GameManager.Instance.TriggerGameOver(reason);
        }

        public void RegisterVine(Vine v) => activeVine = v;
        public void UnregisterVine(Vine v) { if (activeVine == v) activeVine = null; }
    }
}
