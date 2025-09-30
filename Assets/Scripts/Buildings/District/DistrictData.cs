using Object = UnityEngine.Object;

using System.Collections.Generic;
using UnityEngine.InputSystem;
using Buildings.District.ECS;
using WaveFunctionCollapse;
using Unity.Collections;
using Unity.Mathematics;
using Gameplay.Buffs;
using Gameplay.Event;
using UnityEngine;
using InputCamera;
using Gameplay;
using Effects;
using Utility;
using System;

namespace Buildings.District
{
    [Serializable]
    public class DistrictData : IDisposable, IBuffable
    {
        public event Action<HashSet<int3>> OnChunksLost;
        public event Action<DistrictData> OnClicked;
        public event Action OnDisposed;
        public event Action OnLevelup;

        protected Dictionary<ChunkIndex, List<int3>> cachedBuildingChunkIndexes = new Dictionary<ChunkIndex, List<int3>>();

        private MeshCollider meshCollider;
        protected TowerData towerData;
        
        private float hoveredTimer;
        private bool hovered;

        public Dictionary<int3, QueryChunk> DistrictChunks { get; } 
        public DistrictHandler DistrictHandler { get; set; }
        public DistrictGenerator DistrictGenerator { get; }
        public IGameSpeed GameSpeed { get; set; }
        public DistrictState State { get; set; }
        public Vector3 Position { get; }
        
        public List<IUpgradeStat> UpgradeStats => State.UpgradeStats;
        public TowerData TowerData => towerData;
        public Stats Stats => State.Stats;

        public DistrictData(TowerData towerData, HashSet<QueryChunk> chunks, Vector3 position, 
            IChunkWaveFunction<QueryChunk> chunkDistrictGenerator, int key)
        {
            DistrictChunks = new Dictionary<int3, QueryChunk>();
            foreach (QueryChunk chunk in chunks)
            {
                DistrictChunks.Add(chunk.ChunkIndex, chunk);
            }

            DistrictGenerator = chunkDistrictGenerator as DistrictGenerator;
            Position = position;
            GenerateCollider();
            
            this.towerData = towerData;
            State = towerData.GetDistrictState(this, position, key);
            
            Events.OnTurnComplete += OnTurnComplete;
            Events.OnGameReset += Dispose;
        }

        public void Update()
        {
            if (!hovered || FocusManager.Instance.GetIsFocused()) return;
            
            hoveredTimer += Time.deltaTime;
            if (hoveredTimer >= 0.5f)
            {
                hoveredTimer = float.MinValue;
                DistrictHandler.DisplayDistrictDisplay(this);
            }
        }
        
        private void OnTurnComplete()
        {
            if (State is ITurnCompleteSubscriber turnComplete)
            {
                turnComplete.TurnComplete();
            }
        }

        private void GenerateCollider()
        {
            ClickCallbackComponent clickCallback = DistrictUtility.GenerateCollider(DistrictChunks.Values, DistrictGenerator.ChunkScale, DistrictGenerator.ChunkWaveFunction.CellSize, Position, ref meshCollider);
            if (!clickCallback) return;
            
            clickCallback.OnClick += InvokeOnClicked;
            clickCallback.OnHoverEnter += OnHoverEnter;
            clickCallback.OnHoverExit += OnHoverExit;
        }

        public void ExpandDistrict(HashSet<QueryChunk> chunks)
        {
            foreach (QueryChunk chunk in chunks)
            {
                DistrictChunks.TryAdd(chunk.ChunkIndex, chunk);
            }
            
            GenerateCollider();
        }

        /// <summary>
        /// Called from OnBuiltIndexDestroyed (and the district generator rebuilding the district based on the shape of the walls)
        /// </summary>
        public bool RemoveDistrictChunk(ICollection<QueryChunk> chunks)
        {
            HashSet<int3> destroyedIndexes = new HashSet<int3>();

            foreach (IChunk chunk in chunks)
            {
                if (!DistrictChunks.ContainsKey(chunk.ChunkIndex)) 
                {
#if UNITY_EDITOR
                    if (DistrictHandler.IsDebug)
                    {
                        Debug.Log($"({this.towerData.DistrictType}) DistrictChunks did not contain: {chunk.ChunkIndex}");
                    }
#endif
                    continue;
                }
                
                destroyedIndexes.Add(chunk.ChunkIndex);
                DistrictChunks.Remove(chunk.ChunkIndex);
            }
            
            bool isDead = DistrictChunks.Count <= 0;
            if (destroyedIndexes.Count > 0)
            {
                OnChunksLost?.Invoke(destroyedIndexes);
                
                if (!isDead)
                {
                    GenerateCollider();
                    State.RemoveEntities(destroyedIndexes);
                }
            }

            if (isDead)
            {
                Dispose();
            }
            
            return true;
        }

        private void OnHoverEnter()
        {
            if (FocusManager.Instance.GetIsFocused())
            {
                return;
            }
            
            hovered = true;
            hoveredTimer = 0.0f;
            DistrictHandler.SetHoverOnObjects(DistrictChunks.Values, true);
        }

        private void OnHoverExit()
        {
            hovered = false;
            DistrictHandler.SetHoverOnObjects(DistrictChunks.Values, false);
        }

        private void InvokeOnClicked()
        {
            if (CameraController.IsDragging 
                || UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject()
                || FocusManager.Instance.GetIsFocused())
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

        public void LevelUp()
        {
            OnLevelup?.Invoke();
        }

        public void PerformAttack(DistrictEntityDataComponent dataComponent)
        {
            State.OriginPosition = dataComponent.OriginPosition;
            State.PerformAttack(dataComponent.TargetPosition);
        }

        public void ChangeState(TowerData upgradeStateData)
        {
            int key = State.Key;
            UIEvents.OnFocusChanged?.Invoke();
            State.Dispose();
            
            State = upgradeStateData.GetDistrictState(this, Position, key);
            // Add chunks if needed
            DistrictGenerator.AddAction(() => DistrictGenerator.RegenerateChunks(DistrictChunks.Values, _ => upgradeStateData.PrototypeInfoData));
        }
        
        public void Dispose()
        {
            State.Dispose();
            
            InputManager.Instance.Cancel.performed -= OnDeselected;
            UIEvents.OnFocusChanged -= Deselect;
            Events.OnTurnComplete -= OnTurnComplete;
            Events.OnGameReset -= Dispose;
            
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
                if (IsInvalid(chunk))
                {
                    continue;
                }
                
                // Assuming each chunk has a position and contains 2x2x2 cells
                Vector3 chunkPosition = chunk.Position - pos; 

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

            bool IsInvalid(IChunk chunk)
            {
                foreach (IChunk x in chunk.AdjacentChunks)
                {
                    if (x == null || x.PrototypeInfoData != chunk.PrototypeInfoData)
                    {
                        return false;
                    }
                }

                return true;
            }
        }
    }
}