
using System.Collections.Generic;
using UnityEngine;

public class EndlessGround : MonoBehaviour
{
    [Header("Visual Ground")]
    [SerializeField] private GameObject sandBlockPrefab;
    [SerializeField] private int blocksPerChunk = 20;
    [SerializeField] private int blocksWide = 5;
    [SerializeField] private int chunkCount = 4;

    [Header("Physical Ground")]
    [SerializeField] private Transform player;
    [SerializeField] private float groundTopY = 0f;
    [SerializeField] private float physicalGroundLength = 100f;

    private readonly List<Transform> chunks = new();

    private float ChunkLength => blocksPerChunk;

    private void Start()
    {
        if (sandBlockPrefab == null)
        {
            Debug.LogError("Assign the Sand Block Prefab.");
            enabled = false;
            return;
        }

        if (blocksPerChunk < 1 || blocksWide < 1 || chunkCount < 2)
        {
            Debug.LogError("Ground dimensions are invalid.");
            enabled = false;
            return;
        }

        CreatePhysicalGround();

        float startX = player != null
            ? player.position.x
            : transform.position.x;

        for (int i = 0; i < chunkCount; i++)
        {
            CreateVisualChunk(startX + i * ChunkLength);
        }
    }

    private void CreatePhysicalGround()
    {
        GameObject ground = new GameObject("Physical Ground");
        ground.transform.SetParent(transform);

        float playerX = player != null
            ? player.position.x
            : transform.position.x;

        ground.transform.position = new Vector3(
            playerX,
            groundTopY - 0.5f,
            0f
        );

        BoxCollider groundCollider = ground.AddComponent<BoxCollider>();
        groundCollider.size = new Vector3(
            physicalGroundLength,
            1f,
            blocksWide
        );

        // Keep this collider enabled for reliable grounding.
        groundCollider.enabled = true;
        ground.tag = "Ground";
    }

    private void CreateVisualChunk(float centerX)
    {
        GameObject chunk = new GameObject("Visual Ground Chunk");
        chunk.transform.SetParent(transform);
        chunk.transform.position = new Vector3(centerX, 0f, 0f);

        for (int x = 0; x < blocksPerChunk; x++)
        {
            for (int z = 0; z < blocksWide; z++)
            {
                GameObject block = Instantiate(
                    sandBlockPrefab,
                    chunk.transform
                );

                block.transform.localPosition = new Vector3(
                    -ChunkLength / 2f + x + 0.5f,
                    groundTopY - 0.5f,
                    -blocksWide / 2f + z + 0.5f
                );

                block.transform.localRotation = Quaternion.identity;
                block.transform.localScale = Vector3.one;

                // Visual blocks must not affect player physics.
                foreach (Collider col in
                         block.GetComponentsInChildren<Collider>())
                {
                    col.enabled = false;
                }
            }
        }

        chunks.Add(chunk.transform);
    }

    private void Update()
    {
        GameManager gm = GameManager.Instance;

        if (gm == null || gm.State != GameState.Running)
            return;

        float speed = gm.GameSpeed;

        foreach (Transform chunk in chunks)
        {
            chunk.position += Vector3.left * speed * Time.deltaTime;
        }

        RecycleChunks();
    }

    private void RecycleChunks()
    {
        float playerX = player != null
            ? player.position.x
            : transform.position.x;

        foreach (Transform chunk in chunks)
        {
            if (chunk.position.x + ChunkLength / 2f
                >= playerX - ChunkLength)
                continue;

            float farthestX = float.NegativeInfinity;

            foreach (Transform other in chunks)
            {
                if (other.position.x > farthestX)
                    farthestX = other.position.x;
            }

            chunk.position = new Vector3(
                farthestX + ChunkLength,
                chunk.position.y,
                chunk.position.z
            );
        }
    }
}
