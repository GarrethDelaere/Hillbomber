using UnityEngine;

public class RampYeeter : MonoBehaviour
{
    [SerializeField] private float _rampYeetHeight = 5f;
    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("Workin");
        if (other.TryGetComponent(out PlayerScript player))
        {
            player.ApplyHeight(_rampYeetHeight);
        }
    }
}
