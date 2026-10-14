
using System.Collections.Generic;
using UnityEngine;

public class EndlessMountainGenerator : MonoBehaviour
{
    [Header("Mountain Prefabs")]
    [SerializeField] private GameObject[] mountainPrefabs;

    [Header("Generation")]
    [SerializeField] private int initialMountainCount = 6;
    [SerializeField] private float spawnAheadDistance = 40f;
    [SerializeField] private float minSpacing = 18f;
    [SerializeField] private float maxSpacing = 35f;

    [Header("Position")]
    [SerializeField] private float groundY = 0f;
    [SerializeField] private float zPosition = 20f;
    [SerializeField] private float randomZOffset = 0f;
    [SerializeField] private float randomScaleMin = 0.9f;
    [SerializeField] private float randomScaleMax = 1.2f;

    [Header("Movement")]
    [SerializeField] private float speedMultiplier = 0.25f;
    [SerializeField] private float recycleMargin = 30f;

    private readonly List<Transform> mountains = new();
    private Camera mainCamera;
    private float nextSpawnX;

    private void Start()
    {
        if (mountainPrefabs == null || mountainPrefabs.Length == 0)
        {
            Debug.LogError("Assign at least one mountain prefab.");
            enabled = false;
            return;
        }

        mainCamera = Camera.main;

        if (mainCamera == null)
        {
            Debug.LogError("No Main Camera found.");
            enabled = false;
            return;
        }

        float cameraHalfWidth = GetCameraHalfWidth();
        float cameraLeft = mainCamera.transform.position.x - cameraHalfWidth;
        float cameraRight = mainCamera.transform.position.x + cameraHalfWidth;

        // Begin spawning just before the visible left edge.
        nextSpawnX = cameraLeft - maxSpacing;

        float visibleWidth = cameraRight - cameraLeft;
        int requiredCount = Mathf.CeilToInt(
            visibleWidth / Mathf.Max(1f, minSpacing)
        ) + initialMountainCount;

        int count = Mathf.Max(initialMountainCount, requiredCount);

        for (int i = 0; i < count; i++)
        {
            float spacing = GetSpacing();
            nextSpawnX += spacing;
            SpawnMountain(nextSpawnX);
        }

        // Keep the next spawn position beyond the visible area.
        nextSpawnX = Mathf.Max(nextSpawnX, cameraRight + spawnAheadDistance);
    }

    private void Update()
    {
        GameManager gm = GameManager.Instance;

        if (gm == null || gm.State != GameState.Running)
            return;

        float speed = gm.GameSpeed * speedMultiplier;
        float cameraLeft = mainCamera.transform.position.x
                           - GetCameraHalfWidth();

        for (int i = 0; i < mountains.Count; i++)
        {
            Transform mountain = mountains[i];

            if (mountain == null)
                continue;

            mountain.position += Vector3.left * speed * Time.deltaTime;

            if (mountain.position.x < cameraLeft - recycleMargin)
            {
                // Move the mountain to the next position ahead.
                mountain.position = new Vector3(
                    nextSpawnX,
                    groundY,
                    mountain.position.z
                );

                SetRandomMountain(mountain);
                nextSpawnX += GetSpacing();
            }
        }
    }

    private void SpawnMountain(float x)
    {
        GameObject prefab = GetRandomPrefab();

        GameObject mountain = Instantiate(
            prefab,
            new Vector3(
                x,
                groundY,
                zPosition + Random.Range(-randomZOffset, randomZOffset)
            ),
            prefab.transform.rotation,
            transform
        );

        ApplyRandomScale(mountain.transform, prefab);
        mountains.Add(mountain.transform);
    }

    private void SetRandomMountain(Transform mountain)
    {
        GameObject prefab = GetRandomPrefab();

        Vector3 position = mountain.position;
        Transform parent = mountain.parent;
        int index = mountains.IndexOf(mountain);

        Destroy(mountain.gameObject);

        GameObject replacement = Instantiate(
            prefab,
            position,
            prefab.transform.rotation,
            parent
        );

        replacement.transform.position = new Vector3(
            position.x,
            groundY,
            zPosition + Random.Range(-randomZOffset, randomZOffset)
        );

        ApplyRandomScale(replacement.transform, prefab);
        mountains[index] = replacement.transform;
    }

    private void ApplyRandomScale(Transform mountain, GameObject prefab)
    {
        float scale = Random.Range(randomScaleMin, randomScaleMax);
        mountain.localScale = prefab.transform.localScale * scale;
    }

    private float GetCameraHalfWidth()
    {
        if (mainCamera.orthographic)
            return mainCamera.orthographicSize * mainCamera.aspect;

        float distance = Mathf.Abs(zPosition - mainCamera.transform.position.z);

        return distance * Mathf.Tan(
            mainCamera.fieldOfView * 0.5f * Mathf.Deg2Rad
        ) * mainCamera.aspect;
    }

    private GameObject GetRandomPrefab()
    {
        return mountainPrefabs[Random.Range(0, mountainPrefabs.Length)];
    }

    private float GetSpacing()
    {
        if (maxSpacing < minSpacing)
            return minSpacing;

        return Random.Range(minSpacing, maxSpacing);
    }
}
