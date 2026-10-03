using TMPro;
using UnityEngine;

public class SpeedTracker : MonoBehaviour
{
    public static SpeedTracker Instance;
    public float Speed { get; private set; }

    [SerializeField] private TextMeshProUGUI _speedText;

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
    public void SetSpeed(float amount)
    {
        Speed = amount;
        UpdateSpeed();
    }

    private void UpdateSpeed()
    {
        if (_speedText == null) return;

        _speedText.text = $"Speed: {(int)Speed}";
    }
}
