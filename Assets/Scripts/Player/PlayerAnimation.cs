using UnityEngine;

public class PlayerAnimation : MonoBehaviour
{
    [SerializeField] Animator animator;
    [SerializeField] PlayerController player;
    [SerializeField] PlayerHealth playerHealth; // optional, found automatically from the player if left empty

    static readonly int IsMovingHash = Animator.StringToHash("IsMoving");
    static readonly int IsDashingHash = Animator.StringToHash("IsDashing");
    static readonly int IsGroundedHash = Animator.StringToHash("IsGrounded");
    static readonly int IsSlidingHash = Animator.StringToHash("IsSliding");
    static readonly int IsGameOverHash = Animator.StringToHash("IsGameover");
    static readonly int GotHitHash = Animator.StringToHash("GotHit");

    void Start()
    {
        if (playerHealth == null && player != null)
            playerHealth = player.GetComponent<PlayerHealth>();

        if (playerHealth != null)
            playerHealth.Damaged += OnDamaged;

        if (GameManager.Instance != null)
            GameManager.Instance.OnStateChanged += OnStateChanged;

        OnStateChanged(GameManager.Instance != null
            ? GameManager.Instance.State
            : GameState.Idle);
    }

    void Update()
    {
        animator.SetBool(IsDashingHash, player.IsDashing);
        animator.SetBool(IsGroundedHash, player.IsGrounded);
        animator.SetBool(IsSlidingHash, player.IsSliding);
    }

    void OnDamaged()
    {
        // The killing hit goes straight to the game over animation instead
        if (playerHealth != null && playerHealth.IsDead)
            return;

        animator.SetTrigger(GotHitHash);
    }

    void OnStateChanged(GameState state)
    {
        animator.SetBool(IsMovingHash,
            state == GameState.Running || state == GameState.Paused);

        animator.SetBool(IsGameOverHash, state == GameState.GameOver);
    }

    void OnDestroy()
    {
        if (playerHealth != null)
            playerHealth.Damaged -= OnDamaged;

        if (GameManager.Instance != null)
            GameManager.Instance.OnStateChanged -= OnStateChanged;
    }
}