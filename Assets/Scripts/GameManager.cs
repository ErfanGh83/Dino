using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum GameState { Idle, Running, Paused, GameOver }

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public GameState State { get; private set; } = GameState.Idle;
    public int StateChangedFrame { get; private set; }

    public float GameSpeed { get; private set; } = 8f;

    // ---- Score ----
    const string HighScoreKey = "HighScore";

    public int Score { get; private set; }
    public int HighScore { get; private set; }

    /// <summary>True when the current run has beaten the high score that was saved before it started.</summary>
    public bool IsNewHighScore => Score > highScoreAtRunStart;

    // ---- Coins ----
    const string TotalCoinsKey = "TotalCoins";

    /// <summary>All coins the player ever collected. Saved between sessions.</summary>
    public int TotalCoins { get; private set; }

    /// <summary>Coins collected in the current run only.</summary>
    public int CoinsThisRun { get; private set; }

    public event Action<GameState> OnStateChanged;
    public event Action<int> OnScoreChanged;
    public event Action<int> OnHighScoreChanged;
    public event Action<int, int> OnCoinsChanged; // (totalCoins, coinsThisRun)

    InputSystem_Actions input;

    [SerializeField] float sprintMultiplier = 1.5f;

    [Header("Score")]
    [Tooltip("Points per second = game speed x this value.")]
    [SerializeField] float scoreMultiplier = 1f;

    float scoreValue;          // exact score, the public Score is this rounded down
    int highScoreAtRunStart;

    void Awake()
    {
        Instance = this;
        Time.timeScale = 1f; // make sure a restart after a pause never starts frozen
        input = new InputSystem_Actions();

        HighScore = PlayerPrefs.GetInt(HighScoreKey, 0);
        highScoreAtRunStart = HighScore;

        TotalCoins = PlayerPrefs.GetInt(TotalCoinsKey, 0);
    }

    void OnEnable()
    {
        input.Player.Enable();
        input.Player.Jump.performed += OnConfirm;
        input.Player.Pause.performed += OnPausePerformed;
    }

    void OnDisable()
    {
        input.Player.Jump.performed -= OnConfirm;
        input.Player.Pause.performed -= OnPausePerformed;
        input.Player.Disable();
    }

    void OnDestroy()
    {
        if (input != null)
            input.Dispose();

        if (Instance == this)
            Time.timeScale = 1f;
    }

    void OnApplicationQuit()
    {
        // Covers closing the game (or stopping Play mode) in the middle of a run
        SaveData();
    }

    void Update()
    {
        if (State == GameState.Running)
        {
            GameSpeed += 0.1f * Time.deltaTime;

            bool sprinting = input.Player.Sprint.IsPressed();
            scoreValue += GetGameSpeed(sprinting) * scoreMultiplier * Time.deltaTime;
            SetScore(Mathf.FloorToInt(scoreValue));
        }
    }

    void SetScore(int newScore)
    {
        if (newScore == Score)
            return;

        Score = newScore;
        OnScoreChanged?.Invoke(Score);

        // The high score follows the score live, but it is only written to disk at game over / quit
        if (Score > HighScore)
        {
            HighScore = Score;
            OnHighScoreChanged?.Invoke(HighScore);
        }
    }

    public void AddCoins(int amount = 1)
    {
        if (amount <= 0)
            return;

        TotalCoins += amount;
        CoinsThisRun += amount;

        PlayerPrefs.SetInt(TotalCoinsKey, TotalCoins); // stored in memory now, written to disk in SaveData
        OnCoinsChanged?.Invoke(TotalCoins, CoinsThisRun);
    }

    /// <summary>Spend coins (for a future shop). Returns false if the player can't afford it.</summary>
    public bool TrySpendCoins(int amount)
    {
        if (amount <= 0 || amount > TotalCoins)
            return false;

        TotalCoins -= amount;
        PlayerPrefs.SetInt(TotalCoinsKey, TotalCoins);
        OnCoinsChanged?.Invoke(TotalCoins, CoinsThisRun);
        return true;
    }

    void SaveData()
    {
        if (HighScore > PlayerPrefs.GetInt(HighScoreKey, 0))
            PlayerPrefs.SetInt(HighScoreKey, HighScore);

        PlayerPrefs.SetInt(TotalCoinsKey, TotalCoins);
        PlayerPrefs.Save();
    }

    // Right-click the GameManager component header in the Inspector to use these while testing
    [ContextMenu("Reset High Score")]
    void ResetHighScore()
    {
        PlayerPrefs.DeleteKey(HighScoreKey);
        PlayerPrefs.Save();

        HighScore = 0;
        highScoreAtRunStart = 0;
        OnHighScoreChanged?.Invoke(HighScore);
    }

    [ContextMenu("Reset Coins")]
    void ResetCoins()
    {
        PlayerPrefs.DeleteKey(TotalCoinsKey);
        PlayerPrefs.Save();

        TotalCoins = 0;
        CoinsThisRun = 0;
        OnCoinsChanged?.Invoke(TotalCoins, CoinsThisRun);
    }

    void OnConfirm(UnityEngine.InputSystem.InputAction.CallbackContext ctx)
    {
        if (State == GameState.Idle)
            SetState(GameState.Running);
        else if (State == GameState.GameOver)
            Restart();
        // Paused: Space does nothing, only the pause key resumes
    }

    void OnPausePerformed(UnityEngine.InputSystem.InputAction.CallbackContext ctx)
    {
        TogglePause();
    }

    public void TogglePause()
    {
        if (State == GameState.Running) Pause();
        else if (State == GameState.Paused) Resume();
    }

    public void Pause()
    {
        if (State != GameState.Running) return;
        SetState(GameState.Paused);
    }

    public void Resume()
    {
        if (State != GameState.Paused) return;
        SetState(GameState.Running);
    }

    public void GameOver()
    {
        if (State == GameState.GameOver) return; // ignore duplicate calls
        SetState(GameState.GameOver);
    }

    public void Restart()
    {
        SaveData();
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    void SetState(GameState s)
    {
        State = s;
        StateChangedFrame = Time.frameCount;

        // Pausing freezes physics, animations and Time.deltaTime-based movement
        Time.timeScale = (s == GameState.Paused) ? 0f : 1f;

        if (s == GameState.GameOver)
            SaveData();

        OnStateChanged?.Invoke(s);
    }

    public float GetGameSpeed(bool sprinting)
    {
        return sprinting
            ? GameSpeed * sprintMultiplier
            : GameSpeed;
    }
}