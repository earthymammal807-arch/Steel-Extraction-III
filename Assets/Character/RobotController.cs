using UnityEngine;

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

    [Header("Mesh Settings")]
    public Mesh characterMesh;
    public Material characterMaterial;

    [Header("Movement Settings")]
    public float MovementSpd = 10.0f;
    public float RotSpd = 15.0f;

    [Header("Physics Settings")]
    public float Gravity = -9.81f;

    [Header("Camera Offset")]
    [Range(-100, 100)] public float X;
    [Range(-100, 100)] public float Y;
    [Range(-100, 100)] public float Z;

    void Awake()
    {
        _robotController = GetComponent<UnityEngine.CharacterController>();

        // Child object for mesh
        _meshChildObj = new GameObject("RobotMesh");
        _meshChildObj.transform.SetParent(this.transform);
        _meshChildObj.transform.localPosition = Vector3.zero;
        _meshChildObj.transform.localRotation = Quaternion.identity;

        meshFilter = _meshChildObj.AddComponent<MeshFilter>();
        MeshRenderer renderer = _meshChildObj.AddComponent<MeshRenderer>();
        if (characterMesh != null) meshFilter.mesh = characterMesh;
        if (characterMaterial != null) renderer.material = characterMaterial;

        // Child object for camera
        GameObject cameraObj = new GameObject("RobotCamera");
        _camera = cameraObj.AddComponent<Camera>();
        cameraObj.transform.SetParent(this.transform);
        cameraObj.transform.localPosition = new Vector3(X, Y, Z);
        cameraObj.transform.localRotation = Quaternion.identity;
    }

    public void ProcessMove(Vector2 movementVector)
    {
        if (_camera == null) return;

        Vector3 camForward = _camera.transform.forward; camForward.y = 0f; camForward.Normalize();
        Vector3 camRight = _camera.transform.right; camRight.y = 0f; camRight.Normalize();

        Vector3 move = camForward * movementVector.y + camRight * movementVector.x;

        if (_robotController.isGrounded && _velocity.y < 0)
            _velocity.y = -2f;

        _velocity.y += Gravity * Time.deltaTime;

        _robotController.Move((move * MovementSpd + _velocity) * Time.deltaTime);
    }

    public void Rotate(Vector2 rotvec)
    {
        if (_camera == null || _meshChildObj == null) return;

        _yaw += rotvec.x * RotSpd * Time.deltaTime;
        _pitch -= rotvec.y * RotSpd * Time.deltaTime;
        _pitch = Mathf.Clamp(_pitch, -80f, 80f);

        // Camera gets full yaw + pitch
        _camera.transform.localRotation = Quaternion.Euler(_pitch, _yaw, 0);

        // Model lerps toward the camera's yaw independently
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
}