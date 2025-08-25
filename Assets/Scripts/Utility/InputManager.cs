using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class InputManager : Singleton<InputManager>
{
    private InputActions InputActions;

    public InputAction Move { get; private set; }
    public InputAction Escape { get; private set; }
    public InputAction Scroll { get; private set; }
    public InputAction Mouse { get; private set; }
    public InputAction Fire { get; private set; }
    public InputAction PanCamera { get; private set; }
    public InputAction Rotate { get; private set; }
    public InputAction Shift { get; private set; }
    public InputAction Cancel {  get; private set; }
    public InputAction Space {  get; private set; }
    public InputAction Tab {  get; private set; }

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

        Cancel = InputActions.Player.Cancel;
        Cancel.Enable();

        Mouse = InputActions.Player.Mouse;
        Mouse.Enable();
        
        Space = InputActions.Player.Space;
        Space.Enable();

        PanCamera = InputActions.Player.PanCamera;
        PanCamera.Enable();
        
        Scroll = InputActions.Player.Scroll;
        Scroll.Enable();
        
        Rotate = InputActions.Player.Rotate;
        Rotate.Enable();
        
        Tab = InputActions.Player.Tab;
        Tab.Enable();
        
        Escape = InputActions.Player.Escape;
        Escape.Enable();
    }
    
    private void OnDisable()
    {
        if (Instance != this)
        {
            return;
        }
        
        PanCamera.Disable();
        Cancel.Disable();
        Rotate.Disable();
        Escape.Disable();
        Scroll.Disable();
        Shift.Disable();
        Mouse.Disable();
        Space.Disable();
        Move.Disable();
        Fire.Disable();
        Tab.Disable();
    }

    public bool MouseOverUI()
    {
        return EventSystem.current.IsPointerOverGameObject();
    }
}
