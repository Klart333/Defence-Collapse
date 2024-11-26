using System;
using System.Collections.Generic;
using UnityEngine;

public class Indexer : MonoBehaviour
{
    public event Action OnRebuilt;

    [SerializeField]
    private Collider indexCollider;

    private List<int> indexes = new List<int>();

    private bool needsRebuilding = true;

    public List<int> Indexes => indexes;

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
        indexes.Clear();
        float xMin = Mathf.Max(0, indexCollider.bounds.min.x);
        float xMax = Mathf.Min(PathManager.Instance.GridWidth, indexCollider.bounds.max.x);
        float zMin = Mathf.Max(0, indexCollider.bounds.min.z);
        float zMax = Mathf.Min(PathManager.Instance.GridHeight, indexCollider.bounds.max.z);

        bool isSphere = indexCollider is SphereCollider;
        float increment = PathManager.Instance.CellScale;
        for (float xPos = xMin; xPos < xMax; xPos+= increment)
        {
            for (float zPos = zMin; zPos < zMax; zPos += increment)
            {
                if (!PathManager.Instance.CheckIfValid(xPos, zPos))
                    continue;

                if (isSphere)
                {
                    float xRange = xMax - xMin;
                    float zRange = zMax - zMin;
                    float xDistance = xPos * 2 - xMax - xMin + 1;
                    float zDistance = zPos * 2 - zMax - zMin + 1;
                    if (xDistance * xDistance + zDistance * zDistance < xRange * zRange)
                    {
                        indexes.Add(PathManager.Instance.GetIndex(xPos, zPos));
                    }
                }
                //else if (GetComponent<Collider>().OverlapPoint(new Vector2(xPos, zPos)))
                //{
                //    indexes.Add(xPos + zPos * PathManager.Instance.GridWidth);
                //}
            }
        }

        if (indexes.Count == 0)
        {
            Vector2 pos = indexCollider.bounds.center.XZ();
            if (PathManager.Instance.CheckIfValid(pos))
            {
                indexes.Add(PathManager.Instance.GetIndex(pos));
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

        Gizmos.color = Color.green;
        for (int i = 0; i < indexes.Count; i++)
        {
            Gizmos.DrawCube(PathManager.Instance.GetPos(indexes[i]).ToXyZ(), Vector3.one * 0.5f * PathManager.Instance.CellScale);
        }
    }
}
