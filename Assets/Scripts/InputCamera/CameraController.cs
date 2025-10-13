using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using InputCamera.ECS;
using Gameplay.Event;
using Unity.Entities;
using UnityEngine;
using Gameplay;

namespace InputCamera
{
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
        private float minZoomHeight = 1f;

        [SerializeField]
        private float maxZoomHeight = 40;

        [Title("Pan Settings")]
        [SerializeField]
        private float panSensitivity = 0.5f;

        private const float startDraggingThreshold = 64; // 8^2 px
        
        private EntityManager entityManager;
        private InputManager inputManager;
        private Camera controlledCamera;
        private Entity cameraEntity;

        private Vector3 panStartPosition;
        private Vector2 panStartScreenPosition;

        private bool isPressed;

        private void Awake()
        {
            controlledCamera = GetComponentInChildren<Camera>();
            entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            cameraEntity = entityManager.CreateEntity();
            entityManager.AddComponentData(cameraEntity, new CameraPositionComponent { Position = transform.position });
        }

        private async UniTaskVoid GetInput()
        {
            inputManager = await InputManager.Get();
            
            // Subscribe to pan start/end events
            inputManager.Fire.started += StartPan;
            inputManager.Fire.canceled += EndPan;

            inputManager.PanCamera.started += StartPan;
            inputManager.PanCamera.canceled += EndPan;
        }

        private void OnEnable()
        { 
            GetInput().Forget();
            
            Events.OnGameReset += OnGameReset;
        }

        private void OnDisable()
        {
            inputManager.Fire.started -= StartPan;
            inputManager.Fire.canceled -= EndPan;

            inputManager.PanCamera.started -= StartPan;
            inputManager.PanCamera.canceled -= EndPan;
            
            Events.OnGameReset -= OnGameReset;
        }

        private void Update()
        {
            HandleMovement();
            HandleZoom();
            HandleRotation();
            HandlePan();

            if (!GameManager.Instance.IsGameOver)
            {
                entityManager.AddComponentData(cameraEntity, new CameraPositionComponent { Position = transform.position });
            }
        }

        private void HandleMovement()
        {
            Vector2 moveInput = inputManager.Move.ReadValue<Vector2>();
            if (moveInput == Vector2.zero) return;

            float speedMultiplier = inputManager.Shift.IsPressed() ? fastMoveMultiplier : 1f;
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
            float zoomInput = inputManager.Scroll.ReadValue<Vector2>().y;
            if (Mathf.Abs(zoomInput) < 0.1f) return;

            float zoomDirection = Mathf.Sign(zoomInput);
            float zoomAmount = zoomSensitivity * zoomDirection * Time.deltaTime;

            // Raycast-based zoom (towards what the camera is pointing at)
            Vector3 groundPos = Utility.Math.GetGroundIntersectionPoint(controlledCamera, Mouse.current.position.ReadValue());
            Vector3 directionToTarget = (transform.position - groundPos).normalized;
            float distance = Vector3.Distance(transform.position, groundPos);
            float hypotenuse = distance - zoomAmount;
            Vector3 targetPosition = groundPos + directionToTarget * hypotenuse;
            if (targetPosition.y > maxZoomHeight)
            {
                targetPosition.y = maxZoomHeight;
            }
            else if (targetPosition.y < minZoomHeight)
            {
                targetPosition.y = minZoomHeight;
            }

            transform.position = targetPosition;
        }

        private void HandleRotation()
        {
            float rotateInput = inputManager.Rotate.ReadValue<float>();
            if (rotateInput == 0) return;

            Vector3 groundPos = Utility.Math.GetGroundIntersectionPoint(transform.position, transform.forward);
            if (groundPos == Vector3.zero) return;

            float rotationAmount = rotationSpeed * Time.deltaTime * (inputManager.Shift.IsPressed() ? fastMoveMultiplier : 1f);
            float yaw = rotateInput * rotationAmount;

            transform.RotateAround(groundPos, Vector3.up, yaw);
        }

        private void StartPan(InputAction.CallbackContext obj)
        {
            if (EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            isPressed = true;
            panStartPosition = Utility.Math.GetGroundIntersectionPoint(controlledCamera, Mouse.current.position.ReadValue());
            panStartScreenPosition = Mouse.current.position.ReadValue();
        }

        private void EndPan(InputAction.CallbackContext obj)
        {
            EndPanAsync().Forget();
        }

        private async UniTaskVoid EndPanAsync()
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

            Vector3 currentMousePoint = Utility.Math.GetGroundIntersectionPoint(controlledCamera, mousePosition);
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
