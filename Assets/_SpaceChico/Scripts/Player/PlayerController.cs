using System.Collections;
using UnityEngine;

namespace SpaceChico
{
    public enum PlayerState
    {
        Idle, Run, Jump, Fall, Hurt, Dead, Climbing, Crouch, Celebrate
    }

    // Controlador principal do jogador (movimentação, pulo, agachar e subir cipó)
    public class PlayerController : MonoBehaviour, IDamageable
    {
        [Header("Movimentação")]
        public float moveSpeed = 7.5f;
        public float acceleration = 45f;
        public float deceleration = 55f;

        [Header("Pulo")]
        public float jumpForce = 13.5f;
        public float jumpCutMultiplier = 0.5f;
        public float fallGravityMultiplier = 2.4f;
        public float coyoteTime = 0.12f;
        public float jumpBufferTime = 0.15f;

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
        public Inventory Inventory { get; private set; }
        public PlayerShooter Shooter { get; private set; }
        public PlayerPowers Powers { get; private set; }

        private Rigidbody2D rb;
        private Collider2D mainCollider;
        private float initialGravityScale;
        private float coyoteTimer;
        private float jumpBufferTimer;
        private bool canDoubleJump;
        private float runAnimTimer;
        private bool runAnimFrame;
        private Vine activeVine;
        private float hurtTimer;
        private float shootPoseTimer;
        private float initialScaleX;
        private float initialScaleY;
        private float initialColliderHeight = 1.4f;
        private float initialColliderOffsetY = 0.7f;

        void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            mainCollider = GetComponent<Collider2D>();
            Stats = GetComponent<PlayerStats>();
            Inventory = GetComponent<Inventory>();
            Shooter = GetComponent<PlayerShooter>();
            Powers = GetComponent<PlayerPowers>();

            if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();

            initialGravityScale = rb.gravityScale > 0 ? rb.gravityScale : 3.2f;
            rb.gravityScale = initialGravityScale;
            initialScaleX = Mathf.Abs(transform.localScale.x);
            initialScaleY = transform.localScale.y;

            if (mainCollider is CapsuleCollider2D capCol)
            {
                initialColliderHeight = capCol.size.y;
                initialColliderOffsetY = capCol.offset.y;
            }
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

            CheckGrounded();
            HandleTimers();
            HandleInput();
            UpdateState();
            UpdateSprite();
            UpdateClimbHint();
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
            bool wasGrounded = IsGrounded;
            Vector2 pos = groundCheck != null ? (Vector2)groundCheck.position : (Vector2)transform.position + Vector2.down * 0.5f;
            IsGrounded = Physics2D.OverlapBox(pos, groundCheckSize, 0f, groundLayer) != null;

            if (IsGrounded)
            {
                coyoteTimer = coyoteTime;
                canDoubleJump = true;
                if (!wasGrounded && State == PlayerState.Fall)
                {
                    AudioManager.Play(Sfx.Land, 0.5f);
                }
            }
        }

        void HandleTimers()
        {
            if (!IsGrounded) coyoteTimer -= Time.deltaTime;
            if (jumpBufferTimer > 0f) jumpBufferTimer -= Time.deltaTime;
            if (shootPoseTimer > 0f) shootPoseTimer -= Time.deltaTime;

            if (State == PlayerState.Hurt)
            {
                hurtTimer -= Time.deltaTime;
                if (hurtTimer <= 0f) ChangeState(IsGrounded ? PlayerState.Idle : PlayerState.Fall);
            }
        }

        public void NotifyShot()
        {
            shootPoseTimer = 0.22f;
        }

        void HandleInput()
        {
            // Guarda o comando de pulo no buffer
            if (GameInput.Pressed(GameAction.Jump))
                jumpBufferTimer = jumpBufferTime;

            // Soltar espaço cedo diminui o pulo
            if (GameInput.Released(GameAction.Jump) && rb.linearVelocity.y > 0f)
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * jumpCutMultiplier);

            // Executa o pulo normal ou pulo duplo
            if (jumpBufferTimer > 0f)
            {
                if (coyoteTimer > 0f)
                {
                    ExecuteJump();
                }
                else if (canDoubleJump && Powers != null && Powers.IsDoubleJumpActive)
                {
                    canDoubleJump = false;
                    ExecuteJump();
                }
            }

