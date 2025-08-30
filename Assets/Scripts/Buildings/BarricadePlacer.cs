using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine.InputSystem;
using Sirenix.OdinInspector;
using WaveFunctionCollapse;
using Unity.Mathematics;
using Gameplay.Money;
using System.Linq;
using DG.Tweening;
using InputCamera;
using UnityEngine;

namespace Buildings
{
    public class BarricadePlacer : MonoBehaviour
    {
        public static bool Displaying = false;
        
        [Title("Placing")]
        [SerializeField]
        private PooledMonoBehaviour unableToPlacePrefab;

        [SerializeField]
        private PlaceSquare placeSquarePrefab;

        [SerializeField]
        private BuildingPlacer buildingPlacer;

        private readonly Dictionary<ChunkIndex, PlaceSquare> spawnedSpawnPlaces = new Dictionary<ChunkIndex, PlaceSquare>();
        private readonly List<PooledMonoBehaviour> spawnedUnablePlaces = new List<PooledMonoBehaviour>();
        
        private BarricadeGenerator barricadeGenerator;
        private BarricadeHandler barricadeHandler;
        private GroundGenerator groundGenerator;
        private PlaceSquare hoveredSquare;
        private PlaceSquare pressedSquare;
        private InputManager inputManager;
        private Vector3 targetScale;
        private Camera cam;

        private bool manualCancel;

        private bool Canceled => inputManager.Cancel.WasPerformedThisFrame() || manualCancel;
        public bool SquareWasPressed { get; set; }
        public ChunkIndex? SquareIndex { get; set; }

        private void OnEnable()
        {
            cam = Camera.main;
            groundGenerator = FindFirstObjectByType<GroundGenerator>();
            barricadeGenerator = FindFirstObjectByType<BarricadeGenerator>();
            barricadeHandler = FindFirstObjectByType<BarricadeHandler>();
            barricadeGenerator.OnLoaded += InitializeSpawnPlaces;

            UIEvents.OnFocusChanged += OnPlacingCanceled;
            Events.OnBuildingClicked += BuildingClicked;
            
            Events.OnBuiltIndexBuilt += BuiltIndexBuilt;
            Events.OnBuiltIndexDestroyed += BuiltIndexRemoved;

            GetInput().Forget();
        }

        private async UniTaskVoid GetInput()
        {
            inputManager = await InputManager.Get();
            inputManager.Fire.started += MouseOnDown;
            inputManager.Fire.canceled += MouseOnUp;
        }

        private void OnDisable()
        {
            barricadeGenerator.OnLoaded -= InitializeSpawnPlaces;
            Events.OnBuiltIndexDestroyed -= BuiltIndexRemoved;
            Events.OnBuiltIndexBuilt -= BuiltIndexBuilt;
            Events.OnBuildingClicked -= BuildingClicked;
            UIEvents.OnFocusChanged -= OnPlacingCanceled;
            inputManager.Fire.started -= MouseOnDown;
            inputManager.Fire.canceled -= MouseOnUp;
        }
        
        private void Update()
        {
            if (!Displaying || CameraController.IsDragging)
            {
                SquareIndex = null;
                return;
            }
            
            Vector3 mousePoint = Utility.Math.GetGroundIntersectionPoint(cam, Mouse.current.position.ReadValue());
            if (!barricadeGenerator.TryGetIndex(mousePoint, out ChunkIndex chunkIndex))
            {
                SquareIndex = null;
                return;
            }

            if (!spawnedSpawnPlaces.TryGetValue(chunkIndex, out PlaceSquare placeSquare))
            {
                hoveredSquare?.OnHoverExit();
                hoveredSquare = null;
                SquareIndex = null;
                return;
            }

            if (hoveredSquare != placeSquare && !placeSquare.Locked)
            {
                SquareWasPressed = false;
                SquareIndex = chunkIndex;
                
                hoveredSquare?.OnHoverExit();
                hoveredSquare = placeSquare;
                hoveredSquare.OnHover();  
            }
        }
        
        private void MouseOnUp(InputAction.CallbackContext obj)
        {
            if (!CameraController.IsDragging 
                && pressedSquare != null && pressedSquare == hoveredSquare)
            {
                SquareWasPressed = true;
            }
        }

        private void MouseOnDown(InputAction.CallbackContext obj)
        {
            pressedSquare = hoveredSquare;
        }

