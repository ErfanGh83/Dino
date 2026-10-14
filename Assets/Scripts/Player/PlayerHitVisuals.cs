using System.Collections;
using UnityEngine;

public class PlayerHitVisuals : MonoBehaviour
{
    [SerializeField] private PlayerHealth playerHealth;

    [Tooltip("Drag every body part renderer here (Mesh Renderer or Skinned Mesh Renderer).")]
    [SerializeField] private Renderer[] bodyParts;

    [Header("Hit Flash")]
    [SerializeField] private Color hitColor = Color.red;
    [SerializeField] private float hitFlashDuration = 0.2f;

    [Header("Immunity Shine")]
    [SerializeField] private Color immunityColor = new Color(1f, 0.85f, 0.1f);
    [Tooltip("Pulses per second.")]
    [SerializeField] private float pulseSpeed = 4f;
    [Tooltip("Lowest blend toward the shine color, 0 = back to the normal look between pulses.")]
    [SerializeField, Range(0f, 1f)] private float minShine = 0.1f;
    [Tooltip("Highest blend toward the shine color.")]
    [SerializeField, Range(0f, 1f)] private float maxShine = 0.9f;

    private enum Mode { None, Flash, Immune }

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor"); // URP / HDRP Lit
    private static readonly int ColorId = Shader.PropertyToID("_Color");         // Built-in Standard

    private MaterialPropertyBlock block;
    private Coroutine flashRoutine;
    private Mode currentMode = Mode.None;
    private bool isFlashing;
    private bool isImmune;

    private void Awake()
    {
        block = new MaterialPropertyBlock();
    }

    private void OnEnable()
    {
        if (playerHealth == null)
            playerHealth = GetComponent<PlayerHealth>();

        if (playerHealth == null)
            return;

        playerHealth.Damaged += OnDamaged;
        playerHealth.ImmunityChanged += OnImmunityChanged;

        isImmune = playerHealth.IsImmune;
    }

    private void OnDisable()
    {
        if (playerHealth != null)
        {
            playerHealth.Damaged -= OnDamaged;
            playerHealth.ImmunityChanged -= OnImmunityChanged;
        }

        isFlashing = false;
        isImmune = false;
        flashRoutine = null;
        RestoreColors();
        currentMode = Mode.None;
    }

    private void Update()
    {
        // Priority: red flash > immunity shine > normal
        Mode wanted = isFlashing ? Mode.Flash : (isImmune ? Mode.Immune : Mode.None);

        switch (wanted)
        {
            case Mode.None:
                if (currentMode != Mode.None)
                    RestoreColors();
                break;

            case Mode.Flash:
                if (currentMode != Mode.Flash)
                    ApplyColor(hitColor);
                break;

            case Mode.Immune:
                float wave = (Mathf.Sin(Time.time * pulseSpeed * Mathf.PI * 2f) + 1f) * 0.5f;
                float blend = Mathf.Lerp(minShine, maxShine, wave);
                ApplyColor(Color.Lerp(Color.white, immunityColor, blend));
                break;
        }

        currentMode = wanted;
    }

    private void OnDamaged()
    {
        if (flashRoutine != null)
            StopCoroutine(flashRoutine);

        flashRoutine = StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        isFlashing = true;
        yield return new WaitForSeconds(hitFlashDuration); // scaled time, so pausing freezes the flash too
        isFlashing = false;
        flashRoutine = null;
    }

    private void OnImmunityChanged(bool immune)
    {
        isImmune = immune;
    }

    private void ApplyColor(Color color)
    {
        block.Clear();
        block.SetColor(BaseColorId, color);
        block.SetColor(ColorId, color);

        foreach (Renderer r in bodyParts)
        {
            if (r != null)
                r.SetPropertyBlock(block);
        }
    }

    private void RestoreColors()
    {
        // Passing null removes the override, so every part returns to its own original material color
        foreach (Renderer r in bodyParts)
        {
            if (r != null)
                r.SetPropertyBlock(null);
        }
    }
}