using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Pathfinding
{
    public class LineVisualizer : PooledMonoBehaviour
    {
        [Title("Settings")]
        [SerializeField]
        private float lineThickness = 0.5f;
        
        [Title("Animation")]
        [SerializeField]
        private float totalDelay = 0.5f;

        private MeshRenderer meshRenderer;
        private MeshFilter meshFilter;
        private Mesh mesh;
        
        private readonly List<Vector3> vertices = new List<Vector3>();
        private readonly List<int> triangles = new List<int>();
        private readonly List<Vector2> uvs = new List<Vector2>();

        
        public PathIndex StartIndex { get; set; }
        
        private Vector3[] currentPoints;

        private void Awake()
        {
            meshRenderer = GetComponent<MeshRenderer>();
            meshFilter = GetComponent<MeshFilter>();
            
            mesh = new Mesh();
            meshFilter.sharedMesh = mesh;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            
            currentPoints = null;
            ClearMeshData();
        }

        public void DisplayLine(List<Vector3> points)
        {
            Vector3[] newPoints = points.ToArray();
            if (currentPoints != null && CheckEquality(currentPoints, newPoints))
            {
                return;
            }
            
            currentPoints = newPoints;
            UpdateLine(newPoints);
        }
        
        public void UpdateLine(Vector3[] points)
        {
            if (points.Length < 2) return;

            ClearMeshData();

            UpdateLineAsync(points).Forget();
        }

        private async UniTaskVoid UpdateLineAsync(Vector3[] points)
        {
            float delay = totalDelay / points.Length;
            for (int i = 0; i < points.Length - 1; i++)
            {
                AddLineSegment(points[i], points[i + 1]);
                UpdateMesh();

                await UniTask.Delay(TimeSpan.FromSeconds(delay));
            }
        }

        private void ClearMeshData()
        {
            vertices.Clear();
            triangles.Clear();
            uvs.Clear();
        }

        private void UpdateMesh()
        {
            mesh.Clear();
            mesh.vertices = vertices.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.uv = uvs.ToArray();
        
            // Calculate normals (all facing up)
            Vector3[] normals = new Vector3[vertices.Count];
            for (int i = 0; i < normals.Length; i++) normals[i] = Vector3.up;
            mesh.normals = normals;
        
            mesh.RecalculateBounds();
        }

        private void AddLineSegment(Vector3 start, Vector3 end)
        {
            Vector3 offset = (end - start);
            float distance = offset.magnitude;
            Vector3 direction = offset.normalized;
            Vector3 perpendicular = new Vector3(-direction.z, 0, direction.x) * (lineThickness * 0.5f);

            int vertexIndex = vertices.Count;

            // Add vertices (two per segment point)
            vertices.Add(start + perpendicular); // right
            vertices.Add(start - perpendicular); // left
            vertices.Add(end + perpendicular);  // right
            vertices.Add(end - perpendicular);  // left

            // Add triangles (two per quad)
            triangles.Add(vertexIndex);
            triangles.Add(vertexIndex + 1);
            triangles.Add(vertexIndex + 2);
        
            triangles.Add(vertexIndex + 1);
            triangles.Add(vertexIndex + 3);
            triangles.Add(vertexIndex + 2);

            // Add UVs (0,0 to 1,1 per quad)
            uvs.Add(new Vector2(1, distance)); // start right
            uvs.Add(new Vector2(0, distance)); // start left
            uvs.Add(new Vector2(1, 0)); // end right
            uvs.Add(new Vector2(0, 0)); // end left
        }

        private bool CheckEquality(Vector3[] oldPoints, Vector3[] newPoints)
        {
            if (oldPoints.Length != newPoints.Length)
            {
                return false;
            }

            for (int i = 0; i < oldPoints.Length; i++)
            {
                if ((oldPoints[i] - newPoints[i]).sqrMagnitude > 0.001f)
                {
                    return false;
                }
            }

            return true;
        }
    }
}