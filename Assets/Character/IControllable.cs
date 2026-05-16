using UnityEngine;

public interface IControllable
{
    void ProcessMove(Vector2 movementVector);
    void Rotate(Vector2 rotationVector);
    // Add this line to handle camera/focus states
    void SetFocus(bool isFocused);
}
