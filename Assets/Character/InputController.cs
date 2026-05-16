using UnityEngine;
using UnityEngine.InputSystem;

public class InputController : MonoBehaviour
{
    public CharacterController CharacterController;

    public InputAction _moveAction, _lookAction; //can add more in project settings
    
    void Start()
    {
        _moveAction = InputSystem.actions.FindAction("Move");
        _lookAction = InputSystem.actions.FindAction("Look");

        Cursor.visible = false;
    }

    // Update is called once per frame
    void Update()
    {
        Vector2 movementVec = _moveAction.ReadValue<Vector2>();
        CharacterController.PrcoessMove(movementVec);

        Vector2 lookVec = _lookAction.ReadValue<Vector2>();
        CharacterController.Rotate(lookVec);
    }

  
}
