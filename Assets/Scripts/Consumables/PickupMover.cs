using UnityEngine;

public class PickupMover : MonoBehaviour
{
    [SerializeField] private float despawnX = -20f;

    private static PlayerController player; // shared, so each pickup doesn't search for it

    void Update()
    {
        var gm = GameManager.Instance;
        if (gm == null || gm.State != GameState.Running)
            return;

        if (player == null)
            player = FindAnyObjectByType<PlayerController>();

        bool sprinting = player != null && player.IsDashing;
        float speed = gm.GetGameSpeed(sprinting);

        transform.Translate(Vector3.left * speed * Time.deltaTime, Space.World);

        if (transform.position.x < despawnX)
            Destroy(gameObject);
    }
}