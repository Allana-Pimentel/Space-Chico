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
        public float climbSpeed = 4.6f;
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
        private Collider2D ignoredPlatform;
        private float climbAnimTimer;
        private bool climbStepSide;
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

            // Se cair em um buraco fora da fase, morre e reinicia
            if (transform.position.y < -6.5f)
            {
                TriggerDeath("Chico caiu no abismo!");
                return;
            }

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
            Vector2 pos = groundCheck != null ? (Vector2)groundCheck.position : (Vector2)transform.position + Vector2.down * 0.05f;
            Collider2D[] hits = Physics2D.OverlapBoxAll(pos, groundCheckSize, 0f, groundLayer);

            bool groundedNow = false;
            for (int i = 0; i < hits.Length; i++)
            {
                Collider2D hit = hits[i];
                if (hit == null || hit == ignoredPlatform) continue;

                // Só considera no chão se os pés estiverem realmente no topo do piso/plataforma
                if (rb.linearVelocity.y <= 0.5f && transform.position.y >= hit.bounds.max.y - 0.22f)
                {
                    groundedNow = true;
                    break;
                }
            }

            IsGrounded = groundedNow;

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

        public void EnableDoubleJump()
        {
            canDoubleJump = true;
        }

        void HandleInput()
        {
            // Guarda o comando de pulo no buffer
            if (GameInput.Pressed(GameAction.Jump))
                jumpBufferTimer = jumpBufferTime;

            // Soltar espaço cedo diminui o pulo
            if (GameInput.Released(GameAction.Jump) && rb.linearVelocity.y > 0f)
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * jumpCutMultiplier);

            // Executa o pulo normal ou pulo duplo (quando fora da escada)
            if (State != PlayerState.Climbing && jumpBufferTimer > 0f)
            {
                if (coyoteTimer > 0f)
                {
                    ExecuteJump(false);
                }
                else if (canDoubleJump && Powers != null && Powers.IsDoubleJumpActive)
                {
                    canDoubleJump = false;
                    ExecuteJump(true);
                }
            }

            // Subir ou descer no cipó ao apertar W/S ou Setas Cima/Baixo
            if (activeVine != null && State != PlayerState.Climbing && State != PlayerState.Hurt && State != PlayerState.Dead)
            {
                bool wantsClimbUp = GameInput.Vertical > 0.1f && transform.position.y < activeVine.topY - 0.1f;
                bool wantsClimbDown = GameInput.Vertical < -0.1f && transform.position.y > activeVine.bottomY + 0.3f;

                if (wantsClimbUp || wantsClimbDown)
                {
                    if (wantsClimbDown && transform.position.y >= activeVine.topY - 0.2f)
                    {
                        // Desce suavemente atravessando a plataforma de mão única
                        transform.position = new Vector3(activeVine.transform.position.x, activeVine.topY - 0.35f, transform.position.z);
                    }
                    ChangeState(PlayerState.Climbing);
                }
            }
        }

        void ExecuteJump(bool isDoubleJump)
        {
            jumpBufferTimer = 0f;
            coyoteTimer = 0f;

            float force = isDoubleJump ? jumpForce * 1.05f : jumpForce;
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, force);
            ChangeState(PlayerState.Jump);
            AudioManager.Play(Sfx.Jump);

            if (isDoubleJump)
            {
                VFX.Burst(transform.position, new Color(0.3f, 1f, 0.85f), 16, 4f, 0.22f, 0.35f);
                VFX.PopText(transform.position + Vector3.up * 0.4f, "PULO DUPLO!", new Color(0.4f, 1f, 0.85f), 0.75f);
            }
        }

        public void BounceFromStomp(float force)
        {
            float vx = Mathf.Abs(rb.linearVelocity.x) > 1.5f ? rb.linearVelocity.x : -Facing * 3.8f;
            rb.linearVelocity = new Vector2(vx, force);
            ChangeState(PlayerState.Jump);
            canDoubleJump = true;
            GameManager.Hitstop(0.06f);
        }

        void FixedUpdateMovement()
        {
            float inputH = GameInput.Horizontal;

            // Agachar quando estiver no chão segurando para baixo
            if (IsGrounded && GameInput.Held(GameAction.Down) && Mathf.Abs(inputH) < 0.1f && activeVine == null)
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
            canDoubleJump = true;

            if (activeVine == null)
            {
                ExitClimbing(IsGrounded ? PlayerState.Idle : PlayerState.Fall);
                return;
            }

            // Ignora colisão física com a plataforma enquanto está subindo/descendo pelo cipó
            SetPlatformIgnore(activeVine.platformCollider, true);

            float inputV = GameInput.Vertical;
            float inputH = GameInput.Horizontal;

            // Centraliza suavemente o Chico no cipó enquanto escala
            if (Mathf.Abs(inputV) > 0.1f)
            {
                float targetX = Mathf.MoveTowards(transform.position.x, activeVine.transform.position.x, 8f * Time.fixedDeltaTime);
                transform.position = new Vector3(targetX, transform.position.y, transform.position.z);
            }

            // Ritmo realista de puxada na corda/cipó (pequena variação de impulso a cada braçada)
            float pullPulse = Mathf.Abs(inputV) > 0.1f ? (1f + 0.18f * Mathf.Sin(Time.time * 18f)) : 0f;
            float vy = inputV * climbSpeed * pullPulse;
            float vx = inputH * moveSpeed * 0.35f;

            // Chegou ao topo da plataforma subindo: desembarca suavemente em cima da plataforma
            if (inputV > 0.1f && transform.position.y >= activeVine.topY - 0.08f)
            {
                SetPlatformIgnore(activeVine.platformCollider, false);
                transform.position = new Vector3(transform.position.x, activeVine.topY + 0.03f, transform.position.z);
                rb.linearVelocity = Vector2.zero;
                IsGrounded = true;
                AudioManager.Play(Sfx.Land, 0.45f);
                ExitClimbing(PlayerState.Idle);
                return;
            }

            // Chegou ao chão descendo: pisa no chão suavemente
            if (inputV < -0.1f && transform.position.y <= activeVine.bottomY + 0.15f)
            {
                SetPlatformIgnore(activeVine.platformCollider, false);
                rb.linearVelocity = Vector2.zero;
                IsGrounded = true;
                ExitClimbing(PlayerState.Idle);
                return;
            }

            rb.linearVelocity = new Vector2(vx, vy);

            // Pular para sair do cipó
            if (jumpBufferTimer > 0f || GameInput.Pressed(GameAction.Jump))
            {
                jumpBufferTimer = 0f;
                SetPlatformIgnore(activeVine.platformCollider, false);
                ExitClimbing(PlayerState.Jump);
                if (Mathf.Abs(inputH) > 0.1f) Facing = Mathf.Sign(inputH);
                rb.linearVelocity = new Vector2(Facing * moveSpeed * 0.85f, jumpForce * 0.9f);
                AudioManager.Play(Sfx.Jump);
            }
        }

        void SetPlatformIgnore(Collider2D platformCol, bool ignore)
        {
            if (mainCollider == null) return;

            if (!ignore)
            {
                if (ignoredPlatform != null)
                {
                    Physics2D.IgnoreCollision(mainCollider, ignoredPlatform, false);
                    ignoredPlatform = null;
                }
                return;
            }

            if (platformCol != null && ignoredPlatform != platformCol)
            {
                if (ignoredPlatform != null)
                    Physics2D.IgnoreCollision(mainCollider, ignoredPlatform, false);

                Physics2D.IgnoreCollision(mainCollider, platformCol, true);
                ignoredPlatform = platformCol;
            }
        }

        void ExitClimbing(PlayerState nextState)
        {
            SetPlatformIgnore(null, false);
            if (spriteRenderer != null) spriteRenderer.flipX = false;
            transform.localRotation = Quaternion.identity;
            rb.gravityScale = initialGravityScale;
            ChangeState(nextState);
        }

        void UpdateState()
        {
            if (State == PlayerState.Hurt || State == PlayerState.Dead || State == PlayerState.Celebrate) return;

            if (State == PlayerState.Climbing)
            {
                // Se estiver com os pés no chão na base do cipó e andar para longe, sai da escalada
                if (IsGrounded && Mathf.Abs(GameInput.Vertical) < 0.1f && Mathf.Abs(GameInput.Horizontal) > 0.2f)
                {
                    ExitClimbing(PlayerState.Run);
                    return;
                }
                if (activeVine == null)
                {
                    ExitClimbing(IsGrounded ? PlayerState.Idle : PlayerState.Fall);
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

            if (State == PlayerState.Climbing && newState != PlayerState.Climbing)
            {
                SetPlatformIgnore(null, false);
                if (spriteRenderer != null) spriteRenderer.flipX = false;
                transform.localRotation = Quaternion.identity;
            }

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

                    // Animação realista de braçada ao subir/descer no cipó
                    if (Mathf.Abs(GameInput.Vertical) > 0.1f)
                    {
                        climbAnimTimer += Time.deltaTime;
                        if (climbAnimTimer >= 0.16f)
                        {
                            climbAnimTimer = 0f;
                            climbStepSide = !climbStepSide;
                            spriteRenderer.flipX = climbStepSide;
                            transform.localRotation = Quaternion.Euler(0f, 0f, climbStepSide ? 4.5f : -4.5f);
                            AudioManager.Play(Sfx.Land, 0.18f);
                            VFX.Burst(transform.position + Vector3.up * 0.6f, new Color(0.35f, 0.9f, 0.45f), 2, 1.4f, 0.1f, 0.2f);
                        }
                    }
                    else
                    {
                        transform.localRotation = Quaternion.identity;
                    }
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

            float dirX = Mathf.Abs(hitDirection.x) > 0.1f ? Mathf.Sign(hitDirection.x) : -Facing;
            rb.gravityScale = initialGravityScale;
            rb.linearVelocity = new Vector2(dirX * 6.8f, 6.5f);

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
            if (State == PlayerState.Dead) return;
            ChangeState(PlayerState.Dead);
            rb.linearVelocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Kinematic;
            if (spriteHurt != null && spriteRenderer != null) spriteRenderer.sprite = spriteHurt;
            VFX.Burst(transform.position + Vector3.up * 0.5f, new Color(1f, 0.35f, 0.3f), 18, 4f, 0.22f, 0.45f);
            VFX.PopText(transform.position + Vector3.up * 1.3f, reason, new Color(1f, 0.4f, 0.4f), 1.1f);
            if (GameManager.Instance != null) GameManager.Instance.TriggerGameOver(reason);
        }

        public void RegisterVine(Vine v) => activeVine = v;
        public void UnregisterVine(Vine v) { if (activeVine == v) activeVine = null; }
    }
}
