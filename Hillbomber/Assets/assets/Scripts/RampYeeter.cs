using UnityEngine;

public class RampYeeter : MonoBehaviour
{
    [SerializeField] private float _rampYeetHeight = 5f;
    [SerializeField] private float _scoreAddition = 20f;
    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out PlayerScript player))
        {
            player.ApplyHeight(_rampYeetHeight);

            ScoreTracker.Instance.AddScore(_scoreAddition);
        }
    }
}
