using Object = UnityEngine.Object;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using WaveFunctionCollapse;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Assertions;
using UnityEngine;
using Gameplay;
using Utility;
using System;
using System.Linq;
using InputCamera;

namespace Buildings.District
{
    public class DistrictData : IDisposable
    {
        public event Action<DistrictData> OnClicked;
        public event Action<HashSet<int3>> OnChunksLost;
        
        public event Action OnDisposed;
        public event Action OnLevelup;

        protected readonly Dictionary<ChunkIndex, List<int3>> cachedChunkIndexes = new Dictionary<ChunkIndex, List<int3>>();

        private MeshCollider meshCollider;

        public List<IUpgradeStat> UpgradeStats => State.UpgradeStats;
        public Dictionary<int3, QueryChunk> DistrictChunks { get; } 
        public IGameSpeed GameSpeed { get; set; }
        public ChunkIndex Index { get; set; }
        public DistrictState State { get; }
        public Vector3 Position { get; }
        
        private IChunkWaveFunction<QueryChunk> waveFunction { get; }

        public DistrictData(DistrictType districtType, HashSet<QueryChunk> chunks, Vector3 position, IChunkWaveFunction<QueryChunk> chunkWaveFunction, int key)
        {
            DistrictChunks = new Dictionary<int3, QueryChunk>();
            foreach (QueryChunk chunk in chunks)
            {
                if (chunk.IsTop)
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
                _ => throw new ArgumentOutOfRangeException(nameof(districtType), districtType, null)
            };

            waveFunction = chunkWaveFunction;
            Position = position;
            DistrictUtility.GenerateCollider(DistrictChunks.Values, waveFunction.ChunkScale, waveFunction.ChunkWaveFunction.CellSize, Position, InvokeOnClicked, ref meshCollider);
            CreateChunkIndexCache(chunks);

            Events.OnWaveStarted += OnWaveStarted;
            Events.OnWallsDestroyed += OnWallsDestroyed;
        }

