using UnityEngine;

public class BombScript : MonoBehaviour
{
    [SerializeField] private float _bombHeight = 15f;
    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out PlayerScript script))
        {
            script.ApplyHeight(_bombHeight);
            Destroy(this);
        }
    }
}
