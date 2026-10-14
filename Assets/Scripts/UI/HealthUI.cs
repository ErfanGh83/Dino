using UnityEngine;
using UnityEngine.UI;

public class HealthUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private Image[] heartImages = new Image[3];

    [Header("Sprites")]
    [SerializeField] private Sprite fullHeart;
    [SerializeField] private Sprite emptyHeart;

    private void OnEnable()
    {
        Bind();
    }

    private void Start()
    {
        // Safety refresh: by now every Awake/OnEnable in the scene has finished
        Bind();
    }

    private void OnDisable()
    {
        if (playerHealth != null)
            playerHealth.HealthChanged -= UpdateHearts;
    }

    private void Bind()
    {
        if (playerHealth == null)
            playerHealth = FindAnyObjectByType<PlayerHealth>();

        if (playerHealth == null)
        {
            Debug.LogWarning("HealthUI: no PlayerHealth found", this);
            return;
        }

        playerHealth.HealthChanged -= UpdateHearts;
        playerHealth.HealthChanged += UpdateHearts;

        Debug.Log($"HealthUI bound to {playerHealth.name}", playerHealth);
        UpdateHearts(playerHealth.CurrentHealth, playerHealth.MaxHealth);
    }

    private void UpdateHearts(int currentHealth, int maxHealth)
    {
        Debug.Log($"HealthUI update: {currentHealth}/{maxHealth}", this);
        for (int i = 0; i < heartImages.Length; i++)
        {
            if (heartImages[i] == null)
                continue;

            heartImages[i].enabled = true;
            heartImages[i].sprite = i < currentHealth ? fullHeart : emptyHeart;
        }
    }
}