        private void InitializeSpawnPlaces(QueryMarchedChunk chunk)
        {
            targetScale = groundGenerator.ChunkWaveFunction.CellSize.MultiplyByAxis(barricadeGenerator.ChunkWaveFunction.CellSize);
            for (int x = 0; x < chunk.Width - 1; x++)
            {
                for (int z = 0; z < chunk.Depth - 1; z++)
                {
                    ChunkIndex chunkIndex = new ChunkIndex(chunk.ChunkIndex, new int3(x, 0, z));
                    if (spawnedSpawnPlaces.ContainsKey(chunkIndex)) continue;

                    Vector3 pos = chunk.Cells[x, 0, z].Position + new Vector3(targetScale.x / 2.0f, 0.1f, targetScale.z / 2.0f);
                    SpawnSquare(pos, chunkIndex);
                }
            }

            Dictionary<int3,QueryMarchedChunk> chunks = barricadeGenerator.ChunkWaveFunction.Chunks;
            CheckIntersection(chunk);
            if (chunks.TryGetValue(chunk.ChunkIndex - new int3(0, 0, 1), out QueryMarchedChunk bottomChunk))
                CheckIntersection(bottomChunk);
            if (chunks.TryGetValue(chunk.ChunkIndex - new int3(1, 0, 0), out QueryMarchedChunk leftChunk))
                CheckIntersection(leftChunk);
            if (chunks.TryGetValue(chunk.ChunkIndex - new int3(1, 0, 1), out QueryMarchedChunk bottomLeftChunk))
                CheckIntersection(bottomLeftChunk); 

            void CheckIntersection(QueryMarchedChunk chunk)
            {
                // Top
                if (chunks.TryGetValue(chunk.ChunkIndex + new int3(0, 0, 1), out QueryMarchedChunk topChunk))
                {
                    int z = chunk.Depth - 1;
                    for (int x = 0; x < chunk.Width - 1; x++)
                    {
                        ChunkIndex chunkIndex = new ChunkIndex(chunk.ChunkIndex, new int3(x, 0, z));
                        if (spawnedSpawnPlaces.ContainsKey(chunkIndex)) continue;

                        Vector3 pos = chunk.Cells[x, 0, z].Position + new Vector3(targetScale.x / 2.0f, 0.1f, targetScale.z / 2.0f);
                        SpawnSquare(pos, chunkIndex);
                    }
                }

                // Right
                if (chunks.TryGetValue(chunk.ChunkIndex + new int3(1, 0, 0), out QueryMarchedChunk rightChunk))
                {
                    int x = chunk.Width - 1;
                    for (int z = 0; z < chunk.Depth - 1; z++)
                    {
                        ChunkIndex chunkIndex = new ChunkIndex(chunk.ChunkIndex, new int3(x, 0, z));
                        if (spawnedSpawnPlaces.ContainsKey(chunkIndex) ) continue;

                        Vector3 pos = chunk.Cells[x, 0, z].Position + new Vector3(targetScale.x / 2.0f, 0.1f, targetScale.z / 2.0f);
                        SpawnSquare(pos, chunkIndex);
                    }
                }
                
                // Top Right
                if (chunks.TryGetValue(chunk.ChunkIndex + new int3(1, 0, 1), out QueryMarchedChunk topRightChunk) 
                    && topChunk != null && rightChunk != null)
                {
                    int x = chunk.Width - 1;
                    int z = chunk.Depth - 1;
                    ChunkIndex chunkIndex = new ChunkIndex(chunk.ChunkIndex, new int3(x, 0, z));
                    if (spawnedSpawnPlaces.ContainsKey(chunkIndex)) return;

                    Vector3 pos = chunk.Cells[x, 0, z].Position + new Vector3(targetScale.x / 2.0f, 0.1f, targetScale.z / 2.0f);
                    SpawnSquare(pos, chunkIndex);
                }
            }
            
            void SpawnSquare(Vector3 pos, ChunkIndex chunkIndex)
            {
                PlaceSquare placeSquare = Instantiate(placeSquarePrefab, pos, placeSquarePrefab.transform.rotation);
                placeSquare.transform.localScale = targetScale * 0.95f;
                placeSquare.transform.SetParent(transform, true);
                placeSquare.gameObject.SetActive(false);
                spawnedSpawnPlaces.Add(chunkIndex, placeSquare);
            }
        }
        
        private void OnPlacingCanceled()
        {
            manualCancel = true;
        }

        private void BuildingClicked(BuildingType buildingType)
        {
            if (buildingType is not BuildingType.Barricade) return;
            
            if (Displaying)
            {
                OnPlacingCanceled();
                return;
            }
            
            UIEvents.OnFocusChanged?.Invoke();
            PlacingTower().Forget();
        }

