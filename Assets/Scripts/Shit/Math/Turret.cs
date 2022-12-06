using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

[ExecuteAlways]
public class Turret : MonoBehaviour
{
    [Header("Graphic")]
    [SerializeField]
    private Transform turret;

    [SerializeField]
    private Transform pole;

    [Header("Range")]
    [SerializeField]
    private Shape shape;

    [SerializeField]
    private float range;

    [SerializeField]
    private float height;

    [SerializeField]
    private float heightOffset;

    [SerializeField]
    private float minAngle;

    [SerializeField]
    private float minDistance;

    [SerializeField]
    private float rotationSpeed;

    [Header("Debug")]
    [SerializeField]
    private int segments;

    private Target[] targets;

    private void Update()
    {
        targets = FindObjectsOfType<Target>();

        for (int i = 0; i < targets.Length; i++)
        {
            if (InRange(targets[i].transform.position))
            {
                if (InAngle(targets[i].transform.position))
                {
                    RotateTowards(targets[i].transform.position);
                    return;
                }
            }
        }

        turret.rotation = Quaternion.Slerp(turret.rotation, transform.rotation, rotationSpeed / 2.0f);
    }

    private void RotateTowards(Vector3 position)
    {
        Vector3 dir = (position - turret.position).normalized;

        turret.rotation = Quaternion.Slerp(turret.rotation, Quaternion.LookRotation(dir, pole.up), rotationSpeed);
    }

    private void OnDrawGizmos()
    {
        float anglePer = (Mathf.PI * 2) / segments;

        for (int g = 0; g < 2; g++)
        {
            for (int i = 0; i < segments; i++)
            {
                float angle = i * anglePer;

                // Local Space
                Vector3 pos = new Vector3(Mathf.Cos(angle) * range, ((g * 2.0f) - 1.0f) * height + heightOffset, Mathf.Sin(angle) * range);

                // World Space
                pos = transform.TransformPoint(pos);

                if (!InAngle(pos))
                {
                    continue;
                }

                Gizmos.color = Color.red;
                Gizmos.DrawSphere(pos, 0.1f);
            }
        }
    }

    private bool InRange(Vector3 pos)
    {
        float dist = Vector3.Distance(turret.position, pos);
        float distXY = Vector2.Distance(new Vector2(turret.position.x, turret.position.z), new Vector2(pos.x, pos.z));

        Vector3 localLos = transform.InverseTransformPoint(pos);
        float distY = Mathf.Abs((turret.position.y + heightOffset) - localLos.y);

        // Constaints depending on shape
        switch (shape)
        {
            case Shape.Cheese:
                if (distXY < minDistance || distXY > range)
                {
                    return false;
                }

                if (distY > height)
                {
                    return false;
                }

                break;
            case Shape.Ball:
                if (dist < minDistance || dist > range)
                {
                    return false;
                }

                break;
            case Shape.CheeseBall:
                if (dist < minDistance || dist > range)
                {
                    return false;
                }

                break;
            default:
                break;
        }
        
        return true;
    }

    private bool InAngle(Vector3 pos)
    {
        float dot = Vector3.Dot(turret.forward, (pos - turret.position).normalized);

        switch (shape)
        {
            case Shape.Cheese:
                if (dot < minAngle)
                {
                    return false;
                }
                break;
            case Shape.Ball:
                break;
            case Shape.CheeseBall:
                if (dot < minAngle)
                {
                    return false;
                }
                break;
            default:
                break;
        }
        return true;
    }
}

public enum Shape
{
    Cheese,
    Ball,
    CheeseBall
}
