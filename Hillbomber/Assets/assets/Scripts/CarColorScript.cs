using UnityEngine;
using UnityEngine.UIElements;

public class CarColorScript : MonoBehaviour
{
    [SerializeField] private Material _mat;
    void Start()
    {
        Renderer renderer = GetComponent<Renderer>();
        renderer.material.color = new Color(Random.value, Random.value, Random.value);
    }
}
