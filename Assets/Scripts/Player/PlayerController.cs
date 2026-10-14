using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [Header("Jump")]
    [SerializeField] private float jumpForce = 5f;
    [SerializeField] private float maxJumpHoldTime = 0.5f;
    [SerializeField] private float jumpHoldForce = 35f;
    [SerializeField] private float maxUpwardVelocity = 14f;

    [Header("Jump Collider")]
    [SerializeField, Range(0.3f, 1f)]
    private float jumpColliderHeightMultiplier = 0.65f;
    [Tooltip("Collider starts growing back once vertical velocity drops below this value.")]
    [SerializeField] private float fallVelocityThreshold = -0.1f;
    [Tooltip("Seconds the collider takes to grow back to full size. Keep it shorter than your fall time. Set to 0 for an instant restore.")]
    [SerializeField] private float colliderRestoreDuration = 0.25f;

    [Header("Slide")]
    [SerializeField] private float slideDuration = 0.5f;
    [SerializeField] private float slideHeight = 1f;
    [SerializeField] private float slideWidthMultiplier = 1.1f;

    [Header("Hit")]
    [Tooltip("Optional, found automatically on this object if left empty.")]
    [SerializeField] private PlayerHealth playerHealth;
    [Tooltip("Seconds jump and slide are blocked after being hit. Set this to the length of your hit animation.")]
    [SerializeField] private float hitLockDuration = 0.5f;

    private Rigidbody rb;
    private InputSystem_Actions input;
    private CapsuleCollider capsuleCollider;

    private bool isGrounded;
    private bool isSliding;
    private bool isJumpHeld;
    private bool isJumpColliderActive;
    private bool isRestoringCollider;

    private float jumpHoldTimer;
    private float jumpColliderHeight;
    private float hitLockEndTime; // Time.time value when the hit lock ends
    private Coroutine slideCoroutine;

    private float originalHeight;
    private float originalRadius;
    private Vector3 originalCenter;

    public bool IsGrounded => isGrounded;
    public bool IsDashing { get; private set; }
    public bool IsSliding => isSliding;

    /// <summary>True while the player is recovering from a hit and can't jump or slide.</summary>
    public bool IsHitLocked => Time.time < hitLockEndTime;

    public float JumpCharge => maxJumpHoldTime > 0f
        ? Mathf.Clamp01(jumpHoldTimer / maxJumpHoldTime)
        : 0f;

    public event Action OnJump;
    public event Action OnSlide;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        capsuleCollider = GetComponent<CapsuleCollider>();
        input = new InputSystem_Actions();

        if (playerHealth == null)
            playerHealth = GetComponent<PlayerHealth>();

        if (capsuleCollider != null)
        {
            originalHeight = capsuleCollider.height;
            originalRadius = capsuleCollider.radius;
            originalCenter = capsuleCollider.center;

            jumpColliderHeight = Mathf.Max(
                originalRadius * 2f,
                originalHeight * jumpColliderHeightMultiplier
            );
        }
    }

    void OnEnable()
    {
        input.Player.Enable();
        input.Player.Jump.performed += OnJumpPerformed;
        input.Player.Jump.canceled += OnJumpCanceled;
        input.Player.Slide.performed += OnSlidePerformed;

        if (playerHealth != null)
            playerHealth.Damaged += OnDamaged;
    }

    void OnDisable()
    {
        input.Player.Jump.performed -= OnJumpPerformed;
        input.Player.Jump.canceled -= OnJumpCanceled;
        input.Player.Slide.performed -= OnSlidePerformed;
        input.Player.Disable();

        if (playerHealth != null)
            playerHealth.Damaged -= OnDamaged;
    }

    void Update()
    {
        if (GameManager.Instance == null)
            return;

        IsDashing = GameManager.Instance.State == GameState.Running
                    && input.Player.Sprint.IsPressed();

        if (GameManager.Instance.State != GameState.Running)
        {
            isJumpHeld = false;
            jumpHoldTimer = 0f;
        }
    }

    void FixedUpdate()
    {
        // Grow the collider back while falling. This runs BEFORE the early returns below.
        if (isJumpColliderActive && !isSliding)
        {
            if (!isRestoringCollider && rb.linearVelocity.y < fallVelocityThreshold)
                isRestoringCollider = true;

            if (isRestoringCollider)
                SmoothRestoreCollider();
        }

        if (!isJumpHeld || isSliding)
            return;

        if (jumpHoldTimer >= maxJumpHoldTime || rb.linearVelocity.y <= 0f)
        {
            isJumpHeld = false;
            return;
        }

        rb.AddForce(Vector3.up * jumpHoldForce, ForceMode.Acceleration);
        jumpHoldTimer += Time.fixedDeltaTime;

        if (rb.linearVelocity.y > maxUpwardVelocity)
        {
            Vector3 velocity = rb.linearVelocity;
            velocity.y = maxUpwardVelocity;
            rb.linearVelocity = velocity;
        }
    }

    void OnDamaged()
    {
        hitLockEndTime = Time.time + hitLockDuration;
    }

    void SmoothRestoreCollider()
    {
        if (colliderRestoreDuration <= 0f)
        {
            SetJumpCollider(false);
            return;
        }

        float speed = (originalHeight - jumpColliderHeight) / colliderRestoreDuration;
        float newHeight = Mathf.MoveTowards(
            capsuleCollider.height,
            originalHeight,
            speed * Time.fixedDeltaTime
        );

        capsuleCollider.height = newHeight;
        // Keeps the collider's top fixed, same rule used when shrinking
        capsuleCollider.center = new Vector3(
            originalCenter.x,
            originalCenter.y + (originalHeight - newHeight) / 2f,
            originalCenter.z
        );

        if (Mathf.Approximately(newHeight, originalHeight))
            SetJumpCollider(false);
    }

    void OnJumpPerformed(InputAction.CallbackContext ctx)
    {
        var gm = GameManager.Instance;

        if (gm == null || gm.State != GameState.Running)
            return;

        if (gm.StateChangedFrame == Time.frameCount)
            return;

        if (IsHitLocked)
            return;

        if (!isGrounded || isSliding)
            return;

        isGrounded = false;
        SetJumpCollider(true);

        isJumpHeld = true;
        jumpHoldTimer = 0f;

        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        OnJump?.Invoke();
    }

    void OnJumpCanceled(InputAction.CallbackContext ctx)
    {
        isJumpHeld = false;
    }

    void OnSlidePerformed(InputAction.CallbackContext ctx)
    {
        var gm = GameManager.Instance;

        if (gm == null || gm.State != GameState.Running)
            return;

        if (gm.StateChangedFrame == Time.frameCount)
            return;

        if (IsHitLocked)
            return;

        if (!isGrounded || isSliding || capsuleCollider == null)
            return;

        slideCoroutine = StartCoroutine(SlideRoutine());
    }

    IEnumerator SlideRoutine()
    {
        isSliding = true;
        isJumpHeld = false;

        OnSlide?.Invoke();

        float newHeight = Mathf.Max(
            originalRadius * 2f,
            slideHeight * 0.85f
        );

        capsuleCollider.height = newHeight;
        capsuleCollider.radius = Mathf.Min(
            originalRadius * slideWidthMultiplier,
            newHeight * 0.5f
        );

        capsuleCollider.center = new Vector3(
            originalCenter.x,
            originalCenter.y - (originalHeight - newHeight) / 2f,
            originalCenter.z
        );

        yield return new WaitForSeconds(slideDuration);

        isJumpColliderActive = false;
        isRestoringCollider = false;
        RestoreCollider();

        isSliding = false;
        slideCoroutine = null;
    }

    void SetJumpCollider(bool active)
    {
        if (capsuleCollider == null || isSliding)
            return;

        if (isJumpColliderActive == active)
            return;

        isJumpColliderActive = active;
        isRestoringCollider = false;

        if (active)
        {
            capsuleCollider.height = jumpColliderHeight;
            capsuleCollider.center = new Vector3(
                originalCenter.x,
                originalCenter.y + (originalHeight - jumpColliderHeight) / 2f,
                originalCenter.z
            );
        }
        else
        {
            RestoreCollider();
        }
    }

    void RestoreCollider()
    {
        if (capsuleCollider == null)
            return;

        capsuleCollider.height = originalHeight;
        capsuleCollider.radius = originalRadius;
        capsuleCollider.center = originalCenter;
    }

    void OnCollisionEnter(Collision c)
    {
        if (c.gameObject.CompareTag("Ground"))
        {
            isGrounded = true;

            // Last resort for very short hops that never finished restoring
            if (isJumpColliderActive && !isSliding)
                SetJumpCollider(false);
        }
    }

    void OnCollisionStay(Collision c)
    {
        if (c.gameObject.CompareTag("Ground") && rb.linearVelocity.y <= 0.1f)
            isGrounded = true;
    }

    void OnCollisionExit(Collision c)
    {
        if (c.gameObject.CompareTag("Ground"))
            isGrounded = false;
    }

    void OnDestroy()
    {
        if (input != null)
            input.Dispose();
    }
}