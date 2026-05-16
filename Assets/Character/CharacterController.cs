using UnityEngine;

public class CharacterController : MonoBehaviour
{
    private UnityEngine.CharacterController _characterController;

    [Header("Movement Settings")]
    public float MovementSpd = 10.0f;
    public float RotSpd = 15.0f;

    [Header("Physics Settings")]
    public float Gravity = -9.81f;    
    private Vector3 _velocity;         // Tracks vertical speed 

    void Start()
    {
        _characterController = GetComponent<UnityEngine.CharacterController>();
    }

    public void PrcoessMove(Vector2 movementVector)
    {
     
        Vector3 move = transform.forward * movementVector.y + transform.right * movementVector.x;
        Vector3 horizontalMove = move * MovementSpd;


        if (_characterController.isGrounded && _velocity.y < 0)
        {
            _velocity.y = -2f; // Slight downward force to keep the player snapped to slopes
        }

     
        _velocity.y += Gravity * Time.deltaTime;

        Vector3 finalMovement = horizontalMove + _velocity;

        _characterController.Move(finalMovement * Time.deltaTime);
    }

    public void Rotate(Vector2 rotvec)
    {
        if (rotvec.sqrMagnitude > 0.01f)
        {
            _rotY += rotvec.x * RotSpd * Time.deltaTime;
            transform.localRotation = Quaternion.Euler(0, _rotY, 0);
        }
    }
    private float _rotY;
}
