using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using System.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;
using System.Linq;
using DG.Tweening;
using Unity.Mathematics;
using WaveFunctionCollapse;

public class BuildingPlacer : MonoBehaviour
{
    [Title("Placing")]
    [SerializeField]
    private LayerMask layerMask;

    [SerializeField]
    private PooledMonoBehaviour unableToPlacePrefab;

    [SerializeField]
    private PlaceSquare placeSquarePrefab;

    private readonly List<PooledMonoBehaviour> spawnedUnablePlaces = new List<PooledMonoBehaviour>();
    private readonly List<PlaceSquare> spawnedSpawnPlaces = new List<PlaceSquare>();

    private GroundGenerator groundGenerator;
    private Vector3 targetScale;
    
    private bool manualCancel;
    private bool Canceled => InputManager.Instance.Cancel.WasPerformedThisFrame() || manualCancel;
    public int2? SquareIndex { get; set; }
    public int SpawnSquareIndex { get; set; }

    private void OnEnable()
    {
        Events.OnBuildingCanceled += OnBuildingCanceled;
        groundGenerator = FindFirstObjectByType<GroundGenerator>();
        groundGenerator.OnMapGenerated += InitializeSpawnPlaces;
        
        Events.OnBuildingDestroyed += OnBuildingDestroyed;
    }
    
    private void OnDisable()
    {
        Events.OnBuildingCanceled -= OnBuildingCanceled;
        groundGenerator.OnMapGenerated -= InitializeSpawnPlaces;
        Events.OnBuildingDestroyed -= OnBuildingDestroyed;
    }

    private void Start()
    {
        Events.OnBuildingPurchased += BuildingPurchased;
    }
    
    private void InitializeSpawnPlaces()
    {
        if (BuildingManager.Instance is null) return;
        
        InitalizeSpawnPlacesAsync().Forget(ex =>
        {
            Debug.LogError($"Async function failed: {ex}");
        });
    }

    private async UniTask InitalizeSpawnPlacesAsync()
    {
        await UniTask.WaitUntil(() => BuildingManager.Instance.Cells != null);
        
        targetScale = groundGenerator.WaveFunction.GridScale * BuildingManager.Instance.CellSize;
        for (int z = 0; z < BuildingManager.Instance.Cells.GetLength(1) - 1; z++)
        {
            for (int x = 0; x < BuildingManager.Instance.Cells.GetLength(0) - 1; x++)
            {
                if (!BuildingManager.Instance.Cells[x, z].Buildable
                    || !BuildingManager.Instance.Cells[x + 1, z].Buildable
                    || !BuildingManager.Instance.Cells[x, z + 1].Buildable
                    || !BuildingManager.Instance.Cells[x + 1, z + 1].Buildable) continue;
                    
                Vector3 pos = BuildingManager.Instance.Cells[x, z].Position + new Vector3(targetScale.x / 2.0f, 0.1f, targetScale.z / 2.0f);
                PlaceSquare placeSquare = Instantiate(placeSquarePrefab, pos, placeSquarePrefab.transform.rotation);
                placeSquare.Placer = this;
                placeSquare.Index = new int2(x, z);
                placeSquare.transform.localScale = targetScale * 0.95f;
                placeSquare.transform.SetParent(transform, true);
                placeSquare.gameObject.SetActive(false);
                placeSquare.SquareIndex = spawnedSpawnPlaces.Count;
                spawnedSpawnPlaces.Add(placeSquare);
            }
        }

    }
    
    private void OnBuildingCanceled()
    {
        manualCancel = true;
    }

    private void BuildingPurchased(BuildingType buildingType)
    {
        PlacingTower(buildingType).Forget(Debug.LogError);
    }

    private async UniTask PlacingTower(BuildingType type)
    {
        manualCancel = false;
        
        ToggleSpawnPlaces(true);

        int2 queryIndex = new int2();
        Dictionary<int2, IBuildable> buildables = new Dictionary<int2, IBuildable>();
        while (!Canceled)
        {
            await UniTask.Yield();

            if (!SquareIndex.HasValue) continue;

            if (math.all(queryIndex == SquareIndex.Value))
            {
                if (buildables.Count > 0 && InputManager.Instance.Fire.WasPerformedThisFrame())
                {
                    PlaceBuilding();
                }
                continue;
            }
            
            DisablePlaces();

            BuildingManager.Instance.RevertQuery();
            await UniTask.NextFrame();
            if (!SquareIndex.HasValue) continue;

            queryIndex = SquareIndex.Value;
            buildables = BuildingManager.Instance.Query(queryIndex, type);
            
            foreach (var item in buildables)
            {
                item.Value.ToggleIsBuildableVisual(true);
            }
            
            if (buildables.Count == 0) 
            {
                ShowUnablePlaces(BuildingManager.Instance.GetCellsToCollapse(queryIndex, type).Select(x => BuildingManager.Instance.GetPos(x) + Vector3.up * BuildingManager.Instance.CellSize / 2.0f).ToList());
                continue;
            }

            if (!InputManager.Instance.Fire.WasPerformedThisFrame()) continue;

            PlaceBuilding();
        }

        if (Canceled)
        {
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
        for (int i = 0; i < spawnedSpawnPlaces.Count; i++)
        {
            PlaceSquare placeSquare = spawnedSpawnPlaces[i];
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
        spawnedSpawnPlaces[SpawnSquareIndex].OnPlaced();
        BuildingManager.Instance.Place();
    }
    
    private void OnBuildingDestroyed(Building building)
    {
        for (int i = 0; i < spawnedSpawnPlaces.Count; i++)
        {
            if (math.all(spawnedSpawnPlaces[i].Index == building.Index))
            {
                spawnedSpawnPlaces[i].UnPlaced();
            }
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