using UnityEngine;
using UnityEngine.Rendering.Universal;

[RequireComponent(typeof(UnityEngine.CharacterController))]
public class RobotController : MonoBehaviour, IControllable
{
    private UnityEngine.CharacterController _robotController;
    private GameObject _meshChildObj;
    private MeshFilter meshFilter;

    private Vector3 _velocity;
    private float _yaw;
    private float _modelYaw;
    private float _pitch;
    private Camera _camera;

    private bool _isThrusting;
    private float _maxFuel; // Used to cap fuel limit changes

    [Header("Mesh Settings")]
    public Mesh characterMesh;
    public Material characterMaterial;

    [Header("Movement Settings")]
    public float MovementSpd = 10.0f;
    public float RotSpd = 15.0f;

    [Header("Physics Settings")]
    public float Gravity = -9.81f;

    [Header("Thruster Settings")]
    [Range(0, 100)] public float mechFuel = 5.0f;
    [Range(1, 100)] public float mechThrustAccel = 25.0f;
    [Range(1, 100)] public float mechThrustPower = 15.0f; 
    [Range(-1, 100)] public float fuelIntake = 2.0f;



    [Header("Curve Sharpness")]
    [Tooltip("Higher numbers make it stay slow longer, then snap to top speed instantly at the end.")]
    [Range(0, 10)] public float curveSharpness = 3.0f;

    private float _activeThrustTime = 0f;

    [Header("Camera Offset")]
    [Range(-100, 100)] public float X;
    [Range(-100, 100)] public float Y;
    [Range(-100, 100)] public float Z;

    void Awake()
    {

        // Core component registration
        _robotController = GetComponent<UnityEngine.CharacterController>();
        _maxFuel = mechFuel;

        // Child object for mesh
        _meshChildObj = new GameObject("RobotMesh");
        _meshChildObj.transform.SetParent(this.transform);
        _meshChildObj.transform.localPosition = Vector3.zero;
        _meshChildObj.transform.localRotation = Quaternion.identity;

        meshFilter = _meshChildObj.AddComponent<MeshFilter>();
        MeshRenderer renderer = _meshChildObj.AddComponent<MeshRenderer>();

        if (characterMesh != null)
        {
            meshFilter.mesh = characterMesh;

            // Dynamically scale CharacterController to match the assigned mesh geometry
            Bounds meshBounds = characterMesh.bounds;
            _robotController.height = meshBounds.size.y;

            // Radius is half of the largest horizontal width (X or Z)
            _robotController.radius = Mathf.Max(meshBounds.size.x, meshBounds.size.z) * 0.5f;
            _robotController.center = meshBounds.center;

            // Padding prevents large meshes from sinking vertically past collider surfaces
            _robotController.skinWidth = _robotController.radius * 0.08f;
        }

        if (characterMaterial != null) renderer.material = characterMaterial;

        // Child object for camera
        GameObject cameraObj = new GameObject("RobotCamera");
        _camera = cameraObj.AddComponent<Camera>();
        cameraObj.transform.SetParent(this.transform);
        cameraObj.transform.localPosition = new Vector3(X, Y, Z);
        cameraObj.transform.localRotation = Quaternion.identity;
        UniversalAdditionalCameraData cameraData = cameraObj.AddComponent<UniversalAdditionalCameraData>();
        cameraData.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing; // SMAA
        cameraData.antialiasingQuality = AntialiasingQuality.High;
        _camera.fieldOfView = 80.0f;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void ProcessMove(Vector2 movementVector)
    {
        if (_camera == null) return;

        // Base ground movement direction calculation
        Vector3 camForward = _camera.transform.forward; camForward.y = 0f; camForward.Normalize();
        Vector3 camRight = _camera.transform.right; camRight.y = 0f; camRight.Normalize();

        Vector3 move = camForward * movementVector.y + camRight * movementVector.x;
        move *= MovementSpd; // Multiply standard speed before blending momentum

        // Ground handling
        if (_robotController.isGrounded)
        {
            // Replenish fuel when grounded
            if (mechFuel < _maxFuel) mechFuel += Time.deltaTime * 2f;

            if (_velocity.y < 0 && !_isThrusting)
                _velocity.y = -2f;
        }

        // Thruster execution step
        if (_isThrusting && mechFuel > 0)
        {
            _activeThrustTime += Time.deltaTime;

            // 1. Get our baseline progress fraction (0.0 to 1.0)
            float progress = Mathf.Clamp01(_activeThrustTime / _maxFuel);

            // 2. Clear math curve generation using a power function.
            // It smoothly scales from 0.0 to 1.0 on a steep curve without going negative.
            float curveProfile = Mathf.Pow(progress, curveSharpness);

            // 3. Scale your multiplier smoothly from a low 0.2x up to your 3.0x peak
            float currentMultiplier = Mathf.Lerp(0.2f, 3.0f, curveProfile);

            // 4. Accelerate along your camera gaze vector
            _velocity += _camera.transform.forward * (mechThrustAccel * currentMultiplier * Time.deltaTime);
            _velocity = Vector3.ClampMagnitude(_velocity, mechThrustPower * currentMultiplier);

            mechFuel -= Time.deltaTime * fuelIntake;




            // Paste this inside the thruster block:
            // Debug.Log($"Multi: {currentMultiplier:F2} | Cur Speed: {_velocity.magnitude:F2}");

        }
        else
        {
            // Reset the curve timeline if we release space or deplete fuel resources
            _activeThrustTime = 0f;

            // Apply standard gravity when thruster isn't active
            _velocity.y += Gravity * Time.deltaTime;

            // Horizontal deceleration friction loop to clean up lingering thrust forces
            _velocity.x = Mathf.MoveTowards(_velocity.x, 0f, Time.deltaTime * 10f);
            _velocity.z = Mathf.MoveTowards(_velocity.z, 0f, Time.deltaTime * 10f);
        }

        // Combine base joystick movement with active momentum force matrix
        Vector3 finalFrameMovement = move + _velocity;

        // Final movement calculation uses a single Time.deltaTime pass
        _robotController.Move(finalFrameMovement * Time.deltaTime);
    }

    public void Rotate(Vector2 rotvec)
    {
        if (_camera == null || _meshChildObj == null) return;

        _yaw += rotvec.x * RotSpd * Time.deltaTime;
        _pitch -= rotvec.y * RotSpd * Time.deltaTime;
        _pitch = Mathf.Clamp(_pitch, -80f, 80f);

        _camera.transform.localRotation = Quaternion.Euler(_pitch, _yaw, 0);

        _modelYaw = Mathf.MoveTowardsAngle(_modelYaw, _yaw, Time.deltaTime * 70.0f);
        _meshChildObj.transform.localRotation = Quaternion.Euler(0, _modelYaw, 0);
    }

    public void SetFocus(bool isFocused)
    {
        if (_camera != null)
        {
            _camera.enabled = isFocused;
            AudioListener listener = _camera.GetComponent<AudioListener>();
            if (listener != null) listener.enabled = isFocused;
        }
    }

    public void Jump()
    {
        if (mechFuel > 0)
        {
            _isThrusting = true;
        }
    }

    public void JumpCancelled()
    {
        _isThrusting = false;
    }
}
