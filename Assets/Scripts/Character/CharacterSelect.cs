using NUnit.Framework.Constraints;
using UnityEngine;

public class CharacterSelect : MonoBehaviour
{
    public RobotController _rController;
    public PlayerController _playerController;

    public enum StartingSelection { Human, Robot }
    [Header("Choose Start Character")]
    public StartingSelection startAs = StartingSelection.Human;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        InputController input = FindAnyObjectByType<InputController>();

        if (startAs == StartingSelection.Human)
            input.SetStartingCharacter(_playerController, _rController);
        else
            input.SetStartingCharacter(_rController, _playerController);
    }
}
