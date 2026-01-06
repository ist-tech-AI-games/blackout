using UnityEngine;
using UnityEngine.InputSystem;

public class UnitController : MonoBehaviour
{
    [Header("Units")]
    [SerializeField] private Unit[] units;
    private int selection = -1;

    [Header("Input Actions")]
    [SerializeField] private InputActionReference moveAction;
    [SerializeField] private InputActionReference cancelAction; // ESC

    [Header("Camera")]
    [SerializeField] private Transform virtualCamera;
    [SerializeField] private float cameraMoveSpeed = 5f;

    private void OnEnable()
    {
        moveAction.action.Enable();
        cancelAction.action.Enable();
    }

    private void OnDisable()
    {
        moveAction.action.Disable();
        cancelAction.action.Disable();
    }

    private void Update()
    {
        HandleUnitSelection();
        HandleCancel();
        HandleMovement();
    }

    private void HandleUnitSelection()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        for (int i = 1; i <= 10; i++)
        {
            Key key = Key.Digit1 + i - 1;
            if (keyboard[key].wasPressedThisFrame)
            {
                int ind = i % 10;
                if (ind < units.Length)
                {
                    selection = ind;
                    Debug.Log($"Unit {selection} selected");
                }
            }
        }
    }

    private void HandleCancel()
    {
        if (cancelAction.action.WasPressedThisFrame())
        {
            selection = -1;
            Debug.Log("Unit selection cleared");
        }
    }

    private void HandleMovement()
    {
        Vector2 moveInput = moveAction.action.ReadValue<Vector2>();
        if (moveInput == Vector2.zero)
            return;

        if (selection >= 0 && selection < units.Length)
            units[selection].Move(moveInput * Time.deltaTime);
        else
            MoveCamera(moveInput);
    }

    private void MoveCamera(Vector2 input)
    {
        if (virtualCamera == null)
            return;

        Transform camTransform = virtualCamera.transform;
        Vector3 delta = new Vector3(input.x, input.y, 0f)
                        * cameraMoveSpeed
                        * Time.deltaTime;

        camTransform.position += delta;
    }
}
