using TMPro;
using UnityEngine;

public class ScoreTracker : MonoBehaviour
{
    public static ScoreTracker Instance;
    public int Score { get; private set; }

    [SerializeField] private TextMeshProUGUI _scoreText;

    public void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(this);
        }
    }
    public void AddScore(int amount)
    {
        Score += amount;
        UpdateScores();
    }

    private void UpdateScores()
    {
        if (_scoreText == null) return;

        _scoreText.text = $"Score: {Score}";
    }
}
