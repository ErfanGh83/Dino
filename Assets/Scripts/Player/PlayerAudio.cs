using UnityEngine;

public class PlayerAudio : MonoBehaviour
{
    [Header("References")]
    [SerializeField] AudioSource sfxSource;       // one-shot sounds (added automatically if empty)
    [SerializeField] AudioSource dashLoopSource;  // loops while sprinting (added automatically if empty)
    [SerializeField] AudioSource slideLoopSource; // loops while sliding (added automatically if empty)
    [SerializeField] AudioSource coinSource;      // coin pickups (added automatically if empty)
    [SerializeField] AudioSource healSource;      // healing (added automatically if empty)
    [SerializeField] PlayerController player;
    [SerializeField] PlayerHealth playerHealth;   // optional, found automatically from the player if left empty

    [Header("Clips")]
    [SerializeField] AudioClip[] footsteps;
    [SerializeField] AudioClip[] jump;
    [SerializeField] AudioClip[] land;
    [SerializeField] AudioClip[] dashStart;       // one-shot when sprinting begins
    [SerializeField] AudioClip dashLoop;          // fast jogging loop while sprinting
    [SerializeField] AudioClip slideStart;
    [SerializeField] AudioClip slideLoop;
    [SerializeField] AudioClip[] hit;
    [SerializeField] AudioClip healClip;
    [SerializeField] AudioClip coinClip;
    [SerializeField] AudioClip gameOver;

    [Header("Settings")]
    [SerializeField] float footstepInterval = 0.3f;
    [SerializeField, Range(0f, 0.3f)] float pitchVariation = 0.08f;
    [SerializeField, Range(0f, 1f)] float volume = 1f;

    bool wasDashing;
    bool wasGrounded;
    bool wasSliding;
    float footstepTimer;
    int lastCoinsThisRun;
    int lastHealth;
    GameState currentState = GameState.Idle;

    void Awake()
    {
        if (sfxSource == null)
            sfxSource = CreateSource();
        if (dashLoopSource == null)
            dashLoopSource = CreateSource();
        if (slideLoopSource == null)
            slideLoopSource = CreateSource();
        if (coinSource == null)
            coinSource = CreateSource();
        if (healSource == null)
            healSource = CreateSource();
    }

    AudioSource CreateSource()
    {
        var source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        return source;
    }

    void Start()
    {
        if (playerHealth == null && player != null)
            playerHealth = player.GetComponent<PlayerHealth>();

        if (playerHealth != null)
        {
            playerHealth.Damaged += OnDamaged;
            lastHealth = playerHealth.CurrentHealth;
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnStateChanged += OnStateChanged;
            GameManager.Instance.OnCoinsChanged += OnCoinsChanged;
            currentState = GameManager.Instance.State;
            lastCoinsThisRun = GameManager.Instance.CoinsThisRun;
        }

        wasDashing = player.IsDashing;
        wasGrounded = player.IsGrounded;
        wasSliding = player.IsSliding;
    }

    void Update()
    {
        bool dashing = player.IsDashing; // IsDashing is the sprint input
        bool grounded = player.IsGrounded;
        bool sliding = player.IsSliding;

        // One-shots on state changes
        bool dashJustStarted = dashing && !wasDashing;

        // No sprint start sound while sliding
        if (dashJustStarted && !sliding)
            PlayRandom(dashStart);

        // Skip only on the exact frame sprinting starts, in case it lifts the player off the ground
        if (!grounded && wasGrounded && !dashJustStarted)
            PlayRandom(jump);

        if (grounded && !wasGrounded)
            PlayRandom(land);

        if (sliding && !wasSliding)
            Play(slideStart);

        // Loops only play while grounded (and not paused / game over)
        // The sprint loop is also muted while sliding, and resumes after the slide if sprint is still held
        bool audible = currentState != GameState.Paused && currentState != GameState.GameOver;
        SetLoop(dashLoopSource, dashLoop, audible && dashing && grounded && !sliding);
        SetLoop(slideLoopSource, slideLoop, audible && sliding && grounded);

        UpdateFootsteps(grounded, sliding, dashing);
        UpdateHeal();

        wasDashing = dashing;
        wasGrounded = grounded;
        wasSliding = sliding;
    }

    void UpdateFootsteps(bool grounded, bool sliding, bool dashing)
    {
        // No footsteps in the air; sprinting has its own jogging loop
        bool shouldStep = currentState == GameState.Running && grounded && !sliding && !dashing;

        if (!shouldStep)
        {
            // Reset so the first step plays right away when running resumes
            footstepTimer = 0f;
            return;
        }

        footstepTimer -= Time.deltaTime;
        if (footstepTimer <= 0f)
        {
            PlayRandom(footsteps);
            footstepTimer = footstepInterval;
        }
    }

    void UpdateHeal()
    {
        if (playerHealth == null)
            return;

        int health = playerHealth.CurrentHealth;

        // Health went up = healed (damage lowers it, so it never triggers this)
        if (health > lastHealth && healClip != null)
            healSource.PlayOneShot(healClip, volume);

        lastHealth = health;
    }

    void OnDamaged()
    {
        // The killing hit goes straight to the game over sound instead
        if (playerHealth != null && playerHealth.IsDead)
            return;

        PlayRandom(hit);
    }

    void OnStateChanged(GameState state)
    {
        currentState = state;

        if (state == GameState.GameOver)
            Play(gameOver);
    }

    void OnCoinsChanged(int totalCoins, int coinsThisRun)
    {
        // Only play when coins were collected, not when they're spent in a shop
        if (coinsThisRun > lastCoinsThisRun && coinClip != null)
            coinSource.PlayOneShot(coinClip, volume);

        lastCoinsThisRun = coinsThisRun;
    }

    // ---- Helpers ----

    void SetLoop(AudioSource source, AudioClip clip, bool shouldPlay)
    {
        if (source == null || clip == null) return;

        if (shouldPlay && !source.isPlaying)
        {
            source.clip = clip;
            source.loop = true;
            source.volume = volume;
            source.Play();
        }
        else if (!shouldPlay && source.isPlaying)
        {
            source.Stop();
        }
    }

    void PlayRandom(AudioClip[] clips)
    {
        if (clips == null || clips.Length == 0) return;
        Play(clips[Random.Range(0, clips.Length)]);
    }

    void Play(AudioClip clip)
    {
        if (clip == null) return;
        sfxSource.pitch = 1f + Random.Range(-pitchVariation, pitchVariation);
        sfxSource.PlayOneShot(clip, volume);
    }

    void OnDestroy()
    {
        if (playerHealth != null)
            playerHealth.Damaged -= OnDamaged;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnStateChanged -= OnStateChanged;
            GameManager.Instance.OnCoinsChanged -= OnCoinsChanged;
        }
    }
}