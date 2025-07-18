using Object = UnityEngine.Object;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using Cysharp.Threading.Tasks;
using WaveFunctionCollapse;
using Unity.Mathematics;
using Unity.Collections;
using UnityEngine;
using System.Linq;
using InputCamera;
using Gameplay;
using Utility;
using System;
using Gameplay.Buffs;

namespace Buildings.District
{
    [Serializable]
    public class DistrictData : IDisposable, IBuffable
    {
        public event Action<DistrictData> OnClicked;
        public event Action<HashSet<int3>> OnChunksLost;
        
        public event Action OnDisposed;
        public event Action OnLevelup;

        protected readonly Dictionary<ChunkIndex, List<int3>> cachedChunkIndexes = new Dictionary<ChunkIndex, List<int3>>();

        private MeshCollider meshCollider;
        protected TowerData towerData;

        public List<IUpgradeStat> UpgradeStats => State.UpgradeStats;
        public Dictionary<int3, QueryChunk> DistrictChunks { get; } 
        public DistrictHandler DistrictHandler { get; set; }
        public TowerData TowerData => towerData;
        public IGameSpeed GameSpeed { get; set; }
        public DistrictState State { get; }
        public Stats Stats => State.Stats;
        public Vector3 Position { get; }
        
        public IChunkWaveFunction<QueryChunk> DistrictGenerator { get; }

        public DistrictData(TowerData towerData, HashSet<QueryChunk> chunks, Vector3 position, IChunkWaveFunction<QueryChunk> chunkDistrictGenerator, int key)
        {
            DistrictChunks = new Dictionary<int3, QueryChunk>();
            foreach (QueryChunk chunk in chunks)
            {
                if (chunk.IsTop) // Remove if a state uses the below chunks in future
                {
                    DistrictChunks.Add(chunk.ChunkIndex, chunk);
                }
            }

            DistrictGenerator = chunkDistrictGenerator;
            Position = position;
            GenerateCollider();
            CreateChunkIndexCache();
            this.towerData = towerData;
            
            State = towerData.DistrictType switch
            {
                DistrictType.TownHall => new TownHallState(this, towerData, position, key),
                DistrictType.Archer => new ArcherState(this, towerData, position, key),
                DistrictType.Bomb => new BombState(this, towerData, position, key),
                DistrictType.Mine => new MineState(this, towerData, position, key),
                DistrictType.Flame => new FlameState(this, towerData, position, key),
                DistrictType.Lightning => new LightningState(this, towerData, position, key),
                DistrictType.Church => new ChurchState(this, towerData, position, key),
                DistrictType.Barracks => new BarracksState(this, towerData, position, key),
                _ => throw new ArgumentOutOfRangeException(nameof(towerData.DistrictType), towerData.DistrictType, null)
            };

            Events.OnWaveStarted += OnWaveStarted;
            Events.OnWaveEnded += OnWaveEnded;
            Events.OnWallsDestroyed += OnWallsDestroyed;
        }

        private void GenerateCollider()
        {
            ClickCallbackComponent clickCallback = DistrictUtility.GenerateCollider(DistrictChunks.Values, DistrictGenerator.ChunkScale, DistrictGenerator.ChunkWaveFunction.CellSize, Position, ref meshCollider);
            if (!clickCallback) return;
            
            clickCallback.OnClick += InvokeOnClicked;
            clickCallback.OnHoverEnter += OnHoverEnter;
            clickCallback.OnHoverExit += OnHoverExit;
        }

        private void CreateChunkIndexCache()
        {
            cachedChunkIndexes.Clear();
            foreach (QueryChunk chunk in DistrictChunks.Values)
            {
                ChunkIndex? index = BuildingManager.Instance.GetIndex(chunk.Position);
                Debug.Assert(index != null, nameof(index) + " != null");
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

        public void ExpandDistrict(HashSet<QueryChunk> chunks) // To-do: Add callback to DistrictState
        {
            foreach (QueryChunk chunk in chunks)
            {
                if (chunk.IsTop)
                {
                    DistrictChunks.TryAdd(chunk.ChunkIndex, chunk);
                }
            }
            
            GenerateCollider();
            CreateChunkIndexCache();

            State.RemoveEntities();
            State.SpawnEntities();
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

            bool isDead = DistrictChunks.Count <= 0;
            if (destroyedIndexes.Count > 0)
            {
                OnChunksLost?.Invoke(destroyedIndexes);
                
                //State.OnIndexesDestroyed(destroyedIndexes);
                if (!isDead)
                {
                    GenerateCollider();
                    DelayedUpdateEntities().Forget();
                }
            }

            if (isDead)
            {
                Dispose();
            }
        }

        private async UniTaskVoid DelayedUpdateEntities()
        {
            await UniTask.WaitWhile(() => DistrictGenerator.IsGenerating);
            State.RemoveEntities();
            State.SpawnEntities();
        }

        public bool OnDistrictChunkRemoved(IChunk chunk)
        {
            if (!DistrictChunks.ContainsKey(chunk.ChunkIndex))
            {
                return false;
            }

            HashSet<int3> destroyedIndexes = new HashSet<int3> { chunk.ChunkIndex };
            OnChunksLost?.Invoke(destroyedIndexes);
            
            DistrictChunks.Remove(chunk.ChunkIndex);
            GenerateCollider();
            
            DelayedUpdateEntities().Forget();
            return true;
        }

        private void OnHoverEnter()
        {
            if (DistrictPlacer.Placing || BarricadePlacer.Displaying || BuildingPlacer.Displaying)
            {
                return;
            }
            
            DistrictHandler.SetHoverOnObjects(DistrictChunks.Values, true);
        }

        private void OnHoverExit()
        {
            DistrictHandler.SetHoverOnObjects(DistrictChunks.Values, false);
        }

        private void InvokeOnClicked()
        {
            if (CameraController.IsDragging 
                || UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject()
                || BarricadePlacer.Displaying || BuildingPlacer.Displaying || DistrictPlacer.Placing)
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

        private void OnWaveEnded()
        {
            State.OnWaveEnd();
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
            State.Dispose();
            
            InputManager.Instance.Cancel.performed -= OnDeselected;
            Events.OnWallsDestroyed -= OnWallsDestroyed;
            UIEvents.OnFocusChanged -= Deselect;
            Events.OnWaveStarted -= OnWaveStarted;
            Events.OnWaveEnded -= OnWaveEnded;
            
            if (meshCollider)
            {
                Object.Destroy(meshCollider.gameObject);
            }
            
            OnDisposed?.Invoke();
        }
    }

    public static class DistrictUtility
    {
        public static ClickCallbackComponent GenerateCollider(IEnumerable<IChunk> chunks, Vector3 ChunkScale, Vector3 CellSize, Vector3 pos, ref MeshCollider meshCollider)
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

            ClickCallbackComponent clickCallbackComponent = null;
            if (!meshCollider)
            {
                meshCollider = new GameObject("District Collider").AddComponent<MeshCollider>();
                clickCallbackComponent = meshCollider.gameObject.AddComponent<ClickCallbackComponent>();
                //meshCollider.gameObject.AddComponent<MeshRenderer>().sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                //meshCollider.gameObject.AddComponent<MeshFilter>();
            }

            //meshCollider.gameObject.GetComponent<MeshFilter>().sharedMesh = mesh;
            meshCollider.transform.position = pos;
            meshCollider.sharedMesh = mesh;
            
            return clickCallbackComponent;
            
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