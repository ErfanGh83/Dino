using UnityEngine;

public class ExitButton : MonoBehaviour
{
    public void ExitGame()
    {
#if UNITY_EDITOR
        // Application.Quit() does nothing in the editor, so stop Play mode instead
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}