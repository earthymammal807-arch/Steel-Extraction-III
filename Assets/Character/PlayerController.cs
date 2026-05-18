using UnityEngine;
using UnityEngine.Rendering.Universal;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour, IControllable
{
    private CharacterController _playerController;
    private Camera _camera;

    [SerializeField] private bool _isPossessed;

    private Vector3 _velocity;
    private float _yaw;
    private float _pitch;

    [Header("Movement Settings")]
    public float WalkSpeed = 5.0f;
    public float SprintSpeed = 8.0f;
    public float RotSpd = 15.0f;

    [Header("Acceleration Settings")]
    public float Accel = 20f;
    public float Decel = 40f;
    public float AirAccel = 10f;
    public float AirDecel = 8f;

    [Header("Jump Settings")]
    public float JumpVelocity = 4.5f;
    public float JumpBoost = 2.5f;

    [Header("Friction Settings")]
    public float GroundFriction = 20f;

    [Header("Physics Settings")]
    public float Gravity = -20f;

    [Header("Camera Offset")]
    [Range(-100, 100)] public float X;
    [Range(-100, 100)] public float Y;
    [Range(-100, 100)] public float Z;

    private Vector3 _currentShakeOffset = Vector3.zero;
    private float _currentSpeed;
    private bool _jumpQueued = false;
    private Vector3 _jumpDir = Vector3.zero;
    private bool _wasOnGround = false;
    private Vector2 _moveInput = Vector2.zero;

    void Awake()
    {
        _playerController = GetComponent<CharacterController>();
        _currentSpeed = WalkSpeed;

        GameObject cameraObj = new GameObject("PlayerCamera");
        _camera = cameraObj.AddComponent<Camera>();
        cameraObj.transform.SetParent(transform);
        cameraObj.transform.localPosition = new Vector3(X, Y, Z);
        cameraObj.transform.localRotation = Quaternion.identity;
        _camera.nearClipPlane = 0.01f;
        _camera.farClipPlane = 1000f;

        UniversalAdditionalCameraData cameraData = cameraObj.AddComponent<UniversalAdditionalCameraData>();
        cameraData.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
        cameraData.antialiasingQuality = AntialiasingQuality.High;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void OnEnable() => CameraShake.OnShakeOffset += HandleShakeOffset;
    void OnDisable() => CameraShake.OnShakeOffset -= HandleShakeOffset;

    public void ProcessMove(Vector2 movementVector) => _moveInput = movementVector;

    void Update()
    {
        if (_playerController == null || _camera == null) return;

        float dt = Time.deltaTime;
        bool onGround = _playerController.isGrounded;

        _velocity.y = onGround && _velocity.y < 0f
            ? -2f
            : _velocity.y + Gravity * dt;

        Vector3 camForward = Vector3.Scale(_camera.transform.forward, new Vector3(1, 0, 1)).normalized;
        Vector3 camRight = Vector3.Scale(_camera.transform.right, new Vector3(1, 0, 1)).normalized;
        Vector3 desiredDir = camForward * _moveInput.y + camRight * _moveInput.x;

        if (desiredDir.sqrMagnitude > 1f) desiredDir.Normalize();

        Vector3 flatVel = _isPossessed ? new Vector3(_velocity.x, 0f, _velocity.z) : Vector3.zero;
        float speed = flatVel.magnitude;
        if (speed < 0.01f) { flatVel = Vector3.zero; speed = 0f; }

        if (desiredDir != Vector3.zero)
        {
            bool wrongWay = speed > 0.01f && Vector3.Dot(flatVel.normalized, desiredDir) < 0f;
            float rate = wrongWay
                ? (onGround ? Decel : AirDecel)
                : (onGround ? Accel : AirAccel);

            Vector3 target = desiredDir * (speed > _currentSpeed ? speed : _currentSpeed);
            flatVel = Vector3.MoveTowards(flatVel, target, rate * dt);
        }
        else if (onGround && _wasOnGround)
        {
            speed = Mathf.MoveTowards(speed, 0f, GroundFriction * dt);
            flatVel = speed > 0f ? flatVel.normalized * speed : Vector3.zero;
        }

        _velocity.x = flatVel.x;
        _velocity.z = flatVel.z;

        if (_jumpQueued && onGround)
        {
            _velocity.y = JumpVelocity;
            _velocity.x += _jumpDir.x * JumpBoost;
            _velocity.z += _jumpDir.z * JumpBoost;
            _jumpQueued = false;
        }

        _wasOnGround = onGround;
        _playerController.Move(_velocity * dt);
    }

    public void Rotate(Vector2 rotvec)
    {
        if (_camera == null) return;

        _yaw += rotvec.x * RotSpd * 0.005f;
        _pitch -= rotvec.y * RotSpd * 0.005f;
        _pitch = Mathf.Clamp(_pitch, -80f, 80f);

        _camera.transform.localRotation = Quaternion.Euler(_pitch, _yaw, 0f);
    }

    public void Jump()
    {
        if (_playerController == null || _camera == null || !_playerController.isGrounded) return;

        _jumpQueued = true;
        Vector3 camForward = Vector3.Scale(_camera.transform.forward, new Vector3(1, 0, 1)).normalized;
        Vector3 camRight = Vector3.Scale(_camera.transform.right, new Vector3(1, 0, 1)).normalized;
        _jumpDir = (camForward * _moveInput.y + camRight * _moveInput.x).normalized;
    }

    public void JumpCancelled() => CameraShake.Instance?.TriggerShake(0.5f, 0.2f);

    public void SetSprint(bool isSprinting) => _currentSpeed = isSprinting ? SprintSpeed : WalkSpeed;

    public void SetFocus(bool isFocused)
    {
        _isPossessed = isFocused;

        if (_camera != null)
        {
            _camera.enabled = isFocused;
            AudioListener listener = _camera.GetComponent<AudioListener>();
            if (listener != null) listener.enabled = isFocused;
        }
    }

    void LateUpdate()
    {
        if (_camera == null) return;
        _camera.transform.localPosition = new Vector3(X, Y, Z) + _currentShakeOffset;
    }

    private void HandleShakeOffset(Vector3 offset) =>
        _currentShakeOffset = _isPossessed ? offset : Vector3.zero;
}