using FocusType = Utility.FocusType;

using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine.InputSystem;
using Sirenix.OdinInspector;
using WaveFunctionCollapse;
using Unity.Mathematics;
using UnityEngine;
using InputCamera;
using Utility;
using System;
using Gameplay.Event;
using Juice;

namespace Buildings
{
    public class TileBuilder : MonoBehaviour
    {
        public delegate TileAction IsTileBuildable(ChunkIndex chunkIndex);
        
        public event Action<ChunkIndex, TileAction> OnTilePressed;
        public event Action OnCancelPlacement;
        
        public event IsTileBuildable OnTileEntered;
        
        [Title("References")]
        [SerializeField]
        private SelectedTileHandler selectedTileHandler;
        
        [SerializeField]
        private GroundGenerator groundGenerator;

        private BuildingManager buildingManager;
        private InputManager inputManager;
        private FocusManager focusManager;
        private Focus focus;
        
        private ChunkIndex? selectedTile;
        private ChunkIndex? pressedTile;

        private TileAction pendingAction;
        private BuildingType currentType;
        private GroundType buildableGroundType;
        
        private bool displaying;
        
        public Dictionary<ChunkIndex, BuildingType> Tiles { get; } = new Dictionary<ChunkIndex, BuildingType>();

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
            
            Events.OnBuiltIndexDestroyed += OnBuiltIndexDestroyed;
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
            Events.OnBuiltIndexDestroyed -= OnBuiltIndexDestroyed;
            
            buildingManager.OnLoaded -= InitializeSpawnPlaces;
            inputManager.Cancel.performed -= CancelPerformed;
            inputManager.Fire.started -= MouseOnDown;
            inputManager.Fire.canceled -= MouseOnUp;
        }

        private void Update()
        {
            if (!inputManager || !buildingManager) return;

            if (!ShouldDisplay())
            {
                UnSelect();
                return;
            }
            
            Vector3 mousePosition = inputManager.CurrentMouseWorldPosition;
            ChunkIndex index = ChunkWaveUtility.GetChunkIndex(mousePosition + Vector3.up * 0.1f, groundGenerator.ChunkScale, groundGenerator.ChunkWaveFunction.CellSize);
            if (selectedTile.HasValue && selectedTile.Value.Equals(index)) return;
            
            if (IsTileValid(index, out TileAction tileAction))
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
            if (!selectedTile.HasValue)
            {
                return;
            }
            
            selectedTile = null;
            selectedTileHandler.Hide();
        }

        private void SelectTile(ChunkIndex index, TileAction tileAction)
        {
            pendingAction = tileAction;
            selectedTile = index;
            selectedTileHandler.SelectTile(index, tileAction);
        }

        private bool IsTileValid(ChunkIndex index, out TileAction action)
        {
            bool isValid = Tiles.TryGetValue(index, out _)
                           && IsGroundTypeValid(index) 
                           && IsTileChunkLoaded(index);
            if (!isValid)
            {
                action = TileAction.None;
                return false;
            }
            
            action = OnTileEntered?.Invoke(index) ?? TileAction.None;
            return action != TileAction.None;
        }
        
        private bool IsGroundTypeValid(ChunkIndex index)
        {
            GroundType groundType = buildingManager.ChunkWaveFunction.Chunks[index.Index].GroundTypes[index.CellIndex.x, index.CellIndex.y, index.CellIndex.z, 1];
            return (buildableGroundType & groundType) > 0;
        }

        private bool IsTileChunkLoaded(ChunkIndex index)
        {
            if (groundGenerator.LoadedFullChunks.Contains(index.Index))
            {
                return true;
            }

            bool onRightSide = index.CellIndex.x >= groundGenerator.ChunkSize.x - 1;
            if (onRightSide)
            {
                if (groundGenerator.LoadedFullChunks.Contains(index.Index + new int3(1, 0, 0)))
                {
                    return true;
                }
            }
            
            bool onTopSide = index.CellIndex.z >= groundGenerator.ChunkSize.z - 1;
            if (onTopSide)
            {
                if (groundGenerator.LoadedFullChunks.Contains(index.Index + new int3(0, 0, 1)))
                {
                    return true;
                }
            }

            if (onRightSide && onTopSide)
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
                ChunkIndex chunkIndex = new ChunkIndex(chunk.ChunkIndex, new int3(x, 0, z));
                Tiles.TryAdd(chunkIndex, 0);
            }
        }
        
        private void MouseOnUp(InputAction.CallbackContext obj)
        {
            if (!CameraController.IsDragging && pressedTile.HasValue && selectedTile.Equals(pressedTile))
            {
                OnTilePressed?.Invoke(pressedTile.Value, pendingAction);
                selectedTile = null;
            }
            
            pressedTile = null;
        }

        private void MouseOnDown(InputAction.CallbackContext obj)
        {
            pressedTile = selectedTile;
        }
        
        private void OnBuiltIndexDestroyed(ChunkIndex chunkIndex)
        {
            Tiles[chunkIndex] = 0;
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

        public void Display(BuildingType type, GroundType groundBuildable, IsTileBuildable buildable)
        {
            focusManager.RegisterFocus(focus);
            
            buildableGroundType = groundBuildable;
            currentType = type;
            displaying = true;
            
            OnTileEntered = buildable; 
        }

        private void Hide()
        {
            if (!displaying) return;
            
            OnCancelPlacement?.Invoke();
            
            displaying = false;
            UnSelect();
        }

        public bool GetIsDisplaying(out BuildingType placingType)
        {
            placingType = currentType;
            return displaying;
        }
    }

    public enum TileAction
    {
        None,
        Build,
        Sell,
    }
}