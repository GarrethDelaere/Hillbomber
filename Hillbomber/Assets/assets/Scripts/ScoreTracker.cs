using TMPro;
using UnityEngine;

public class ScoreTracker : MonoBehaviour
{
    public static ScoreTracker Instance;
    public float Speed { get; private set; }

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
        Speed += amount;
        UpdateScores();
    }

    private void UpdateScores()
    {
        if (_scoreText == null) return;

        _scoreText.text = $"Score: {(int)Speed}";
    }
}
