using UnityEngine;

public class PlayerScript : MonoBehaviour
{
    [SerializeField] private MapGeneration _mapGenerator;
    [SerializeField] private GameObject _playerObject;

    [SerializeField] private Camera _camera;

    [SerializeField] private float _speed = 5f;
    [SerializeField] private float _acceleration = 10f;
    [SerializeField] private float _deceleration = 12f;
    [SerializeField] private float _cameraFOV = 70f;
    [SerializeField] private float _cameraMaxFOVOffset = 2f;
    [SerializeField] private float _cameraFOVOffsetMultiplier = 0.1f;
    [SerializeField] private float _gravity = -9.81f;
    [SerializeField] private float _cameraDistance = 2f;
}
