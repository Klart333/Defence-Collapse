using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine.InputSystem;
using Sirenix.OdinInspector;
using WaveFunctionCollapse;
using Unity.Mathematics;
using UnityEngine;
using System.Linq;
using System.Threading.Tasks;
using DG.Tweening;
using Gameplay.Money;
using InputCamera;

public class BuildingPlacer : MonoBehaviour
{
    public static bool Displaying = false;
    
    [Title("Placing")]
    [SerializeField]
    private PooledMonoBehaviour unableToPlacePrefab;

    [SerializeField]
    private PlaceSquare placeSquarePrefab;

    [Title("References")]
    [SerializeField]
    private DistrictGenerator districtGenerator;

    private readonly Dictionary<ChunkIndex, PlaceSquare> spawnedSpawnPlaces = new Dictionary<ChunkIndex, PlaceSquare>();
    private readonly List<PooledMonoBehaviour> spawnedUnablePlaces = new List<PooledMonoBehaviour>();

    private BuildingHandler buildingHandler;
    private GroundGenerator groundGenerator;
    private PlaceSquare hoveredSquare;
    private PlaceSquare pressedSquare;
    private Vector3 targetScale;
    private Camera cam;

    private bool manualCancel;
    
    private bool Canceled => InputManager.Instance.Cancel.WasPerformedThisFrame() || manualCancel;
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

        AwaitManagers().Forget();
    }

    private async UniTaskVoid AwaitManagers()
    {
        await UniTask.WaitUntil(() => BuildingManager.Instance != null && InputManager.Instance != null);
        BuildingManager.Instance.OnLoaded += InitializeSpawnPlaces;
        InputManager.Instance.Fire.started += MouseOnDown;
        InputManager.Instance.Fire.canceled += MouseOnUp;
    }

    private void OnDisable()
    {
        BuildingManager.Instance.OnLoaded -= InitializeSpawnPlaces;
        Events.OnBuiltIndexDestroyed -= OnBuiltIndexDestroyed;
        InputManager.Instance.Fire.started -= MouseOnDown;
        InputManager.Instance.Fire.canceled -= MouseOnUp;
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
        Vector3 mousePoint = Utility.Math.GetGroundIntersectionPoint(cam, Mouse.current.position.ReadValue());
        ChunkIndex? chunkIndex = BuildingManager.Instance.GetIndex(mousePoint);
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

        if (hoveredSquare != placeSquare)
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
        targetScale = groundGenerator.ChunkWaveFunction.CellSize.MultiplyByAxis(BuildingManager.Instance.ChunkWaveFunction.CellSize);
        for (int x = 0; x < chunk.Width - 1; x++)
        {
            for (int z = 0; z < chunk.Depth - 1; z++)
            {
                ChunkIndex chunkIndex = new ChunkIndex(chunk.ChunkIndex, new int3(x, 0, z));
                if (spawnedSpawnPlaces.ContainsKey(chunkIndex) 
                           || !chunk.Cells[x,     0, z].Buildable
                           || !chunk.Cells[x + 1, 0, z].Buildable
                           || !chunk.Cells[x,     0, z + 1].Buildable
                           || !chunk.Cells[x + 1, 0, z + 1].Buildable) continue;

                Vector3 pos = chunk.Cells[x, 0, z].Position + new Vector3(targetScale.x / 2.0f, 0.1f, targetScale.z / 2.0f);
                SpawnSquare(pos, chunkIndex);
            }
        }

        Dictionary<int3,QueryMarchedChunk> chunks = BuildingManager.Instance.ChunkWaveFunction.Chunks;
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
                        || !chunk.Cells[x,     0, z].Buildable
                        || !chunk.Cells[x + 1, 0, z].Buildable
                        || !topChunk.Cells[x,     0, 0].Buildable
                        || !topChunk.Cells[x + 1, 0, 0].Buildable) continue;

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
                        || !chunk.Cells[x, 0, z].Buildable
                        || !chunk.Cells[x, 0, z + 1].Buildable
                        || !rightChunk.Cells[0, 0, z].Buildable
                        || !rightChunk.Cells[0, 0, z + 1].Buildable) continue;

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
                    || !chunk.Cells[x, 0, z].Buildable
                    || !rightChunk.Cells[0, 0, z].Buildable
                    || !topChunk.Cells[x, 0, 0].Buildable
                    || !topRightChunk.Cells[0, 0, 0].Buildable) return;

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
                BuildingManager.Instance.RevertQuery();
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
            
            BuildingManager.Instance.RevertQuery();
            await UniTask.NextFrame();
            if (!SquareIndex.HasValue) continue;

            queryIndex = SquareIndex.Value;
            buildables = BuildingManager.Instance.Query(queryIndex);
            
            foreach (IBuildable item in buildables.Values)
            {
                item.ToggleIsBuildableVisual(true, hoveredSquare.Placed);
            }
            
            if (buildables.Count == 0) 
            {
                ShowUnablePlaces(BuildingManager.Instance.GetCellsToCollapse(queryIndex).Select(x => BuildingManager.Instance.GetPos(x) + Vector3.up * BuildingManager.Instance.ChunkScale.y / 2.0f).ToList());
                continue;
            }

            if (!SquareWasPressed ||  districtGenerator.IsGenerating) continue;
            
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
            BuildingManager.Instance.RevertQuery();

            if (!manualCancel)
            {
                Events.OnBuildingCanceled?.Invoke();
            }
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
    
    private void PlaceBuilding()
    {
        SquareWasPressed = false;
        if (!SquareIndex.HasValue)
        {
            return;
        }
        
        if (!MoneyManager.Instance.CanPurchase(BuildingType.Building))
        {
            //BuildingManager.Instance.RevertQuery();
            return;
        }

        MoneyManager.Instance.Purchase(BuildingType.Building);
        
        BuildingManager.Instance.Place();
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
        
        buildingHandler.BuildingDestroyed(SquareIndex.Value).Forget();
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
}

public enum BuildingType
{
    Building,
    Path
}