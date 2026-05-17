using UnityEngine;
using UnityEngine.InputSystem;

public class InputController : MonoBehaviour
{
    private IControllable currentPossessedObject;
    private IControllable humanInterface;
    private IControllable vehicleInterface;

    private InputAction moveAction;
    private InputAction lookAction;
    private InputAction interactAction;
    private InputAction jumpAction;
    private InputAction sprintAction;

    [Header("Targets")]
    [SerializeField] private PlayerController humanCharacter;
    [SerializeField] private RobotController mechCharacter;

    [Header("Starting Settings")]
    [SerializeField] private MonoBehaviour startingControlTarget;

    void Awake()
    {
        // Build actions
        moveAction = new InputAction("Move", InputActionType.Value);
        moveAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w")
            .With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a")
            .With("Right", "<Keyboard>/d");

        lookAction = new InputAction("Look", InputActionType.Value);
        lookAction.AddBinding("<Mouse>/delta");

        interactAction = new InputAction("Interact", InputActionType.Button);
        interactAction.AddBinding("<Keyboard>/e");

        jumpAction = new InputAction("Jump", InputActionType.Button);
        jumpAction.AddBinding("<Keyboard>/space");

        sprintAction = new InputAction("Sprint", InputActionType.Button);
        sprintAction.AddBinding("<Keyboard>/leftShift");

        // Resolve interfaces
        humanInterface = humanCharacter;
        vehicleInterface = mechCharacter;

        if (startingControlTarget == null)
            startingControlTarget = humanCharacter;

        currentPossessedObject = startingControlTarget as IControllable;

        if (humanInterface != null) humanInterface.SetFocus(currentPossessedObject == humanInterface);
        if (vehicleInterface != null) vehicleInterface.SetFocus(currentPossessedObject == vehicleInterface);
    }

    void OnEnable()
    {
        // Enable first, then subscribe
        moveAction.Enable();
        lookAction.Enable();
        interactAction.Enable();
        jumpAction.Enable();
        sprintAction.Enable();

        interactAction.started += HandleInteraction;
        sprintAction.performed += OnSprintPressed;
        sprintAction.canceled += OnSprintReleased;
        jumpAction.performed += OnJump;
        jumpAction.canceled += OnJumpReleased;
    }

    void OnDisable()
    {
        // Unsubscribe before disabling to avoid stale references
        interactAction.started -= HandleInteraction;
        sprintAction.performed -= OnSprintPressed;
        sprintAction.canceled -= OnSprintReleased;
        jumpAction.performed -= OnJump;
        jumpAction.canceled -= OnJumpReleased;

        moveAction.Disable();
        lookAction.Disable();
        interactAction.Disable();
        jumpAction.Disable();
        sprintAction.Disable();
    }

    void Update()
    {
        if (currentPossessedObject == null) return;
        currentPossessedObject.ProcessMove(moveAction.ReadValue<Vector2>());
        currentPossessedObject.Rotate(lookAction.ReadValue<Vector2>());



    }
    

    private void HandleInteraction(InputAction.CallbackContext ctx)
    {
        IControllable oldTarget = currentPossessedObject;
        IControllable newTarget = (currentPossessedObject == humanInterface) ? vehicleInterface : humanInterface;

        if (oldTarget != null) oldTarget.SetFocus(false);
        if (newTarget != null) newTarget.SetFocus(true);

        currentPossessedObject = newTarget;
        Debug.Log("Swapped to: " + currentPossessedObject);
    }

    private void OnSprintPressed(InputAction.CallbackContext ctx)
    {
        Debug.Log("Sprint pressed");
        if (currentPossessedObject is PlayerController p) p.SetSprint(true);
    }

    private void OnSprintReleased(InputAction.CallbackContext ctx)
    {
        Debug.Log("Sprint released");
        if (currentPossessedObject is PlayerController p) p.SetSprint(false);
    }

    private void OnJump(InputAction.CallbackContext ctx)
    {
        currentPossessedObject.Jump();
    }
    private void OnJumpReleased(InputAction.CallbackContext ctx)
    {
        currentPossessedObject.JumpCancelled();
    }
}