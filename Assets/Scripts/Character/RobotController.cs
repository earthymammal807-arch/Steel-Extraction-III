using UnityEngine;
using UnityEngine.Rendering.Universal;

[RequireComponent(typeof(CharacterController))]
public class RobotController : MonoBehaviour, IControllable
{
    private CharacterController _robotController;
    private GameObject _meshChildObj;
    private Camera _camera;
    private bool _isFocused = false;
    private bool _isThrusting;
    private float _maxFuel;
    private float _activeThrustTime = 0f;
    private float _yaw;
    private float _modelYaw;
    private float _pitch;
    private Vector3 _velocity;
    private Vector3 _currentShakeOffset = Vector3.zero;
    private bool _isThrusterLocked = false;

    [Header("Mesh Settings")]
    public Mesh characterMesh;
    public Material characterMaterial;

    [Header("Movement Settings")]
    public float MovementSpd = 10.0f;
    public float RotSpd = 15.0f;

    [Header("Physics Settings")]
    public float Gravity = -4.81f;

    [Header("Thruster Settings")]
    [Range(0, 100)] public float mechFuel = 5.0f;
    [Range(1, 100)] public float mechThrustAccel = 25.0f;
    [Range(1, 100)] public float mechThrustPower = 15.0f;
    [Range(-1, 100)] public float fuelIntake = 2.0f;
    [Range(0f, 1f)] public float fuelRecoveryThreshold = 0.2f;

    [Header("Dampening Settings")]
    public float ThrustDampen = 10f;
    public float AirDampen = 5f;

    [Header("Curve Sharpness")]
    [Tooltip("Higher numbers make it stay slow longer, then snap to top speed instantly at the end.")]
    [Range(0, 10)] public float curveSharpness = 3.0f;

    [Header("Camera Offset")]
    [Range(-100, 100)] public float X;
    [Range(-100, 100)] public float Y;
    [Range(-100, 100)] public float Z;

    [Header("Audio Settings")]
    public AudioClip thrusterClip;

    void Awake()
    {
        _robotController = GetComponent<CharacterController>();
        _maxFuel = mechFuel;



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
        // FIXED: All dynamic ObjectAudioManager generation code has been stripped out from here!

        _meshChildObj = new GameObject("RobotMesh");
        _meshChildObj.transform.SetParent(transform);
        _meshChildObj.transform.localPosition = Vector3.zero;
        _meshChildObj.transform.localRotation = Quaternion.identity;

        MeshFilter meshFilter = _meshChildObj.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = _meshChildObj.AddComponent<MeshRenderer>();

        if (characterMesh != null)
        {
            meshFilter.mesh = characterMesh;
            Bounds b = characterMesh.bounds;
            _robotController.height = b.size.y;
            _robotController.radius = Mathf.Max(b.size.x, b.size.z) * 0.5f;
            _robotController.center = b.center;
            _robotController.skinWidth = _robotController.radius * 0.08f;
        }

        if (characterMaterial != null)
            meshRenderer.material = characterMaterial;
    }

    void OnEnable() => CameraShake.OnShakeOffset += HandleShakeOffset;
    void OnDisable() => CameraShake.OnShakeOffset -= HandleShakeOffset;

