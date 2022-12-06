using Cinemachine;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Windows.WebCam;

public class CameraController : MonoBehaviour
{
    [SerializeField]
    private float flySpeed = 1;

    [SerializeField]
    private float sensitivity = 1;

    private InputActions inputActions;
    private InputAction scroll;
    private InputAction up;
    private InputAction down;
    private InputAction move;

    private float zoom = 1;

    private void OnEnable()
    {
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

        transform.position += new Vector3(movee.x, 0, movee.y);

        HandleScroll();
    }

    private void HandleScroll()
    {
        float scrollDiff = scroll.ReadValue<Vector2>().y / -120.0f;
        float moveDiff = (up.ReadValue<float>() - down.ReadValue<float>()) / 20.0f;
        transform.position += Vector3.up * scrollDiff * sensitivity * Time.deltaTime;
        transform.position += Vector3.up * moveDiff * sensitivity * Time.deltaTime;
    }
}
