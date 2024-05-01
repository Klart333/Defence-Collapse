using Sirenix.OdinInspector;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;

public class BuildingPlacer : MonoBehaviour
{
    [Title("Placing")]
    [SerializeField]
    private LayerMask layerMask;

    [SerializeField]
    private PooledMonoBehaviour unableToPlacePrefab;

    private List<PooledMonoBehaviour> spawnedPlaces = new List<PooledMonoBehaviour>();

    private BuildingManager buildingManager;
    private Camera cam;

    private InputActions InputActions;
    private InputAction fire;
    private InputAction mouse;
    private InputAction cancel;

    [Title("Debug")]
    [SerializeField]
    private bool placingCastle = true;

    private bool manualCancel = false;
    private bool canceled
    {
        get
        {
            return cancel.ReadValue<float>() > 0 || manualCancel;
        }
    }

    private void OnEnable()
    {
        InputActions = new InputActions();

        fire = InputActions.Player.Fire;
        fire.Enable();
        
        cancel = InputActions.Player.Cancel;
        cancel.Enable();

        mouse = InputActions.Player.Mouse;
        mouse.Enable();

        Events.OnBuildingCanceled += () => manualCancel = true;
    }

    private void OnDisable()
    {
        fire.Disable();
        cancel.Disable();
        mouse.Disable();

        Events.OnBuildingCanceled -= () => manualCancel = false;
    }

    private void Start()
    {
        cam = Camera.main;
        Events.OnBuildingPurchased += BuildingPurchased;

        buildingManager = GetComponent<BuildingManager>();
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
        while (!canceled)
        {
            await Task.Yield();

            Vector3 mousePos = GetRayPoint();
            if (mousePos == Vector3.zero || mousePos.x % 1 == 0 || mousePos.z % 1 == 0 || mousePos.y < 1) continue;

            List<Vector3Int> newIndexes = buildingManager.GetCellsToCollapse(mousePos, type);
            if (indexes.LooseEquals(newIndexes) || newIndexes.Count == 0)
            {
                if (buildables.Count > 0 && fire.WasPerformedThisFrame())
                {
                    PlaceBuilding(type);
                }
                continue;
            }
            else
            {
                DisablePlaces();

                buildingManager.RevertQuery();
                await Task.Delay(50);

                indexes = newIndexes;
            }

            buildables = buildingManager.Query(mousePos, type);

            foreach (var item in buildables)
            {
                item.Value.ToggleIsBuildableVisual(true);
            }
            
            if (buildables.Count == 0) 
            {
                ShowPlaces(indexes.Select(x => buildingManager.GetPos(x) + Vector3.up).ToList());
                
                continue;
            }

            if (!fire.WasPerformedThisFrame()) continue;

            PlaceBuilding(type);
        }

        if (canceled)
        {
            DisablePlaces();
            buildingManager.RevertQuery();

            if (!manualCancel)
            {
                Events.OnBuildingCanceled?.Invoke();
            }
            return;
        }
    }

    private void PlaceBuilding(BuildingType buildingType)
    {
        buildingManager.Place();

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
        Ray ray = cam.ScreenPointToRay(mouse.ReadValue<Vector2>());
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