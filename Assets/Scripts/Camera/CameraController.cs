using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    [SerializeField]
    private float flySpeed = 1;

    [SerializeField]
    private float sensitivity = 1;

    [SerializeField]
    private float rotationSens = 500;

    private InputActions inputActions;
    private InputAction scroll;
    private InputAction up;
    private InputAction down;
    private InputAction move;
    private Camera cam;

    private float zoom = 1;

    private void OnEnable()
    {
        cam = Camera.main;
        inputActions = new InputActions();

        scroll = inputActions.Player.Scroll;
        scroll.Enable();

        move = inputActions.Player.Move;
        move.Enable();

        down = inputActions.Player.Down;
        down.Enable();

        up = inputActions.Player.Up;
        up.Enable();
    }

    private void OnDisable()
    {
        scroll.Disable();
        move.Disable();
        up.Disable();
        down.Disable();
    }

    private void Update()
    {
        Vector2 movee = move.ReadValue<Vector2>() * Time.deltaTime * flySpeed;

        transform.position += Quaternion.AngleAxis(transform.eulerAngles.y, Vector3.up) * Vector3.forward * movee.y;
        transform.position += transform.right * movee.x;

        HandleScroll();
    }

    private void HandleScroll()
    {
        float scrollDiff = scroll.ReadValue<Vector2>().y / -12.0f;
        float moveDiff = (up.ReadValue<float>() - down.ReadValue<float>()) / 20.0f;

        if (Mathf.Abs(scrollDiff) > 0.1f)
        {
            Vector3 mousePosition = Input.mousePosition;
            Ray ray = cam.ScreenPointToRay(mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hitInfo))
            {
                Vector3 dir = (transform.position - hitInfo.point).normalized;
                transform.position += dir * scrollDiff * sensitivity * 0.01f;
            }
            else
            {
                transform.position += Vector3.up * scrollDiff * sensitivity * 0.01f;
            }
        }

        if (Mathf.Abs(moveDiff) != 0)
        {
            Vector3 mousePosition = Input.mousePosition;
            Ray ray = cam.ScreenPointToRay(mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hitInfo))
            {
                transform.rotation = Quaternion.AngleAxis(moveDiff * rotationSens * Time.deltaTime, Vector3.up) * transform.rotation;

                Vector3 dir = (transform.position - hitInfo.point);
                Vector3 newPos = Quaternion.AngleAxis(moveDiff * rotationSens * Time.deltaTime, Vector3.up) * dir;
                transform.position += newPos - dir;
            }
            else
            {
                transform.rotation = Quaternion.AngleAxis(moveDiff * rotationSens * Time.deltaTime, Vector3.up) * transform.rotation;
            }
        }
    }
}
