using System.Collections.Generic;
using UnityEngine;
using System;

namespace Pathfinding
{
    public class Indexer : MonoBehaviour
    {
        public event Action OnRebuilt;

        [SerializeField]
        private Collider indexCollider;

        [SerializeField]
        private bool getChildrenColliders;

        [SerializeField]
        private bool indexColliderCenter;

        private readonly List<Collider> colliders = new List<Collider>();

        public bool NeedsRebuilding { get; set; }
        public int DelayFrames { get; set; }

        public List<PathIndex> Indexes { get; } = new List<PathIndex>();
        public List<Collider> Colliders => colliders;

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

        private void FixedUpdate()
        {
            if (DelayFrames > 0)
            {
                DelayFrames--;
                return;
            }
        
            if (transform.hasChanged)
            {
                NeedsRebuilding = true;
            }

            if (NeedsRebuilding)
            {
                BuildIndexes(); 
                
                NeedsRebuilding = false;
                transform.hasChanged = false;
            }
        }

        private void BuildIndexes()
        {
            Indexes.Clear();

            for (int i = 0; i < colliders.Count; i++)
            {
                if (indexColliderCenter)
                {
                    Vector2 pos = colliders[i].bounds.center.XZ();
                    PathIndex index = PathUtility.GetIndex(pos.x, pos.y);
                    Indexes.Add(index);
                }
                else
                {
                    IndexerUtility.BuildColliderIndexes(colliders[i], Indexes);
                }
            }

            if (Indexes.Count == 0)
            {
                Debug.LogWarning($"Indexer {this} failed to build valid indexes", this);

                /*Vector2 pos = transform.position.XZ();
                if (PathManager.Instance.CheckIfValid(pos))
                {
                    Indexes.Add(PathManager.Instance.GetIndex(pos));
                }*/
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
                Gizmos.DrawCube(PathUtility.GetPos(Indexes[i]), Vector3.one * 0.5f * PathUtility.CELL_SCALE);
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
        public static void BuildColliderIndexes(Collider collider, List<PathIndex> indexes)
        {
            float xMin = collider.bounds.min.x;
            float xMax = collider.bounds.max.x;
            float zMin = collider.bounds.min.z;
            float zMax = collider.bounds.max.z;

            ColliderType colliderType = collider switch
            {
                BoxCollider => ColliderType.Box,
                SphereCollider => ColliderType.Circle,
                MeshCollider => ColliderType.Mesh,
                _ => ColliderType.None
            };
            const float increment = PathUtility.CELL_SCALE;

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
                    switch (colliderType)
                    {
                        case ColliderType.Circle:
                            float xRange = xMax - xMin;
                            float zRange = zMax - zMin;
                            float xDistance = xPos * 2 - xMax - xMin + 1;
                            float zDistance = zPos * 2 - zMax - zMin + 1;
                            if (xDistance * xDistance + zDistance * zDistance <= xRange * zRange)
                            {
                                indexes.Add(PathUtility.GetIndex(xPos, zPos));
                            }

                            break;
                        case ColliderType.Box:
                            // Transform point to local space of the rotated rectangle
                            Vector3 worldPoint = new Vector3(xPos, boxCenter.y, zPos);
                            Vector3 localPoint = Quaternion.Inverse(boxRotation) * (worldPoint - boxCenter);

                            // Check if the point lies within the rectangle's bounds in local space
                            if (Mathf.Abs(localPoint.x) <= boxSize.x / 2 && Mathf.Abs(localPoint.z) <= boxSize.z / 2)
                            {
                                indexes.Add(PathUtility.GetIndex(xPos, zPos));
                            }

                            break;
                        case ColliderType.Mesh:
                            MeshCollider meshCollider = (MeshCollider)collider;
                            Vector3 point = new Vector3(xPos, meshCollider.bounds.center.y, zPos);
                            Vector3 closestPoint = meshCollider.ClosestPoint(point);

                            // Check if the closest point is effectively the same as the original point (inside the mesh)
                            if (Vector3.Distance(point, closestPoint) < 0.01f)
                            {
                                indexes.Add(PathUtility.GetIndex(xPos, zPos));
                            }

                            break;
                    }
                }
            }
        }
    }
}
