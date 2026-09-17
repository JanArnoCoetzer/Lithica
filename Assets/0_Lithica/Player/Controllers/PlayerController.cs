using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(PlayerInput))]
[RequireComponent(typeof(CapsuleCollider2D))]
[RequireComponent(typeof(PlayerInteraction))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 8f;
    [SerializeField] private float groundAcceleration = 60f;
    [SerializeField] private float groundDeceleration = 80f;
    [SerializeField] private float airAcceleration = 35f;
    [SerializeField] private float airDeceleration = 12f;

    [Header("Jump")]
    [SerializeField] private float jumpForce = 14f;
    [SerializeField] private float coyoteTime = 0.12f;
    [SerializeField] private float jumpBufferTime = 0.12f;

    [Header("Jump Cut")]
    [SerializeField] private float jumpCutMultiplier = 0.5f;

    [Header("Gravity")]
    [SerializeField] private float baseGravityScale = 1f;
    [SerializeField] private float fallGravityMultiplier = 2.5f;
    [SerializeField] private float maxFallSpeed = 18f;

    [Header("Ground Check")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private Vector2 groundCheckSize = new Vector2(0.8f, 0.1f);
    [SerializeField] private LayerMask groundLayer;

    [Header("Visual")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("Step Up")]
    [SerializeField] private float stepHeight = 1f;
    [SerializeField] private float maxStepHeight = 0.5f;
    [SerializeField] private float stepCheckWidth = 0.08f;
    [SerializeField] private float checkOffset = 0.12f;
    [SerializeField] private float checkVerticalOffset = 0f;
    [SerializeField] private LayerMask stepLayer;

    private Rigidbody2D rb;
    private CapsuleCollider2D bodyCollider;
    private PlayerInteraction playerInteraction;

    private float moveInput;
    private bool jumpReleased;
    private bool isGrounded;
    private float coyoteCounter;
    private float jumpBufferCounter;

    public bool IsFacingLeft
    {
        get
        {
            if (spriteRenderer == null)
                return false;

            return spriteRenderer.flipX;
        }
    }

    public int FacingDirection => IsFacingLeft ? -1 : 1;
    public float MoveInput => moveInput;
    public Rigidbody2D Rigidbody => rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        bodyCollider = GetComponent<CapsuleCollider2D>();
        playerInteraction = GetComponent<PlayerInteraction>();

        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (stepLayer == 0)
            stepLayer = groundLayer;
    }

    private void Update()
    {
        ForceStopMovementOnRelease();

        CheckGround();
        UpdateCoyoteTime();
        UpdateJumpBuffer();
        HandleJump();
        HandleJumpCut();
        HandleFlip();
    }

    private void FixedUpdate()
    {
        HandleMovement();
        HandleStepUp();
        HandleBetterFall();
    }

    public void OnMove(InputValue value)
    {
        moveInput = value.Get<float>();
    }

    public void OnJump(InputValue value)
    {
        if (value.isPressed)
            jumpBufferCounter = jumpBufferTime;
        else
            jumpReleased = true;
    }

    public void OnBreak(InputValue value)
    {
        if (playerInteraction != null)
            playerInteraction.OnBreak(value);
    }

    private void ForceStopMovementOnRelease()
    {
        if (Keyboard.current == null)
            return;

        bool leftHeld =
            Keyboard.current.aKey.isPressed ||
            Keyboard.current.leftArrowKey.isPressed;

        bool rightHeld =
            Keyboard.current.dKey.isPressed ||
            Keyboard.current.rightArrowKey.isPressed;

        if (!leftHeld && !rightHeld)
            moveInput = 0f;
    }

    private void HandleMovement()
    {
        float targetSpeed = moveInput * moveSpeed;

        float currentAcceleration;
        float currentDeceleration;

        if (isGrounded)
        {
            currentAcceleration = groundAcceleration;
            currentDeceleration = groundDeceleration;
        }
        else
        {
            currentAcceleration = airAcceleration;
            currentDeceleration = airDeceleration;
        }

        float rate = Mathf.Abs(moveInput) > 0.01f
            ? currentAcceleration
            : currentDeceleration;

        float newX = Mathf.MoveTowards(
            rb.linearVelocity.x,
            targetSpeed,
            rate * Time.fixedDeltaTime
        );

        rb.linearVelocity = new Vector2(newX, rb.linearVelocity.y);
    }

    private void HandleStepUp()
    {
        if (bodyCollider == null)
            return;

        if (!isGrounded)
            return;

        if (Mathf.Abs(moveInput) < 0.01f)
            return;

        float dir = Mathf.Sign(moveInput);
        Bounds bounds = bodyCollider.bounds;

        float step = Mathf.Min(stepHeight, maxStepHeight);
        Vector2 checkSize = new Vector2(stepCheckWidth, step * 0.9f);

        float checkX = bounds.center.x + dir * (bounds.extents.x + checkOffset);
        float baseY = bounds.min.y + checkVerticalOffset;

        Vector2 lowerCheckCenter = new Vector2(checkX, baseY + checkSize.y * 0.5f);
        Vector2 upperCheckCenter = new Vector2(checkX, baseY + step + checkSize.y * 0.5f);

        Collider2D lowerHit = Physics2D.OverlapBox(lowerCheckCenter, checkSize, 0f, stepLayer);
        Collider2D upperHit = Physics2D.OverlapBox(upperCheckCenter, checkSize, 0f, stepLayer);

        if (lowerHit != null && upperHit == null)
        {
            rb.position += new Vector2(0f, step);
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
        }
    }

    private void HandleJump()
    {
        if (jumpBufferCounter > 0f && coyoteCounter > 0f)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            jumpBufferCounter = 0f;
            coyoteCounter = 0f;
        }
    }

    private void HandleJumpCut()
    {
        if (jumpReleased && rb.linearVelocity.y > 0f)
            rb.linearVelocity = new Vector2(
                rb.linearVelocity.x,
                rb.linearVelocity.y * jumpCutMultiplier
            );

        jumpReleased = false;
    }

    private void HandleBetterFall()
    {
        if (rb.linearVelocity.y < 0f)
            rb.gravityScale = baseGravityScale * fallGravityMultiplier;
        else
            rb.gravityScale = baseGravityScale;

        if (rb.linearVelocity.y < -maxFallSpeed)
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, -maxFallSpeed);
    }

    private void CheckGround()
    {
        if (groundCheck == null)
        {
            isGrounded = false;
            return;
        }

        isGrounded = Physics2D.OverlapBox(
            groundCheck.position,
            groundCheckSize,
            0f,
            groundLayer
        );
    }

    private void UpdateCoyoteTime()
    {
        if (isGrounded)
            coyoteCounter = coyoteTime;
        else
            coyoteCounter -= Time.deltaTime;
    }

    private void UpdateJumpBuffer()
    {
        if (jumpBufferCounter > 0f)
            jumpBufferCounter -= Time.deltaTime;
    }

    private void HandleFlip()
    {
        if (spriteRenderer == null)
            return;

        if (moveInput > 0.01f)
            spriteRenderer.flipX = false;
        else if (moveInput < -0.01f)
            spriteRenderer.flipX = true;
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(groundCheck.position, groundCheckSize);
        }

        CapsuleCollider2D col = GetComponent<CapsuleCollider2D>();
        if (col != null && Mathf.Abs(moveInput) > 0.01f)
        {
            float dir = Mathf.Sign(moveInput);
            Bounds bounds = col.bounds;

            float step = Mathf.Min(stepHeight, maxStepHeight);
            Vector2 checkSize = new Vector2(stepCheckWidth, step * 0.9f);

            float checkX = bounds.center.x + dir * (bounds.extents.x + checkOffset);
            float baseY = bounds.min.y + checkVerticalOffset;

            Vector2 lowerCheckCenter = new Vector2(checkX, baseY + checkSize.y * 0.5f);
            Vector2 upperCheckCenter = new Vector2(checkX, baseY + step + checkSize.y * 0.5f);

            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(lowerCheckCenter, checkSize);

            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(upperCheckCenter, checkSize);
        }
    }
}