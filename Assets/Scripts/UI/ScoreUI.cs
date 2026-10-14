using TMPro;
using UnityEngine;

public class ScoreUI : MonoBehaviour
{
    [Header("Texts (TextMeshPro)")]
    [SerializeField] TMP_Text scoreText;
    [SerializeField] TMP_Text highScoreText;

    [Header("Format")]
    [Tooltip("D5 = always 5 digits with leading zeros (00123). Use D1 for no leading zeros.")]
    [SerializeField] string numberFormat = "D5";
    void Start()
    {
        // Start (not OnEnable) so GameManager.Instance is guaranteed to exist
        if (GameManager.Instance == null) return;

        GameManager.Instance.OnScoreChanged += UpdateScore;
        GameManager.Instance.OnHighScoreChanged += UpdateHighScore;

        // Show the saved high score right away, even before the game starts
        UpdateScore(GameManager.Instance.Score);
        UpdateHighScore(GameManager.Instance.HighScore);
    }

    void OnDestroy()
    {
        if (GameManager.Instance == null) return;

        GameManager.Instance.OnScoreChanged -= UpdateScore;
        GameManager.Instance.OnHighScoreChanged -= UpdateHighScore;
    }

    void UpdateScore(int score)
    {
        if (scoreText != null)
            scoreText.text = score.ToString(numberFormat);
    }

    void UpdateHighScore(int highScore)
    {
        if (highScoreText != null)
            highScoreText.text = highScore.ToString(numberFormat);
    }
}