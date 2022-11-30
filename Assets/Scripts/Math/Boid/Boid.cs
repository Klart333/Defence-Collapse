using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class Boid : MonoBehaviour
{
    [SerializeField]
    private float speed;

    [SerializeField]
    private float rotationDiff = 0.02f;

    public Vector3 Direction { get; set; }
    public float ExtraSpeed { get; set; } = 1;

    private void Update()
    {
        RotateTowardsDirection();
        Direction = transform.forward;

        transform.position += transform.forward * speed * ExtraSpeed * Time.deltaTime;
    }

    private void RotateTowardsDirection()
    {
        Quaternion targetRotation = Quaternion.LookRotation(Direction.normalized, transform.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationDiff);
    }
}
