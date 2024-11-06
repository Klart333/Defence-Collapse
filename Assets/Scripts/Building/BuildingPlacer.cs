using Sirenix.OdinInspector;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

public class BuildingPlacer : MonoBehaviour
{
    [Title("Placing")]
    [SerializeField]
    private LayerMask layerMask;

    [SerializeField]
    private PooledMonoBehaviour unableToPlacePrefab;

    private readonly List<PooledMonoBehaviour> spawnedPlaces = new List<PooledMonoBehaviour>();

    private Camera cam;

    private bool manualCancel = false;
    private bool Canceled
    {
        get
        {
            return InputManager.Instance.Cancel.WasPerformedThisFrame() || manualCancel;
        }
    }

    private void OnEnable()
    {
        Events.OnBuildingCanceled += () => manualCancel = true;
    }

    private void OnDisable()
    {
        Events.OnBuildingCanceled -= () => manualCancel = false;
    }

    private void Start()
    {
        cam = Camera.main;
        Events.OnBuildingPurchased += BuildingPurchased;
    }

    private void BuildingPurchased(BuildingType buildingType)
    {
        PlacingTower(buildingType);
    }

    private async void PlacingTower(BuildingType type)
    {
        manualCancel = false;

        List<Vector3Int> indexes = new List<Vector3Int>();
        Dictionary<Vector3Int, IBuildable> buildables = new Dictionary<Vector3Int, IBuildable>();
        while (!Canceled)
        {
            await Task.Yield();

            Vector3 mousePos = GetRayPoint();
            if (mousePos == Vector3.zero || mousePos.x % 1 == 0 || mousePos.z % 1 == 0 || mousePos.y < 0.2f) continue;

            List<Vector3Int> newIndexes = BuildingManager.Instance.GetCellsToCollapse(mousePos, type);
            if (indexes.LooseEquals(newIndexes) || newIndexes.Count == 0)
            {
                if (buildables.Count > 0 && InputManager.Instance.Fire.WasPerformedThisFrame())
                {
                    PlaceBuilding(type);
                }
                continue;
            }
            else
            {
                DisablePlaces();

                BuildingManager.Instance.RevertQuery();
                await Task.Delay(50);

                indexes = newIndexes;
            }

            buildables = BuildingManager.Instance.Query(mousePos, type);

            foreach (var item in buildables)
            {
                item.Value.ToggleIsBuildableVisual(true);
            }
            
            if (buildables.Count == 0) 
            {
                ShowPlaces(indexes.Select(x => BuildingManager.Instance.GetPos(x) + Vector3.up).ToList());
                
                continue;
            }

            if (!InputManager.Instance.Fire.WasPerformedThisFrame()) continue;

            PlaceBuilding(type);
        }

        if (Canceled)
        {
            DisablePlaces();
            BuildingManager.Instance.RevertQuery();

            if (!manualCancel)
            {
                Events.OnBuildingCanceled?.Invoke();
            }
            return;
        }
    }

    private void PlaceBuilding(BuildingType buildingType)
    {
        BuildingManager.Instance.Place();

        if (buildingType == BuildingType.Castle)
        {
            Events.OnBuildingCanceled?.Invoke();
        }
    }

    private void ShowPlaces(List<Vector3> positions)
    {
        for (int i = 0; i < positions.Count; i++)
        {
            spawnedPlaces.Add(unableToPlacePrefab.GetAtPosAndRot<PooledMonoBehaviour>(positions[i], Quaternion.identity));
        }
    }

    private void DisablePlaces()
    {
        for (int i = 0; i < spawnedPlaces.Count; i++)
        {
            spawnedPlaces[i].gameObject.SetActive(false);
        }
    }

    private Vector3 GetRayPoint()
    {
        Ray ray = cam.ScreenPointToRay(InputManager.Instance.Mouse.ReadValue<Vector2>());
        if (Physics.Raycast(ray, out RaycastHit hit, 100, layerMask))
        {
            return hit.point;
        }

        return Vector3.zero;
    }
}

public enum BuildingType
{
    Castle,
    Building,
    Path
}