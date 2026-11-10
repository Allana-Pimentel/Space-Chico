using System.Collections;
using UnityEngine;

namespace SpaceChico
{
    public enum PlayerState { Idle, Run, Jump, Fall, Hurt, Dead, Climbing, Celebrate }

    public class PlayerController : MonoBehaviour, IDamageable
    {
        public float moveSpeed = 7.5f;
        public float jumpForce = 13.8f;
        public float jumpCutMultiplier = 0.5f;
        public float fallGravityMultiplier = 2.2f;

        public Transform groundCheck;
        public Vector2 groundCheckSize = new Vector2(0.55f, 0.12f);
        public LayerMask groundLayer;
        public float climbSpeed = 4.5f;
        public GameObject climbHintObject;

        public SpriteRenderer spriteRenderer;
        public Sprite spriteIdle, spriteRunA, spriteRunB, spriteJump, spriteFall, spriteHurt, spriteClimb, spriteCelebrate;
        public float runAnimSpeed = 0.14f;

        public PlayerState State { get; private set; } = PlayerState.Idle;
        public float Facing { get; private set; } = 1f;
        public bool IsGrounded { get; private set; }
        public Vector2 CurrentVelocity => rb != null ? rb.linearVelocity : Vector2.zero;
        public PlayerStats Stats { get; private set; }

        private Rigidbody2D rb;
        private Collider2D mainCollider;
        private float initialGravity, initialScaleX, initialScaleY, runTimer, hurtTimer;
        private bool runFrame;
        private bool canDoubleJump;
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
            if (transform.position.y < -6.5f) { TriggerDeath("Chico caiu no abismo!"); return; }

            CheckGrounded();
            if (State == PlayerState.Hurt)
            {
                if ((hurtTimer -= Time.deltaTime) <= 0f) State = IsGrounded ? PlayerState.Idle : PlayerState.Fall;
                return;
            }

            if (State != PlayerState.Climbing && GameInput.Pressed(GameAction.Jump))
            {
                if (IsGrounded)
                {
                    Jump();
                }
                else if (canDoubleJump)
                {
                    canDoubleJump = false;
                    Jump();
                    VFX.Burst(transform.position, new Color(0.3f, 1f, 0.85f), 12, 3.5f, 0.2f, 0.3f);
                }
            }
            if (GameInput.Released(GameAction.Jump) && rb.linearVelocity.y > 0f)
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * jumpCutMultiplier);

            if (activeVine != null && State != PlayerState.Climbing && Mathf.Abs(GameInput.Vertical) > 0.1f)
            {
                if (GameInput.Vertical < -0.1f && transform.position.y >= activeVine.topY - 0.2f)
                    transform.position = new Vector3(activeVine.transform.position.x, activeVine.topY - 0.35f, transform.position.z);
                State = PlayerState.Climbing;
            }

            if (State != PlayerState.Climbing)
                State = IsGrounded ? (Mathf.Abs(rb.linearVelocity.x) > 0.2f ? PlayerState.Run : PlayerState.Idle)
                                   : (rb.linearVelocity.y > 0.1f ? PlayerState.Jump : PlayerState.Fall);
            UpdateSprite();

