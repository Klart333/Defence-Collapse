using FocusType = Utility.FocusType;

using System.Collections.Generic;
using UnityEngine.InputSystem;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using WaveFunctionCollapse;
using Unity.Mathematics;
using Gameplay.Event;
using InputCamera;
using UnityEngine;
using Utility;
using System;
using Juice;

namespace Buildings
{
    public class EdgeBuilder : MonoBehaviour
    {
        public event Action<ChunkIndexEdge, TileAction> OnEdgePressed;
        public event Func<ChunkIndexEdge, TileAction> OnEdgeEntered;
        public event Action OnCancelPlacement;
        
        [Title("References")]
        [SerializeField]
        private SelectedEdgeHandler selectedEdgeHandler;
        
        [SerializeField]
        private GroundGenerator groundGenerator;

        private BuildingManager buildingManager;
        private InputManager inputManager;
        private FocusManager focusManager;
        private Focus focus;
        
        private ChunkIndexEdge? selectedEdge;
        private ChunkIndexEdge? pressedEdge;

        private TileAction pendingAction;
        private EdgeBuildingType currentType;
        
        private bool displaying;
        
        public Dictionary<ChunkIndexEdge, EdgeBuildingType> Edges { get; } = new Dictionary<ChunkIndexEdge, EdgeBuildingType>();

        private void OnEnable()
        {
            GetInput().Forget();
            GetBuilding().Forget();
            GetFocus().Forget();
            
            focus = new Focus
            {
                OnFocusExit = Hide,
                FocusType = FocusType.Placing,
                ChangeType = FocusChangeType.Unique,
            };
            
            Events.OnBuiltEdgeDestroyed += OnBuiltEdgeDestroyed;
        }
        
        private async UniTaskVoid GetInput()
        {
            inputManager = await InputManager.Get();
            inputManager.Fire.started += MouseOnDown;
            inputManager.Fire.canceled += MouseOnUp;
            inputManager.Cancel.performed += CancelPerformed;
        }

        private async UniTaskVoid GetBuilding()
        {
            buildingManager = await BuildingManager.Get();
            buildingManager.OnLoaded += InitializeSpawnPlaces;
        } 
        
        private async UniTaskVoid GetFocus()
        {
            focusManager = await FocusManager.Get();
        } 
        
        private void OnDisable()
        {
            Events.OnBuiltEdgeDestroyed -= OnBuiltEdgeDestroyed;
            
            buildingManager.OnLoaded -= InitializeSpawnPlaces;
            inputManager.Cancel.performed -= CancelPerformed;
            inputManager.Fire.started -= MouseOnDown;
            inputManager.Fire.canceled -= MouseOnUp;
        }

        private void Update()
        {
            if (!inputManager || !buildingManager) return;

            if (Input.GetKeyDown(KeyCode.G))
            {
                Display(EdgeBuildingType.Barricade, _ => TileAction.Build);
            }
            
            if (!ShouldDisplay())
            {
                UnSelect();
                return;
            }
            
            Vector3 mousePosition = inputManager.CurrentMouseWorldPosition;
            ChunkIndexEdge index = ChunkWaveUtility.GetChunkIndexEdge(mousePosition + Vector3.up * 0.1f);
            if (selectedEdge.HasValue && selectedEdge.Value.Equals(index)) return;
            
            if (IsEdgeValid(index, out TileAction tileAction))
            {
                SelectTile(index, tileAction);
            }
            else 
            {
                UnSelect();
            }
        }

        private bool ShouldDisplay()
        {
            return !InputManager.MouseOverUI() && displaying;
        }

        private void UnSelect()
        {
            if (!selectedEdge.HasValue)
            {
                return;
            }
            
            selectedEdge = null;
            selectedEdgeHandler.Hide();
        }

        private void SelectTile(ChunkIndexEdge index, TileAction tileAction)
        {
            pendingAction = tileAction;
            selectedEdge = index;
            selectedEdgeHandler.SelectEdge(index, tileAction);
        }

        private bool IsEdgeValid(ChunkIndexEdge index, out TileAction action)
        {
            Debug.Log("Index: " + index);
            bool isValid = Edges.TryGetValue(index, out _)
                           && IsEdgeLoaded(index);
            if (!isValid)
            {
                action = TileAction.None;
                return false;
            }
            
            action = OnEdgeEntered?.Invoke(index) ?? TileAction.None;
            return action != TileAction.None;
        }
        
