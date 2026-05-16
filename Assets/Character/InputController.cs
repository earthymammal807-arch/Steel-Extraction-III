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

    [Header("Targets")]
    [SerializeField] private PlayerController humanCharacter;
    [SerializeField] private RobotController mechCharacter;

    [Header("Starting Settings")]
    [SerializeField] private MonoBehaviour startingControlTarget;

    void Awake()
    {
        // Build actions with bindings in code
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

        // Resolve interfaces
        humanInterface = humanCharacter;
        vehicleInterface = mechCharacter;

        if (startingControlTarget == null)
            startingControlTarget = humanCharacter;

        currentPossessedObject = startingControlTarget as IControllable;

        // Set correct camera on frame one
        if (humanInterface != null) humanInterface.SetFocus(currentPossessedObject == humanInterface);
        if (vehicleInterface != null) vehicleInterface.SetFocus(currentPossessedObject == vehicleInterface);

        interactAction.started += ctx => HandleInteraction();
    }

    void Update()
    {
        if (currentPossessedObject == null) return;

        Vector2 moveInput = moveAction.ReadValue<Vector2>();
        Vector2 lookInput = lookAction.ReadValue<Vector2>();

        currentPossessedObject.ProcessMove(moveInput);
        currentPossessedObject.Rotate(lookInput);
    }

    private void HandleInteraction()
    {
        IControllable oldTarget = currentPossessedObject;
        IControllable newTarget = (currentPossessedObject == humanInterface) ? vehicleInterface : humanInterface;

        if (oldTarget != null) oldTarget.SetFocus(false);
        if (newTarget != null) newTarget.SetFocus(true);

        currentPossessedObject = newTarget;
        Debug.Log("Swapped active controls!");
    }

    void OnEnable()
    {
        moveAction.Enable();
        lookAction.Enable();
        interactAction.Enable();
    }

    void OnDisable()
    {
        moveAction.Disable();
        lookAction.Disable();
        interactAction.Disable();
    }
}