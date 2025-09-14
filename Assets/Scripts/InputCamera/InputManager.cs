using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace InputCamera
{
    public class InputManager : Singleton<InputManager>
    {
        private InputActions InputActions;
        private Camera cam;

        public InputAction Move { get; private set; }
        public InputAction Escape { get; private set; }
        public InputAction Scroll { get; private set; }
        public InputAction Mouse { get; private set; }
        public InputAction Fire { get; private set; }
        public InputAction PanCamera { get; private set; }
        public InputAction Rotate { get; private set; }
        public InputAction Shift { get; private set; }
        public InputAction Cancel { get; private set; }
        public InputAction Space { get; private set; }
        public InputAction Tab { get; private set; }
        
        public Vector2 CurrentMouseScreenPosition { get; private set; }
        public Vector3 CurrentMouseWorldPosition { get; private set; }
        
        public bool GetShift => Shift.IsPressed();

        private void OnEnable()
        {
            if (Instance != this) return;
            
            cam = Camera.main;
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
            
            SceneManager.sceneLoaded += SceneManagerOnsceneLoaded;
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
            
            SceneManager.sceneLoaded -= SceneManagerOnsceneLoaded;
        }

        private void SceneManagerOnsceneLoaded(Scene arg0, LoadSceneMode arg1)
        {
            cam = Camera.main;
        }

        private void Update()
        {
            if (cam == null)
            {
                return;
            }
            
            CurrentMouseScreenPosition = Mouse.ReadValue<Vector2>();
            CurrentMouseWorldPosition = Utility.Math.GetGroundIntersectionPoint(cam, CurrentMouseScreenPosition);
        }

        public bool MouseOverUI()
        {
            return EventSystem.current.IsPointerOverGameObject();
        }
    }
}
