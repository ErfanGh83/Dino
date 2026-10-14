using UnityEngine;

[CreateAssetMenu(fileName = "NewObstacle", menuName = "Dino Game/Obstacle")]
public class ObstacleData : ScriptableObject
{
    public GameObject prefab;

    [Header("Spawn Settings")]
    [Min(0f)]
    public float minDistance = 5f;

    [Min(0f)]
    public float maxDistance = 10f;
}