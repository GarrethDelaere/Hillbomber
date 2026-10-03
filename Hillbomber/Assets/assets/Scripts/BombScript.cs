using System.Collections;
using UnityEngine;

public class BombScript : MonoBehaviour
{
    [SerializeField] private float _bombHeight = 15f;
    [SerializeField] private float _scoreAddition = 35f;
    [SerializeField] private ParticleSystem _particle;
    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out PlayerScript script))
        {
            script.ApplyHeight(_bombHeight);
            ScoreTracker.Instance.AddScore(_scoreAddition);

            StartCoroutine(WaitThenDestroy(2.5f));
        }
    }

    private IEnumerator WaitThenDestroy(float time)
    {
        yield return new WaitForSeconds(time);

        Destroy(this);

        yield break;
    }
}