            // Subir no cipó ao apertar W ou Seta para Cima/Baixo
            if (activeVine != null && Mathf.Abs(GameInput.Vertical) > 0.1f)
            {
                if (State != PlayerState.Climbing && State != PlayerState.Hurt && State != PlayerState.Dead)
                    ChangeState(PlayerState.Climbing);
            }
        }

        void ExecuteJump()
        {
            jumpBufferTimer = 0f;
            coyoteTimer = 0f;
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            ChangeState(PlayerState.Jump);
            AudioManager.Play(Sfx.Jump);
        }

        public void BounceFromStomp(float force)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, force);
            ChangeState(PlayerState.Jump);
            canDoubleJump = true;
            GameManager.Hitstop(0.06f);
        }

        void FixedUpdateMovement()
        {
            float inputH = GameInput.Horizontal;

            // Agachar quando estiver no chão segurando para baixo
            if (IsGrounded && GameInput.Held(GameAction.Down) && Mathf.Abs(inputH) < 0.1f)
            {
                if (State != PlayerState.Crouch) ChangeState(PlayerState.Crouch);
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
                return;
            }

            // Vira o sprite do personagem para o lado que está andando
            if (Mathf.Abs(inputH) > 0.05f)
            {
                Facing = Mathf.Sign(inputH);
                float scaleY = State == PlayerState.Crouch ? initialScaleY * 0.7f : initialScaleY;
                transform.localScale = new Vector3(initialScaleX * Facing, scaleY, transform.localScale.z);
            }

            float targetSpeed = inputH * moveSpeed;
            float accel = Mathf.Abs(targetSpeed) > 0.01f ? acceleration : deceleration;
            float newSpeedX = Mathf.MoveTowards(rb.linearVelocity.x, targetSpeed, accel * Time.fixedDeltaTime);

            // Ajuste de gravidade no ar (leve no topo do pulo e mais rápida na queda)
            float targetGravity = initialGravityScale;
            if (!IsGrounded && Mathf.Abs(rb.linearVelocity.y) < 1.4f)
                targetGravity = initialGravityScale * 0.65f;
            else if (rb.linearVelocity.y < 0f)
                targetGravity = initialGravityScale * fallGravityMultiplier;

            rb.gravityScale = targetGravity;
            rb.linearVelocity = new Vector2(newSpeedX, rb.linearVelocity.y);
        }

        void FixedUpdateClimbing()
        {
            rb.gravityScale = 0f;

            // Alinha suavemente o Chico ao centro do cipó quando está subindo/descendo
            if (activeVine != null && Mathf.Abs(GameInput.Vertical) > 0.1f && Mathf.Abs(GameInput.Horizontal) < 0.1f)
            {
                float targetX = Mathf.MoveTowards(transform.position.x, activeVine.transform.position.x, 6f * Time.fixedDeltaTime);
                transform.position = new Vector3(targetX, transform.position.y, transform.position.z);
            }

            rb.linearVelocity = new Vector2(GameInput.Horizontal * moveSpeed * 0.5f, GameInput.Vertical * climbSpeed);

            if (GameInput.Pressed(GameAction.Jump))
            {
                ChangeState(PlayerState.Jump);
                rb.gravityScale = initialGravityScale;
                rb.linearVelocity = new Vector2(Facing * moveSpeed * 0.8f, jumpForce * 0.85f);
            }
        }

        void UpdateState()
        {
            if (State == PlayerState.Hurt || State == PlayerState.Dead || State == PlayerState.Celebrate) return;

            if (State == PlayerState.Climbing)
            {
                if (IsGrounded && (Mathf.Abs(GameInput.Horizontal) > 0.1f || GameInput.Pressed(GameAction.Jump)))
                {
                    ChangeState(PlayerState.Idle);
                    rb.gravityScale = initialGravityScale;
                    return;
                }
                if (activeVine == null)
                {
                    ChangeState(IsGrounded ? PlayerState.Idle : PlayerState.Fall);
                    rb.gravityScale = initialGravityScale;
                }
                return;
            }

            if (IsGrounded)
            {
                if (State == PlayerState.Crouch)
                {
                    if (!GameInput.Held(GameAction.Down)) ChangeState(PlayerState.Idle);
                    return;
                }
                ChangeState(Mathf.Abs(rb.linearVelocity.x) > 0.2f ? PlayerState.Run : PlayerState.Idle);
            }
            else
            {
                ChangeState(rb.linearVelocity.y > 0.1f ? PlayerState.Jump : PlayerState.Fall);
            }
        }

        public void ChangeState(PlayerState newState)
        {
            if (State == newState) return;

            // Volta ao tamanho normal ao sair do agachado
            if (State == PlayerState.Crouch && newState != PlayerState.Crouch)
            {
                transform.localScale = new Vector3(initialScaleX * Facing, initialScaleY, transform.localScale.z);
                if (mainCollider is CapsuleCollider2D cap)
                {
                    cap.size = new Vector2(cap.size.x, initialColliderHeight);
                    cap.offset = new Vector2(cap.offset.x, initialColliderOffsetY);
                }
            }

            State = newState;

            // Diminui o tamanho e o colisor quando agachado
            if (State == PlayerState.Crouch)
            {
                transform.localScale = new Vector3(initialScaleX * Facing, initialScaleY * 0.7f, transform.localScale.z);
                if (mainCollider is CapsuleCollider2D cap)
                {
                    cap.size = new Vector2(cap.size.x, initialColliderHeight * 0.6f);
                    cap.offset = new Vector2(cap.offset.x, initialColliderOffsetY * 0.5f);
                }
            }
        }

        void UpdateSprite()
        {
            if (spriteRenderer == null) return;

            switch (State)
            {
                case PlayerState.Idle:
                    if (shootPoseTimer > 0f && spriteRunA != null)
                        spriteRenderer.sprite = spriteRunA;
                    else if (spriteIdle != null)
                        spriteRenderer.sprite = spriteIdle;
                    break;
                case PlayerState.Crouch:
                    if (spriteIdle != null) spriteRenderer.sprite = spriteIdle;
                    break;
                case PlayerState.Run:
                    runAnimTimer += Time.deltaTime;
                    if (runAnimTimer >= runAnimSpeed)
                    {
                        runAnimTimer = 0f;
                        runAnimFrame = !runAnimFrame;
                        if (spriteRunA != null && spriteRunB != null)
                            spriteRenderer.sprite = runAnimFrame ? spriteRunB : spriteRunA;
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

        void UpdateClimbHint()
        {
            if (climbHintObject == null) return;
            bool show = activeVine != null && State != PlayerState.Climbing && State != PlayerState.Dead && State != PlayerState.Hurt;
            if (climbHintObject.activeSelf != show) climbHintObject.SetActive(show);
            if (show) climbHintObject.transform.localScale = new Vector3(Facing, 1f, 1f);
        }

        public void TakeDamage(int damage, Vector2 hitDirection, GameObject damageSource)
        {
            if (State == PlayerState.Dead || Stats.IsInvulnerable) return;
            if (Powers != null && Powers.IsShieldActive)
            {
                AudioManager.Play(Sfx.Land);
                return;
            }

            Stats.TakeDamage(damage);
            if (Stats.CurrentLife <= 0) return;

            ChangeState(PlayerState.Hurt);
            hurtTimer = 0.28f;

            rb.linearVelocity = Vector2.zero;
            Vector2 kb = (hitDirection.normalized + Vector2.up * 0.8f).normalized * 7.5f;
            rb.AddForce(kb, ForceMode2D.Impulse);

            AudioManager.Play(Sfx.Hurt);
            CameraFollow.Shake(0.25f, 0.2f);
            GameManager.Hitstop(0.08f);
        }

        void OnInvulnerabilityChanged(bool invuln)
        {
            if (invuln) StartCoroutine(FlashInvulnerable());
        }

        IEnumerator FlashInvulnerable()
        {
            while (Stats != null && Stats.IsInvulnerable)
            {
                if (spriteRenderer != null) spriteRenderer.color = new Color(1f, 1f, 1f, 0.3f);
                yield return new WaitForSeconds(0.08f);
                if (spriteRenderer != null) spriteRenderer.color = Color.white;
                yield return new WaitForSeconds(0.08f);
            }
            if (spriteRenderer != null) spriteRenderer.color = Color.white;
        }

        public void TriggerCelebrate()
        {
            ChangeState(PlayerState.Celebrate);
            rb.linearVelocity = Vector2.zero;
        }

        public void TriggerDeath(string reason)
        {
            ChangeState(PlayerState.Dead);
            rb.linearVelocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Kinematic;
            if (spriteHurt != null && spriteRenderer != null) spriteRenderer.sprite = spriteHurt;
            GameManager.Instance.TriggerGameOver(reason);
        }

        public void RegisterVine(Vine v) => activeVine = v;
        public void UnregisterVine(Vine v) { if (activeVine == v) activeVine = null; }
    }
}
