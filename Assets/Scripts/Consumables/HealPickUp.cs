using UnityEngine;

public class HealPickup : Interactable
{
    [SerializeField] private int healAmount = 1;
    [Tooltip("If off, the item is ignored (not used up) while the player is at full health.")]
    [SerializeField] private bool consumeWhenFull = false;

    private bool collected;

    public override void Interact(GameObject player)
    {
        if (collected) return;

        PlayerHealth health = player.GetComponentInParent<PlayerHealth>();
        if (health == null) return;

        if (!consumeWhenFull && health.CurrentHealth >= health.MaxHealth)
            return;

        collected = true;
        health.Heal(healAmount);
        Destroy(gameObject);
    }
}