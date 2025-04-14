using Object = UnityEngine.Object;
using System.Collections.Generic;
using WaveFunctionCollapse;
using Unity.Mathematics;
using System.Linq;
using UnityEngine;
using Utility;
using System;
using UnityEngine.InputSystem;

namespace Buildings.District
{
    public class DistrictData
    {
        public event Action<DistrictData> OnClicked;
        public event Action OnLevelup;

        private readonly MeshCollider meshCollider;
        
        private readonly int cellCount;
        
        public UpgradeData UpgradeData { get; private set; }
        public HashSet<Chunk> DistrictChunks { get; set; } 
        public DistrictState State { get; }
        public Vector3 Position { get; }
        
        public ChunkIndex Index { get; set; }

        public DistrictData(DistrictType districtType, HashSet<Chunk> chunks, Vector3 position, IChunkWaveFunction<Chunk> chunkWaveFunction, int key)
        {
            UpgradeData = new UpgradeData(1, 1, 1);
            cellCount = chunks.Count;
            DistrictChunks = chunks;
            
            State = districtType switch
            {
                DistrictType.Archer => new ArcherState(this, DistrictUpgradeManager.Instance.ArcherData, chunks, position, key),
                DistrictType.Bomb => new BombState(this, DistrictUpgradeManager.Instance.BombData, chunks, position, key),
                DistrictType.Mine => new MineState(this, DistrictUpgradeManager.Instance.MineData, chunks, position, key),
                //DistrictType.Church => expr,
                //DistrictType.Farm => expr,
                _ => throw new ArgumentOutOfRangeException(nameof(districtType), districtType, null)
            };

            Position = position;
            DistrictUtility.GenerateCollider(chunks, chunkWaveFunction, Position, InvokeOnClicked, ref meshCollider);

            Events.OnWaveStarted += OnWaveStarted;
            State.OnStateEntered();
        }

        public void Destroy()
        {
            Events.OnWaveStarted -= OnWaveStarted;
            if (meshCollider != null)
            {
                Object.Destroy(meshCollider);
            }
        }

        private void InvokeOnClicked()
        {
            if (CameraController.IsDragging || UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }
            
            OnClicked?.Invoke(this);
            State.OnSelected(Position);

            InputManager.Instance.Cancel.performed += OnDeselected;
        }

        private void OnDeselected(InputAction.CallbackContext obj)
        {
            InputManager.Instance.Cancel.performed -= OnDeselected;
            State.OnDeselected();
        }


        private void OnWaveStarted()
        {
            State.OnWaveStart(cellCount);
        }

        public void LevelUp()
        {
            OnLevelup?.Invoke();
        }

        public void Update()
        {
            State.Update();
        }
    
    }

