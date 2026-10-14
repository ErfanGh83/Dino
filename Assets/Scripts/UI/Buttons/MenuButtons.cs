using UnityEngine;

public class MenuButtons : MonoBehaviour
{
    public void Resume()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.Resume();
    }

    public void Pause()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.Pause();
    }

    public void Restart()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.Restart();
    }

    public void Exit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}