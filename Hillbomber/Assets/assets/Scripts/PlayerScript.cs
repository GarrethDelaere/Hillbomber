using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerScript : MonoBehaviour
{
    [SerializeField] private MapGeneration _mapGenerator;
    [SerializeField] private GameObject _playerObject;
    [SerializeField] private GameObject _deathParticle;
    [SerializeField] private Camera _camera;

    [SerializeField] private float _gracePeriod = 5f;
    [SerializeField] private float _speed = 15f;
    [SerializeField] private float _extraSpeed = 5f;
    [SerializeField] private float _acceleration = 10f;
    [SerializeField] private float _deceleration = 12f;
    [SerializeField] private float _extraSpeedAcceleration = 0.5f;
    [SerializeField] private float _extraSpeedDeceleration = 20f;
    [SerializeField] private float _permanentExtraSpeedAcceleration = 0.01f;
    [SerializeField] private float _gravity = -15f;

    [SerializeField] private float _steeringSpeed = 12f;
    [SerializeField] private float _steeringDamping = 8f;
    [SerializeField] private float _airSteerPenalty = 0.1f;
    [SerializeField] private float _roadWidthLimit = 3.5f;

    [SerializeField] private float _jumpingHeight = 2f;

    [SerializeField] private float _cameraDistance = 6f;
    [SerializeField] private float _cameraFOV = 70f;
    [SerializeField] private float _cameraMaxFOVOffset = 15f;
    [SerializeField] private float _cameraFOVOffsetMultiplier = 0.3f;
    [SerializeField] private float _airborneFOVBoost = 5f;

    [SerializeField] private float _cameraRollAngle = 3f;
    [SerializeField] private float _speedShakeIntensity = 0.03f;
    [SerializeField] private float _landingShakeMultiplier = 0.08f;
    [SerializeField] private float _speedShakeAirMultiplier = 3f;

    [SerializeField] private float _visualYawAngle = 7f;
    [SerializeField] private float _visualRollAngle = 3f;
    [SerializeField] private float _tiltSmoothSpeed = 10f;

    private float _smoothedSteeringInput;
    private float _currentForwardSpeed;
    private float _currentExtraSpeed;
    private float _currentPermanentSpeed;
    private Vector3 _verticalVelocity;
    private Vector2 _moveInput;
    private CharacterController _characterController;
    private bool _isJumping;

    private bool _wasGroundedLastFrame;
    private float _currentImpactShake;

    private Vector3 _lastPosition;

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
        HandleDeath();
        _lastPosition = transform.position;
        _gracePeriod = Mathf.Clamp(_gracePeriod - Time.deltaTime, 0f, _gracePeriod);
    }

    private void LateUpdate()
    {
        UpdateCamera();
    }

    private void HandleDeath()
    {
        if (Time.timeScale <= 0f) return;
        if (_lastPosition == null) return;

        if (Vector3.Distance(_lastPosition, transform.position) > 0.01) return;

        if (_gracePeriod > 0f) return;

        Instantiate(_deathParticle, transform);
        StartCoroutine(DelayedDeath(2.5f));
        enabled = false;
    }

    private IEnumerator DelayedDeath(float delay)
    {
        yield return new WaitForSeconds(delay);
        StartingScript.Instance.OnDeath();
    }

    private void HandleSpeed()
    {
        if (_currentForwardSpeed < _speed)
        {
            _currentForwardSpeed += _acceleration * Time.deltaTime;
            _currentForwardSpeed = Mathf.Min(_currentForwardSpeed, _speed);
        }
        else if (_currentForwardSpeed > _speed)
        {
            _currentForwardSpeed -= _deceleration * Time.deltaTime;
        }

        if (_characterController.isGrounded && _currentForwardSpeed >= _speed)
        {
            _currentExtraSpeed += _extraSpeedAcceleration * Time.deltaTime;
            _currentExtraSpeed = Mathf.Min(_currentExtraSpeed, _extraSpeed);
        }
        else if (!_characterController.isGrounded)
        {
            _currentExtraSpeed = Mathf.MoveTowards(_currentExtraSpeed, 0f, _extraSpeedDeceleration * Time.deltaTime);
        }

        _currentPermanentSpeed += _permanentExtraSpeedAcceleration * Time.deltaTime;

        float targetSteering = _moveInput.x * _steeringSpeed * (_characterController.isGrounded ? 1f : _airSteerPenalty);
        _smoothedSteeringInput = Mathf.Lerp(_smoothedSteeringInput, targetSteering, Time.deltaTime * _steeringDamping);
    }

    private void HandleMovement()
    {
        bool isGrounded = _characterController.isGrounded;

        if (isGrounded && !_wasGroundedLastFrame)
        {
            float impactForce = Mathf.Abs(_verticalVelocity.y);
            _currentImpactShake = impactForce * _landingShakeMultiplier;
        }
        _wasGroundedLastFrame = isGrounded;

        float totalSpeed = _currentForwardSpeed + _currentExtraSpeed + _currentPermanentSpeed;
        Vector3 forwardMove = transform.forward * totalSpeed;
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

        float inputX = _moveInput.x;
        float targetYaw = inputX * _visualYawAngle;
        float targetRoll = -inputX * _visualRollAngle;

        Quaternion targetRotation = Quaternion.Euler(_mapGenerator.GenerationIncline, targetYaw, targetRoll);

        transform.localRotation = Quaternion.Slerp(
            transform.localRotation,
            targetRotation,
            Time.deltaTime * _tiltSmoothSpeed
        );
    }

    private void UpdateCamera()
    {
        if (_camera == null) return;

        Vector3 targetCameraPos = transform.position
            - (transform.forward * _cameraDistance)
            + (transform.up * (_cameraDistance * 0.4f));

        float totalSpeed = _currentForwardSpeed + _currentExtraSpeed + _currentPermanentSpeed;
        float maxPossibleSpeed = _speed + _extraSpeed;

        float speedRatio = Mathf.Clamp01(totalSpeed / maxPossibleSpeed);
        float continuousRumble = speedRatio * _speedShakeIntensity * (_characterController.isGrounded ? 1 : _speedShakeAirMultiplier);

        _currentImpactShake = Mathf.Lerp(_currentImpactShake, 0f, Time.deltaTime * 2f);

        float totalShake = continuousRumble + _currentImpactShake;
        Vector3 shakeOffset = Random.insideUnitSphere * totalShake;

        _camera.transform.position = Vector3.Lerp(
            _camera.transform.position,
            targetCameraPos + shakeOffset,
            Time.deltaTime * 12f
        );

        float targetCameraRoll = -_moveInput.x * _cameraRollAngle;
        Quaternion targetRotation = transform.rotation * Quaternion.Euler(5f, 0f, targetCameraRoll);
        _camera.transform.rotation = Quaternion.Slerp(_camera.transform.rotation, targetRotation, Time.deltaTime * 8f);

        float speedFovOffset = Mathf.Clamp(totalSpeed * _cameraFOVOffsetMultiplier, 0f, _cameraMaxFOVOffset);
        float airborneFovOffset = !_characterController.isGrounded ? _airborneFOVBoost : 0f;

        float targetFOV = _cameraFOV + speedFovOffset + airborneFovOffset;
        _camera.fieldOfView = Mathf.Lerp(_camera.fieldOfView, targetFOV, Time.deltaTime * 5f);
    }

    public void ApplyHeight(float height)
    {
        float jumpVelocity = Mathf.Sqrt(height * -2f * _gravity);
        _verticalVelocity.y = jumpVelocity;
        _isJumping = true;
    }
}