    public void ProcessMove(Vector2 movementVector)
    {
        if (_robotController == null || _camera == null) return;

        // 1. GATED PHYSICS CONTROL: If not focused, only apply gravity and exit early
        if (!_isFocused)
        {
            _isThrusting = false; // Force thrusters off if we leave the mech mid-air
            _activeThrustTime = 0f;

            if (!_robotController.isGrounded)
            {
                _velocity.y += Gravity * Time.deltaTime;
                _robotController.Move(new Vector3(0f, _velocity.y, 0f) * Time.deltaTime);
            }
            else
            {
                _velocity.y = -2f; // Keep grounded robot snapped to floor
            }
            return;
        }

        // 2. ACTIVE POSSESSED MOVEMENT
        Vector3 camForward = Vector3.Scale(_camera.transform.forward, new Vector3(1, 0, 1)).normalized;
        Vector3 camRight = Vector3.Scale(_camera.transform.right, new Vector3(1, 0, 1)).normalized;
        Vector3 move = (camForward * movementVector.y + camRight * movementVector.x) * MovementSpd;

        if (_robotController.isGrounded)
        {
            if (mechFuel < _maxFuel) mechFuel += Time.deltaTime * 2f;
            if (_velocity.y < 0f && !_isThrusting) _velocity.y = -2f;
        }

        if (_isThrusterLocked && mechFuel >= (_maxFuel * fuelRecoveryThreshold))
        {
            _isThrusterLocked = false;
        }

        if (_isThrusting && !_isThrusterLocked && mechFuel > 0f)
        {
            _activeThrustTime += Time.deltaTime;
            float curveProfile = Mathf.Pow(Mathf.Clamp01(_activeThrustTime / _maxFuel), curveSharpness);
            float multiplier = Mathf.Lerp(0.2f, 3.0f, curveProfile);

            _velocity += _camera.transform.forward * (mechThrustAccel * multiplier * Time.deltaTime);
            _velocity = Vector3.ClampMagnitude(_velocity, mechThrustPower * multiplier);
            mechFuel -= Time.deltaTime * fuelIntake;

            if (mechFuel <= 0f)
            {
                mechFuel = 0f;
                _isThrusterLocked = true;
                JumpCancelled();
            }
        }
        else
        {
            _activeThrustTime = 0f;
            _velocity.y += Gravity * Time.deltaTime;
            _velocity.x = Mathf.MoveTowards(_velocity.x, 0f, Time.deltaTime * AirDampen);
            _velocity.z = Mathf.MoveTowards(_velocity.z, 0f, Time.deltaTime * AirDampen);
        }

        _robotController.Move((move + _velocity) * Time.deltaTime);
    }


    public void Rotate(Vector2 rotvec)
    {
        if (_camera == null || _meshChildObj == null) return;

        _yaw += rotvec.x * RotSpd * 0.005f;
        _pitch -= rotvec.y * RotSpd * 0.005f;
        _pitch = Mathf.Clamp(_pitch, -80f, 80f);

        _camera.transform.localRotation = Quaternion.Euler(_pitch, _yaw, 0f);

        _modelYaw = Mathf.MoveTowardsAngle(_modelYaw, _yaw, Time.deltaTime * 70.0f);
        _meshChildObj.transform.localRotation = Quaternion.Euler(0f, _modelYaw, 0f);
    }

    public void SetFocus(bool isFocused)
    {
        _isFocused = isFocused;
        if (_camera != null)
        {
            _camera.enabled = isFocused;
        }

        if (!isFocused)
        {
            _isThrusting = false;
            ObjectAudioManager.Instance?.StopAudio(true);
        }
    }

    public void Jump()
    {
        if (mechFuel <= 0f || _isThrusterLocked) return;

        _isThrusting = true;

        if (CameraShake.Instance != null)
        {
            CameraShake.Instance.TriggerShake(mechFuel * fuelIntake, 0.10f);
        }

        // FIXED: Swapped to global ObjectAudioManager singleton reference
        if (ObjectAudioManager.Instance != null && thrusterClip != null)
        {
            ObjectAudioManager.Instance.PlayLoopTimed(thrusterClip, mechFuel / fuelIntake, true, 1f);
        }
    }

    public void JumpCancelled()
    {
        _isThrusting = false;

        if (CameraShake.Instance != null)
        {
            CameraShake.Instance.CancelShake();
        }

        // FIXED: Swapped to global ObjectAudioManager singleton reference
        if (ObjectAudioManager.Instance != null)
        {
            ObjectAudioManager.Instance.StopAudio(fade: true);
        }
    }

    void LateUpdate()
    {
        if (_camera == null) return;
        _camera.transform.localPosition = new Vector3(X, Y, Z) + _currentShakeOffset;
    }

    private void HandleShakeOffset(Vector3 offset) =>
        _currentShakeOffset = _isFocused ? offset : Vector3.zero;

    public void InitializeCharacter(bool startsPossessed)
    {

        SetFocus(startsPossessed);
    }
}
