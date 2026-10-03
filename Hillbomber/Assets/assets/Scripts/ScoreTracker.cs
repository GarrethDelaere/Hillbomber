using TMPro;
using UnityEngine;

public class ScoreTracker : MonoBehaviour
{
    public static ScoreTracker Instance;
    public float Score { get; private set; }

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
    public void AddScore(float amount)
    {
        Score += amount;
        UpdateScores();

        Debug.Log($"Score: {(int)Score}");
    }

    private void UpdateScores()
    {
        if (_scoreText == null) return;

        _scoreText.text = $"Score: {(int)Score}";
    }
}