        private void CreateChunkIndexCache(HashSet<QueryChunk> chunks)
        {
            cachedChunkIndexes.Clear();
            foreach (QueryChunk chunk in chunks)
            {
                if (!chunk.IsTop)
                {
                    continue; // Remove if a state uses the below chunks in future
                }
                
                ChunkIndex? index = BuildingManager.Instance.GetIndex(chunk.Position + BuildingManager.Instance.CellSize / 2.0f);
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

        public void ExpandDistrict(HashSet<QueryChunk> chunks) // To-do: Add callback to DistrictState in case of further merging
        {
            foreach (QueryChunk chunk in chunks)
            {
                if (chunk.IsTop)
                {
                    DistrictChunks.TryAdd(chunk.ChunkIndex, chunk);
                }
            }
            
            DistrictUtility.GenerateCollider(DistrictChunks.Values, waveFunction.ChunkScale, waveFunction.ChunkWaveFunction.CellSize, Position, InvokeOnClicked, ref meshCollider);
            CreateChunkIndexCache(chunks);

            if (State is EntityDistrictState entityState)
            {
                entityState.RemoveEntities();
                entityState.SpawnEntities();
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
                    if (!DistrictChunks.TryGetValue(indexes[j], out QueryChunk chunk)) continue;
                    
                    destroyedIndexes.Add(chunk.ChunkIndex);
                    DistrictChunks.Remove(indexes[j]);
                    indexes.RemoveAtSwapBack(j);
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
                OnChunksLost?.Invoke(destroyedIndexes);
                State.OnIndexesDestroyed(destroyedIndexes);
                DistrictUtility.GenerateCollider(DistrictChunks.Values, waveFunction.ChunkScale, waveFunction.ChunkWaveFunction.CellSize, Position, InvokeOnClicked, ref meshCollider);
            }
        }
        
        public bool OnDistrictChunkRemoved(IChunk chunk)
        {
            if (!DistrictChunks.ContainsKey(chunk.ChunkIndex))
            {
                return false;
            }

            HashSet<int3> destroyedIndexes = new HashSet<int3> { chunk.ChunkIndex };
            State.OnIndexesDestroyed(destroyedIndexes);
            OnChunksLost?.Invoke(destroyedIndexes);

            DistrictChunks.Remove(chunk.ChunkIndex);
            DistrictUtility.GenerateCollider(DistrictChunks.Values, waveFunction.ChunkScale, waveFunction.ChunkWaveFunction.CellSize, Position, InvokeOnClicked, ref meshCollider);
            return true;
        }

        private void InvokeOnClicked()
        {
            if (CameraController.IsDragging 
                || UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject()
                || BuildingPlacer.Displaying || PathPlacer.Displaying || DistrictPlacer.Placing)
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
        public static void GenerateCollider(IEnumerable<IChunk> chunks, Vector3 ChunkScale, Vector3 CellSize, Vector3 pos, Action action, ref MeshCollider meshCollider)
        {
            List<Vector3> vertices = new List<Vector3>();
            List<int> triangles = new List<int>();
            Dictionary<Vector3, int> vertexIndices = new Dictionary<Vector3, int>();

            float chunkWidth = ChunkScale.x;
            float chunkHeight = ChunkScale.y;
            float chunkDepth = ChunkScale.z;
            
            foreach (IChunk chunk in chunks)
            {
                if (chunk.AdjacentChunks.All(x => x != null && x.PrototypeInfoData == chunk.PrototypeInfoData))
                {
                    continue;
                }
                
                // Assuming each chunk has a position and contains 2x2x2 cells
                Vector3 chunkPosition = chunk.Position - pos - CellSize / 2.0f; 

                // Define the 8 corners of the 3D cell
                Vector3 bottomLeftFront = new Vector3(chunkPosition.x, chunkPosition.y, chunkPosition.z);
                Vector3 bottomRightFront = new Vector3(chunkPosition.x + chunkWidth, chunkPosition.y, chunkPosition.z);
                Vector3 topRightFront = new Vector3(chunkPosition.x + chunkWidth, chunkPosition.y + chunkHeight, chunkPosition.z);
                Vector3 topLeftFront = new Vector3(chunkPosition.x, chunkPosition.y + chunkHeight, chunkPosition.z);
                Vector3 bottomLeftBack = new Vector3(chunkPosition.x, chunkPosition.y, chunkPosition.z + chunkDepth);
                Vector3 bottomRightBack = new Vector3(chunkPosition.x + chunkWidth, chunkPosition.y, chunkPosition.z + chunkDepth);
                Vector3 topRightBack = new Vector3(chunkPosition.x + chunkWidth, chunkPosition.y + chunkHeight, chunkPosition.z + chunkDepth);
                Vector3 topLeftBack = new Vector3(chunkPosition.x, chunkPosition.y + chunkHeight, chunkPosition.z + chunkDepth);

                // Add vertices if they don't already exist
                int bottomLeftFrontIndex = AddVertex(bottomLeftFront);
                int bottomRightFrontIndex = AddVertex(bottomRightFront);
                int topRightFrontIndex = AddVertex(topRightFront);
                int topLeftFrontIndex = AddVertex(topLeftFront);
                int bottomLeftBackIndex = AddVertex(bottomLeftBack);
                int bottomRightBackIndex = AddVertex(bottomRightBack);
                int topRightBackIndex = AddVertex(topRightBack);
                int topLeftBackIndex = AddVertex(topLeftBack);

                // Generate triangles for all 6 faces of the 3D cell with consistent winding order
                // Bottom face
                AddQuad(bottomLeftFrontIndex, bottomRightFrontIndex, bottomLeftBackIndex, bottomRightBackIndex);
                // Front face
                AddQuad(bottomLeftFrontIndex,topLeftFrontIndex, bottomRightFrontIndex, topRightFrontIndex );
                // Back face
                AddQuad(bottomRightBackIndex, topRightBackIndex, bottomLeftBackIndex, topLeftBackIndex);
                // Top face
                AddQuad(topLeftFrontIndex, topLeftBackIndex, topRightFrontIndex, topRightBackIndex);
                // Left face
                AddQuad(bottomLeftBackIndex, topLeftBackIndex, bottomLeftFrontIndex, topLeftFrontIndex);
                // Right face
                AddQuad(bottomRightFrontIndex, topRightFrontIndex, bottomRightBackIndex, topRightBackIndex);
            }
            
            Mesh mesh = new Mesh
            {
                vertices = vertices.ToArray(),
                triangles = triangles.ToArray()
            };
            mesh.RecalculateNormals(); // Make sure to recalculate normals
            mesh.RecalculateBounds();

            if (!meshCollider)
            {
                meshCollider = new GameObject("District Collider").AddComponent<MeshCollider>();
                meshCollider.gameObject.AddComponent<ClickCallbackComponent>().OnClick += action;
                //meshCollider.gameObject.AddComponent<MeshRenderer>().sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                //meshCollider.gameObject.AddComponent<MeshFilter>();
            }

            //meshCollider.gameObject.GetComponent<MeshFilter>().sharedMesh = mesh;
            meshCollider.transform.position = pos;
            meshCollider.sharedMesh = mesh;
            
            return;
            
            // Helper function to add a vertex if it doesn't already exist
            int AddVertex(Vector3 vertex)
            {
                if (vertexIndices.TryGetValue(vertex, out int index))
                {
                    return index;
                }

                vertices.Add(vertex);
                vertexIndices[vertex] = vertices.Count - 1;
                return vertices.Count - 1;
            }

            // Helper function to add a quad (two triangles) to the triangles list
            void AddQuad(int a, int b, int c, int d)
            {
                // First triangle (counter-clockwise order)
                triangles.Add(a);
                triangles.Add(b);
                triangles.Add(c);

                // Second triangle (counter-clockwise order)
                triangles.Add(c);
                triangles.Add(b);
                triangles.Add(d);
            }
        }
    }
}