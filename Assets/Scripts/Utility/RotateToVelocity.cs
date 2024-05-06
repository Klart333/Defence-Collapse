using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotateToVelocity : MonoBehaviour
{
    private Vector3 previousPosition;
    private Vector3 currentVelocity;

    void Start()
    {
        previousPosition = transform.position;
    }

    void Update()
    {
        currentVelocity = (transform.position - previousPosition) / Time.deltaTime;

        previousPosition = transform.position;

        if (currentVelocity.sqrMagnitude > 0.0001f)  
        {
            Quaternion targetRotation = Quaternion.LookRotation(currentVelocity);
            transform.rotation = targetRotation;
        }
    }
}
