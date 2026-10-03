using UnityEngine;

public class BombScript : MonoBehaviour
{
    [SerializeField] private float _bombHeight = 15f;
    [SerializeField] private float _scoreAddition = 35f;
    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out PlayerScript script))
        {
            script.ApplyHeight(_bombHeight);
            ScoreTracker.Instance.AddScore(_scoreAddition);
            Destroy(this);
        }
    }
}