        private async UniTaskVoid PlacingTower()
        {
            Displaying = true;
            manualCancel = false;
            SquareWasPressed = false;
            
            ToggleSpawnPlaces(true);

            ChunkIndex queryIndex = new ChunkIndex();
            Dictionary<ChunkIndex, IBuildable> buildables = new Dictionary<ChunkIndex, IBuildable>();
            while (!Canceled)
            {
                await UniTask.Yield();

                if (!SquareIndex.HasValue)
                {
                    barricadeGenerator.RevertQuery();
                    queryIndex = default;
                    continue;
                }

                if (queryIndex.Equals(SquareIndex.Value))
                {
                    if (buildables.Count > 0 && SquareWasPressed)
                    {
                        if (pressedSquare.Placed)
                        {
                            RemoveBarricade();
                        }
                        else
                        {
                            PlaceBarricade();
                        }
                    }
                    continue;
                }
                
                DisablePlaces();
                barricadeGenerator.RevertQuery();
                await UniTask.NextFrame();
                if (!SquareIndex.HasValue || spawnedSpawnPlaces[SquareIndex.Value].Locked) continue;

                queryIndex = SquareIndex.Value;
                buildables = barricadeGenerator.Query(queryIndex);
                
                foreach (IBuildable item in buildables.Values)
                {
                    item.ToggleIsBuildableVisual(true, hoveredSquare.Placed);
                }
                
                if (buildables.Count == 0) 
                {
                    ShowUnablePlaces(barricadeGenerator.GetCellsToCollapse(queryIndex).Select(x => barricadeGenerator.GetPos(x) + Vector3.up * barricadeGenerator.ChunkScale.y / 2.0f).ToList());
                    continue;
                }

                if (!SquareWasPressed) continue;
                
                if (pressedSquare.Placed)
                {
                    RemoveBarricade();
                }
                else
                {
                    PlaceBarricade();
                }
            }

            Displaying = false;
            if (Canceled)
            {
                SquareIndex = null;
                ToggleSpawnPlaces(false);
                DisablePlaces();
                barricadeGenerator.RevertQuery();
            }
        }

        private void ToggleSpawnPlaces(bool enabled)
        {
            foreach (PlaceSquare placeSquare in spawnedSpawnPlaces.Values)
            {
                if (enabled)
                {
                    placeSquare.gameObject.SetActive(true);
                    placeSquare.transform.DOKill();
                    placeSquare.transform.DOScale(targetScale * 0.95f, 0.5f).SetEase(Ease.OutCirc);
                }
                else
                {
                    placeSquare.transform.DOKill();
                    placeSquare.transform.DOScale(Vector3.zero, 0.5f).SetEase(Ease.OutCirc).OnComplete(() =>
                    {
                        placeSquare.gameObject.SetActive(false);
                    });
                    
                }
            }
        }
        
        private void PlaceBarricade()
        {
            SquareWasPressed = false;
            if (!SquareIndex.HasValue)
            {
                return;
            }
            
            if (!MoneyManager.Instance.CanPurchase(BuildingType.Barricade))
            {
                //pathGenerator.RevertQuery();
                return;
            }

            MoneyManager.Instance.Purchase(BuildingType.Barricade);
            
            spawnedSpawnPlaces[SquareIndex.Value].OnPlaced();
            barricadeGenerator.Place();

            Events.OnBuiltIndexBuilt?.Invoke(new List<ChunkIndex> { SquareIndex.Value });
        }

        private void RemoveBarricade()
        {
            SquareWasPressed = false;
            if (!SquareIndex.HasValue)
            {
                return;
            }
            
            MoneyManager.Instance.AddMoneyParticles(MoneyManager.Instance.PathCost, pressedSquare.transform.position);
            
            barricadeHandler.BarricadeDestroyed(SquareIndex.Value);
        }
        
        private void BuiltIndexBuilt(IEnumerable<ChunkIndex> indexes)
        {
            foreach (ChunkIndex index in indexes)
            {
                if (spawnedSpawnPlaces.TryGetValue(index, out PlaceSquare square))
                {
                    if (!square.Placed)
                    {
                        square.Locked = true;
                    }
                }   
            }
        }
        
        private void BuiltIndexRemoved(ChunkIndex index)
        {
            if (!spawnedSpawnPlaces.TryGetValue(index, out PlaceSquare square)) return;

            square.Locked = false;

            if (Displaying)
            {
                square.UnPlaced();
            }
            else
            {
                square.Placed = false;
            }
        }

        private void ShowUnablePlaces(List<Vector3> positions)
        {
            for (int i = 0; i < positions.Count; i++)
            {
                spawnedUnablePlaces.Add(unableToPlacePrefab.GetAtPosAndRot<PooledMonoBehaviour>(positions[i], Quaternion.identity));
            }
        }

        private void DisablePlaces()
        {
            for (int i = 0; i < spawnedUnablePlaces.Count; i++)
            {
                spawnedUnablePlaces[i].gameObject.SetActive(false);
            }
        }
    }
}