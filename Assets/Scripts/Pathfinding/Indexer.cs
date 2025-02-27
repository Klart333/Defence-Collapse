using System;
using System.Collections.Generic;
using UnityEngine;

namespace Pathfinding
{
    public class Indexer : MonoBehaviour
    {
        public event Action OnRebuilt;

        [SerializeField]
        private Collider indexCollider;

        [SerializeField]
        private bool getChildrenColliders;

        private readonly List<Collider> colliders = new List<Collider>();

        private bool needsRebuilding = true;

        public List<int> Indexes { get; } = new List<int>();

        private void OnValidate()
        {
            indexCollider ??= GetComponent<Collider>();
        }

        private void OnEnable()
        {
            indexCollider ??= GetComponent<Collider>();
            if (indexCollider != null)
            {
                colliders.Add(indexCollider);
            }

            if (getChildrenColliders)
            {
                colliders.AddRange(GetComponentsInChildren<Collider>(false));
            }
        }

        private void OnDisable()
        {
            colliders.Clear();
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

            for (int i = 0; i < colliders.Count; i++)
            {
                IndexerUtility.BuildColliderIndexes(colliders[i], Indexes);
            }

            if (Indexes.Count == 0)
            {
                Vector2 pos = transform.position;
                if (PathManager.Instance.CheckIfValid(pos))
                {
                    //Indexes.Add(PathManager.Instance.GetIndex(pos));
                }
                else
                {
                    Debug.LogWarning($"Indexer {this} failed to build valid indexes", this);
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

            Gizmos.color = Color.blue;
            for (int i = 0; i < Indexes.Count; i++)
            {
                Gizmos.DrawCube(PathManager.Instance.GetPos(Indexes[i]).ToXyZ(1), Vector3.one * 0.5f * PathManager.Instance.CellScale);
            }
        }
    }
    
    public enum ColliderType
    {
        None,
        Circle,
        Box,
        Mesh
    }
    
    public static class IndexerUtility
    {
        public static void BuildColliderIndexes(Collider collider, List<int> indexes)
        {
            float xMin = Mathf.Max(0, collider.bounds.min.x);
            float xMax = Mathf.Min(PathManager.Instance.GridWorldWidth, collider.bounds.max.x);
            float zMin = Mathf.Max(0, collider.bounds.min.z);
            float zMax = Mathf.Min(PathManager.Instance.GridWorldHeight, collider.bounds.max.z);

            ColliderType colliderType = collider switch
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
                BoxCollider boxCollider = (BoxCollider)collider;
                boxCenter = boxCollider.bounds.center;
                boxSize = boxCollider.size;
                boxRotation = boxCollider.transform.rotation;
            }

            for (float xPos = xMin; xPos <= xMax; xPos += increment)
            {
                for (float zPos = zMin; zPos <= zMax; zPos += increment)
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
                            if (xDistance * xDistance + zDistance * zDistance <= xRange * zRange)
                            {
                                indexes.Add(PathManager.Instance.GetIndex(xPos, zPos));
                            }

                            break;
                        case ColliderType.Box:
                            // Transform point to local space of the rotated rectangle
                            Vector3 worldPoint = new Vector3(xPos, boxCenter.y, zPos);
                            Vector3 localPoint = Quaternion.Inverse(boxRotation) * (worldPoint - boxCenter);

                            // Check if the point lies within the rectangle's bounds in local space
                            if (Mathf.Abs(localPoint.x) <= boxSize.x / 2 && Mathf.Abs(localPoint.z) <= boxSize.z / 2)
                            {
                                indexes.Add(PathManager.Instance.GetIndex(xPos, zPos));
                            }

                            break;
                        case ColliderType.Mesh:
                            MeshCollider meshCollider = (MeshCollider)collider;
                            Vector3 point = new Vector3(xPos, meshCollider.bounds.center.y, zPos);
                            Vector3 closestPoint = meshCollider.ClosestPoint(point);

                            // Check if the closest point is effectively the same as the original point (inside the mesh)
                            if (Vector3.Distance(point, closestPoint) < 0.01f)
                            {
                                indexes.Add(PathManager.Instance.GetIndex(xPos, zPos));
                            }

                            break;
                    }
                }
            }
        }
    }
}
