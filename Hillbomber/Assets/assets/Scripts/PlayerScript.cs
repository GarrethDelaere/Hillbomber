using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerScript : MonoBehaviour
{
    [SerializeField] private MapGeneration _mapGenerator;
    [SerializeField] private GameObject _playerObject;
    [SerializeField] private Camera _camera;

    [SerializeField] private float _speed = 15f;
    [SerializeField] private float _acceleration = 10f;
    [SerializeField] private float _deceleration = 12f;
    [SerializeField] private float _cameraFOV = 70f;
    [SerializeField] private float _cameraMaxFOVOffset = 10f;
    [SerializeField] private float _cameraFOVOffsetMultiplier = 0.2f;
    [SerializeField] private float _gravity = -15f;
    [SerializeField] private float _cameraDistance = 6f;

    [SerializeField] private float _steeringSpeed = 12f;
    [SerializeField] private float _steeringDamping = 8f;
    [SerializeField] private float _roadWidthLimit = 3.5f;

    private float _smoothedSteeringInput;
    private float _currentSteeringSpeed;
    private float _currentForwardSpeed;
    private Vector3 _verticalVelocity;
    private Vector2 _moveInput;
    private CharacterController _characterController;

    public void OnMoveAction(InputAction.CallbackContext ctx)
    {
        _moveInput = ctx.ReadValue<Vector2>();
    }

    private void Start()
    {
        if (!TryGetComponent(out _characterController))
        {
            _characterController = gameObject.AddComponent<CharacterController>();
            _characterController.center = new Vector3(0, 1f, 0);
            _characterController.height = 2f;
        }

        if (_mapGenerator != null)
        {
            // Align player pitch to match map incline
            transform.rotation = Quaternion.Euler(_mapGenerator.GenerationIncline, 0f, 0f);
            _mapGenerator.PlayerTransform = transform;
        }

        if (_camera == null)
        {
            _camera = Camera.main;
        }
    }

    private void Update()
    {
        HandleSpeed();
        HandleMovement();
        HandleVisualTilt();
    }

    private void LateUpdate()
    {
        UpdateCamera();
    }

    private void HandleSpeed()
    {
        if (_currentForwardSpeed < _speed)
        {
            _currentForwardSpeed += _acceleration * Time.deltaTime;
        }
        else if (_currentForwardSpeed > _speed)
        {
            _currentForwardSpeed -= _deceleration * Time.deltaTime;
        }

        float targetSteering = _moveInput.x * _steeringSpeed;
        _smoothedSteeringInput = Mathf.Lerp(_smoothedSteeringInput, targetSteering, Time.deltaTime * _steeringDamping);
    }

    private void HandleMovement()
    {
        Vector3 forwardMove = transform.forward * _currentForwardSpeed;
        Vector3 lateralMove = transform.right * _smoothedSteeringInput;

        if (_characterController.isGrounded)
        {
            _verticalVelocity.y = -2f;
        }
        else
        {
            _verticalVelocity.y += _gravity * Time.deltaTime;
        }

        Vector3 totalMotion = (forwardMove + lateralMove + _verticalVelocity) * Time.deltaTime;
        _characterController.Move(totalMotion);

        // Soft-clamp road edge boundaries to avoid jarring position resets
        Vector3 currentPos = transform.position;
        if (Mathf.Abs(currentPos.x) > _roadWidthLimit)
        {
            if ((currentPos.x > _roadWidthLimit && _smoothedSteeringInput > 0) ||
                (currentPos.x < -_roadWidthLimit && _smoothedSteeringInput < 0))
            {
                _smoothedSteeringInput = 0f;
            }

            float clampedX = Mathf.Clamp(currentPos.x, -_roadWidthLimit, _roadWidthLimit);
            _characterController.enabled = false;
            transform.position = new Vector3(clampedX, currentPos.y, currentPos.z);
            _characterController.enabled = true;
        }
    }

    private void HandleVisualTilt()
    {
        if (_playerObject == null) return;

        // Banking/tilt angle when steering left or right
        float targetTilt = -_moveInput.x * 15f;
        Quaternion targetRotation = Quaternion.Euler(0f, 0f, targetTilt);
        _playerObject.transform.localRotation = Quaternion.Slerp(
            _playerObject.transform.localRotation,
            targetRotation,
            Time.deltaTime * 10f
        );
    }

    private void UpdateCamera()
    {
        if (_camera == null) return;
        Vector3 targetCameraPos = transform.position
            - (transform.forward * _cameraDistance)
            + (transform.up * (_cameraDistance * 0.4f));
        _camera.transform.position = Vector3.Lerp(_camera.transform.position, targetCameraPos, Time.deltaTime * 12f);
        float fovOffset = Mathf.Clamp(_currentForwardSpeed * _cameraFOVOffsetMultiplier, 0f, _cameraMaxFOVOffset);
        _camera.fieldOfView = Mathf.Lerp(_camera.fieldOfView, _cameraFOV + fovOffset, Time.deltaTime * 5f);
    }
}