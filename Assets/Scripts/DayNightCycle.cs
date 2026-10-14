using System.Collections;
using UnityEngine;

public class DayNightCycle : MonoBehaviour
{
    [Header("Skyboxes")]
    [SerializeField] Material daySkybox;
    [SerializeField] Material nightSkybox;

    [Header("Switching")]
    [Tooltip("The sky toggles every time the score passes a multiple of this value (500 = night at 500, day at 1000, night at 1500...).")]
    [SerializeField] int pointsPerSwitch = 500;
    [SerializeField] bool startAtNight = false;

    [Header("Directional Light (optional)")]
    [SerializeField] Light sun;
    [SerializeField] Color dayLightColor = new Color(1f, 0.96f, 0.84f);
    [SerializeField] float dayLightIntensity = 1f;
    [SerializeField] Color nightLightColor = new Color(0.45f, 0.55f, 0.9f);
    [SerializeField] float nightLightIntensity = 0.25f;

    [Header("Ambient Light (optional)")]
    [Tooltip("Tints the ambient light too. Only works if Environment Lighting Source is set to Color in the Lighting window.")]
    [SerializeField] bool changeAmbient = false;
    [SerializeField] Color dayAmbient = new Color(0.6f, 0.65f, 0.7f);
    [SerializeField] Color nightAmbient = new Color(0.1f, 0.12f, 0.2f);

    [Header("Transition")]
    [Tooltip("Seconds the light takes to fade between day and night. The skybox itself swaps instantly. Set to 0 for an instant change.")]
    [SerializeField] float transitionDuration = 2f;

    bool isNight;
    public bool IsNight => isNight;
    public event System.Action<bool> NightChanged;
    bool initialized;
    Coroutine transition;

    void Start()
    {
        int score = 0;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnScoreChanged += OnScoreChanged;
            score = GameManager.Instance.Score;
        }

        // Apply the starting look immediately, without a fade
        isNight = ShouldBeNight(score);
        ApplySkybox(isNight);
        ApplyLighting(isNight ? 1f : 0f);
        initialized = true;
    }

    void Awake()
    {
        isNight = ShouldBeNight(0);
    }

    void OnScoreChanged(int score)
    {
        if (!initialized) return;

        bool shouldBeNight = ShouldBeNight(score);
        if (shouldBeNight == isNight) return;

        isNight = shouldBeNight;
        ApplySkybox(isNight);
        NightChanged?.Invoke(isNight);

        if (transition != null)
            StopCoroutine(transition);

        if (transitionDuration <= 0f)
            ApplyLighting(isNight ? 1f : 0f);
        else
            transition = StartCoroutine(FadeLighting(isNight));
    }

    bool ShouldBeNight(int score)
    {
        if (pointsPerSwitch <= 0)
            return startAtNight;

        int phase = score / pointsPerSwitch; // 0 before the first switch, 1 after it, ...
        bool oddPhase = phase % 2 == 1;
        return startAtNight ? !oddPhase : oddPhase;
    }

    void ApplySkybox(bool night)
    {
        Material sky = night ? nightSkybox : daySkybox;
        if (sky == null) return;

        RenderSettings.skybox = sky;
        DynamicGI.UpdateEnvironment(); // refreshes skybox-based ambient light and reflections
    }

    IEnumerator FadeLighting(bool toNight)
    {
        float from = GetCurrentBlend();
        float to = toNight ? 1f : 0f;
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime / transitionDuration;
            ApplyLighting(Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t)));
            yield return null;
        }

        ApplyLighting(to);
        transition = null;
    }

    // 0 = full day, 1 = full night
    void ApplyLighting(float blend)
    {
        if (sun != null)
        {
            sun.color = Color.Lerp(dayLightColor, nightLightColor, blend);
            sun.intensity = Mathf.Lerp(dayLightIntensity, nightLightIntensity, blend);
        }

        if (changeAmbient)
            RenderSettings.ambientLight = Color.Lerp(dayAmbient, nightAmbient, blend);
    }

    float GetCurrentBlend()
    {
        // Works out how far the light is currently between day and night, so an interrupted fade continues smoothly
        if (sun == null || Mathf.Approximately(dayLightIntensity, nightLightIntensity))
            return isNight ? 0f : 1f; // no light to measure, so just restart from the opposite end

        return Mathf.InverseLerp(dayLightIntensity, nightLightIntensity, sun.intensity);
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnScoreChanged -= OnScoreChanged;
    }
}