using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FPSCamera : MonoBehaviour
{
    [SerializeField]
    private float sensitivity;

    private Vector2 lastMousePosition;

    private void Start()
    {
        Cursor.lockState = CursorLockMode.None;
    }

    private void Update()
    {
        if (lastMousePosition.x == 0)
            return;

        float mouseDeltaX = Input.mousePosition.x - lastMousePosition.x;
        float mouseDeltaY = Input.mousePosition.y - lastMousePosition.y;

        Quaternion xAxis = Quaternion.AngleAxis(mouseDeltaY * sensitivity, Vector3.right);
        Quaternion yAxis = Quaternion.AngleAxis(mouseDeltaX * sensitivity, Vector3.up);

        transform.rotation = xAxis * transform.rotation;
        transform.rotation = yAxis * transform.rotation;
    }

    private void LateUpdate()
    {
        lastMousePosition = Input.mousePosition;
    }
}
