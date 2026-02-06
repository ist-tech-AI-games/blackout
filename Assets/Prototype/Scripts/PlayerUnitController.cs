using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerUnitController : MonoBehaviour
{
    [Header("Input Actions")]
    [SerializeField] private InputActionReference moveAction;
    [SerializeField] private InputActionReference cancelAction;
    [Header("Camera")]
    [SerializeField] private Transform virtualCamera;
    [SerializeField] private float cameraMoveSpeed = 5f;

    private GameScenario scenario; 
    private int selectedUnitIndex = -1;

    public void Initialize(GameScenario scenario)
    {
        this.scenario = scenario;
    }

    public void OnUpdate(float deltaTime)
    {
        if (scenario == null) return;

        HandleUnitSelection();
        HandleCancel();
        HandleMovement(deltaTime);
        
        if (Keyboard.current[Key.R].wasPressedThisFrame)
           scenario.EpisodeBegin();
    }

    private void HandleUnitSelection()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        for (int i = 1; i <= 10; i++)
        {
            if (keyboard[Key.Digit1 + i - 1].wasPressedThisFrame)
            {
                selectedUnitIndex = i % 10; 
                Debug.Log($"Unit Index {selectedUnitIndex} selected");
            }
        }
    }

    private void HandleCancel()
    {
        if (cancelAction.action.WasPressedThisFrame())
        {
            selectedUnitIndex = -1;
        }
    }

    private void HandleMovement(float deltaTime)
    {
        Vector2 moveInput = moveAction.action.ReadValue<Vector2>();
        if (moveInput == Vector2.zero) return;

        if (selectedUnitIndex >= 0)
            scenario.MoveUnit(selectedUnitIndex, moveInput, deltaTime);
        else
            MoveCamera(moveInput, deltaTime);
    }

    private void MoveCamera(Vector2 input, float deltaTime)
    {
        if (virtualCamera == null) return;

        Vector3 delta = new Vector3(input.x, input.y, 0f) * cameraMoveSpeed * deltaTime;
        virtualCamera.position += delta;
    }
    
    private void OnEnable() { moveAction.action.Enable(); cancelAction.action.Enable(); }
    private void OnDisable() { moveAction.action.Disable(); cancelAction.action.Disable(); }
}