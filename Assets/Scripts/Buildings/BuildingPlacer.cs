using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine.InputSystem;
using Sirenix.OdinInspector;
using WaveFunctionCollapse;
using Math = Utility.Math;
using Unity.Mathematics;
using Gameplay.Money;
using UnityEngine;
using System.Linq;
using InputCamera;
using DG.Tweening;
using System;

namespace Buildings
{
    public class BuildingPlacer : MonoBehaviour
    {
        public static bool Displaying = false;
        
        public event Action<ChunkIndex> OnIndexBuilt;
        public event Action<ChunkIndex> OnIndexSold;

        [Title("Placing")]
        [SerializeField]
        private PooledMonoBehaviour unableToPlacePrefab;

        [SerializeField]
        private PlaceSquare placeSquarePrefab;

        [SerializeField]
        private BarricadePlacer barricadePlacer;
        
        [Title("References")]
        [SerializeField]
        private DistrictGenerator districtGenerator;

        private readonly Dictionary<ChunkIndex, PlaceSquare> spawnedSpawnPlaces = new Dictionary<ChunkIndex, PlaceSquare>();
        private readonly List<PooledMonoBehaviour> spawnedUnablePlaces = new List<PooledMonoBehaviour>();

        private BuildingHandler buildingHandler;
        private BuildingManager buildingManager;
        private GroundGenerator groundGenerator;
        private PlaceSquare hoveredSquare;
        private InputManager inputManager;
        private PlaceSquare pressedSquare;
        private Vector3 targetScale;
        private Camera cam;

        private bool manualCancel;

        public Dictionary<ChunkIndex, PlaceSquare> SpawnedSpawnPlaces => spawnedSpawnPlaces;
        private bool Canceled => inputManager.Cancel.WasPerformedThisFrame() || manualCancel;
        public bool SquareWasPressed { get; set; }
        public ChunkIndex? SquareIndex { get; set; }

        private void OnEnable()
        {
            cam = Camera.main;
            groundGenerator = FindFirstObjectByType<GroundGenerator>();
            buildingHandler = FindFirstObjectByType<BuildingHandler>();

            Events.OnBuiltIndexDestroyed += OnBuiltIndexDestroyed;
            UIEvents.OnFocusChanged += OnBuildingCanceled;
            Events.OnBuildingClicked += BuildingClicked;
            Events.OnBuiltIndexBuilt += OnBuildingBuilt;
            
            barricadePlacer.OnIndexBuilt += BarricadeIndexBuilt;
            barricadePlacer.OnIndexSold += BarricadeIndexSold;

            AwaitManagers().Forget();
        }

        private async UniTaskVoid AwaitManagers()
        {
            buildingManager = await BuildingManager.Get();
            buildingManager.OnLoaded += InitializeSpawnPlaces;
            
            inputManager = await InputManager.Get();
            inputManager.Fire.started += MouseOnDown;
            inputManager.Fire.canceled += MouseOnUp;
        }

        private void OnDisable()
        {
            buildingManager.OnLoaded -= InitializeSpawnPlaces;
            Events.OnBuiltIndexDestroyed -= OnBuiltIndexDestroyed;
            barricadePlacer.OnIndexBuilt -= BarricadeIndexBuilt;
            barricadePlacer.OnIndexSold -= BarricadeIndexSold;
            inputManager.Fire.started -= MouseOnDown;
            inputManager.Fire.canceled -= MouseOnUp;
            UIEvents.OnFocusChanged -= OnBuildingCanceled;
            Events.OnBuildingClicked -= BuildingClicked;
            Events.OnBuiltIndexBuilt -= OnBuildingBuilt;
        }

