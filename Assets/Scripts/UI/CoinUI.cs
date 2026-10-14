using TMPro;
using UnityEngine;

public class CoinUI : MonoBehaviour
{
    [SerializeField] TMP_Text coinText;
    [SerializeField] string prefix = "";   // e.g. "x " or "Coins: "

    void Start()
    {
        // Start (not OnEnable) so GameManager.Instance is guaranteed to exist
        if (GameManager.Instance == null) return;

        GameManager.Instance.OnCoinsChanged += Refresh;
        Refresh(GameManager.Instance.TotalCoins, GameManager.Instance.CoinsThisRun);
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnCoinsChanged -= Refresh;
    }

    void Refresh(int total, int thisRun)
    {
        if (coinText != null)
            coinText.text = prefix + total;
    }
}