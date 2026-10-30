using System;
using System.Collections;
using UnityEngine;

namespace SpaceChico
{
    public enum PlayerState
    {
        Idle,
        Run,
        Jump,
        Fall,
        Hurt,
        Dead,
        Climbing,
        Crouch,
        Celebrate
    }

    /// <summary>
    /// Controlador principal do Space Chico (GDD Seção 4, 5, 7.1, 11 e 13).
    /// Máquina de Estados Finitos (FSM) com física responsiva, coyote time,
    /// jump buffer, pulo variável, pulo esmagador e suporte a vinhas.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    [RequireComponent(typeof(PlayerStats))]
    [RequireComponent(typeof(Inventory))]
    public class PlayerController : MonoBehaviour, IDamageable
    {
        [Header("Movimentação Horizontal")]
        public float moveSpeed = 7.5f;
        public float acceleration = 45f;
        public float deceleration = 55f;

        [Header("Pulo e Game Feel (Risco Técnico 1)")]
        public float jumpForce = 13.5f;
        public float jumpCutMultiplier = 0.5f;
        public float fallGravityMultiplier = 2.4f;
        public float coyoteTime = 0.12f;
        public float jumpBufferTime = 0.15f;

        [Header("Detecção de Chão")]
        public Transform groundCheck;
        public Vector2 groundCheckSize = new Vector2(0.55f, 0.12f);
        public LayerMask groundLayer;

        [Header("Escalar Vinhas")]
        public float climbSpeed = 4.5f;

        [Header("Sprites por Estado")]
        public SpriteRenderer spriteRenderer;
        public Sprite spriteIdle;
        public Sprite spriteRunA;
        public Sprite spriteRunB;
        public Sprite spriteJump;
        public Sprite spriteFall;
        public Sprite spriteHurt;
        public Sprite spriteClimb;
        public Sprite spriteCelebrate;

        [Header("Efeitos e Animações")]
        public float runAnimSpeed = 0.14f;

        public PlayerState State { get; private set; } = PlayerState.Idle;
        public float Facing { get; private set; } = 1f;
        public bool IsGrounded { get; private set; }
        public Vector2 CurrentVelocity => rb.linearVelocity;

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
        private float initialScaleX;

        public event Action<PlayerState> StateChanged;

        private void Awake()
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

            if (groundCheck == null)
            {
                var gc = new GameObject("GroundCheck");
                gc.transform.SetParent(transform);
                gc.transform.localPosition = new Vector3(0, -0.6f, 0);
                groundCheck = gc.transform;
            }

            gameObject.tag = Layers.TagPlayer;
            gameObject.layer = LayerMask.NameToLayer(Layers.Player);
        }

        private void Start()
        {
            if (groundLayer.value == 0) groundLayer = Layers.GroundMask;
            if (Stats != null) Stats.InvulnerabilityChanged += OnInvulnerabilityChanged;
        }

        private void OnDestroy()
        {
            if (Stats != null) Stats.InvulnerabilityChanged -= OnInvulnerabilityChanged;
        }

        private void Update()
        {
            if (GameManager.Instance != null && !GameManager.Instance.IsGameplayRunning)
                return;

            if (State == PlayerState.Dead || State == PlayerState.Celebrate)
                return;

            CheckGrounded();
            HandleTimers();
            HandleInput();
            UpdateFSM();
            UpdateSprite();
        }

        private void FixedUpdate()
        {
            if (GameManager.Instance != null && !GameManager.Instance.IsGameplayRunning)
                return;

            if (State == PlayerState.Dead || State == PlayerState.Celebrate)
            {
                rb.linearVelocity = Vector2.zero;
                return;
            }

            if (State == PlayerState.Hurt)
            {
                return; // Controle suspenso durante hurt knockback
            }

            if (State == PlayerState.Climbing)
            {
                FixedUpdateClimbing();
            }
            else
            {
                FixedUpdateMovement();
            }
        }

        private void CheckGrounded()
        {
            bool wasGrounded = IsGrounded;
            Vector2 checkPos = groundCheck != null ? (Vector2)groundCheck.position : (Vector2)transform.position + Vector2.down * 0.5f;
            IsGrounded = Physics2D.OverlapBox(checkPos, groundCheckSize, 0f, groundLayer) != null;

            if (IsGrounded)
            {
                coyoteTimer = coyoteTime;
                canDoubleJump = true;
                if (!wasGrounded && State == PlayerState.Fall)
                {
                    AudioManager.Play(Sfx.Land, 0.5f);
                    VFX.Burst(checkPos, new Color(0.3f, 0.8f, 0.5f), 6, 1.5f, 0.12f, 0.2f);
                }
            }
        }