        private void Update()
        {
            if (!Displaying || CameraController.IsDragging)
            {
                SquareIndex = null;
                return;
            }

            Vector3 mousePoint = Math.GetGroundIntersectionPoint(cam, Mouse.current.position.ReadValue());
            ChunkIndex? chunkIndex = buildingManager.GetIndex(mousePoint);
            if (!chunkIndex.HasValue)
            {
                SquareIndex = null;
                return;
            }

            if (!spawnedSpawnPlaces.TryGetValue(chunkIndex.Value, out PlaceSquare placeSquare))
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
            targetScale = groundGenerator.ChunkWaveFunction.CellSize.MultiplyByAxis(buildingManager.ChunkWaveFunction.CellSize);
            for (int x = 0; x < chunk.Width - 1; x++)
            {
                for (int z = 0; z < chunk.Depth - 1; z++)
                {
                    ChunkIndex chunkIndex = new ChunkIndex(chunk.ChunkIndex, new int3(x, 0, z));
                    if (spawnedSpawnPlaces.ContainsKey(chunkIndex)
                        || !buildingManager.IsBuildable(chunk, new int3(x    , 0, z    ))
                        || !buildingManager.IsBuildable(chunk, new int3(x + 1, 0, z    ))
                        || !buildingManager.IsBuildable(chunk, new int3(x    , 0, z + 1))
                        || !buildingManager.IsBuildable(chunk, new int3(x + 1, 0, z + 1))) continue;

                    Vector3 pos = chunk.Cells[x, 0, z].Position + new Vector3(targetScale.x / 2.0f, 0.1f, targetScale.z / 2.0f);
                    SpawnSquare(pos, chunkIndex);
                }
            }

            Dictionary<int3, QueryMarchedChunk> chunks = buildingManager.ChunkWaveFunction.Chunks;
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
                        if (spawnedSpawnPlaces.ContainsKey(chunkIndex)
                            || !buildingManager.IsBuildable(chunk   , new int3(x    , 0, z))
                            || !buildingManager.IsBuildable(chunk   , new int3(x + 1, 0, z))
                            || !buildingManager.IsBuildable(topChunk, new int3(x    , 0, 0))
                            || !buildingManager.IsBuildable(topChunk, new int3(x + 1, 0, 0))) continue;

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
                        if (spawnedSpawnPlaces.ContainsKey(chunkIndex)
                            || !buildingManager.IsBuildable(chunk     , new int3(x, 0, z    ))
                            || !buildingManager.IsBuildable(chunk     , new int3(x, 0, z + 1))
                            || !buildingManager.IsBuildable(rightChunk, new int3(0, 0, z    ))
                            || !buildingManager.IsBuildable(rightChunk, new int3(0, 0, z + 1))) continue;

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
                    if (spawnedSpawnPlaces.ContainsKey(chunkIndex)
                        || !buildingManager.IsBuildable(chunk        , new int3(x, 0, z))
                        || !buildingManager.IsBuildable(rightChunk   , new int3(0, 0, z))
                        || !buildingManager.IsBuildable(topChunk     , new int3(x, 0, 0))
                        || !buildingManager.IsBuildable(topRightChunk, new int3(0, 0, 0))) return;

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

        private void OnBuildingCanceled()
        {
            manualCancel = true;
        }

        private void BuildingClicked(BuildingType buildingType)
        {
            if (Displaying || buildingType is not BuildingType.Building) return;

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
                    buildingManager.RevertQuery();
                    queryIndex = default;
                    continue;
                }

                if (queryIndex.Equals(SquareIndex.Value))
                {
                    if (buildables.Count > 0 && SquareWasPressed && !districtGenerator.IsGenerating)
                    {
                        if (pressedSquare.Placed)
                        {
                            RemoveBuilding();
                        }
                        else
                        {
                            PlaceBuilding();
                        }
                    }

                    continue;
                }

                buildingManager.RevertQuery();
                await UniTask.NextFrame();
                if (!SquareIndex.HasValue || spawnedSpawnPlaces[SquareIndex.Value].Locked) continue;

                queryIndex = SquareIndex.Value;
                buildables = buildingManager.Query(queryIndex);

                foreach (IBuildable item in buildables.Values)
                {
                    item.ToggleIsBuildableVisual(true, hoveredSquare.Placed);
                }

                if (buildables.Count == 0)
                {
                    ShowUnablePlaces(buildingManager.GetCellsToCollapse(queryIndex).Select(x => buildingManager.GetPos(x) + Vector3.up * buildingManager.ChunkScale.y / 2.0f)
                        .ToList());
                    continue;
                }

                if (!SquareWasPressed || districtGenerator.IsGenerating) continue;

                if (pressedSquare.Placed)
                {
                    RemoveBuilding();
                }
                else
                {
                    PlaceBuilding();
                }
            }

            Displaying = false;
            if (Canceled)
            {
                SquareIndex = null;
                ToggleSpawnPlaces(false);
                DisablePlaces();
                buildingManager.RevertQuery();

                if (!manualCancel)
                {
                    Events.OnBuildingCanceled?.Invoke();
                }
            }
        }

        public void ToggleSpawnPlaces(bool enabled)
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
                    placeSquare.transform.DOScale(Vector3.zero, 0.5f).SetEase(Ease.OutCirc).OnComplete(() => { placeSquare.gameObject.SetActive(false); });

                }
            }
        }

        private void PlaceBuilding()
        {
            SquareWasPressed = false;
            if (!SquareIndex.HasValue)
            {
                return;
            }

            if (!MoneyManager.Instance.CanPurchase(BuildingType.Building))
            {
                //buildingManager.RevertQuery();
                return;
            }

            MoneyManager.Instance.Purchase(BuildingType.Building);

            buildingManager.Place();
            
            OnIndexBuilt?.Invoke(SquareIndex.Value);
        }

        private void OnBuildingBuilt(IEnumerable<ChunkIndex> indexes)
        {
            foreach (ChunkIndex chunkIndex in indexes)
            {
                if (spawnedSpawnPlaces.TryGetValue(chunkIndex, out PlaceSquare square))
                {
                    square.OnPlaced();
                }
            }
        }

        private void RemoveBuilding()
        {
            SquareWasPressed = false;
            if (!SquareIndex.HasValue)
            {
                return;
            }

            MoneyManager.Instance.AddMoneyParticles(MoneyManager.Instance.BuildingCost, hoveredSquare.transform.position);

            buildingHandler.BuildingDestroyed(SquareIndex.Value);
            OnIndexSold?.Invoke(SquareIndex.Value);
        }

        private void OnBuiltIndexDestroyed(ChunkIndex chunkIndex)
        {
            if (!spawnedSpawnPlaces.TryGetValue(chunkIndex, out PlaceSquare square)) return;

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

        private void BarricadeIndexBuilt(ChunkIndex index)
        {
            if (spawnedSpawnPlaces.TryGetValue(index, out PlaceSquare square))
            {
                square.Locked = true;
            }   
        }
        
        private void BarricadeIndexSold(ChunkIndex index)
        {
            if (spawnedSpawnPlaces.TryGetValue(index, out PlaceSquare square))
            {
                square.Locked = false;
            }
        }
    }

    public enum BuildingType
    {
        Building,
        Barricade
    }
}