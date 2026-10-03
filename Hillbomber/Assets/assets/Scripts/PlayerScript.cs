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
    [SerializeField] private float _gravity = -15f;

    [SerializeField] private float _steeringSpeed = 12f;
    [SerializeField] private float _steeringDamping = 8f;
    [SerializeField] private float _roadWidthLimit = 3.5f;

    [SerializeField] private float _jumpingHeight = 3f;

    [SerializeField] private float _cameraDistance = 6f;
    [SerializeField] private float _cameraFOV = 70f;
    [SerializeField] private float _cameraMaxFOVOffset = 15f;
    [SerializeField] private float _cameraFOVOffsetMultiplier = 0.3f;
    [SerializeField] private float _airborneFOVBoost = 5f;

    [SerializeField] private float _cameraRollAngle = 3f;
    [SerializeField] private float _speedShakeIntensity = 0.03f;
    [SerializeField] private float _landingShakeMultiplier = 0.08f;

    private float _smoothedSteeringInput;
    private float _currentForwardSpeed;
    private Vector3 _verticalVelocity;
    private Vector2 _moveInput;
    private CharacterController _characterController;
    private bool _isJumping;

    private bool _wasGroundedLastFrame;
    private float _currentImpulseShake;
    private float _currentImpactShake;

    public void OnMoveAction(InputAction.CallbackContext ctx)
    {
        _moveInput = ctx.ReadValue<Vector2>();
    }

    public void OnJumpAction(InputAction.CallbackContext ctx)
    {
        if (_characterController.isGrounded)
        {
            ApplyHeight(_jumpingHeight);
        }
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
        bool isGrounded = _characterController.isGrounded;

        // --- LANDING DETECTION FIX ---
        // Detect impact BEFORE resetting _verticalVelocity.y
        if (isGrounded && !_wasGroundedLastFrame)
        {
            float impactForce = Mathf.Abs(_verticalVelocity.y);
            if (impactForce > 3f) // Trigger impact if falling faster than -3 units/sec
            {
                _currentImpactShake = impactForce * _landingShakeMultiplier;
            }
        }
        _wasGroundedLastFrame = isGrounded;

        Vector3 forwardMove = transform.forward * _currentForwardSpeed;
        Vector3 lateralMove = transform.right * _smoothedSteeringInput;

        if (_characterController.isGrounded && !_isJumping)
        {
            _verticalVelocity.y = -2f;
        }
        else
        {
            _verticalVelocity.y += _gravity * Time.deltaTime;
            _isJumping = false;
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

        float speedRatio = Mathf.Clamp01(_currentForwardSpeed / _speed);
        float continuousRumble = speedRatio * _speedShakeIntensity;

        float totalShake = continuousRumble + _currentImpulseShake;
        Vector3 shakeOffset = Random.insideUnitSphere * totalShake;

        _camera.transform.position = Vector3.Lerp(
            _camera.transform.position, 
            targetCameraPos + shakeOffset, 
            Time.deltaTime * 12f
        );

        float targetCameraRoll = -_moveInput.x * _cameraRollAngle;
        Quaternion targetRotation = transform.rotation * Quaternion.Euler(5f, 0f, targetCameraRoll);
        _camera.transform.rotation = Quaternion.Slerp(_camera.transform.rotation, targetRotation, Time.deltaTime * 8f);

        float speedFovOffset = Mathf.Clamp(_currentForwardSpeed * _cameraFOVOffsetMultiplier, 0f, _cameraMaxFOVOffset);
        float airborneFovOffset = !_characterController.isGrounded ? _airborneFOVBoost : 0f;
        
        float targetFOV = _cameraFOV + speedFovOffset + airborneFovOffset;
        _camera.fieldOfView = Mathf.Lerp(_camera.fieldOfView, targetFOV, Time.deltaTime * 5f);
    }

    public void ApplyHeight(float height)
    {
        _verticalVelocity += new Vector3(0, height, 0);
        _isJumping = true;
    }
}