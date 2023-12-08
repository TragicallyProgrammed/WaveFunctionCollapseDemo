using UnityEngine;
using UnityEngine.InputSystem;

public class Controls : MonoBehaviour
{
    public WorldManager WorldManager;
    private PlayerInput _playerInput;

    void Awake()
    {
        _playerInput = GetComponent<PlayerInput>();
        InputAction pressKeyboard = _playerInput.actions["PressKeyboard"];
        pressKeyboard.performed += _ => RegenerateTerrain();
    }

    private void RegenerateTerrain()
    {
        if (!WorldManager.Generating)
            WorldManager.GenerateTerrain();
    }
}
