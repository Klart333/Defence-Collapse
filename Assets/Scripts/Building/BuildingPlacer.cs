using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using System.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;
using System.Linq;

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

    private bool manualCancel;
    private bool Canceled => InputManager.Instance.Cancel.WasPerformedThisFrame() || manualCancel;
    public Vector3Int? SquareIndex { get; set; }
    public int SpawnSquareIndex { get; set; }

    private void OnEnable()
    {
        Events.OnBuildingCanceled += OnBuildingCanceled;
        groundGenerator = FindFirstObjectByType<GroundGenerator>();
        groundGenerator.OnMapGenerated += InitializeSpawnPlaces;
    }

    private void OnDisable()
    {
        Events.OnBuildingCanceled -= OnBuildingCanceled;
        groundGenerator.OnMapGenerated -= InitializeSpawnPlaces;
    }

    private void Start()
    {
        Events.OnBuildingPurchased += BuildingPurchased;
    }
    
    private async void InitializeSpawnPlaces()
    {
        if (BuildingManager.Instance is null) return;
        
        await UniTask.WaitUntil(() => BuildingManager.Instance.Cells != null);
        
        Vector3 scale = groundGenerator.WaveFunction.GridScale * BuildingManager.Instance.CellSize;
        for (int z = 0; z < BuildingManager.Instance.Cells.GetLength(2); z++)
        {
            for (int x = 0; x < BuildingManager.Instance.Cells.GetLength(0); x++)
            {
                for (int y = 0; y < BuildingManager.Instance.Cells.GetLength(1); y++)
                {
                    if (!BuildingManager.Instance.Cells[x, y, z].Buildable
                     || !BuildingManager.Instance.Cells[x + 1, y, z].Buildable
                     || !BuildingManager.Instance.Cells[x, y, z + 1].Buildable
                     || !BuildingManager.Instance.Cells[x + 1, y, z + 1].Buildable) continue;
                    
                    Vector3 pos = BuildingManager.Instance.Cells[x, y, z].Position + new Vector3(scale.x / 2.0f, 0.1f, scale.z / 2.0f);
                    PlaceSquare placeSquare = Instantiate(placeSquarePrefab, pos, placeSquarePrefab.transform.rotation);
                    placeSquare.Placer = this;
                    placeSquare.Index = new Vector3Int(x, y, z);
                    placeSquare.transform.localScale = scale * 0.95f;
                    placeSquare.transform.SetParent(transform, true);
                    placeSquare.gameObject.SetActive(false);
                    placeSquare.SquareIndex = spawnedSpawnPlaces.Count;
                    spawnedSpawnPlaces.Add(placeSquare);
                }
            }
        }
        
    }
    
    private void OnBuildingCanceled()
    {
        manualCancel = true;
    }

    private void BuildingPurchased(BuildingType buildingType)
    {
        PlacingTower(buildingType);
    }

    private async UniTask PlacingTower(BuildingType type)
    {
        manualCancel = false;
        
        ToggleSpawnPlaces(true);

        Vector3Int queryIndex = new Vector3Int();
        Dictionary<Vector3Int, IBuildable> buildables = new Dictionary<Vector3Int, IBuildable>();
        while (!Canceled)
        {
            await UniTask.Yield();

            if (!SquareIndex.HasValue) continue;

            if (queryIndex == SquareIndex.Value)
            {
                if (buildables.Count > 0 && InputManager.Instance.Fire.WasPerformedThisFrame())
                {
                    PlaceBuilding(type);
                }
                continue;
            }
            
            DisablePlaces();

            BuildingManager.Instance.RevertQuery();
            await UniTask.Delay(50);
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

            PlaceBuilding(type);
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

    private void ToggleSpawnPlaces(bool value)
    {
        for (int i = 0; i < spawnedSpawnPlaces.Count; i++)
        {
            spawnedSpawnPlaces[i].gameObject.SetActive(value);
        }
    }
    
    private void PlaceBuilding(BuildingType buildingType)
    {
        spawnedSpawnPlaces[SpawnSquareIndex].Placed = true;
        BuildingManager.Instance.Place();

        if (buildingType == BuildingType.Castle)
        {
            Events.OnBuildingCanceled?.Invoke();
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
    Castle,
    Building,
    Path
}