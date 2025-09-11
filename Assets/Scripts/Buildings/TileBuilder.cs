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
        
        private Dictionary<ChunkIndex, BuildingType> tiles = new Dictionary<ChunkIndex, BuildingType>();

        private BuildingManager buildingManager;
        private InputManager inputManager;
        private FocusManager focusManager;
        private Focus focus;
        
        private ChunkIndex? selectedTile;
        private ChunkIndex? pressedTile;

        private TileAction pendingAction;
        private BuildingType currentType;
        
        private bool displaying;
        
        public Dictionary<ChunkIndex, BuildingType> Tiles => tiles;

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
            return !inputManager.MouseOverUI() && displaying;
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
            selectedTileHandler.SelectTile(index);
            selectedTileHandler.DisplayAction(tileAction);
        }

        private bool IsTileValid(ChunkIndex index, out TileAction action)
        {
            bool isValid = groundGenerator.GeneratedChunks.Contains(index.Index)
                           && tiles.TryGetValue(index, out _);
            if (!isValid)
            {
                action = TileAction.None;
                return false;
            }
            
            action = OnTileEntered?.Invoke(index) ?? TileAction.None;
            return action != TileAction.None;
        }

        private void InitializeSpawnPlaces(QueryMarchedChunk chunk)
        {
            for (int x = 0; x < chunk.Width; x++)
            for (int z = 0; z < chunk.Depth; z++)
            {
                ChunkIndex chunkIndex = new ChunkIndex(chunk.ChunkIndex, new int3(x, 0, z));
                TryAdd(chunkIndex);
            }
            
            void TryAdd(ChunkIndex chunkIndex)
            {
                if (!buildingManager.IsBuildable(chunkIndex))
                {
                    return;
                }
                
                tiles.TryAdd(chunkIndex, 0);
            }
        }
        
        private void MouseOnUp(InputAction.CallbackContext obj)
        {
            if (!CameraController.IsDragging && pressedTile.HasValue && selectedTile.Equals(pressedTile))
            {
                OnTilePressed?.Invoke(pressedTile.Value, pendingAction);
            }
            
            pressedTile = null;
        }

        private void MouseOnDown(InputAction.CallbackContext obj)
        {
            pressedTile = selectedTile;
        }
        
        private void CancelPerformed(InputAction.CallbackContext obj)
        {
            if (displaying)
            {
                CancelDisplay();
            }
        }

        public void Display(BuildingType type, IsTileBuildable buildable)
        {
            currentType = type;
            displaying = true;
            focusManager.RegisterFocus(focus);
            
            OnTileEntered = buildable; 
        }

        public void CancelDisplay()
        {
            focusManager.UnregisterFocus(focus);
        }

        private void Hide()
        {
            if (!displaying) return;
            
            displaying = false;
            UnSelect();
            
            OnCancelPlacement?.Invoke();
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