using UnityEngine;

public class GameStateUI : MonoBehaviour
{
    [SerializeField] GameObject idlePanel;      // "Press Space to start"
    [SerializeField] GameObject pausePanel;     // "PAUSED"
    [SerializeField] GameObject gameOverPanel;  // "GAME OVER - Press Space to restart"

    void Start()
    {
        // Start (not OnEnable) so GameManager.Instance is guaranteed to exist
        if (GameManager.Instance == null) return;

        GameManager.Instance.OnStateChanged += Refresh;
        Refresh(GameManager.Instance.State);
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnStateChanged -= Refresh;
    }

    void Refresh(GameState state)
    {
        if (idlePanel != null) idlePanel.SetActive(state == GameState.Idle);
        if (pausePanel != null) pausePanel.SetActive(state == GameState.Paused);
        if (gameOverPanel != null) gameOverPanel.SetActive(state == GameState.GameOver);
    }
}