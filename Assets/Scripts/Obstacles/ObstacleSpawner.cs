using UnityEngine;

public class ObstacleSpawner : MonoBehaviour
{
    [SerializeField] private ObstacleData[] obstacles;
    [SerializeField] private float spawnX = 30f;
    [SerializeField] private float minDistance = 10f;
    [SerializeField] private float maxDistance = 18f;

    private float distanceUntilNextSpawn;
    private PlayerController player;

    void Start()
    {
        player = FindAnyObjectByType<PlayerController>();
        distanceUntilNextSpawn = 5f;
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
            SpawnObstacle();

            distanceUntilNextSpawn = Random.Range(
                minDistance,
                maxDistance
            );
        }
    }

    void SpawnObstacle()
    {
        if (obstacles.Length == 0)
            return;

        ObstacleData data = obstacles[Random.Range(0, obstacles.Length)];

        Instantiate(
            data.prefab,
            new Vector3(
                spawnX,
                data.prefab.transform.position.y,
                data.prefab.transform.position.z
            ),
            data.prefab.transform.rotation
        );
    }
}