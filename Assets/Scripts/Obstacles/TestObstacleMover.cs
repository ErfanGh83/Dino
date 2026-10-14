using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class TestObstacleMover : MonoBehaviour
{
    [SerializeField] float speed = 8f;
    [SerializeField] bool moveLeft = true;

    Rigidbody rb;
    float logTimer;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
    }

    void FixedUpdate()
    {
        if (GameManager.Instance == null) return;
        if (GameManager.Instance.State != GameState.Running) return;

        float direction = moveLeft ? -1f : 1f;
        Vector3 next = rb.position + Vector3.right * direction * speed * Time.fixedDeltaTime;
        rb.MovePosition(next);

        logTimer += Time.fixedDeltaTime;
        if (logTimer >= 1f)
        {
            logTimer = 0f;
            Debug.Log("Obstacle position: " + rb.position + " | speed: " + speed);
        }
    }
}