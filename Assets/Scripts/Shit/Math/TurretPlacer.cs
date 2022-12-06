using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TurretPlacer : MonoBehaviour
{
    [SerializeField]
    private Transform turret;

    [SerializeField]
    private float placementDist = 0.5f;

    [SerializeField]
    private float sensitivity = 0.1f;

    private Vector3? placement = null;

    private Quaternion targetRotation;

    private void Update()
    {
        if (Input.mouseScrollDelta.y != 0)
        {
            placement = turret.position;
            targetRotation = Quaternion.AngleAxis(Input.mouseScrollDelta.y * sensitivity, turret.up) * turret.rotation;
        }
        turret.rotation = Quaternion.Slerp(turret.rotation, targetRotation, 0.05f);

        if (Physics.Raycast(transform.position, transform.forward, out RaycastHit hitInfo))
        {
            if (placement.HasValue)
            {
                if (Vector3.Distance(placement.Value, hitInfo.point) < placementDist)
                {
                    return;
                }
            }

            placement = null;
            PlaceTurret(hitInfo.point, hitInfo.normal);
        }
    }

    public void PlaceTurret(Vector3 pos, Vector3 surfaceNormal)
    {
        if (turret == null)
        {
            return;
        }

        turret.position = pos;

        Vector3 tangent = Vector3.Cross(Vector3.up, transform.forward).normalized;
        Vector3 disTanget = Vector3.Cross(tangent, surfaceNormal).normalized;

        turret.rotation = Quaternion.LookRotation(disTanget, surfaceNormal);
    }
}
