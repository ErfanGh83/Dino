using UnityEngine;

public class RestartButton : MonoBehaviour
{
    public void Restart()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.Restart();
    }
}