        private void HandleTimers()
        {
            if (!IsGrounded) coyoteTimer -= Time.deltaTime;
            if (jumpBufferTimer > 0f) jumpBufferTimer -= Time.deltaTime;

            if (State == PlayerState.Hurt)
            {
                hurtTimer -= Time.deltaTime;
                if (hurtTimer <= 0f)
                {
                    ChangeState(IsGrounded ? PlayerState.Idle : PlayerState.Fall);
                }
            }
        }

        private void HandleInput()
        {
            // Pulo (Espaço) - Registra no buffer
            if (GameInput.Pressed(GameAction.Jump))
            {
                jumpBufferTimer = jumpBufferTime;
            }

            // Pulo Variável: Soltar a barra de espaço corta a velocidade ascendente
            if (GameInput.Released(GameAction.Jump) && rb.linearVelocity.y > 0f)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * jumpCutMultiplier);
            }

            // Tentar Pular
            if (jumpBufferTimer > 0f)
            {
                if (coyoteTimer > 0f)
                {
                    ExecuteJump();
                }
                else if (canDoubleJump && Powers != null && Powers.IsDoubleJumpActive)
                {
                    // Pulo Duplo concedido pelo Power-up
                    canDoubleJump = false;
                    ExecuteJump();
                    VFX.Burst(transform.position, new Color(1f, 0.9f, 0.3f), 12, 3f, 0.2f, 0.3f);
                }
            }

            // Interagir com Vinha (GDD Seção 4 e 7.1: Setas Cima/Baixo só no Idle ou já escalando)
            if (activeVine != null)
            {
                float v = GameInput.Vertical;
                if (Mathf.Abs(v) > 0.1f && (State == PlayerState.Idle || State == PlayerState.Climbing))
                {
                    if (State != PlayerState.Climbing)
                    {
                        ChangeState(PlayerState.Climbing);
                    }
                }
            }
        }

        private void ExecuteJump()
        {
            jumpBufferTimer = 0f;
            coyoteTimer = 0f;
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            ChangeState(PlayerState.Jump);
            AudioManager.Play(Sfx.Jump);

            if (groundCheck != null)
            {
                VFX.Burst(groundCheck.position, new Color(0.4f, 1f, 0.7f), 8, 2f, 0.15f, 0.25f);
            }
        }

        public void BounceFromStomp(float force)
        {
            // Impulso ao esmagar inimigo (GDD Sprint 3.2 e Seção 7.1)
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, force);
            ChangeState(PlayerState.Jump);
            canDoubleJump = true; // Renova salto aéreo
        }

        private void FixedUpdateMovement()
        {
            float inputH = GameInput.Horizontal;

            // Agachar com Seta Baixo
            if (IsGrounded && GameInput.Held(GameAction.Down) && Mathf.Abs(inputH) < 0.1f)
            {
                if (State != PlayerState.Crouch) ChangeState(PlayerState.Crouch);
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
                return;
            }

            // Direção do personagem
            if (Mathf.Abs(inputH) > 0.05f)
            {
                Facing = Mathf.Sign(inputH);
                transform.localScale = new Vector3(initialScaleX * Facing, transform.localScale.y, transform.localScale.z);
            }

            // Aceleração / Desaceleração horizontal fluida
            float targetSpeed = inputH * moveSpeed;
            float accelRate = Mathf.Abs(targetSpeed) > 0.01f ? acceleration : deceleration;
            float newSpeedX = Mathf.MoveTowards(rb.linearVelocity.x, targetSpeed, accelRate * Time.fixedDeltaTime);

            // Gravidade adaptativa (cai mais rápido para game feel satisfatório - Risco Técnico 1)
            float targetGravity = initialGravityScale;
            if (rb.linearVelocity.y < 0f)
            {
                targetGravity = initialGravityScale * fallGravityMultiplier;
            }
            rb.gravityScale = targetGravity;

            rb.linearVelocity = new Vector2(newSpeedX, rb.linearVelocity.y);
        }

        private void FixedUpdateClimbing()
        {
            rb.gravityScale = 0f;
            float v = GameInput.Vertical;
            float h = GameInput.Horizontal;

            rb.linearVelocity = new Vector2(h * moveSpeed * 0.5f, v * climbSpeed);

            // Pulo para sair da vinha
            if (GameInput.Pressed(GameAction.Jump))
            {
                ChangeState(PlayerState.Jump);
                rb.gravityScale = initialGravityScale;
                rb.linearVelocity = new Vector2(Facing * moveSpeed * 0.8f, jumpForce * 0.85f);
            }
        }

        private void UpdateFSM()
        {
            if (State == PlayerState.Hurt || State == PlayerState.Dead || State == PlayerState.Celebrate)
                return;

            if (State == PlayerState.Climbing)
            {
                // Se saiu da área da vinha, cai
                if (activeVine == null)
                {
                    ChangeState(PlayerState.Fall);
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

                if (Mathf.Abs(rb.linearVelocity.x) > 0.2f)
                {
                    ChangeState(PlayerState.Run);
                }
                else
                {
                    ChangeState(PlayerState.Idle);
                }
            }
            else
            {
                if (rb.linearVelocity.y > 0.1f)
                {
                    ChangeState(PlayerState.Jump);
                }
                else
                {
                    ChangeState(PlayerState.Fall);
                }
            }
        }

        public void ChangeState(PlayerState newState)
        {
            if (State == newState) return;
            State = newState;
            StateChanged?.Invoke(newState);
        }

        private void UpdateSprite()
        {
            if (spriteRenderer == null) return;

            switch (State)
            {
                case PlayerState.Idle:
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
                        {
                            spriteRenderer.sprite = runAnimFrame ? spriteRunB : spriteRunA;
                        }
                    }
                    break;

                case PlayerState.Jump:
                    if (spriteJump != null) spriteRenderer.sprite = spriteJump;
                    break;

                case PlayerState.Fall:
                    if (spriteFall != null) spriteRenderer.sprite = spriteFall;
                    else if (spriteJump != null) spriteRenderer.sprite = spriteJump;
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

            // Se o escudo estiver ativo, absorve o dano totalmente
            if (Powers != null && Powers.IsShieldActive)
            {
                AudioManager.Play(Sfx.Land);
                VFX.Burst(transform.position, new Color(0.3f, 0.8f, 1f), 15, 3f, 0.25f, 0.4f);
                return;
            }

            Stats.TakeDamage(damage);
            if (Stats.CurrentLife <= 0) return;

            // Estado Hurt (GDD Seção 7.1)
            ChangeState(PlayerState.Hurt);
            hurtTimer = 0.28f;

            // Repulsão (Knockback)
            rb.linearVelocity = Vector2.zero;
            Vector2 kb = (hitDirection.normalized + Vector2.up * 0.8f).normalized * 7.5f;
            rb.AddForce(kb, ForceMode2D.Impulse);

            AudioManager.Play(Sfx.Hurt);
            CameraFollow.Shake(0.25f, 0.2f);
            VFX.Burst(transform.position, new Color(1f, 0.3f, 0.3f), 12, 3f, 0.2f, 0.35f);
        }

        private void OnInvulnerabilityChanged(bool invuln)
        {
            if (invuln) StartCoroutine(FlashInvulnerable());
        }

        private IEnumerator FlashInvulnerable()
        {
            while (Stats != null && Stats.IsInvulnerable)
            {
                if (spriteRenderer != null)
                {
                    spriteRenderer.color = new Color(1f, 1f, 1f, 0.3f);
                }
                yield return new WaitForSeconds(0.08f);

                if (spriteRenderer != null)
                {
                    spriteRenderer.color = Color.white;
                }
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

            VFX.Burst(transform.position, new Color(1f, 0.4f, 0.4f), 25, 4.5f, 0.3f, 0.8f);
            GameManager.Instance.TriggerGameOver(reason);
        }

        public void RegisterVine(Vine v) => activeVine = v;
        public void UnregisterVine(Vine v) { if (activeVine == v) activeVine = null; }

        private void OnDrawGizmosSelected()
        {
            if (groundCheck != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireCube(groundCheck.position, groundCheckSize);
            }
        }
    }
}
