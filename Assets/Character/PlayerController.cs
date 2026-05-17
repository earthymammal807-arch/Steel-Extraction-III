using UnityEngine;
using UnityEngine.Rendering.Universal;




[RequireComponent(typeof(UnityEngine.CharacterController))]
public class PlayerController : MonoBehaviour, IControllable
{
    private UnityEngine.CharacterController _playerController;
    private GameObject _meshChildObj;
    private MeshFilter meshFilter;

    private bool _isPossessed;



    // Velocity tracks full 3D movement (X/Z = horizontal, Y = vertical/gravity)
    private Vector3 _velocity;
    private float _yaw;
    private float _modelYaw;
    private float _pitch;
    private Camera _camera;

    [Header("Mesh Settings")]
    public Mesh characterMesh;
    public Material characterMaterial;

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

    // Runtime state
    private float _currentSpeed;
    private bool _isSliding = false;
    private bool _jumpQueued = false;
    private Vector3 _jumpDir = Vector3.zero;
    private bool _wasOnGround = false;
    private Vector2 _moveInput = Vector2.zero;

    void Awake()
    {
        _playerController = GetComponent<UnityEngine.CharacterController>();
        _currentSpeed = WalkSpeed;



        // Child object for mesh
        _meshChildObj = new GameObject("PlayerMesh");
        _meshChildObj.transform.SetParent(this.transform);
        _meshChildObj.transform.localPosition = Vector3.zero;
        _meshChildObj.transform.localRotation = Quaternion.identity;



        meshFilter = _meshChildObj.AddComponent<MeshFilter>();
        MeshRenderer renderer = _meshChildObj.AddComponent<MeshRenderer>();
        if (characterMesh != null) meshFilter.mesh = characterMesh;
        if (characterMaterial != null) renderer.material = characterMaterial;



        // Child object for camera
        GameObject cameraObj = new GameObject("PlayerCamera");
        _camera = cameraObj.AddComponent<Camera>();
        cameraObj.transform.SetParent(this.transform);
        cameraObj.transform.localPosition = new Vector3(X, Y, Z);
        cameraObj.transform.localRotation = Quaternion.identity;
        _camera.nearClipPlane = 0.01f;  // default is 0.3, too large for close surfaces
        _camera.farClipPlane = 1000f;

        UniversalAdditionalCameraData cameraData = cameraObj.AddComponent<UniversalAdditionalCameraData>();
        cameraData.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing; // SMAA
        cameraData.antialiasingQuality = AntialiasingQuality.High;


        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // Called every frame by InputController with the raw WASD vector
    public void ProcessMove(Vector2 movementVector)
    {
        if (_camera == null) return;
        _moveInput = movementVector;
    }




    //USES OLD GODOT CODE TRANSLATED... EXPECT SMALL BUGS!!!!


    void Update()
    {
                             if (_playerController == null) return;

                             float dt = Time.deltaTime;
                             bool onGround = _playerController.isGrounded;

                             //Gravity |
                             //        v
                             if (onGround && _velocity.y < 0f)
                                 _velocity.y = -2f;               // keep snapped to slopes
                             else
                                 _velocity.y += Gravity * dt;


                                 Vector3 camForward = _camera.transform.forward; camForward.y = 0f; camForward.Normalize();
                                 Vector3 camRight = _camera.transform.right; camRight.y = 0f; camRight.Normalize();
                                 Vector3 desiredDir = (camForward * _moveInput.y + camRight * _moveInput.x);


                                 if (desiredDir.sqrMagnitude > 1f) desiredDir.Normalize();

                             //Horizontal velocity ->
                             Vector3 flatVel = _isPossessed ? new Vector3(_velocity.x, 0f, _velocity.z) : new Vector3(0, 0, 0);

                             float speed = flatVel.magnitude;
                                 if (speed < 0.01f) { flatVel = Vector3.zero; speed = 0f; }

                                 if (desiredDir != Vector3.zero)
                                 {
                                     // How aligned are we?
                                     float dot = speed > 0.01f ? Vector3.Dot(flatVel.normalized, desiredDir) : 1f;
                                     bool going_wrong_way = dot < 0f;

                                     float rate = going_wrong_way
                                         ? (onGround ? Decel : AirDecel)
                                         : (onGround ? Accel : AirAccel);

                                     Vector3 targetVel = desiredDir * _currentSpeed;

                                     if (speed > _currentSpeed)
                                     {
                                         // Above max speed — redirect without braking (preserve momentum)
                                         flatVel = Vector3.MoveTowards(flatVel, desiredDir * speed, rate * dt);
                                     }
                                     else
                                     {
                                         flatVel = Vector3.MoveTowards(flatVel, targetVel, rate * dt);
                                     }
                                 }
                                 else if (onGround && !WasJustAirborne())
                                 {
                                     // No input on ground — friction brings us to a stop
                                     speed = Mathf.MoveTowards(speed, 0f, GroundFriction * dt);
                                     flatVel = speed > 0f ? flatVel.normalized * speed : Vector3.zero;
                                 }
                                 // In air with no input — preserve momentum entirely (Quake-style shit)

                             


                             _velocity.x = flatVel.x;
                             _velocity.z = flatVel.z;

                             // Queued jump 
                             if (_jumpQueued && onGround)
                             {
                                 _velocity.y = JumpVelocity;
                                 if (_jumpDir != Vector3.zero)
                                 {
                                     _velocity.x += _jumpDir.x * JumpBoost;
                                     _velocity.z += _jumpDir.z * JumpBoost;
                                 }
                                 _jumpQueued = false;
                             }

        // Apply 
        _wasOnGround = onGround;
        _playerController.Move(_velocity * dt);
    }

    public void Rotate(Vector2 rotvec)
    {
                 if (_camera == null || _meshChildObj == null) return;

                 _yaw += rotvec.x * RotSpd * Time.deltaTime;
                 _pitch -= rotvec.y * RotSpd * Time.deltaTime;
                 _pitch = Mathf.Clamp(_pitch, -80f, 80f);

                 _camera.transform.localRotation = Quaternion.Euler(_pitch, _yaw, 0f);

                 _modelYaw = Mathf.MoveTowardsAngle(_modelYaw, _yaw, Time.deltaTime * 70f);
                 _meshChildObj.transform.localRotation = Quaternion.Euler(0f, _modelYaw, 0f);
    }

    public void Jump()
    {
             if (_playerController.isGrounded)
             {
                 _jumpQueued = true;
                 // Capture the movement direction at jump time for the boost
                 Vector3 camForward = _camera.transform.forward; camForward.y = 0f; camForward.Normalize();
                 Vector3 camRight = _camera.transform.right; camRight.y = 0f; camRight.Normalize();
                 _jumpDir = (camForward * _moveInput.y + camRight * _moveInput.x).normalized;
             }
    }

    public void SetSprint(bool sprinting)
    {
        _currentSpeed = sprinting ? SprintSpeed : WalkSpeed;
        
    }

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

    // True on the first frame we land
    private bool WasJustAirborne() => !_wasOnGround && _playerController.isGrounded;
}