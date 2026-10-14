using UnityEngine;

public class PlayerInteractor : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        var gm = GameManager.Instance;
        if (gm == null || gm.State != GameState.Running)
            return;

        Interactable interactable = other.GetComponentInParent<Interactable>();
        if (interactable != null)
            interactable.Interact(gameObject);
    }
}