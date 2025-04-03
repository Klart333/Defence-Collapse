using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Camera))]
public class CameraController : MonoBehaviour
{
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
    
    [SerializeField] 
    private bool invertPan = false;

    // Input actions
    private InputActions inputActions;
    private InputAction moveAction;
    private InputAction zoomAction;
    private InputAction rotateAction;
    private InputAction panAction;
    private InputAction fastMoveAction;
    
    // Camera reference
    private Camera controlledCamera;
    private bool isPanning = false;
    private Vector3 panStartPosition;
    private Vector3 panCameraStartPosition;

    private void Awake()
    {
        controlledCamera = GetComponent<Camera>();
        InitializeInputActions();
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
    }

    private void Update()
    {
        HandleMovement();
        HandleZoom();
        HandleRotation();
        HandlePan();
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
        Vector3 groundPos = GetGroundIntersectionPoint();
        Vector3 directionToTarget = (transform.position - groundPos).normalized;
        float currentDistance = Vector3.Distance(transform.position, groundPos);
        float newDistance = Mathf.Clamp(currentDistance - zoomAmount, minZoomDistance, maxZoomDistance);
            
        transform.position = groundPos + directionToTarget * newDistance;
    }

    private void HandleRotation()
    {
        float rotateInput = rotateAction.ReadValue<float>();
        if (rotateInput == 0) return;

        Vector3 currentMousePoint = GetGroundIntersectionPoint();
        if (currentMousePoint == Vector3.zero) return;

        float rotationAmount = rotationSpeed * Time.deltaTime * (fastMoveAction.IsPressed() ? fastMoveMultiplier : 1f);
        float yaw = rotateInput * rotationAmount;
        
        transform.RotateAround(currentMousePoint, Vector3.up, yaw);
        //transform.LookAt(currentMousePoint);
    }

    private void StartPan(InputAction.CallbackContext obj)
    {
        isPanning = true;
        panStartPosition = GetGroundIntersectionPoint();
        panCameraStartPosition = transform.position;
    }

    private void EndPan(InputAction.CallbackContext obj)
    {
        isPanning = false;
    }

    private void HandlePan()
    {
        if (!isPanning) return;
        
        Vector3 currentMousePoint = GetGroundIntersectionPoint();
        if (currentMousePoint == Vector3.zero) return;

        Vector3 delta = panStartPosition - currentMousePoint;
        transform.position += delta * 0.5f;
    }
    
    private Vector3 GetGroundIntersectionPoint()
    {
        Ray ray = controlledCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
        
        if (groundPlane.Raycast(ray, out float distance))
        {
            return ray.GetPoint(distance);
        }
        return Vector3.zero;
    }
}
