using Object = UnityEngine.Object;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using WaveFunctionCollapse;
using System.Linq;
using UnityEngine;
using Gameplay;
using Utility;
using System;
using NUnit.Framework;
using Unity.Collections;
using Unity.Mathematics;

namespace Buildings.District
{
    public class DistrictData : IDisposable
    {
        public event Action<DistrictData> OnClicked;
        public event Action OnDisposed;
        public event Action OnLevelup;

        protected readonly Dictionary<ChunkIndex, List<int3>> cachedChunkIndexes = new Dictionary<ChunkIndex, List<int3>>();

        private MeshCollider meshCollider;

        public Dictionary<int3, Chunk> DistrictChunks { get; } 
        public IGameSpeed GameSpeed { get; set; }
        public UpgradeData UpgradeData { get; }
        public ChunkIndex Index { get; set; }
        public DistrictState State { get; }
        public Vector3 Position { get; }
        
        private IChunkWaveFunction<Chunk> waveFunction { get; }

        public DistrictData(DistrictType districtType, HashSet<Chunk> chunks, Vector3 position, IChunkWaveFunction<Chunk> chunkWaveFunction, int key)
        {
            UpgradeData = new UpgradeData(1, 1, 1);
            DistrictChunks = new Dictionary<int3, Chunk>();
            foreach (Chunk chunk in chunks)
            {
                if (chunk.AdjacentChunks[2] == null)
                {
                    DistrictChunks.Add(chunk.ChunkIndex, chunk);
                }
            }
            
            State = districtType switch
            {
                DistrictType.TownHall => new TownHallState(this, DistrictUpgradeManager.Instance.TownHallData, position, key),
                DistrictType.Archer => new ArcherState(this, DistrictUpgradeManager.Instance.ArcherData, position, key),
                DistrictType.Bomb => new BombState(this, DistrictUpgradeManager.Instance.BombData, position, key),
                DistrictType.Mine => new MineState(this, DistrictUpgradeManager.Instance.MineData, position, key),
                //DistrictType.Church => expr,
                //DistrictType.Farm => expr,
                _ => throw new ArgumentOutOfRangeException(nameof(districtType), districtType, null)
            };

            waveFunction = chunkWaveFunction;
            Position = position;
            DistrictUtility.GenerateCollider(chunks, waveFunction, Position, InvokeOnClicked, ref meshCollider);
            CreateChunkIndexCache(chunks);

            Events.OnWaveStarted += OnWaveStarted;
            Events.OnWallsDestroyed += OnWallsDestroyed;
        }

        private void CreateChunkIndexCache(HashSet<Chunk> chunks)
        {
            foreach (Chunk chunk in chunks)
            {
                if (chunk.AdjacentChunks[2] != null)
                {
                    continue; // Remove if a state uses the below chunks in future
                }
                
                ChunkIndex? index = BuildingManager.Instance.GetIndex(chunk.Position + BuildingManager.Instance.GridScale / 2.0f);
                Assert.IsTrue(index.HasValue);
                if (!cachedChunkIndexes.TryGetValue(index.Value, out List<int3> chunkIndex))
                {
                    cachedChunkIndexes.Add(index.Value, new List<int3> { chunk.ChunkIndex });
                }
                else
                {
                    chunkIndex.Add(chunk.ChunkIndex);
                }
            }
        }

        private void OnWallsDestroyed(List<ChunkIndex> chunkIndexes)
        {
            HashSet<int3> destroyedIndexes = new HashSet<int3>();
            for (int i = 0; i < chunkIndexes.Count; i++)
            {
                if (!cachedChunkIndexes.TryGetValue(chunkIndexes[i], out List<int3> indexes)) continue;
            
                for (int j = indexes.Count - 1; j >= 0; j--)
                {
                    if (DistrictChunks.TryGetValue(indexes[j], out Chunk chunk))
                    {
                        destroyedIndexes.Add(chunk.ChunkIndex);
                    
                        DistrictChunks.Remove(indexes[j]);
                        indexes.RemoveAtSwapBack(j);
                    }
                    
                }

                if (indexes.Count == 0)
                {
                    cachedChunkIndexes.Remove(chunkIndexes[i]);
                }
            }

            if (DistrictChunks.Count <= 0)
            {
                Dispose();
            }
            else if (destroyedIndexes.Count > 0)
            {
                State.OnIndexesDestroyed(destroyedIndexes);
                DistrictUtility.GenerateCollider(DistrictChunks.Values, waveFunction, Position, InvokeOnClicked, ref meshCollider);
            }
        }
        
        public bool OnDistrictChunkRemoved(Chunk chunk)
        {
            if (!DistrictChunks.ContainsKey(chunk.ChunkIndex))
            {
                return false;
            }

            State.OnIndexesDestroyed(new HashSet<int3> { chunk.ChunkIndex });
            DistrictChunks.Remove(chunk.ChunkIndex);
            DistrictUtility.GenerateCollider(DistrictChunks.Values, waveFunction, Position, InvokeOnClicked, ref meshCollider);
            return true;
        }

        private void InvokeOnClicked()
        {
            if (CameraController.IsDragging 
                || UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject()
                || BuildingPlacer.Displaying || PathPlacer.Displaying || UIDistrictPlacementDisplay.Displaying)
            {
                return;
            }
            
            OnClicked?.Invoke(this);
            State.OnSelected(Position);

            InputManager.Instance.Cancel.performed += OnDeselected;
            UIEvents.OnFocusChanged += Deselect;
        }

        private void OnDeselected(InputAction.CallbackContext obj)
        {
            Deselect();
        }

        private void Deselect()
        {
            InputManager.Instance.Cancel.performed -= OnDeselected;
            UIEvents.OnFocusChanged -= Deselect;
            State.OnDeselected();
        }

        private void OnWaveStarted()
        {
            State.OnWaveStart();
        }

        public void LevelUp()
        {
            OnLevelup?.Invoke();
        }

        public void Update()
        {
            State.Update();
        }

        public void Dispose()
        {
            State.Die();
            
            Events.OnWaveStarted -= OnWaveStarted;
            if (meshCollider)
            {
                Object.Destroy(meshCollider.gameObject);
            }
            
            OnDisposed?.Invoke();
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
                    if (AllInvalid(chunk, x, y, z)) continue;

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

            if (!meshCollider)
            {
                meshCollider = new GameObject("District Collider").AddComponent<MeshCollider>();
                meshCollider.gameObject.AddComponent<ClickCallbackComponent>().OnClick += action;
            }

            meshCollider.transform.position = pos;
            meshCollider.sharedMesh = mesh;
            
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

            bool AllInvalid(Chunk chunk, int x, int y, int z)
            {
                bool all = true;
                foreach (short s in chunk.Cells[x, y, z].PossiblePrototypes[0].Keys)
                {
                    if (s != -1)
                    {
                        all = false;
                        break;
                    }
                }

                if (all)
                {
                    return true;
                }

                return false;
            }
        }

        public static List<Chunk> GetTopChunks(IEnumerable<Chunk> chunks)
        {
            List<Chunk> result = new List<Chunk>();
            foreach (Chunk chunk in chunks)
            {
                if (chunk.AdjacentChunks[2] == null)
                {
                    result.Add(chunk);
                }
            }
            
            return result;
        }
    }
}