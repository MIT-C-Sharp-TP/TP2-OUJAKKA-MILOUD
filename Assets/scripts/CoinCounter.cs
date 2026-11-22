using TMPro;
using UnityEngine;

// Counts the coins and the score, and shows them on the screen.
// It listens to Coin.Collected: no coin needs to know that this object exists.
public class CoinCounter : MonoBehaviour
{
    [SerializeField] private TMP_Text scoreText;   // drag the TextMeshPro text here

    private int coins;        // coins collected so far
    private int coinsToWin;   // coins in the scene at the start
    private int score;        // points collected so far
    private int maxScore;     // points of all the coins together
    private bool won;

    void OnEnable()
    {
        Coin.Collected += AddCoin;   // subscribe
    }

    void OnDisable()
    {
        Coin.Collected -= AddCoin;   // always unsubscribe
    }

    void Start()
    {
        Coin[] allCoins = FindObjectsByType<Coin>(FindObjectsSortMode.None);
        coinsToWin = allCoins.Length;

        foreach (Coin coin in allCoins)
        {
            maxScore += coin.Points;
        }

        Debug.Log($"Collect the {coinsToWin} coins! ({maxScore} points in total)");
        UpdateText();
    }

    private void AddCoin(int points)
    {
        coins++;
        score += points;
        Debug.Log($"Coins: {coins} / {coinsToWin}, score: {score} / {maxScore}");

        if (coins == coinsToWin)
        {
            won = true;
            Debug.Log("All coins collected!");
        }

        UpdateText();
    }

    private void UpdateText()
    {
        if (scoreText == null)
        {
            return;
        }

        scoreText.text = $"Score: {score} / {maxScore}\nCoins: {coins} / {coinsToWin}";

        if (won)
        {
            scoreText.text += "\nYou win!";
        }
    }
}
