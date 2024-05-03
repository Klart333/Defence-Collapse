using UnityEngine.InputSystem;

public class InputManager : Singleton<InputManager>
{
    private InputActions InputActions;

    public InputAction Move { get; private set; }
    public InputAction Fire { get; private set; }
    public InputAction Shift { get; private set; }

    public bool GetFire => Fire.IsPressed();
    public bool GetShift => Shift.IsPressed();

    private void OnEnable()
    {
        InputActions = new InputActions();

        Move = InputActions.Player.Move;
        Move.Enable();

        Fire = InputActions.Player.Fire;
        Fire.Enable();

        Shift = InputActions.Player.Shift;
        Shift.Enable();
    }

    private void OnDisable()
    {
        Move.Disable();
        Fire.Disable();
        Shift.Disable();
    }
}
