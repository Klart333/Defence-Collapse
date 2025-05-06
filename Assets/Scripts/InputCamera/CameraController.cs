using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using Cysharp.Threading.Tasks;
using InputCamera.ECS;
using Sirenix.OdinInspector;
using Unity.Entities;
using UnityEngine;

namespace InputCamera
{
    [RequireComponent(typeof(Camera))]
    public class CameraController : MonoBehaviour
    {
        public static bool IsDragging;

        [Title("Movement Settings")]
        [SerializeField]
        private float moveSpeed = 5f;

        [SerializeField]
        private float fastMoveMultiplier = 2f;

        [Title("Rotation Settings")]
        [SerializeField]
        private float rotationSpeed = 500f;

        [SerializeField]
        private float maxVerticalAngle = 80f;

        [Title("Zoom Settings")]
        [SerializeField]
        private float zoomSensitivity = 1f;

        [SerializeField]
        private float minZoomDistance = 1f;

        [SerializeField]
        private float maxZoomDistance = 100f;

        [Title("Pan Settings")]
        [SerializeField]
        private float panSensitivity = 0.5f;

        private const float startDraggingThreshold = 64; // 8^2 px
        
        private EntityManager entityManager;
        private Entity cameraEntity;

        // Input actions
        private InputAction fastMoveAction;
        private InputActions inputActions;
        private InputAction rotateAction;
        private InputAction moveAction;
        private InputAction zoomAction;
        private InputAction panAction;

        private Camera controlledCamera;
        private Vector3 panStartPosition;
        private Vector2 panStartScreenPosition;

        private bool isPressed;

        private void Awake()
        {
            controlledCamera = GetComponent<Camera>();
            InitializeInputActions();
            entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            cameraEntity = entityManager.CreateEntity();
            entityManager.AddComponentData(cameraEntity, new CameraPositionComponent { Position = transform.position });
        }

        private void InitializeInputActions()
        {
            inputActions = new InputActions();

            // Set up input actions
            moveAction = inputActions.Player.Move;
            zoomAction = inputActions.Player.Scroll;
            rotateAction = inputActions.Player.Rotate;
            panAction = inputActions.Player.Fire;
            fastMoveAction = inputActions.Player.Shift;
        }

        private void OnEnable()
        {
            // Enable all actions
            moveAction.Enable();
            zoomAction.Enable();
            rotateAction.Enable();
            panAction.Enable();
            fastMoveAction.Enable();

            // Subscribe to pan start/end events
            panAction.started += StartPan;
            panAction.canceled += EndPan;

            Events.OnGameReset += OnGameReset;
        }

        private void OnDisable()
        {
            // Disable all actions
            moveAction.Disable();
            zoomAction.Disable();
            rotateAction.Disable();
            panAction.Disable();
            fastMoveAction.Disable();

            // Unsubscribe from events
            panAction.started -= StartPan;
            panAction.canceled -= EndPan;

            Events.OnGameReset -= OnGameReset;
        }

        private void Update()
        {
            HandleMovement();
            HandleZoom();
            HandleRotation();
            HandlePan();
            
            entityManager.AddComponentData(cameraEntity, new CameraPositionComponent { Position = transform.position });
        }

        private void HandleMovement()
        {
            Vector2 moveInput = moveAction.ReadValue<Vector2>();
            if (moveInput == Vector2.zero) return;

            float speedMultiplier = fastMoveAction.IsPressed() ? fastMoveMultiplier : 1f;
            float currentSpeed = moveSpeed * speedMultiplier * Time.deltaTime;

            // Forward/back movement (relative to camera rotation)
            Vector3 forwardMovement = transform.forward * (moveInput.y * currentSpeed);
            // Left/right movement (relative to camera rotation)
            Vector3 rightMovement = transform.right * (moveInput.x * currentSpeed);

            // Move while keeping y position (unless moving up/down)
            forwardMovement.y = 0;
            rightMovement.y = 0;

            transform.position += forwardMovement + rightMovement;
        }

        private void HandleZoom()
        {
            float zoomInput = zoomAction.ReadValue<Vector2>().y;
            if (Mathf.Abs(zoomInput) < 0.1f) return;

            float zoomDirection = Mathf.Sign(zoomInput);
            float zoomAmount = zoomSensitivity * zoomDirection * Time.deltaTime;

            // Raycast-based zoom (towards what the camera is pointing at)
            Vector3 groundPos = Math.GetGroundIntersectionPoint(controlledCamera, Mouse.current.position.ReadValue());
            Vector3 directionToTarget = (transform.position - groundPos).normalized;
            float currentDistance = Vector3.Distance(transform.position, groundPos);
            float newDistance = Mathf.Clamp(currentDistance - zoomAmount, minZoomDistance, maxZoomDistance);

            transform.position = groundPos + directionToTarget * newDistance;
        }

        private void HandleRotation()
        {
            float rotateInput = rotateAction.ReadValue<float>();
            if (rotateInput == 0) return;

            Vector3 currentMousePoint = Math.GetGroundIntersectionPoint(controlledCamera, Mouse.current.position.ReadValue());
            if (currentMousePoint == Vector3.zero) return;

            float rotationAmount = rotationSpeed * Time.deltaTime * (fastMoveAction.IsPressed() ? fastMoveMultiplier : 1f);
            float yaw = rotateInput * rotationAmount;

            transform.RotateAround(currentMousePoint, Vector3.up, yaw);
            //transform.LookAt(currentMousePoint);
        }

        private void StartPan(InputAction.CallbackContext obj)
        {
            if (EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            isPressed = true;
            panStartPosition = Math.GetGroundIntersectionPoint(controlledCamera, Mouse.current.position.ReadValue());
            panStartScreenPosition = Mouse.current.position.ReadValue();
        }

        private async void EndPan(InputAction.CallbackContext obj)
        {
            isPressed = false;
            if (!IsDragging)
            {
                return;
            }

            await UniTask.WaitForEndOfFrame();
            IsDragging = false;
        }

        private void HandlePan()
        {
            if (!isPressed) return;

            Vector2 mousePosition = Mouse.current.position.ReadValue();
            if (!IsDragging && (panStartScreenPosition - mousePosition).sqrMagnitude < startDraggingThreshold)
            {
                return;
            }

            Vector3 currentMousePoint = Math.GetGroundIntersectionPoint(controlledCamera, mousePosition);
            if (currentMousePoint == Vector3.zero) return;

            Vector3 delta = panStartPosition - currentMousePoint;

            IsDragging = true;
            transform.position += delta * 0.5f;
        }


        private void OnGameReset()
        {
            IsDragging = false;
        }
    }
}
