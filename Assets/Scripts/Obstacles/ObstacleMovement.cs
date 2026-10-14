using UnityEngine;

public class ObstacleMovement : MonoBehaviour
{
    [SerializeField] private float destroyX = -20f;

    private PlayerController player;

    void Start()
    {
        player = FindAnyObjectByType<PlayerController>();
    }

    void Update()
    {
        if (GameManager.Instance == null)
            return;

        if (GameManager.Instance.State != GameState.Running)
            return;

        bool sprinting = player != null && player.IsDashing;

        float speed = GameManager.Instance.GetGameSpeed(sprinting);

        transform.position += Vector3.left * speed * Time.deltaTime;

        if (transform.position.x <= destroyX)
            Destroy(gameObject);
    }
}