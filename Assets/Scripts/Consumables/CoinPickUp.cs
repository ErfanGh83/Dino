using UnityEngine;

public class CoinPickup : Interactable
{
    [SerializeField] private int value = 1;

    private bool collected;

    public override void Interact(GameObject player)
    {
        if (collected) return;
        collected = true;

        if (GameManager.Instance != null)
            GameManager.Instance.AddCoins(value);

        Destroy(gameObject);
    }
}