    public static class DistrictUtility
    {
        public static void GenerateCollider(IEnumerable<Chunk> chunks, IChunkWaveFunction<Chunk> chunkWaveFunction, Vector3 pos, Action action, ref MeshCollider meshCollider)
        {
            List<Vector3> vertices = new List<Vector3>();
            List<int> triangles = new List<int>();
            Dictionary<Vector3, int> vertexIndices = new Dictionary<Vector3, int>();
            Vector3 offset = new Vector3(chunkWaveFunction.ChunkWaveFunction.GridScale.x, 0, chunkWaveFunction.ChunkWaveFunction.GridScale.z) / -2.0f;

            float chunkWidth = chunkWaveFunction.ChunkScale.x / 2;
            float chunkHeight = chunkWaveFunction.ChunkScale.y / 2;
            float chunkDepth = chunkWaveFunction.ChunkScale.z / 2;
            
            foreach (Chunk chunk in chunks)
            {
                // Assuming each chunk has a position and contains 2x2x2 cells
                Vector3 chunkPosition = chunk.Position - pos;
                for (int x = 0; x < 2; x++)
                for (int y = 0; y < 2; y++)
                for (int z = 0; z < 2; z++)
                {
                    if (chunk.Cells[x, y, z].PossiblePrototypes[0].Keys.All(s => s == -1))
                    {
                        continue;
                    }

                    // Calculate cell's bottom-left-front corner position
                    Vector3 cellPosition = chunkPosition + new Vector3(x, y, z).MultiplyByAxis(chunkWaveFunction.ChunkScale / 2) + offset;

                    // Define the 8 corners of the 3D cell
                    Vector3 bottomLeftFront = new Vector3(cellPosition.x, cellPosition.y, cellPosition.z);
                    Vector3 bottomRightFront = new Vector3(cellPosition.x + chunkWidth, cellPosition.y, cellPosition.z);
                    Vector3 topRightFront = new Vector3(cellPosition.x + chunkWidth, cellPosition.y + chunkHeight, cellPosition.z);
                    Vector3 topLeftFront = new Vector3(cellPosition.x, cellPosition.y + chunkHeight, cellPosition.z);
                    Vector3 bottomLeftBack = new Vector3(cellPosition.x, cellPosition.y, cellPosition.z + chunkDepth);
                    Vector3 bottomRightBack = new Vector3(cellPosition.x + chunkWidth, cellPosition.y, cellPosition.z + chunkDepth);
                    Vector3 topRightBack = new Vector3(cellPosition.x + chunkWidth, cellPosition.y + chunkHeight, cellPosition.z + chunkDepth);
                    Vector3 topLeftBack = new Vector3(cellPosition.x, cellPosition.y + chunkHeight, cellPosition.z + chunkDepth);

                    // Add vertices if they don't already exist
                    int bottomLeftFrontIndex = AddVertex(bottomLeftFront);
                    int bottomRightFrontIndex = AddVertex(bottomRightFront);
                    int topRightFrontIndex = AddVertex(topRightFront);
                    int topLeftFrontIndex = AddVertex(topLeftFront);
                    int bottomLeftBackIndex = AddVertex(bottomLeftBack);
                    int bottomRightBackIndex = AddVertex(bottomRightBack);
                    int topRightBackIndex = AddVertex(topRightBack);
                    int topLeftBackIndex = AddVertex(topLeftBack);

                    // Generate triangles for all 6 faces of the 3D cell
                    AddQuad(bottomLeftFrontIndex, bottomRightFrontIndex, topRightFrontIndex, topLeftFrontIndex);
                    AddQuad(bottomRightBackIndex, bottomLeftBackIndex, topLeftBackIndex, topRightBackIndex);
                    AddQuad(topLeftFrontIndex, topRightFrontIndex, topRightBackIndex, topLeftBackIndex);
                    AddQuad(bottomRightFrontIndex, bottomLeftFrontIndex, bottomLeftBackIndex, bottomRightBackIndex);
                    AddQuad(bottomLeftFrontIndex, bottomLeftBackIndex, topLeftBackIndex, topLeftFrontIndex);
                    AddQuad(bottomRightBackIndex, bottomRightFrontIndex, topRightFrontIndex, topRightBackIndex);
                }
            }
            Mesh mesh = new Mesh
            {
                vertices = vertices.ToArray(),
                triangles = triangles.ToArray()
            };
            mesh.RecalculateBounds();

            meshCollider = new GameObject("District Collider").AddComponent<MeshCollider>();
            meshCollider.transform.position = pos;
            meshCollider.sharedMesh = mesh;
            
            meshCollider.gameObject.AddComponent<ClickCallbackComponent>().OnClick += action;
            return;
            
            // Helper function to add a vertex if it doesn't already exist
            int AddVertex(Vector3 vertex)
            {
                if (vertexIndices.TryGetValue(vertex, out int index))
                {
                    return index; // Return the existing index if the vertex is already in the list
                }

                vertices.Add(vertex); // Add the vertex to the list
                vertexIndices[vertex] = vertices.Count - 1; // Store its index
                return vertices.Count - 1; // Return the new index
            }

            // Helper function to add a quad (two triangles) to the triangles list
            void AddQuad(int a, int b, int c, int d)
            {
                // First triangle
                triangles.Add(a);
                triangles.Add(b);
                triangles.Add(c);

                // Second triangle
                triangles.Add(a);
                triangles.Add(c);
                triangles.Add(d);
            }
        }

        /// <summary>
        /// Get Perimeter of chunks
        /// </summary>
        public static List<Chunk> GetTopPerimeter(IEnumerable<Chunk> chunks)
        {
            List<Chunk> convexHull = new List<Chunk>();

            foreach (Chunk chunk in chunks)
            {
                if (chunk.AdjacentChunks[2] != null) continue; // Not-top chunks shouldn't shoot
                
                int adjacentChunks = 0;
                for (int i = 0; i < 4; i++)
                {
                    int index = i > 1 ? i + 2 : i;
                    if (chunk.AdjacentChunks[index] != null)
                    {
                        adjacentChunks++;
                    }
                }
                
                if (adjacentChunks < 4)
                {
                    convexHull.Add(chunk);
                }
            }

            return convexHull;
        }
    }
}