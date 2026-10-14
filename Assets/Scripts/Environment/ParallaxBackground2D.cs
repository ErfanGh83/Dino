using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class ParallaxBackground2D : MonoBehaviour
{
    [Header("Background")]
    [FormerlySerializedAs("backgroundSprite")]
    [SerializeField] private Sprite daySprite;
    [Tooltip("Optional. Should have the same width as the day sprite. If empty, the day sprite is tinted with the Night Tint color at night instead.")]
    [SerializeField] private Sprite nightSprite;
    [SerializeField] private float scrollSpeed = 1f;

    [Header("Day / Night")]
    [Tooltip("Optional, found automatically in the scene if left empty.")]
    [SerializeField] private DayNightCycle dayNightCycle;
    [Tooltip("Only used when there is no Night Sprite. White = unchanged. Use a dark blue to make the day sprite look like night.")]
    [SerializeField] private Color nightTint = new Color(0.35f, 0.4f, 0.65f, 1f);
    [Tooltip("Seconds the tint takes to fade between day and night. Set to 0 for an instant change.")]
    [SerializeField] private float tintFadeDuration = 2f;

    [Header("Position")]
    [SerializeField] private float backgroundY = 0f;
    [SerializeField] private float backgroundZ = 10f;
    [SerializeField] private int sortingOrder = -10;

    [Header("Movement")]
    [SerializeField] private bool moveOnlyWhenRunning = false;
    [SerializeField] private float speedMultiplier = 0.15f;

    [SerializeField] private Camera mainCamera;
    private readonly List<Transform> tiles = new();
    private readonly List<SpriteRenderer> renderers = new();
    private Sprite currentSprite;
    private float tileWidth;
    private float nextSpawnX;

    private float tintBlend;       // 0 = day color, 1 = night color
    private float targetTintBlend;

    private bool UsesTint => nightSprite == null;

    private void Start()
    {
        if (daySprite == null)
        {
            Debug.LogError("ParallaxBackground2D: Assign a day background sprite.", this);
            enabled = false;
            return;
        }

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        if (mainCamera == null)
        {
            Debug.LogError("ParallaxBackground2D: Assign a Main Camera.", this);
            enabled = false;
            return;
        }

        tileWidth = daySprite.bounds.size.x;

        if (tileWidth <= 0f)
        {
            Debug.LogError("Background sprite has invalid width.", this);
            enabled = false;
            return;
        }

        if (nightSprite != null &&
            !Mathf.Approximately(nightSprite.bounds.size.x, tileWidth))
        {
            Debug.LogWarning(
                "ParallaxBackground2D: The night sprite has a different width than the day sprite. " +
                "Tiles may overlap or leave gaps. Use the same size for both.", this);
        }

        if (dayNightCycle == null)
            dayNightCycle = FindAnyObjectByType<DayNightCycle>();

        // Start with the correct look for the current time of day
        bool startNight = dayNightCycle != null && dayNightCycle.IsNight;
        currentSprite = GetSprite(startNight);
        targetTintBlend = startNight ? 1f : 0f;
        tintBlend = targetTintBlend;

        float cameraHalfWidth = GetCameraHalfWidth();
        float cameraLeft = mainCamera.transform.position.x - cameraHalfWidth;
        float cameraRight = mainCamera.transform.position.x + cameraHalfWidth;

        // Start one tile before the left edge of the camera.
        float firstCenterX = cameraLeft - tileWidth / 2f;
        nextSpawnX = firstCenterX;

        int tileCount = Mathf.CeilToInt(
            (cameraRight - cameraLeft) / tileWidth
        ) + 3;

        for (int i = 0; i < tileCount; i++)
        {
            CreateTile(nextSpawnX + tileWidth / 2f);
            nextSpawnX += tileWidth;
        }

        if (dayNightCycle != null)
            dayNightCycle.NightChanged += OnNightChanged;
    }

    private void Update()
    {
        UpdateTint();

        if (moveOnlyWhenRunning)
        {
            GameManager gm = GameManager.Instance;

            if (gm == null || gm.State != GameState.Running)
                return;
        }

        float movement = scrollSpeed * speedMultiplier * Time.deltaTime;

        float cameraHalfWidth = mainCamera.orthographicSize * mainCamera.aspect;
        float cameraLeft = mainCamera.transform.position.x - cameraHalfWidth;

        for (int i = 0; i < tiles.Count; i++)
        {
            Transform tile = tiles[i];
            tile.position += Vector3.left * movement;

            if (tile.position.x + tileWidth / 2f < cameraLeft - tileWidth)
            {
                tile.position = new Vector3(
                    nextSpawnX,
                    backgroundY,
                    backgroundZ
                );

                nextSpawnX += tileWidth;
            }
        }
    }

    private void UpdateTint()
    {
        if (!UsesTint || Mathf.Approximately(tintBlend, targetTintBlend))
            return;

        if (tintFadeDuration <= 0f)
            tintBlend = targetTintBlend;
        else
            tintBlend = Mathf.MoveTowards(
                tintBlend,
                targetTintBlend,
                Time.deltaTime / tintFadeDuration
            );

        ApplyTint();
    }

    private void ApplyTint()
    {
        Color color = UsesTint
            ? Color.Lerp(Color.white, nightTint, tintBlend)
            : Color.white;

        for (int i = 0; i < renderers.Count; i++)
            renderers[i].color = color;
    }

    private void OnNightChanged(bool night)
    {
        // With a night sprite: swap the sprite. Without one: fade the tint.
        currentSprite = GetSprite(night);
        targetTintBlend = night ? 1f : 0f;

        for (int i = 0; i < renderers.Count; i++)
            renderers[i].sprite = currentSprite;
    }

    private Sprite GetSprite(bool night)
    {
        return night && nightSprite != null ? nightSprite : daySprite;
    }

    private void CreateTile(float centerX)
    {
        GameObject tile = new GameObject("Background Tile");
        tile.transform.SetParent(transform);
        tile.transform.position = new Vector3(centerX, backgroundY, backgroundZ);

        SpriteRenderer renderer = tile.AddComponent<SpriteRenderer>();
        renderer.sprite = currentSprite;
        renderer.sortingOrder = sortingOrder;
        renderer.color = UsesTint
            ? Color.Lerp(Color.white, nightTint, tintBlend)
            : Color.white;

        tiles.Add(tile.transform);
        renderers.Add(renderer);
    }

    private float GetCameraHalfWidth()
    {
        if (mainCamera.orthographic)
        {
            return mainCamera.orthographicSize * mainCamera.aspect;
        }

        float distance = Mathf.Abs(
            backgroundZ - mainCamera.transform.position.z
        );

        return distance *
               Mathf.Tan(mainCamera.fieldOfView * 0.5f * Mathf.Deg2Rad) *
               mainCamera.aspect;
    }

    private void OnDestroy()
    {
        if (dayNightCycle != null)
            dayNightCycle.NightChanged -= OnNightChanged;
    }
}