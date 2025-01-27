using System;
using System.Collections.Generic;
using UnityEngine;

public class Indexer : MonoBehaviour
{
    private enum ColliderType {None, Circle, Box, Mesh}
    
    public event Action OnRebuilt;

    [SerializeField]
    private Collider indexCollider;

    private bool needsRebuilding = true;

    public List<int> Indexes { get; } = new List<int>();

    private void OnValidate()
    {
        indexCollider ??= GetComponent<Collider>();
    }

    private void OnEnable()
    {
        indexCollider ??= GetComponent<Collider>();
    }

    private void Update()
    {
        if (transform.hasChanged)
        {
            needsRebuilding = true;
        }

        if (needsRebuilding)
        {
            needsRebuilding = false;
            BuildIndexes();
            transform.hasChanged = false;
        }
    }

    public void BuildIndexes()
    {
        Indexes.Clear();
        float xMin = Mathf.Max(0, indexCollider.bounds.min.x);
        float xMax = Mathf.Min(PathManager.Instance.GridWidth, indexCollider.bounds.max.x);
        float zMin = Mathf.Max(0, indexCollider.bounds.min.z);
        float zMax = Mathf.Min(PathManager.Instance.GridHeight, indexCollider.bounds.max.z);

        ColliderType colliderType = indexCollider switch
        {
            BoxCollider => ColliderType.Box,
            SphereCollider => ColliderType.Circle,
            MeshCollider => ColliderType.Mesh,
            _ => ColliderType.None
        };
        float increment = PathManager.Instance.CellScale;

        Vector3 boxCenter = Vector3.zero;
        Vector3 boxSize = Vector3.zero;
        Quaternion boxRotation = Quaternion.identity;

        if (colliderType is ColliderType.Box)
        {
            BoxCollider boxCollider = (BoxCollider)indexCollider;
            boxCenter = boxCollider.bounds.center;
            boxSize = boxCollider.size;
            boxRotation = boxCollider.transform.rotation;
        }
        
        for (float xPos = xMin; xPos < xMax; xPos+= increment)
        {
            for (float zPos = zMin; zPos < zMax; zPos += increment)
            {
                if (!PathManager.Instance.CheckIfValid(xPos, zPos))
                    continue;

                switch (colliderType)
                {
                    case ColliderType.Circle:
                        float xRange = xMax - xMin;
                        float zRange = zMax - zMin;
                        float xDistance = xPos * 2 - xMax - xMin + 1;
                        float zDistance = zPos * 2 - zMax - zMin + 1;
                        if (xDistance * xDistance + zDistance * zDistance < xRange * zRange)
                        {
                            Indexes.Add(PathManager.Instance.GetIndex(xPos, zPos));
                        }
                        break;
                    case ColliderType.Box:
                        // Transform point to local space of the rotated rectangle
                        Vector3 worldPoint = new Vector3(xPos, boxCenter.y, zPos);
                        Vector3 localPoint = Quaternion.Inverse(boxRotation) * (worldPoint - boxCenter);

                        // Check if the point lies within the rectangle's bounds in local space
                        if (Mathf.Abs(localPoint.x) <= boxSize.x / 2 && Mathf.Abs(localPoint.z) <= boxSize.z / 2)
                        {
                            Indexes.Add(PathManager.Instance.GetIndex(xPos, zPos));
                        }
                        break;
                    case ColliderType.Mesh:
                        MeshCollider meshCollider = (MeshCollider)indexCollider;
                        Vector3 point = new Vector3(xPos, meshCollider.bounds.center.y, zPos);
                        Vector3 closestPoint = meshCollider.ClosestPoint(point);

                        // Check if the closest point is effectively the same as the original point (inside the mesh)
                        if (Vector3.Distance(point, closestPoint) < 0.01f)
                        {
                            Indexes.Add(PathManager.Instance.GetIndex(xPos, zPos));
                        }
                        break;
                }
            }
        }

        if (Indexes.Count == 0)
        {
            Vector2 pos = indexCollider.bounds.center.XZ();
            if (PathManager.Instance.CheckIfValid(pos))
            {
                Indexes.Add(PathManager.Instance.GetIndex(pos));
            }
            else
            {
                //Debug.LogWarning($"Indexer {this} failed to build valid indexes", this);
            }
        }

        OnRebuilt?.Invoke();
    }

    private void OnDrawGizmosSelected()
    {
        if (PathManager.Instance == null)
        {
            return;
        }

        Gizmos.color = Color.green;
        for (int i = 0; i < Indexes.Count; i++)
        {
            Gizmos.DrawCube(PathManager.Instance.GetPos(Indexes[i]).ToXyZ(0), Vector3.one * 0.5f * PathManager.Instance.CellScale);
        }
    }
}
