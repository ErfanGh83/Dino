
using UnityEngine;

public class Obstacle : Interactable
{
    public override void Interact(GameObject player)
    {
        PlayerHealth health = player.GetComponentInParent<PlayerHealth>();

        if (health != null)
        {
            health.TakeDamage();
        }
    }
}