        private bool IsEdgeLoaded(ChunkIndexEdge index)
        {
            if (groundGenerator.LoadedFullChunks.Contains(index.Index))
            {
                return true;
            }

            bool onLeftSide = index.CellIndex.x >= groundGenerator.ChunkSize.x - 1;
            if (onLeftSide)
            {
                if (groundGenerator.LoadedFullChunks.Contains(index.Index + new int3(1, 0, 0)))
                {
                    return true;
                }
            }

            bool onRightSide = index.CellIndex.x < 1 && index.EdgeType == EdgeType.West;
            if (onRightSide)
            {
                if (groundGenerator.LoadedFullChunks.Contains(index.Index + new int3(-1, 0, 0)))
                {
                    return true;
                }
            }

            bool onBotSide = index.CellIndex.z >= groundGenerator.ChunkSize.z - 1  
                             || (index.CellIndex.z >= groundGenerator.ChunkSize.z - 2 && index.EdgeType == EdgeType.North);
            if (onBotSide)
            {
                if (groundGenerator.LoadedFullChunks.Contains(index.Index + new int3(0, 0, 1)))
                {
                    return true;
                }
            }

            if (onRightSide && onBotSide)
            {
                if (groundGenerator.LoadedFullChunks.Contains(index.Index + new int3(-1, 0, 1)))
                {
                    return true;
                }
            }
            
            if (onLeftSide && onBotSide)
            {
                if (groundGenerator.LoadedFullChunks.Contains(index.Index + new int3(1, 0, 1)))
                {
                    return true;
                }
            }
            
            return false;
        }

        private void InitializeSpawnPlaces(QueryMarchedChunk chunk)
        {
            for (int x = 0; x < chunk.Width; x++)
            for (int z = 0; z < chunk.Depth; z++)
            {
                ChunkIndexEdge northEdge = new ChunkIndexEdge(chunk.ChunkIndex, new int3(x, 0, z), EdgeType.North);
                ChunkIndexEdge westEdge = new ChunkIndexEdge(chunk.ChunkIndex, new int3(x, 0, z), EdgeType.West);
                Edges.TryAdd(northEdge, 0);
                Edges.TryAdd(westEdge, 0);

                //if (x == chunk.Width - 1)
                //{
                //    ChunkIndexEdge neighbourEdge = new ChunkIndexEdge(chunk.ChunkIndex + new int3(1, 0, 0), new int3(0, 0, z), EdgeType.West);
                //    Edges.TryAdd(neighbourEdge, 0);
                //}
                //
                //if (z == 0)
                //{
                //    ChunkIndexEdge neighbourEdge = new ChunkIndexEdge(chunk.ChunkIndex + new int3(0, 0, -1), new int3(x, 0, chunk.Depth - 1), EdgeType.North);
                //    Edges.TryAdd(neighbourEdge, 0);
                //}
            }
        }
        
        private void MouseOnUp(InputAction.CallbackContext obj)
        {
            if (!CameraController.IsDragging && pressedEdge.HasValue && selectedEdge.Equals(pressedEdge))
            {
                OnEdgePressed?.Invoke(pressedEdge.Value, pendingAction);
                selectedEdge = null;
            }
            
            pressedEdge = null;
        }

        private void MouseOnDown(InputAction.CallbackContext obj)
        {
            pressedEdge = selectedEdge;
        }
        
        private void OnBuiltEdgeDestroyed(ChunkIndexEdge chunkIndex)
        {
            Edges[chunkIndex] = 0;
        }
        
        private void CancelPerformed(InputAction.CallbackContext obj)
        {
            if (displaying)
            {
                CancelDisplay();
            }
        }

        public void CancelDisplay()
        {
            focusManager.UnregisterFocus(focus);
        }

        public void Display(EdgeBuildingType type, Func<ChunkIndexEdge, TileAction> buildable)
        {
            focusManager.RegisterFocus(focus);
            
            currentType = type;
            displaying = true;
            
            OnEdgeEntered = buildable;
        }

        private void Hide()
        {
            if (!displaying) return;
            
            OnCancelPlacement?.Invoke();
            
            displaying = false;
            UnSelect();
        }

        public bool GetIsDisplaying(out EdgeBuildingType placingType)
        {
            placingType = currentType;
            return displaying;
        }
    }
    
    [System.Serializable]
    public struct ChunkIndexEdge : IEquatable<ChunkIndexEdge>
    {
        public int3 Index;
        public int3 CellIndex;
        public EdgeType EdgeType;

        public ChunkIndexEdge(int3 index, int3 cellIndex, EdgeType edgeType)
        {
            Index = index;
            CellIndex = cellIndex;
            EdgeType = edgeType;
        }

        public override string ToString()
        {
            return $"(ChunkIndexEdge) {Index}, {CellIndex}, {EdgeType}";
        }

        public bool Equals(ChunkIndexEdge other)
        {
            return Index.Equals(other.Index) 
                   && CellIndex.Equals(other.CellIndex)
                   && EdgeType == other.EdgeType;
        }

        public override bool Equals(object obj)
        {
            return obj is ChunkIndexEdge other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Index, CellIndex, EdgeType);
        }
    }

    public enum EdgeType
    {
        North,
        West
    }
    
    [Flags]
    public enum EdgeBuildingType
    {
        None = 0,
        Barricade = 1,
    }
}