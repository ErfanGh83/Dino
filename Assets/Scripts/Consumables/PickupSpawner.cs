using System.Collections.Generic;
using UnityEngine;

public class PickupSpawner : MonoBehaviour
{
    [System.Serializable]
    public class PickupEntry
    {
        public string name = "Coin";
        public GameObject prefab;

        [Tooltip("Relative chance. Coin 90 and Heal 10 means heal is picked about 1 time in 10.")]
        [Min(0f)] public float weight = 90f;

        [Header("Group")]
        [Min(1)] public int minCount = 3;
        [Min(1)] public int maxCount = 6;
        [Tooltip("Distance between items in a group.")]
        public float spacing = 1.5f;

        [Header("Height (added to the prefab's Y)")]
        public float minExtraHeight = 0f;
        public float maxExtraHeight = 2f;

        [Header("Rules")]
        [Tooltip("Only spawn when the player is missing health (use this for healing items).")]
        public bool onlyWhenPlayerDamaged = false;
    }

    [SerializeField] private PickupEntry[] entries;
    [SerializeField] private float spawnX = 30f;
    [SerializeField] private float firstSpawnDistance = 12f;
    [SerializeField] private float minDistance = 8f;
    [SerializeField] private float maxDistance = 16f;

    private float distanceUntilNextSpawn;
    private PlayerController player;
    private PlayerHealth playerHealth;

    void Start()
    {
        player = FindAnyObjectByType<PlayerController>();
        playerHealth = FindAnyObjectByType<PlayerHealth>();
        distanceUntilNextSpawn = firstSpawnDistance;
    }

    void Update()
    {
        if (GameManager.Instance == null)
            return;

        if (GameManager.Instance.State != GameState.Running)
            return;

        bool sprinting = player != null && player.IsDashing;
        float speed = GameManager.Instance.GetGameSpeed(sprinting);

        distanceUntilNextSpawn -= speed * Time.deltaTime;

        if (distanceUntilNextSpawn <= 0f)
        {
            float groupLength = SpawnPickups();
            distanceUntilNextSpawn = Random.Range(minDistance, maxDistance) + groupLength;
        }
    }

    // Returns the length of the spawned group so the next spawn starts after it
    float SpawnPickups()
    {
        PickupEntry entry = PickEntry();
        if (entry == null || entry.prefab == null)
            return 0f;

        int count = Random.Range(entry.minCount, Mathf.Max(entry.minCount, entry.maxCount) + 1);
        float height = Random.Range(entry.minExtraHeight, Mathf.Max(entry.minExtraHeight, entry.maxExtraHeight));

        for (int i = 0; i < count; i++)
        {
            Instantiate(
                entry.prefab,
                new Vector3(
                    spawnX + i * entry.spacing,
                    entry.prefab.transform.position.y + height,
                    entry.prefab.transform.position.z
                ),
                entry.prefab.transform.rotation
            );
        }

        return (count - 1) * entry.spacing;
    }

    bool IsEligible(PickupEntry e)
    {
        if (e == null || e.prefab == null || e.weight <= 0f)
            return false;

        if (e.onlyWhenPlayerDamaged)
        {
            // No player health found, or already at full health: skip healing items
            if (playerHealth == null || playerHealth.CurrentHealth >= playerHealth.MaxHealth)
                return false;
        }

        return true;
    }

    PickupEntry PickEntry()
    {
        if (entries == null || entries.Length == 0)
            return null;

        float total = 0f;
        foreach (PickupEntry e in entries)
            if (IsEligible(e)) total += e.weight;

        if (total <= 0f)
            return null;

        float roll = Random.Range(0f, total);
        PickupEntry last = null;

        foreach (PickupEntry e in entries)
        {
            if (!IsEligible(e)) continue;

            last = e;
            roll -= e.weight;
            if (roll <= 0f)
                return e;
        }

        return last;
    }
}