            if (climbHintObject != null)
            {
                bool show = activeVine != null && State != PlayerState.Climbing && State != PlayerState.Dead && State != PlayerState.Hurt;
                if (climbHintObject.activeSelf != show) climbHintObject.SetActive(show);
            }
        }

        void FixedUpdate()
        {
            if (GameManager.Instance != null && !GameManager.Instance.IsGameplayRunning) return;
            if (State == PlayerState.Dead || State == PlayerState.Celebrate || State == PlayerState.Hurt) return;

            if (State == PlayerState.Climbing) FixedUpdateClimbing();
            else FixedUpdateMovement();
        }

        void CheckGrounded()
        {
            Vector2 pos = groundCheck != null ? (Vector2)groundCheck.position : (Vector2)transform.position + Vector2.down * 0.05f;
            IsGrounded = false;
            foreach (var hit in Physics2D.OverlapBoxAll(pos, groundCheckSize, 0f, groundLayer))
            {
                if (hit != null && hit != ignoredPlatform && rb.linearVelocity.y <= 0.5f && transform.position.y >= hit.bounds.max.y - 0.22f)
                {
                    IsGrounded = true;
                    canDoubleJump = true;
                    break;
                }
            }
        }

        void Jump()
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            State = PlayerState.Jump;
            AudioManager.Play(Sfx.Jump);
        }

        public void BounceFromStomp(float force)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, force);
            State = PlayerState.Jump;
            canDoubleJump = true;
        }

        void FixedUpdateMovement()
        {
            float h = GameInput.Horizontal;
            if (Mathf.Abs(h) > 0.05f)
            {
                Facing = Mathf.Sign(h);
                transform.localScale = new Vector3(initialScaleX * Facing, initialScaleY, 1f);
            }
            rb.gravityScale = (rb.linearVelocity.y < 0f) ? initialGravity * fallGravityMultiplier : initialGravity;
            rb.linearVelocity = new Vector2(h * moveSpeed, rb.linearVelocity.y);
        }

        void FixedUpdateClimbing()
        {
            if (activeVine == null) { ExitClimbing(); return; }
            rb.gravityScale = 0f;
            SetPlatformIgnore(activeVine.platformCollider, true);

            float v = GameInput.Vertical, h = GameInput.Horizontal;
            rb.linearVelocity = new Vector2(h * moveSpeed * 0.4f, v * climbSpeed);

            if (v > 0.1f && transform.position.y >= activeVine.topY - 0.08f)
            {
                transform.position = new Vector3(transform.position.x, activeVine.topY + 0.04f, transform.position.z);
                ExitClimbing();
            }
            else if (v < -0.1f && transform.position.y <= activeVine.bottomY + 0.15f) ExitClimbing();
            else if (GameInput.Pressed(GameAction.Jump)) { ExitClimbing(); Jump(); }
        }

        void ExitClimbing()
        {
            SetPlatformIgnore(null, false);
            if (spriteRenderer != null) spriteRenderer.flipX = false;
            rb.gravityScale = initialGravity;
            rb.linearVelocity = Vector2.zero;
            canDoubleJump = true;
            State = PlayerState.Idle;
        }

        void SetPlatformIgnore(Collider2D col, bool ignore)
        {
            if (mainCollider == null) return;
            if (ignoredPlatform != null) { Physics2D.IgnoreCollision(mainCollider, ignoredPlatform, false); ignoredPlatform = null; }
            if (ignore && col != null) { Physics2D.IgnoreCollision(mainCollider, col, true); ignoredPlatform = col; }
        }

        void UpdateSprite()
        {
            if (spriteRenderer == null) return;
            if (State == PlayerState.Run)
            {
                if ((runTimer += Time.deltaTime) >= runAnimSpeed) { runTimer = 0f; runFrame = !runFrame; }
                spriteRenderer.sprite = runFrame ? spriteRunB : spriteRunA;
            }
            else if (State == PlayerState.Idle && spriteIdle) spriteRenderer.sprite = spriteIdle;
            else if (State == PlayerState.Jump && spriteJump) spriteRenderer.sprite = spriteJump;
            else if (State == PlayerState.Fall && spriteFall) spriteRenderer.sprite = spriteFall;
            else if (State == PlayerState.Hurt && spriteHurt) spriteRenderer.sprite = spriteHurt;
            else if (State == PlayerState.Climbing && spriteClimb)
            {
                spriteRenderer.sprite = spriteClimb;
                spriteRenderer.flipX = Mathf.Abs(GameInput.Vertical) > 0.1f && (Mathf.Sin(Time.time * 12f) > 0f);
            }
            else if (State == PlayerState.Celebrate && spriteCelebrate) spriteRenderer.sprite = spriteCelebrate;
        }

        public void TakeDamage(int damage, Vector2 hitDir, GameObject source)
        {
            if (State == PlayerState.Dead || Stats.IsInvulnerable) return;
            Stats.TakeDamage(damage);
            if (Stats.CurrentLife <= 0) return;

            State = PlayerState.Hurt;
            hurtTimer = 0.25f;
            float dirX = Mathf.Abs(hitDir.x) > 0.1f ? Mathf.Sign(hitDir.x) : -Facing;
            rb.gravityScale = initialGravity;
            rb.linearVelocity = new Vector2(dirX * 6.5f, 6.0f);
            AudioManager.Play(Sfx.Hurt);
        }

        void OnInvulnerabilityChanged(bool invuln) { if (invuln) StartCoroutine(FlashInvulnerable()); }

        IEnumerator FlashInvulnerable()
        {
            while (Stats != null && Stats.IsInvulnerable)
            {
                if (spriteRenderer) spriteRenderer.color = new Color(1f, 1f, 1f, 0.35f);
                yield return new WaitForSeconds(0.08f);
                if (spriteRenderer) spriteRenderer.color = Color.white;
                yield return new WaitForSeconds(0.08f);
            }
            if (spriteRenderer) spriteRenderer.color = Color.white;
        }

        public void TriggerCelebrate() { State = PlayerState.Celebrate; rb.linearVelocity = Vector2.zero; }

        public void TriggerDeath(string reason)
        {
            if (State == PlayerState.Dead) return;
            State = PlayerState.Dead;
            rb.linearVelocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Kinematic;
            if (spriteHurt && spriteRenderer) spriteRenderer.sprite = spriteHurt;
            if (GameManager.Instance != null) GameManager.Instance.TriggerGameOver(reason);
        }

        public void RegisterVine(Vine v) => activeVine = v;
        public void UnregisterVine(Vine v) { if (activeVine == v) activeVine = null; }
    }
}
